using System.Net;
using eQuantic.Payment.Efi;
using eQuantic.Payment.Efi.Cobrancas;
using eQuantic.Payment.Efi.Pix;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Tests.Fakes;

namespace eQuantic.Payment.Tests;

public class EfiProviderTests
{
    private const string TokenJson = """{ "access_token": "tok_123", "token_type": "Bearer", "expires_in": 3600 }""";

    private const string PixCobJson = """
    {
      "txid": "cobabcdef0123456789abcdef01",
      "revisao": 0,
      "status": "ATIVA",
      "calendario": { "criacao": "2026-07-21T10:00:00.000Z", "expiracao": 3600 },
      "loc": { "id": 789, "location": "pix.api.efipay.com.br/v2/qr/x", "tipoCob": "cob" },
      "valor": { "original": "150.00" },
      "chave": "minha-chave",
      "pixCopiaECola": "00020101021226830014br.gov.bcb.pix...6304ABCD"
    }
    """;

    private const string PixQrCodeJson = """
    { "qrcode": "00020101...6304ABCD", "imagemQrcode": "data:image/svg+xml;base64,PHN2Zz4=", "linkVisualizacao": "https://pix.sejaefi.com.br/cob/pagar/789" }
    """;

    [Fact]
    public async Task Pix_create_authenticates_then_maps_charge_with_qr()
    {
        var handler = new StubHttpMessageHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri(EfiDefaults.PixBaseUrl) };
        var tokenProvider = new EfiTokenProvider(http, "id", "secret", EfiDefaults.PixTokenPath);
        var provider = new EfiPixProvider(new EfiPixClient(http, tokenProvider), TestMapperFactory.Create(), "minha-chave");

        // Sequence: OAuth token → PUT /v2/cob/{txid} → GET /v2/loc/{id}/qrcode
        handler.EnqueueJson(HttpStatusCode.OK, TokenJson)
               .EnqueueJson(HttpStatusCode.Created, PixCobJson)
               .EnqueueJson(HttpStatusCode.OK, PixQrCodeJson);

        var response = await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Brl(150.00m),
            Method = PaymentMethodType.Pix,
            Customer = new CustomerRequest { Name = "João", Email = "joao@ex.com", Document = "12345678909" },
        });

        Assert.True(response.Success);
        Assert.Equal("efi@pix", response.Provider.Key);

        var charge = response.Data!;
        Assert.Equal("cobabcdef0123456789abcdef01", charge.Id);
        Assert.Equal(PaymentStatus.Pending, charge.Status);
        Assert.Equal(15000, charge.Amount.AmountInCents); // "150.00" string parsed to 150.00 reais = 15000 centavos
        Assert.StartsWith("00020101", charge.Pix!.QrCode);
        Assert.StartsWith("data:image/svg+xml;base64,", charge.Pix.QrCodeImageUrl);

        // The first call carried the Bearer token obtained from the OAuth step.
        Assert.Equal(3, handler.RequestPaths.Count);
        Assert.Contains("oauth/token", handler.RequestPaths[0]);
    }

    [Fact]
    public async Task Pix_version_rejects_non_pix_methods()
    {
        var handler = new StubHttpMessageHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri(EfiDefaults.PixBaseUrl) };
        var provider = new EfiPixProvider(new EfiPixClient(http, new EfiTokenProvider(http, "id", "secret", EfiDefaults.PixTokenPath)), TestMapperFactory.Create(), "chave");

        var response = await provider.Charges.CreateAsync(new CreateChargeRequest { Amount = Money.Brl(10m), Method = PaymentMethodType.Boleto });

        Assert.False(response.Success);
        Assert.Contains("only supports PIX", response.Error!.Message);
    }

    [Fact]
    public async Task Cobrancas_boleto_authenticates_then_maps_charge_in_centavos()
    {
        var handler = new StubHttpMessageHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri(EfiDefaults.CobrancasBaseUrl) };
        var tokenProvider = new EfiTokenProvider(http, "id", "secret", EfiDefaults.CobrancasTokenPath);
        var provider = new EfiCobrancasProvider(new EfiCobrancasClient(http, tokenProvider), TestMapperFactory.Create());

        handler.EnqueueJson(HttpStatusCode.OK, TokenJson)
               .EnqueueJson(HttpStatusCode.OK, """
               { "code": 200, "data": { "charge_id": 12345, "status": "waiting", "total": 15000, "payment": "banking_billet",
                 "barcode": "00190500954014481606906809350314337370000000100", "pdf": { "charge": "https://efi/boleto.pdf" }, "expire_at": "2026-08-01" } }
               """);

        var response = await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Brl(150.00m),
            Method = PaymentMethodType.Boleto,
            Customer = new CustomerRequest { Name = "Ana", Email = "ana@ex.com", Document = "12345678909" },
        });

        Assert.True(response.Success);
        Assert.Equal("efi@cobrancas", response.Provider.Key);

        var charge = response.Data!;
        Assert.Equal("12345", charge.Id);
        Assert.Equal(PaymentStatus.Pending, charge.Status); // waiting
        Assert.Equal(15000, charge.Amount.AmountInCents);
        Assert.Equal("https://efi/boleto.pdf", charge.Boleto!.Url);
    }
}
