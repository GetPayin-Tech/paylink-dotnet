using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Paylink.Internal;
using Paylink.Signing;

namespace Paylink
{
    /// <summary>Recurring mandates (subscriptions).</summary>
    public sealed class Recurring : IRecurring
    {
        private readonly ResolvedConfig _config;

        internal Recurring(ResolvedConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Creates a mandate and returns a checkout URL for the first (setup) charge
        /// (<c>POST /api/v2/integration/recurring/init</c>). Pass <paramref name="idempotencyKey"/>
        /// (or <see cref="CreateRecurringParams.ExternalReference"/>) to make retries safe.
        /// </summary>
        public async Task<CreateRecurringResult> CreateAsync(
            CreateRecurringParams parameters,
            string? idempotencyKey = null,
            CancellationToken cancellationToken = default)
        {
            var input = new Dictionary<string, object?>
            {
                { "first_name", parameters.FirstName },
                { "last_name", parameters.LastName },
                { "email", parameters.Email },
                { "order_title", parameters.OrderTitle },
                { "order_amount", parameters.OrderAmount },
                { "currency", parameters.Currency },
                { "cadence_interval", parameters.CadenceInterval },
                { "cadence_count", parameters.CadenceCount },
                { "total_cycles", parameters.TotalCycles },
                { "end_date", parameters.EndDate },
                { "consent_text", parameters.ConsentText },
                { "external_reference", parameters.ExternalReference },
                { "redirection_url", parameters.RedirectionUrl },
                { "webhook_url", parameters.WebhookUrl },
            };

            var data = await Api
                .PostSignedAsync(_config, FieldOrders.RecurringCreate, input, idempotencyKey, null, cancellationToken)
                .ConfigureAwait(false);

            return new CreateRecurringResult(
                Json.Str(data, "checkout_url"),
                Json.Str(data, "mandate_id"),
                Json.Int64(data, "invoice_id"),
                Json.Str(data, "expires_at"));
        }

        /// <summary>Queries a mandate's status (<c>GET /api/v2/integration/recurring/{mandate}</c>).</summary>
        public async Task<MandateStatusResult> StatusAsync(string mandateId, CancellationToken cancellationToken = default)
        {
            var data = await RequestExecutor.ExecuteAsync(_config, new RequestOptions
            {
                Method = "GET",
                Path = MandatePath(mandateId),
                Query = new Dictionary<string, string>
                {
                    { "token", _config.PublicToken },
                    { "signature", SignMandate(mandateId) },
                },
                CancellationToken = cancellationToken,
            }).ConfigureAwait(false);

            return new MandateStatusResult(
                Json.Str(data, "mandate_id"),
                Json.Str(data, "status"),
                Json.Int64(data, "amount"),
                Json.Int64(data, "completed_cycles"),
                Json.Int64OrNull(data, "total_cycles"),
                Json.StrOrNull(data, "next_charge_at"));
        }

        /// <summary>Cancels a mandate (<c>POST /api/v2/integration/recurring/{mandate}/cancel</c>).</summary>
        public Task<MandateActionResult> CancelAsync(string mandateId, CancellationToken cancellationToken = default)
        {
            return ActionAsync(mandateId, "cancel", cancellationToken);
        }

        /// <summary>Pauses a mandate (<c>POST /api/v2/integration/recurring/{mandate}/pause</c>).</summary>
        public Task<MandateActionResult> PauseAsync(string mandateId, CancellationToken cancellationToken = default)
        {
            return ActionAsync(mandateId, "pause", cancellationToken);
        }

        /// <summary>Resumes a paused mandate (<c>POST /api/v2/integration/recurring/{mandate}/resume</c>).</summary>
        public Task<MandateActionResult> ResumeAsync(string mandateId, CancellationToken cancellationToken = default)
        {
            return ActionAsync(mandateId, "resume", cancellationToken);
        }

        private async Task<MandateActionResult> ActionAsync(string mandateId, string action, CancellationToken cancellationToken)
        {
            var data = await RequestExecutor.ExecuteAsync(_config, new RequestOptions
            {
                Method = "POST",
                Path = MandatePath(mandateId) + "/" + action,
                Body = new Dictionary<string, string>
                {
                    { "token", _config.PublicToken },
                    { "signature", SignMandate(mandateId) },
                },
                CancellationToken = cancellationToken,
            }).ConfigureAwait(false);

            var raw = (IReadOnlyDictionary<string, object?>)(Json.ToClr(data) ?? new Dictionary<string, object?>());

            return new MandateActionResult(Json.StrOrNull(data, "status"), raw);
        }

        private static string MandatePath(string mandateId)
        {
            return "/api/v2/integration/recurring/" + Uri.EscapeDataString(mandateId);
        }

        private string SignMandate(string mandateId)
        {
            return Signature.Build(new[] { mandateId }, _config.HashToken);
        }
    }
}
