namespace Paylink
{
    /// <summary>The result of creating a checkout invoice.</summary>
    public sealed class CreateInvoiceResult
    {
        public CreateInvoiceResult(string checkoutUrl, long invoiceId, string expiresAt)
        {
            CheckoutUrl = checkoutUrl;
            InvoiceId = invoiceId;
            ExpiresAt = expiresAt;
        }

        /// <summary>The hosted checkout URL to redirect the payer to.</summary>
        public string CheckoutUrl { get; }

        public long InvoiceId { get; }

        public string ExpiresAt { get; }
    }

    /// <summary>The result of a void / settle / reverse / check-status operation.</summary>
    public sealed class PaymentResult
    {
        public PaymentResult(long invoiceId, string paidStatus, string? authCode)
        {
            InvoiceId = invoiceId;
            PaidStatus = paidStatus;
            AuthCode = authCode;
        }

        public long InvoiceId { get; }

        public string PaidStatus { get; }

        public string? AuthCode { get; }
    }

    /// <summary>The result of a refund; <see cref="RefundAmount"/> is null when not reported.</summary>
    public sealed class RefundResult
    {
        public RefundResult(long invoiceId, string paidStatus, string? authCode, decimal? refundAmount)
        {
            InvoiceId = invoiceId;
            PaidStatus = paidStatus;
            AuthCode = authCode;
            RefundAmount = refundAmount;
        }

        public long InvoiceId { get; }

        public string PaidStatus { get; }

        public string? AuthCode { get; }

        public decimal? RefundAmount { get; }
    }

    /// <summary>The result of a server-to-server card charge.</summary>
    public sealed class VccChargeResult
    {
        public VccChargeResult(long invoiceId, string invoiceNumber, decimal amount, string currency, string paidStatus)
        {
            InvoiceId = invoiceId;
            InvoiceNumber = invoiceNumber;
            Amount = amount;
            Currency = currency;
            PaidStatus = paidStatus;
        }

        public long InvoiceId { get; }

        public string InvoiceNumber { get; }

        public decimal Amount { get; }

        public string Currency { get; }

        public string PaidStatus { get; }
    }

    /// <summary>Summary of a tokenized card.</summary>
    public sealed class CardInfo
    {
        public CardInfo(string? brand, string? last4, int? expMonth, int? expYear)
        {
            Brand = brand;
            Last4 = last4;
            ExpMonth = expMonth;
            ExpYear = expYear;
        }

        public string? Brand { get; }

        public string? Last4 { get; }

        public int? ExpMonth { get; }

        public int? ExpYear { get; }
    }

    /// <summary>The result of vaulting a card.</summary>
    public sealed class TokenizeCardResult
    {
        public TokenizeCardResult(string token, CardInfo card, string status)
        {
            Token = token;
            Card = card;
            Status = status;
        }

        public string Token { get; }

        public CardInfo Card { get; }

        public string Status { get; }
    }

    /// <summary>The result of charging a stored card token.</summary>
    public sealed class ChargeTokenResult
    {
        public ChargeTokenResult(long invoiceId, string invoiceNumber, decimal amount, string currency, string paidStatus, string cardToken)
        {
            InvoiceId = invoiceId;
            InvoiceNumber = invoiceNumber;
            Amount = amount;
            Currency = currency;
            PaidStatus = paidStatus;
            CardToken = cardToken;
        }

        public long InvoiceId { get; }

        public string InvoiceNumber { get; }

        public decimal Amount { get; }

        public string Currency { get; }

        public string PaidStatus { get; }

        public string CardToken { get; }
    }

    /// <summary>The result of revoking a card token.</summary>
    public sealed class RevokeTokenResult
    {
        public RevokeTokenResult(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }

    /// <summary>The result of creating a recurring mandate.</summary>
    public sealed class CreateRecurringResult
    {
        public CreateRecurringResult(string checkoutUrl, string mandateId, long invoiceId, string expiresAt)
        {
            CheckoutUrl = checkoutUrl;
            MandateId = mandateId;
            InvoiceId = invoiceId;
            ExpiresAt = expiresAt;
        }

        public string CheckoutUrl { get; }

        public string MandateId { get; }

        public long InvoiceId { get; }

        public string ExpiresAt { get; }
    }

    /// <summary>A mandate's current status. <see cref="Amount"/> is in the currency's minor units.</summary>
    public sealed class MandateStatusResult
    {
        public MandateStatusResult(string mandateId, string status, long amount, long completedCycles, long? totalCycles, string? nextChargeAt)
        {
            MandateId = mandateId;
            Status = status;
            Amount = amount;
            CompletedCycles = completedCycles;
            TotalCycles = totalCycles;
            NextChargeAt = nextChargeAt;
        }

        public string MandateId { get; }

        public string Status { get; }

        public long Amount { get; }

        public long CompletedCycles { get; }

        public long? TotalCycles { get; }

        public string? NextChargeAt { get; }
    }

    /// <summary>The result of a cancel / pause / resume — the new status plus the raw body.</summary>
    public sealed class MandateActionResult
    {
        public MandateActionResult(string? status, System.Collections.Generic.IReadOnlyDictionary<string, object?> raw)
        {
            Status = status;
            Raw = raw;
        }

        public string? Status { get; }

        public System.Collections.Generic.IReadOnlyDictionary<string, object?> Raw { get; }
    }
}
