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

        private static readonly string[] OptionalSigned = { "mandate_id", "external_reference", "subscription_status" };

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

        private static string ComputeSignature(JsonElement payload, string hashToken)
        {
            var values = new List<string>();

            foreach (var key in AlwaysSigned)
            {
                values.Add(FieldToWire(payload, key));
            }

            foreach (var key in OptionalSigned)
            {
                if (payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty(key, out _))
                {
                    values.Add(FieldToWire(payload, key));
                }
            }

            return Signature.Build(values, hashToken);
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
                Json.StrOrNull(payload, "timezone"));
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
