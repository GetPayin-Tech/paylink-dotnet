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

            foreach (var key in new[] { "mandate_id", "external_reference", "subscription_status" })
            {
                if (payload.ContainsKey(key))
                {
                    values.Add(Wire(payload, key));
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
