using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Paylink.Internal;
using Paylink.Signing;

namespace Paylink
{
    /// <summary>Verifies inbound webhook signatures against the integration <c>HashToken</c>.</summary>
    public sealed class Webhooks : IWebhooks
    {
        private static readonly string[] AlwaysSigned = { "success", "invoice_id", "invoice_status", "message" };

        /// <summary>The fields a recurring mandate adds, in the order the server adds them.</summary>
        private static readonly string[] MandateSigned = { "mandate_id", "external_reference", "subscription_status" };

        /// <summary>
        /// The fields a card-token event adds, in the order the server adds them.
        /// <c>card_token.created</c> and <c>card_token.failed</c> are the only events that carry a
        /// tokenisation, and both are on by default for every integration. The server writes
        /// <c>tokenization_id</c> and <c>external_reference</c> first, then the card's details —
        /// and only when the card was actually vaulted, so a failed tokenisation signs the first
        /// two alone. Order matters because the signature is a separator-less concatenation: on an
        /// event with no mandate, <c>external_reference</c> is signed AFTER <c>tokenization_id</c>,
        /// not at the position it holds on a subscription event.
        /// </summary>
        private static readonly string[] TokenizationSigned =
        {
            "tokenization_id", "external_reference", "card_token", "card_brand", "card_last4",
            "card_exp_month", "card_exp_year",
        };

        private readonly ResolvedConfig _config;

        internal Webhooks(ResolvedConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Verifies a webhook from its raw JSON body and returns the parsed event. Throws
        /// <see cref="PaylinkSignatureException"/> if the signature is missing or does not
        /// match. PayLink webhook signatures carry no timestamp, so this does not protect
        /// against replay — pair it with your own idempotency on <c>invoice_id</c>.
        /// </summary>
        public WebhookEvent Verify(string payload, string? hashToken = null)
        {
            return Verify(Parse(payload), hashToken);
        }

        /// <summary>Verifies a webhook from an already-parsed JSON element.</summary>
        public WebhookEvent Verify(JsonElement payload, string? hashToken = null)
        {
            var secret = hashToken ?? _config.HashToken;

            var provided = Json.StrOrNull(payload, "signature");
            if (string.IsNullOrEmpty(provided))
            {
                throw new PaylinkSignatureException("Webhook payload is missing a signature.");
            }

            if (!Signature.SignaturesEqual(ComputeSignature(payload, secret), provided!))
            {
                throw new PaylinkSignatureException();
            }

            return ToEvent(payload);
        }

        /// <summary>Non-throwing variant of <see cref="Verify(string, string?)"/>.</summary>
        public bool IsValid(string payload, string? hashToken = null)
        {
            return IsValid(Parse(payload), hashToken);
        }

        /// <summary>Non-throwing variant of <see cref="Verify(JsonElement, string?)"/>.</summary>
        public bool IsValid(JsonElement payload, string? hashToken = null)
        {
            try
            {
                Verify(payload, hashToken);
                return true;
            }
            catch (PaylinkSignatureException)
            {
                return false;
            }
        }

        /// <summary>
        /// Rebuilds the expected signature the way the server assembles it.
        /// <c>PaymentIntegrationWebhookJob</c> appends a whole block at a time — the mandate fields
        /// when the invoice belongs to one, the tokenisation fields when the event is about a card
        /// — and PHP keeps a key at the position it was first written. A block is therefore gated
        /// on its own leading field rather than on the individual fields inside it:
        /// <c>external_reference</c> belongs to whichever block introduced it, and is never signed
        /// twice.
        /// </summary>
        private static string ComputeSignature(JsonElement payload, string hashToken)
        {
            var keys = new List<string>(AlwaysSigned);

            foreach (var block in new[] { MandateSigned, TokenizationSigned })
            {
                if (!HasField(payload, block[0]))
                {
                    continue;
                }

                foreach (var key in block)
                {
                    if (HasField(payload, key) && !keys.Contains(key))
                    {
                        keys.Add(key);
                    }
                }
            }

            var values = new List<string>();

            foreach (var key in keys)
            {
                values.Add(FieldToWire(payload, key));
            }

            return Signature.Build(values, hashToken);
        }

        private static bool HasField(JsonElement payload, string name)
        {
            return payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty(name, out _);
        }

        private static string FieldToWire(JsonElement payload, string name)
        {
            if (payload.ValueKind != JsonValueKind.Object || !payload.TryGetProperty(name, out var value))
            {
                return "";
            }

            switch (value.ValueKind)
            {
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return "";
                case JsonValueKind.String:
                    return value.GetString() ?? "";
                case JsonValueKind.True:
                    return "1";
                case JsonValueKind.False:
                    return "0";
                case JsonValueKind.Number:
                    return value.TryGetInt64(out var integer)
                        ? integer.ToString(CultureInfo.InvariantCulture)
                        : Coerce.ToWire(value.GetDouble());
                default:
                    return "";
            }
        }

        private static WebhookEvent ToEvent(JsonElement payload)
        {
            return new WebhookEvent(
                Json.Str(payload, "event"),
                FieldToWire(payload, "success") == "1",
                (IReadOnlyDictionary<string, object?>)(Json.ToClr(payload) ?? new Dictionary<string, object?>()),
                Json.Int64OrNull(payload, "invoice_id"),
                Json.StrOrNull(payload, "invoice_status"),
                Json.StrOrNull(payload, "message"),
                Json.StrOrNull(payload, "auth_code"),
                Json.StrOrNull(payload, "mandate_id"),
                Json.StrOrNull(payload, "external_reference"),
                Json.StrOrNull(payload, "subscription_status"),
                Json.StrOrNull(payload, "event_triggered_at"),
                Json.StrOrNull(payload, "timezone"),
                Json.StrOrNull(payload, "tokenization_id"),
                Json.StrOrNull(payload, "card_token"),
                Json.StrOrNull(payload, "card_brand"),
                Json.StrOrNull(payload, "card_last4"),
                Json.StrOrNull(payload, "card_exp_month"),
                Json.StrOrNull(payload, "card_exp_year"));
        }

        private static JsonElement Parse(string payload)
        {
            try
            {
                using (var document = JsonDocument.Parse(payload))
                {
                    return document.RootElement.Clone();
                }
            }
            catch (JsonException error)
            {
                throw new PaylinkException("Webhook payload is not valid JSON.", error);
            }
        }
    }
}
