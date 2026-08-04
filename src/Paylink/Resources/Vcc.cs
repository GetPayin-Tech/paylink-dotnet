using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Paylink.Internal;

namespace Paylink
{
    /// <summary>Server-to-server raw-card charges.</summary>
    public sealed class Vcc : IVcc
    {
        private readonly ResolvedConfig _config;

        internal Vcc(ResolvedConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Charges raw card data directly (<c>POST /api/v2/integration/vcc/charge</c>). Sending
        /// PAN/CVV through your server puts it in PCI scope — prefer the hosted checkout or card
        /// tokens where possible.
        /// </summary>
        public async Task<VccChargeResult> ChargeAsync(
            VccChargeParams parameters,
            string? idempotencyKey = null,
            CancellationToken cancellationToken = default)
        {
            var input = new Dictionary<string, object?>
            {
                { "first_name", parameters.FirstName },
                { "last_name", parameters.LastName },
                { "email", parameters.Email },
                { "phone", parameters.Phone },
                { "currency_id", parameters.CurrencyId },
                { "price", parameters.Price },
                { "product", parameters.Product },
                { "reference_number", parameters.ReferenceNumber },
                { "card_number", parameters.CardNumber },
                { "card_expiry_month", parameters.CardExpiryMonth },
                { "card_expiry_year", parameters.CardExpiryYear },
                { "card_cvv", parameters.CardCvv },
                { "country", parameters.Country },
                { "address", parameters.Address },
                { "city", parameters.City },
                { "us_state", parameters.UsState },
                { "canada_state", parameters.CanadaState },
                { "postal_code", parameters.PostalCode },
            };

            var data = await Api
                .PostSignedAsync(_config, FieldOrders.VccCharge, input, idempotencyKey, null, cancellationToken)
                .ConfigureAwait(false);

            return new VccChargeResult(
                Json.Int64(data, "invoice_id"),
                Json.Str(data, "invoice_number"),
                Json.Decimal(data, "amount"),
                Json.Str(data, "currency"),
                Json.Str(data, "paid_status"));
        }
    }
}
