using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Paylink.Internal;

namespace Paylink
{
    /// <summary>Checkout invoices.</summary>
    public sealed class Invoices : IInvoices
    {
        private readonly ResolvedConfig _config;

        internal Invoices(ResolvedConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Creates an invoice and returns a temporary signed checkout URL
        /// (<c>POST /api/v2/integration/init</c>). Redirect the payer to
        /// <see cref="CreateInvoiceResult.CheckoutUrl"/>. Pass <paramref name="idempotencyKey"/>
        /// to make a retried create safe.
        /// </summary>
        public async Task<CreateInvoiceResult> CreateAsync(
            CreateInvoiceParams parameters,
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
                { "address", parameters.Address },
                { "city", parameters.City },
                { "country", parameters.Country },
                { "state", parameters.State },
                { "currency", parameters.Currency },
                { "redirection_url", parameters.RedirectionUrl },
                { "webhook_url", parameters.WebhookUrl },
                { "order_details", parameters.OrderDetails },
                { "payment_mode", parameters.PaymentMode },
                { "iframe", parameters.Iframe },
            };

            var data = await Api
                .PostSignedAsync(_config, FieldOrders.InvoiceCreate, input, idempotencyKey, null, cancellationToken)
                .ConfigureAwait(false);

            return new CreateInvoiceResult(
                Json.Str(data, "checkout_url"),
                Json.Int64(data, "invoice_id"),
                Json.Str(data, "expires_at"));
        }
    }
}
