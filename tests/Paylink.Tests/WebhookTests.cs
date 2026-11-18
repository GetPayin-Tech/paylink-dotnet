using System.Collections.Generic;
using System.Text.Json;
using Paylink;
using Paylink.Signing;
using Xunit;

namespace Paylink.Tests
{
    public sealed class WebhookTests
    {
        private const string Hash = "test_hash_token_abc123";

        private readonly PaylinkClient _client = new PaylinkClient("pub", Hash);

        [Fact]
        public void VerifiesValidInvoicePaid()
        {
            var json = Signed(new Dictionary<string, object?>
            {
                { "event", "invoice.paid" },
                { "success", 1 },
                { "invoice_id", 123L },
                { "invoice_status", "PAID" },
                { "message", null },
            });

            var webhookEvent = _client.Webhooks.Verify(json);

            Assert.Equal("invoice.paid", webhookEvent.Event);
            Assert.True(webhookEvent.Success);
            Assert.Equal(123L, webhookEvent.InvoiceId);
            Assert.Null(webhookEvent.Message);
        }

        [Fact]
        public void VerifiesSubscriptionOptionalFields()
        {
            var json = Signed(new Dictionary<string, object?>
            {
                { "event", "subscription.charged" },
                { "success", 1 },
                { "invoice_id", 555L },
                { "invoice_status", "PAID" },
                { "message", "ok" },
                { "auth_code", "A1" },
                { "mandate_id", "M-1" },
                { "external_reference", "sub_1" },
                { "subscription_status", "active" },
            });

            var webhookEvent = _client.Webhooks.Verify(json);

            Assert.Equal("M-1", webhookEvent.MandateId);
            Assert.Equal("active", webhookEvent.SubscriptionStatus);
            Assert.Equal("A1", webhookEvent.AuthCode);
        }

        [Fact]
        public void RejectsTamperedPayload()
        {
            var payload = new Dictionary<string, object?>
            {
                { "event", "invoice.paid" },
                { "success", 1 },
                { "invoice_id", 1L },
                { "invoice_status", "PAID" },
                { "message", "ok" },
            };
            var signature = Signature.Build(WebhookValues(payload), Hash);
            payload["invoice_status"] = "UNPAID";
            payload["signature"] = signature;

            Assert.False(_client.Webhooks.IsValid(JsonSerializer.Serialize(payload)));
            Assert.Throws<PaylinkSignatureException>(() => _client.Webhooks.Verify(JsonSerializer.Serialize(payload)));
        }

        [Fact]
        public void RejectsMissingSignature()
        {
            var json = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                { "event", "invoice.paid" },
                { "success", 1 },
                { "invoice_id", 1L },
                { "invoice_status", "PAID" },
            });

            Assert.Throws<PaylinkSignatureException>(() => _client.Webhooks.Verify(json));
        }

        [Fact]
        public void HonorsHashTokenOverride()
        {
            var json = Signed(
                new Dictionary<string, object?>
                {
                    { "event", "invoice.paid" },
                    { "success", 1 },
                    { "invoice_id", 9L },
                    { "invoice_status", "PAID" },
                    { "message", "ok" },
                },
                "other_secret");

            Assert.False(_client.Webhooks.IsValid(json));
            Assert.True(_client.Webhooks.IsValid(json, "other_secret"));
        }

        [Fact]
        public void RejectsInvalidJson()
        {
            Assert.Throws<PaylinkException>(() => _client.Webhooks.Verify("{not json"));
        }

        [Fact]
        public void VerifiesCardTokenCreatedAndExposesTheSavedCard()
        {
            var json = Signed(CardTokenCreated());

            var webhookEvent = _client.Webhooks.Verify(json);

            Assert.Equal(WebhookEventType.CardTokenCreated, webhookEvent.Event);
            Assert.Equal("TKZ-4f21", webhookEvent.TokenizationId);
            Assert.Equal("CRD-9a07", webhookEvent.CardToken);
            Assert.Equal("VISA", webhookEvent.CardBrand);
            Assert.Equal("4242", webhookEvent.CardLast4);
            Assert.Equal("12", webhookEvent.CardExpMonth);
            Assert.Equal("2029", webhookEvent.CardExpYear);
            Assert.Equal("cust-42", webhookEvent.ExternalReference);
        }

        [Fact]
        public void VerifiesCardTokenFailedWhichCarriesNoCard()
        {
            var json = Signed(new Dictionary<string, object?>
            {
                { "event", "card_token.failed" },
                { "success", 0 },
                { "invoice_id", 7782L },
                { "invoice_status", "UNPAID" },
                { "message", "The card could not be stored for future payments." },
                { "tokenization_id", "TKZ-4f22" },
                { "external_reference", "cust-43" },
            });

            var webhookEvent = _client.Webhooks.Verify(json);

            Assert.Equal(WebhookEventType.CardTokenFailed, webhookEvent.Event);
            Assert.False(webhookEvent.Success);
            Assert.Equal("TKZ-4f22", webhookEvent.TokenizationId);
            Assert.Null(webhookEvent.CardToken);
        }

        [Fact]
        public void RejectsATamperedCard()
        {
            var payload = CardTokenCreated();
            Signed(payload);
            payload["card_last4"] = "1111";

            Assert.False(_client.Webhooks.IsValid(JsonSerializer.Serialize(payload)));
        }

        private static Dictionary<string, object?> CardTokenCreated()
        {
            return new Dictionary<string, object?>
            {
                { "event", "card_token.created" },
                { "success", 1 },
                { "invoice_id", 7781L },
                { "invoice_status", "REVERSED" },
                { "message", "Card stored for future payments." },
                { "tokenization_id", "TKZ-4f21" },
                { "external_reference", "cust-42" },
                { "card_token", "CRD-9a07" },
                { "card_brand", "VISA" },
                { "card_last4", "4242" },
                { "card_exp_month", "12" },
                { "card_exp_year", "2029" },
            };
        }

        private static string Signed(Dictionary<string, object?> payload, string hashToken = Hash)
        {
            payload["signature"] = Signature.Build(WebhookValues(payload), hashToken);
            return JsonSerializer.Serialize(payload);
        }

        private static List<string> WebhookValues(Dictionary<string, object?> payload)
        {
            var values = new List<string>
            {
                Wire(payload, "success"),
                Wire(payload, "invoice_id"),
                Wire(payload, "invoice_status"),
                Wire(payload, "message"),
            };

            var blocks = new[]
            {
                new[] { "mandate_id", "external_reference", "subscription_status" },
                new[]
                {
                    "tokenization_id", "external_reference", "card_token", "card_brand",
                    "card_last4", "card_exp_month", "card_exp_year",
                },
            };

            var signed = new List<string>();

            foreach (var block in blocks)
            {
                if (!payload.ContainsKey(block[0]))
                {
                    continue;
                }

                foreach (var key in block)
                {
                    if (payload.ContainsKey(key) && !signed.Contains(key))
                    {
                        signed.Add(key);
                        values.Add(Wire(payload, key));
                    }
                }
            }

            return values;
        }

        private static string Wire(Dictionary<string, object?> payload, string name)
        {
            return payload.TryGetValue(name, out var value) && value != null
                ? value.ToString()!
                : "";
        }
    }
}
