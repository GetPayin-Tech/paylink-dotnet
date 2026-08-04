using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Paylink.Internal;

namespace Paylink
{
    /// <summary>Card tokenization and stored-card charges.</summary>
    public sealed class Cards : ICards
    {
        private readonly ResolvedConfig _config;

        internal Cards(ResolvedConfig config)
        {
            _config = config;
        }

        /// <summary>Vaults a card and returns a reusable token (<c>POST /api/v2/integration/tokens/card</c>).</summary>
        public async Task<TokenizeCardResult> TokenizeAsync(
            TokenizeCardParams parameters,
            string? idempotencyKey = null,
            CancellationToken cancellationToken = default)
        {
            var input = new Dictionary<string, object?>
            {
                { "first_name", parameters.FirstName },
                { "last_name", parameters.LastName },
                { "email", parameters.Email },
                { "customer_reference", parameters.CustomerReference },
                { "external_reference", parameters.ExternalReference },
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
                .PostSignedAsync(_config, FieldOrders.CardTokenize, input, idempotencyKey, null, cancellationToken)
                .ConfigureAwait(false);

            var card = Json.Child(data, "card");

            return new TokenizeCardResult(
                Json.Str(data, "token"),
                new CardInfo(
                    Json.StrOrNull(card, "brand"),
                    Json.StrOrNull(card, "last4"),
                    (int?)Json.Int64OrNull(card, "exp_month"),
                    (int?)Json.Int64OrNull(card, "exp_year")),
                Json.Str(data, "status"));
        }

        /// <summary>Charges a previously vaulted card (<c>POST /api/v2/integration/tokens/charge</c>).</summary>
        public async Task<ChargeTokenResult> ChargeAsync(
            ChargeCardParams parameters,
            string? idempotencyKey = null,
            CancellationToken cancellationToken = default)
        {
            var input = new Dictionary<string, object?>
            {
                { "card_token", parameters.CardToken },
                { "initiator", parameters.Initiator },
                { "first_name", parameters.FirstName },
                { "last_name", parameters.LastName },
                { "email", parameters.Email },
                { "currency", parameters.Currency },
                { "price", parameters.Price },
                { "product", parameters.Product },
                { "reference_number", parameters.ReferenceNumber },
                { "country", parameters.Country },
                { "address", parameters.Address },
                { "city", parameters.City },
                { "us_state", parameters.UsState },
                { "canada_state", parameters.CanadaState },
                { "postal_code", parameters.PostalCode },
            };

            var data = await Api
                .PostSignedAsync(_config, FieldOrders.CardCharge, input, idempotencyKey, null, cancellationToken)
                .ConfigureAwait(false);

            return new ChargeTokenResult(
                Json.Int64(data, "invoice_id"),
                Json.Str(data, "invoice_number"),
                Json.Decimal(data, "amount"),
                Json.Str(data, "currency"),
                Json.Str(data, "paid_status"),
                Json.Str(data, "card_token"));
        }

        /// <summary>Revokes a vaulted card token (<c>POST /api/v2/integration/tokens/revoke</c>).</summary>
        public async Task<RevokeTokenResult> RevokeAsync(string cardToken, CancellationToken cancellationToken = default)
        {
            var input = new Dictionary<string, object?> { { "card_token", cardToken } };

            var data = await Api
                .PostSignedAsync(_config, FieldOrders.CardRevoke, input, null, null, cancellationToken)
                .ConfigureAwait(false);

            return new RevokeTokenResult(Json.StrOrNull(data, "message") ?? "Token revoked.");
        }
    }
}
