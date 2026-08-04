namespace Paylink
{
    /// <summary>
    /// Parameters for <c>Invoices.CreateAsync</c>. <see cref="RedirectionUrl"/> and
    /// <see cref="WebhookUrl"/> must be HTTPS URLs on the integration's registered domain;
    /// <see cref="PaymentMode"/> (<c>capture</c> or <c>authorize</c>) is sent but excluded
    /// from the signature. Pass <see cref="OrderAmount"/> as a string to control the exact
    /// wire form.
    /// </summary>
    public sealed class CreateInvoiceParams
    {
        public string FirstName { get; set; } = "";

        public string LastName { get; set; } = "";

        public string Email { get; set; } = "";

        public string OrderTitle { get; set; } = "";

        public string OrderAmount { get; set; } = "";

        public string Currency { get; set; } = "";

        public string? Address { get; set; }

        public string? City { get; set; }

        public string? Country { get; set; }

        public string? State { get; set; }

        public string? RedirectionUrl { get; set; }

        public string? WebhookUrl { get; set; }

        public string? OrderDetails { get; set; }

        public string? PaymentMode { get; set; }
    }

    /// <summary>
    /// Parameters for <c>Vcc.ChargeAsync</c>. For <see cref="Country"/> "US" pass
    /// <see cref="UsState"/> + <see cref="PostalCode"/>; for "CA" pass <see cref="CanadaState"/>
    /// + <see cref="PostalCode"/>.
    /// </summary>
    public sealed class VccChargeParams
    {
        public string FirstName { get; set; } = "";

        public string LastName { get; set; } = "";

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public long CurrencyId { get; set; }

        public string Price { get; set; } = "";

        public string Product { get; set; } = "";

        public string? ReferenceNumber { get; set; }

        public string CardNumber { get; set; } = "";

        public string CardExpiryMonth { get; set; } = "";

        public string CardExpiryYear { get; set; } = "";

        public string? CardCvv { get; set; }

        public string Country { get; set; } = "";

        public string Address { get; set; } = "";

        public string City { get; set; } = "";

        public string? UsState { get; set; }

        public string? CanadaState { get; set; }

        public string? PostalCode { get; set; }
    }

    /// <summary>Parameters for <c>Cards.TokenizeAsync</c>. See <see cref="VccChargeParams"/> for the state fields.</summary>
    public sealed class TokenizeCardParams
    {
        public string FirstName { get; set; } = "";

        public string LastName { get; set; } = "";

        public string? Email { get; set; }

        public string? CustomerReference { get; set; }

        public string? ExternalReference { get; set; }

        public string CardNumber { get; set; } = "";

        public string CardExpiryMonth { get; set; } = "";

        public string CardExpiryYear { get; set; } = "";

        public string? CardCvv { get; set; }

        public string Country { get; set; } = "";

        public string Address { get; set; } = "";

        public string City { get; set; } = "";

        public string? UsState { get; set; }

        public string? CanadaState { get; set; }

        public string? PostalCode { get; set; }
    }

    /// <summary>Parameters for <c>Cards.ChargeAsync</c>. <see cref="Initiator"/> is <c>merchant</c> or <c>customer</c>.</summary>
    public sealed class ChargeCardParams
    {
        public string CardToken { get; set; } = "";

        public string Initiator { get; set; } = "";

        public string FirstName { get; set; } = "";

        public string LastName { get; set; } = "";

        public string? Email { get; set; }

        public string Currency { get; set; } = "";

        public string Price { get; set; } = "";

        public string Product { get; set; } = "";

        public string? ReferenceNumber { get; set; }

        public string Country { get; set; } = "";

        public string Address { get; set; } = "";

        public string City { get; set; } = "";

        public string? UsState { get; set; }

        public string? CanadaState { get; set; }

        public string? PostalCode { get; set; }
    }

    /// <summary>
    /// Parameters for <c>Recurring.CreateAsync</c>. <see cref="CadenceInterval"/> is one of
    /// <c>day</c>/<c>week</c>/<c>month</c>/<c>year</c>; <see cref="EndDate"/> is
    /// <c>YYYY-MM-DD</c> and must be after today.
    /// </summary>
    public sealed class CreateRecurringParams
    {
        public string FirstName { get; set; } = "";

        public string LastName { get; set; } = "";

        public string Email { get; set; } = "";

        public string OrderTitle { get; set; } = "";

        public string OrderAmount { get; set; } = "";

        public string Currency { get; set; } = "";

        public string CadenceInterval { get; set; } = "";

        public long CadenceCount { get; set; }

        public string ConsentText { get; set; } = "";

        public long? TotalCycles { get; set; }

        public string? EndDate { get; set; }

        public string? ExternalReference { get; set; }

        public string? RedirectionUrl { get; set; }

        public string? WebhookUrl { get; set; }
    }
}
