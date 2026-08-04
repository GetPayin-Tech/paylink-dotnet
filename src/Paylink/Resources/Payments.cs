using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Paylink.Internal;

namespace Paylink
{
    /// <summary>Payment operations on an existing invoice.</summary>
    public sealed class Payments : IPayments
    {
        private readonly ResolvedConfig _config;

        internal Payments(ResolvedConfig config)
        {
            _config = config;
        }

        /// <summary>Voids a paid invoice (<c>POST /api/integration/void</c>).</summary>
        public async Task<PaymentResult> VoidAsync(
            long invoiceId,
            string? idempotencyKey = null,
            CancellationToken cancellationToken = default)
        {
            var data = await Api
                .PostSignedAsync(_config, FieldOrders.PaymentVoid, InvoiceInput(invoiceId), idempotencyKey, null, cancellationToken)
                .ConfigureAwait(false);

            return MapPayment(data);
        }

        /// <summary>
        /// Refunds a paid invoice, full or partial (<c>POST /api/integration/refund</c>). Pass
        /// <paramref name="idempotencyKey"/> to make retries safe.
        /// </summary>
        public async Task<RefundResult> RefundAsync(
            long invoiceId,
            string amount,
            string? idempotencyKey = null,
            CancellationToken cancellationToken = default)
        {
            var input = new Dictionary<string, object?> { { "invoice_id", invoiceId }, { "amount", amount } };

            var data = await Api
                .PostSignedAsync(_config, FieldOrders.PaymentRefund, input, idempotencyKey, null, cancellationToken)
                .ConfigureAwait(false);

            return new RefundResult(
                Json.Int64(data, "invoice_id"),
                Json.Str(data, "paid_status"),
                Json.StrOrNull(data, "auth_code"),
                Json.DecimalOrNull(data, "refund_amount"));
        }

        /// <summary>Captures an authorized invoice (<c>POST /api/integration/settle</c>).</summary>
        public async Task<PaymentResult> SettleAsync(
            long invoiceId,
            string amount,
            string? idempotencyKey = null,
            CancellationToken cancellationToken = default)
        {
            var input = new Dictionary<string, object?> { { "invoice_id", invoiceId }, { "amount", amount } };

            var data = await Api
                .PostSignedAsync(_config, FieldOrders.PaymentSettle, input, idempotencyKey, null, cancellationToken)
                .ConfigureAwait(false);

            return MapPayment(data);
        }

        /// <summary>Reverses an authorization hold (<c>POST /api/integration/reverse-authorization</c>).</summary>
        public async Task<PaymentResult> ReverseAuthorizationAsync(
            long invoiceId,
            string? idempotencyKey = null,
            CancellationToken cancellationToken = default)
        {
            var data = await Api
                .PostSignedAsync(_config, FieldOrders.PaymentReverseAuthorization, InvoiceInput(invoiceId), idempotencyKey, null, cancellationToken)
                .ConfigureAwait(false);

            return MapPayment(data);
        }

        /// <summary>
        /// Queries the current gateway status of an invoice
        /// (<c>POST /api/integration/check-status</c>). A pure read despite being a POST, so
        /// it is retried on transient failures.
        /// </summary>
        public async Task<PaymentResult> CheckStatusAsync(long invoiceId, CancellationToken cancellationToken = default)
        {
            var data = await Api
                .PostSignedAsync(_config, FieldOrders.PaymentCheckStatus, InvoiceInput(invoiceId), null, true, cancellationToken)
                .ConfigureAwait(false);

            return MapPayment(data);
        }

        private static Dictionary<string, object?> InvoiceInput(long invoiceId)
        {
            return new Dictionary<string, object?> { { "invoice_id", invoiceId } };
        }

        private static PaymentResult MapPayment(JsonElement data)
        {
            return new PaymentResult(
                Json.Int64(data, "invoice_id"),
                Json.Str(data, "paid_status"),
                Json.StrOrNull(data, "auth_code"));
        }
    }
}
