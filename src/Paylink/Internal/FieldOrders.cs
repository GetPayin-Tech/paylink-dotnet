namespace Paylink.Internal
{
    /// <summary>One request field: its snake_case wire name and whether it is signed.</summary>
    internal readonly struct FieldSpec
    {
        public FieldSpec(string name, bool signed = true)
        {
            Name = name;
            Signed = signed;
        }

        public string Name { get; }

        public bool Signed { get; }
    }

    /// <summary>One endpoint's signing contract: path, ordered fields, and state block.</summary>
    internal sealed class EndpointSpec
    {
        public EndpointSpec(string path, FieldSpec[] fields, bool countryStateBlock = false)
        {
            Path = path;
            Fields = fields;
            CountryStateBlock = countryStateBlock;
        }

        public string Path { get; }

        public FieldSpec[] Fields { get; }

        public bool CountryStateBlock { get; }
    }

    /// <summary>
    /// The signed-field registry. Each entry lists a request's fields in the EXACT order
    /// the server concatenates them when it rebuilds the HMAC signature (the FormRequest
    /// <c>rules()</c> order, minus <c>token</c>/<c>signature</c>, and minus
    /// <c>payment_mode</c> which is sent but not signed). Getting this order right is the
    /// whole job of the SDK. Keep it in lockstep with the server FormRequests under
    /// <c>app/Http/Requests/Application/ExternalPaymentIntegration</c>.
    /// </summary>
    internal static class FieldOrders
    {
        public static readonly EndpointSpec InvoiceCreate = new EndpointSpec(
            "/api/v2/integration/init",
            new[]
            {
                new FieldSpec("first_name"),
                new FieldSpec("last_name"),
                new FieldSpec("email"),
                new FieldSpec("order_title"),
                new FieldSpec("order_amount"),
                new FieldSpec("address"),
                new FieldSpec("city"),
                new FieldSpec("country"),
                new FieldSpec("state"),
                new FieldSpec("currency"),
                new FieldSpec("redirection_url"),
                new FieldSpec("webhook_url"),
                new FieldSpec("order_details"),
                new FieldSpec("payment_mode", false),
                new FieldSpec("iframe", false),
            });

        public static readonly EndpointSpec PaymentVoid = new EndpointSpec(
            "/api/integration/void",
            new[] { new FieldSpec("invoice_id") });

        public static readonly EndpointSpec PaymentRefund = new EndpointSpec(
            "/api/integration/refund",
            new[] { new FieldSpec("invoice_id"), new FieldSpec("amount") });

        public static readonly EndpointSpec PaymentSettle = new EndpointSpec(
            "/api/integration/settle",
            new[] { new FieldSpec("invoice_id"), new FieldSpec("amount") });

        public static readonly EndpointSpec PaymentReverseAuthorization = new EndpointSpec(
            "/api/integration/reverse-authorization",
            new[] { new FieldSpec("invoice_id") });

        public static readonly EndpointSpec PaymentCheckStatus = new EndpointSpec(
            "/api/integration/check-status",
            new[] { new FieldSpec("invoice_id") });

        public static readonly EndpointSpec VccCharge = new EndpointSpec(
            "/api/v2/integration/vcc/charge",
            new[]
            {
                new FieldSpec("first_name"),
                new FieldSpec("last_name"),
                new FieldSpec("email"),
                new FieldSpec("phone"),
                new FieldSpec("currency_id"),
                new FieldSpec("price"),
                new FieldSpec("product"),
                new FieldSpec("reference_number"),
                new FieldSpec("card_number"),
                new FieldSpec("card_expiry_month"),
                new FieldSpec("card_expiry_year"),
                new FieldSpec("card_cvv"),
                new FieldSpec("country"),
                new FieldSpec("address"),
                new FieldSpec("city"),
            },
            countryStateBlock: true);

        public static readonly EndpointSpec CardTokenize = new EndpointSpec(
            "/api/v2/integration/tokens/card",
            new[]
            {
                new FieldSpec("first_name"),
                new FieldSpec("last_name"),
                new FieldSpec("email"),
                new FieldSpec("customer_reference"),
                new FieldSpec("external_reference"),
                new FieldSpec("card_number"),
                new FieldSpec("card_expiry_month"),
                new FieldSpec("card_expiry_year"),
                new FieldSpec("card_cvv"),
                new FieldSpec("country"),
                new FieldSpec("address"),
                new FieldSpec("city"),
            },
            countryStateBlock: true);

        public static readonly EndpointSpec CardCharge = new EndpointSpec(
            "/api/v2/integration/tokens/charge",
            new[]
            {
                new FieldSpec("card_token"),
                new FieldSpec("initiator"),
                new FieldSpec("first_name"),
                new FieldSpec("last_name"),
                new FieldSpec("email"),
                new FieldSpec("currency"),
                new FieldSpec("price"),
                new FieldSpec("product"),
                new FieldSpec("reference_number"),
                new FieldSpec("country"),
                new FieldSpec("address"),
                new FieldSpec("city"),
            },
            countryStateBlock: true);

        public static readonly EndpointSpec CardRevoke = new EndpointSpec(
            "/api/v2/integration/tokens/revoke",
            new[] { new FieldSpec("card_token") });

        public static readonly EndpointSpec RecurringCreate = new EndpointSpec(
            "/api/v2/integration/recurring/init",
            new[]
            {
                new FieldSpec("first_name"),
                new FieldSpec("last_name"),
                new FieldSpec("email"),
                new FieldSpec("order_title"),
                new FieldSpec("order_amount"),
                new FieldSpec("currency"),
                new FieldSpec("cadence_interval"),
                new FieldSpec("cadence_count"),
                new FieldSpec("total_cycles"),
                new FieldSpec("end_date"),
                new FieldSpec("consent_text"),
                new FieldSpec("external_reference"),
                new FieldSpec("redirection_url"),
                new FieldSpec("webhook_url"),
            });
    }
}
