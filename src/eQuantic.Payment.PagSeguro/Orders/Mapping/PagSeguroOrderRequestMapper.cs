using eQuantic.Mapper;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.PagSeguro.Orders.Models;

namespace eQuantic.Payment.PagSeguro.Orders.Mapping;

/// <summary>
/// Maps the unified <see cref="CreateChargeRequest"/> to a PagSeguro order request. Card/boleto are placed
/// in <c>charges[]</c>; PIX is placed in <c>qr_codes[]</c> (the API has no card-style "charge" for PIX).
/// </summary>
public sealed class PagSeguroOrderRequestMapper : IMapper<CreateChargeRequest, PagSeguroOrderRequest>
{
    public PagSeguroOrderRequest? Map(CreateChargeRequest? source)
    {
        if (source is null)
        {
            return null;
        }

        var order = new PagSeguroOrderRequest
        {
            ReferenceId = source.ReferenceId,
            Customer = MapCustomer(source.Customer),
            Items =
            [
                new PagSeguroItem
                {
                    ReferenceId = source.ReferenceId,
                    Name = source.Description ?? "Charge",
                    Quantity = 1,
                    UnitAmount = source.Amount.AmountInCents,
                },
            ],
        };

        var amount = new PagSeguroAmountRequest { Value = source.Amount.AmountInCents, Currency = source.Amount.Currency };

        if (source.Method == PaymentMethodType.Pix)
        {
            order.QrCodes =
            [
                new PagSeguroQrCodeRequest
                {
                    Amount = amount,
                    ExpirationDate = DateTimeOffset.UtcNow.Add(source.Pix?.ExpiresIn ?? TimeSpan.FromHours(1)),
                },
            ];
            return order;
        }

        order.Charges =
        [
            new PagSeguroChargeRequest
            {
                ReferenceId = source.ReferenceId,
                Description = source.Description,
                Amount = amount,
                PaymentMethod = MapPaymentMethod(source),
            },
        ];
        return order;
    }

    public PagSeguroOrderRequest? Map(CreateChargeRequest? source, PagSeguroOrderRequest? destination) => Map(source);

    private static PagSeguroPaymentMethodRequest MapPaymentMethod(CreateChargeRequest request)
    {
        var method = new PagSeguroPaymentMethodRequest
        {
            Type = request.Method switch
            {
                PaymentMethodType.CreditCard => "CREDIT_CARD",
                PaymentMethodType.DebitCard => "DEBIT_CARD",
                PaymentMethodType.Boleto => "BOLETO",
                _ => "CREDIT_CARD",
            },
        };

        if (request.Method is PaymentMethodType.CreditCard or PaymentMethodType.DebitCard)
        {
            method.Installments = request.Card?.Installments ?? 1;
            method.Capture = request.Capture;
            method.SoftDescriptor = request.Description is { Length: > 0 } d ? d[..Math.Min(22, d.Length)] : null;
            method.Card = MapCard(request);
        }
        else if (request.Method == PaymentMethodType.Boleto)
        {
            method.Boleto = MapBoleto(request);
        }

        return method;
    }

    private static PagSeguroCardRequest MapCard(CreateChargeRequest request)
    {
        var card = request.Card;
        var holder = new PagSeguroCardHolder
        {
            Name = card?.HolderName ?? request.Customer?.Name,
            TaxId = request.Customer?.DocumentDigits,
        };

        // A token is treated as PagBank's client-side "encrypted" card payload.
        if (card?.Token is { } token)
        {
            return new PagSeguroCardRequest { Encrypted = token, Holder = holder };
        }

        return new PagSeguroCardRequest
        {
            Number = card?.Number,
            ExpMonth = card is null ? null : card.ExpirationMonth.ToString("D2"),
            ExpYear = card?.ExpirationYear.ToString(),
            SecurityCode = card?.Cvv,
            Holder = holder,
        };
    }

    private static PagSeguroBoletoRequest MapBoleto(CreateChargeRequest request)
    {
        var due = request.Boleto?.DueDate ?? DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3);
        var customer = request.Customer;
        return new PagSeguroBoletoRequest
        {
            DueDate = due.ToString("yyyy-MM-dd"),
            Holder = new PagSeguroBoletoHolder
            {
                Name = customer?.Name ?? "Cliente",
                TaxId = customer?.DocumentDigits,
                Email = customer?.Email,
                Address = customer?.Address is { } a
                    ? new PagSeguroBoletoAddress
                    {
                        Street = a.Line1,
                        Number = "S/N",
                        Locality = a.Line2,
                        City = a.City,
                        RegionCode = a.State,
                        PostalCode = new string((a.ZipCode ?? string.Empty).Where(char.IsDigit).ToArray()),
                        Country = "BRA",
                    }
                    : null,
            },
            InstructionLines = request.Boleto?.Instructions is { } ins
                ? new PagSeguroInstructionLines { Line1 = ins }
                : null,
        };
    }

    private static PagSeguroCustomer MapCustomer(CustomerRequest? customer)
    {
        if (customer is null)
        {
            return new PagSeguroCustomer();
        }

        return new PagSeguroCustomer
        {
            Name = customer.Name,
            Email = customer.Email,
            TaxId = customer.DocumentDigits,
            Phones = MapPhones(customer.Phone),
        };
    }

    private static List<PagSeguroPhone>? MapPhones(string? phone)
    {
        var digits = phone is null ? null : new string(phone.Where(char.IsDigit).ToArray());
        if (digits is null || digits.Length < 10)
        {
            return null;
        }

        if (digits.Length > 11 && digits.StartsWith("55"))
        {
            digits = digits[2..];
        }

        return [new PagSeguroPhone { Country = "55", Area = digits[..2], Number = digits[2..], Type = "MOBILE" }];
    }
}
