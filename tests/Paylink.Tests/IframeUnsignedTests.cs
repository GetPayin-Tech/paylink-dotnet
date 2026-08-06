using System.Collections.Generic;
using Paylink.Internal;
using Xunit;

namespace Paylink.Tests
{
    /// <summary>
    /// The optional <c>iframe</c> field on invoice create is sent in the body but excluded
    /// from the HMAC signature — exactly like <c>payment_mode</c>. These tests build the
    /// signed body directly (as the golden tests do) to prove both properties.
    /// </summary>
    public sealed class IframeUnsignedTests
    {
        private const string PublicToken = "pub_token";
        private const string HashToken = "hash_token";

        private static Dictionary<string, object?> BaseInput()
        {
            return new Dictionary<string, object?>
            {
                { "first_name", "John" },
                { "last_name", "Doe" },
                { "email", "john@example.com" },
                { "order_title", "Gold Plan" },
                { "order_amount", "250.00" },
                { "currency", "USD" },
                { "redirection_url", "https://shop.example.com/return" },
                { "webhook_url", "https://shop.example.com/webhooks/paylink" },
            };
        }

        [Fact]
        public void IframeTrueGoesIntoBodyAsOne()
        {
            var input = BaseInput();
            input["iframe"] = true;

            var body = SignedBody.Build(FieldOrders.InvoiceCreate, input, PublicToken, HashToken);

            Assert.Equal("1", body["iframe"]);
        }

        [Fact]
        public void IframeFalseGoesIntoBodyAsZero()
        {
            var input = BaseInput();
            input["iframe"] = false;

            var body = SignedBody.Build(FieldOrders.InvoiceCreate, input, PublicToken, HashToken);

            Assert.Equal("0", body["iframe"]);
        }

        [Fact]
        public void OmittedIframeIsAbsentFromBody()
        {
            var body = SignedBody.Build(FieldOrders.InvoiceCreate, BaseInput(), PublicToken, HashToken);

            Assert.False(body.ContainsKey("iframe"));
        }

        [Fact]
        public void IframeDoesNotChangeTheSignature()
        {
            var withoutIframe = SignedBody.Build(FieldOrders.InvoiceCreate, BaseInput(), PublicToken, HashToken);

            var withIframe = new Dictionary<string, object?>(BaseInput())
            {
                { "iframe", true },
            };
            var signed = SignedBody.Build(FieldOrders.InvoiceCreate, withIframe, PublicToken, HashToken);

            // iframe is present in the body but must not alter the signature.
            Assert.Equal("1", signed["iframe"]);
            Assert.Equal(withoutIframe["signature"], signed["signature"]);
        }
    }
}
