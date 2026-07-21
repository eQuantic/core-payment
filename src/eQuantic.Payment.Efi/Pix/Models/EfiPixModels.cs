namespace eQuantic.Payment.Efi.Pix.Models;

// Faithful wire models for the Efí Pix API (camelCase, Portuguese keys). Money is a decimal STRING "10.00".

/// <summary>Request body for <c>PUT /v2/cob/{txid}</c> (immediate Pix charge).</summary>
public sealed class EfiPixCobRequest
{
    public EfiPixCalendario Calendario { get; set; } = new();
    public EfiPixDevedor? Devedor { get; set; }
    public EfiPixValor Valor { get; set; } = new();

    /// <summary>Receiver Pix key.</summary>
    public string Chave { get; set; } = string.Empty;

    /// <summary>Free text shown to the payer (max 140).</summary>
    public string? SolicitacaoPagador { get; set; }
}

public sealed class EfiPixCalendario
{
    /// <summary>Seconds until the charge expires.</summary>
    public int Expiracao { get; set; } = 3600;

    public DateTimeOffset? Criacao { get; set; }
}

public sealed class EfiPixDevedor
{
    public string? Cpf { get; set; }
    public string? Cnpj { get; set; }
    public string? Nome { get; set; }
}

public sealed class EfiPixValor
{
    /// <summary>Decimal string, e.g. <c>"123.45"</c>.</summary>
    public string Original { get; set; } = "0.00";
}

/// <summary>The <c>cob</c> object (response of create/get).</summary>
public sealed class EfiPixCobResponse
{
    public string? Txid { get; set; }
    public int Revisao { get; set; }
    public string? Status { get; set; }
    public EfiPixCalendario? Calendario { get; set; }
    public string? Location { get; set; }
    public EfiPixLoc? Loc { get; set; }
    public EfiPixDevedor? Devedor { get; set; }
    public EfiPixValor? Valor { get; set; }
    public string? Chave { get; set; }

    /// <summary>PIX "copia e cola" (BR Code payload).</summary>
    public string? PixCopiaECola { get; set; }

    public List<EfiPixReceived>? Pix { get; set; }
}

public sealed class EfiPixLoc
{
    public long Id { get; set; }
    public string? Location { get; set; }
    public string? TipoCob { get; set; }
}

public sealed class EfiPixReceived
{
    public string? EndToEndId { get; set; }
    public string? Txid { get; set; }
    public string? Valor { get; set; }
    public DateTimeOffset? Horario { get; set; }
    public List<EfiPixDevolucao>? Devolucoes { get; set; }
}

/// <summary>Response of <c>GET /v2/loc/{id}/qrcode</c>.</summary>
public sealed class EfiPixQrCodeResponse
{
    /// <summary>BR Code copy-paste (same as <c>pixCopiaECola</c>).</summary>
    public string? Qrcode { get; set; }

    /// <summary>Full data URI (<c>data:image/svg+xml;base64,...</c>), not bare base64.</summary>
    public string? ImagemQrcode { get; set; }

    public string? LinkVisualizacao { get; set; }
}

/// <summary>Request body for <c>PUT /v2/pix/{e2eId}/devolucao/{id}</c> (refund).</summary>
public sealed class EfiPixDevolucaoRequest
{
    /// <summary>Decimal string.</summary>
    public string Valor { get; set; } = "0.00";
}

public sealed class EfiPixDevolucao
{
    public string? Id { get; set; }
    public string? RtrId { get; set; }
    public string? Valor { get; set; }

    /// <summary><c>EM_PROCESSAMENTO</c> | <c>DEVOLVIDO</c> | <c>NAO_REALIZADO</c>.</summary>
    public string? Status { get; set; }
}
