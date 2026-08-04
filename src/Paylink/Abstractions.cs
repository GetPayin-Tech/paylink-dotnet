using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Paylink
{
    /// <summary>Checkout invoices.</summary>
    public interface IInvoices
    {
        /// <summary>Creates an invoice and returns a temporary signed checkout URL.</summary>
        Task<CreateInvoiceResult> CreateAsync(
            CreateInvoiceParams parameters,
            string? idempotencyKey = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>Payment operations on an existing invoice.</summary>
    public interface IPayments
    {
        /// <summary>Voids a paid invoice.</summary>
        Task<PaymentResult> VoidAsync(long invoiceId, string? idempotencyKey = null, CancellationToken cancellationToken = default);

        /// <summary>Refunds a paid invoice, full or partial.</summary>
        Task<RefundResult> RefundAsync(long invoiceId, string amount, string? idempotencyKey = null, CancellationToken cancellationToken = default);

        /// <summary>Captures an authorized invoice.</summary>
        Task<PaymentResult> SettleAsync(long invoiceId, string amount, string? idempotencyKey = null, CancellationToken cancellationToken = default);

        /// <summary>Reverses an authorization hold.</summary>
        Task<PaymentResult> ReverseAuthorizationAsync(long invoiceId, string? idempotencyKey = null, CancellationToken cancellationToken = default);

        /// <summary>Queries the current gateway status of an invoice.</summary>
        Task<PaymentResult> CheckStatusAsync(long invoiceId, CancellationToken cancellationToken = default);
    }

    /// <summary>Server-to-server raw-card charges.</summary>
    public interface IVcc
    {
        /// <summary>Charges raw card data directly.</summary>
        Task<VccChargeResult> ChargeAsync(VccChargeParams parameters, string? idempotencyKey = null, CancellationToken cancellationToken = default);
    }

    /// <summary>Card tokenization and stored-card charges.</summary>
    public interface ICards
    {
        /// <summary>Vaults a card and returns a reusable token.</summary>
        Task<TokenizeCardResult> TokenizeAsync(TokenizeCardParams parameters, string? idempotencyKey = null, CancellationToken cancellationToken = default);

        /// <summary>Charges a previously vaulted card.</summary>
        Task<ChargeTokenResult> ChargeAsync(ChargeCardParams parameters, string? idempotencyKey = null, CancellationToken cancellationToken = default);

        /// <summary>Revokes a vaulted card token.</summary>
        Task<RevokeTokenResult> RevokeAsync(string cardToken, CancellationToken cancellationToken = default);
    }

    /// <summary>Recurring mandates (subscriptions).</summary>
    public interface IRecurring
    {
        /// <summary>Creates a mandate and returns a checkout URL for the first charge.</summary>
        Task<CreateRecurringResult> CreateAsync(CreateRecurringParams parameters, string? idempotencyKey = null, CancellationToken cancellationToken = default);

        /// <summary>Queries a mandate's status.</summary>
        Task<MandateStatusResult> StatusAsync(string mandateId, CancellationToken cancellationToken = default);

        /// <summary>Cancels a mandate.</summary>
        Task<MandateActionResult> CancelAsync(string mandateId, CancellationToken cancellationToken = default);

        /// <summary>Pauses a mandate.</summary>
        Task<MandateActionResult> PauseAsync(string mandateId, CancellationToken cancellationToken = default);

        /// <summary>Resumes a paused mandate.</summary>
        Task<MandateActionResult> ResumeAsync(string mandateId, CancellationToken cancellationToken = default);
    }

    /// <summary>Inbound webhook signature verification.</summary>
    public interface IWebhooks
    {
        /// <summary>Verifies a webhook from its raw JSON body and returns the parsed event.</summary>
        WebhookEvent Verify(string payload, string? hashToken = null);

        /// <summary>Verifies a webhook from an already-parsed JSON element.</summary>
        WebhookEvent Verify(JsonElement payload, string? hashToken = null);

        /// <summary>Non-throwing verification from a raw JSON body.</summary>
        bool IsValid(string payload, string? hashToken = null);

        /// <summary>Non-throwing verification from a parsed JSON element.</summary>
        bool IsValid(JsonElement payload, string? hashToken = null);
    }

    /// <summary>The PayLink integration client. Inject this abstraction.</summary>
    public interface IPaylinkClient
    {
        /// <summary>Create checkouts.</summary>
        IInvoices Invoices { get; }

        /// <summary>Void, refund, settle, reverse authorization, and check status.</summary>
        IPayments Payments { get; }

        /// <summary>Server-to-server raw-card charges.</summary>
        IVcc Vcc { get; }

        /// <summary>Tokenize, charge, and revoke stored cards.</summary>
        ICards Cards { get; }

        /// <summary>Create and manage recurring mandates.</summary>
        IRecurring Recurring { get; }

        /// <summary>Verify inbound webhook signatures.</summary>
        IWebhooks Webhooks { get; }
    }
}
