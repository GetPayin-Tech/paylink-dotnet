using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Paylink;
using Paylink.Http;
using Paylink.Internal;
using Xunit;

namespace Paylink.Tests
{
    public sealed class RetryTests
    {
        private const string CheckoutData = "{\"checkout_url\":\"https://x/y\",\"invoice_id\":1,\"expires_at\":\"t\"}";
        private const string PaymentData = "{\"invoice_id\":1,\"paid_status\":\"PAID\",\"auth_code\":\"A\"}";

        [Fact]
        public async Task SuccessEnvelopeIsUnwrapped()
        {
            var (client, transport) = NewClient(2, Ok(CheckoutData));
            var result = await client.Invoices.CreateAsync(Invoice());
            Assert.Equal(1L, result.InvoiceId);
            Assert.Equal(1, transport.Calls);
        }

        [Fact]
        public async Task Retries429WhenIdempotencyKeyPresent()
        {
            var (client, transport) = NewClient(2, Err(429, "rate"), Ok(CheckoutData));
            var result = await client.Invoices.CreateAsync(Invoice(), idempotencyKey: "k");
            Assert.Equal(1L, result.InvoiceId);
            Assert.Equal(2, transport.Calls);
            Assert.Equal("k", transport.Headers[0]["Idempotency-Key"]);
        }

        [Fact]
        public async Task BareChargeIsNotRetried()
        {
            var (client, transport) = NewClient(2, Err(500, "boom"));
            var error = await Assert.ThrowsAsync<PaylinkApiException>(() => client.Vcc.ChargeAsync(Card()));
            Assert.Equal(500, error.Status);
            Assert.Equal(1, transport.Calls);
        }

        [Fact]
        public async Task CheckStatusIsReplaySafe()
        {
            var (client, transport) = NewClient(2, Err(503, "down"), Ok(PaymentData));
            var result = await client.Payments.CheckStatusAsync(1);
            Assert.Equal("PAID", result.PaidStatus);
            Assert.Equal(2, transport.Calls);
        }

        [Fact]
        public async Task MaxRetriesZeroDisables()
        {
            var (client, transport) = NewClient(0, Err(429, "rate"), Ok(CheckoutData));
            await Assert.ThrowsAsync<PaylinkApiException>(() => client.Invoices.CreateAsync(Invoice(), idempotencyKey: "k"));
            Assert.Equal(1, transport.Calls);
        }

        [Fact]
        public async Task NonTransientErrorIsNotRetried()
        {
            var body = "{\"success\":false,\"message\":\"Invalid signature.\",\"errors\":{\"signature\":[\"bad\"]}}";
            var (client, transport) = NewClient(2, new PaylinkHttpResponse(422, body));
            var error = await Assert.ThrowsAsync<PaylinkApiException>(() => client.Invoices.CreateAsync(Invoice(), idempotencyKey: "k"));
            Assert.Equal(422, error.Status);
            Assert.NotNull(error.Errors);
            Assert.Equal(1, transport.Calls);
        }

        [Fact]
        public async Task ConnectionErrorIsWrappedAndRetried()
        {
            var (client, transport) = NewClient(2, new Exception("dns"), Ok(CheckoutData));
            var result = await client.Invoices.CreateAsync(Invoice(), idempotencyKey: "k");
            Assert.Equal(1L, result.InvoiceId);
            Assert.Equal(2, transport.Calls);
        }

        [Fact]
        public async Task ConnectionErrorSurfacesWhenExhausted()
        {
            var (client, _) = NewClient(0, new Exception("dns"));
            await Assert.ThrowsAsync<PaylinkConnectionException>(
                () => client.Invoices.CreateAsync(Invoice(), idempotencyKey: "k"));
        }

        [Fact]
        public void ErrorFlags()
        {
            Assert.True(new PaylinkApiException("x", 409).IsIdempotencyConflict);
            Assert.True(new PaylinkApiException("x", 429).IsRateLimited);
            Assert.True(new PaylinkApiException("x", 403).IsForbidden);
        }

        private static (PaylinkClient client, FakeTransport transport) NewClient(int maxRetries, params object[] responses)
        {
            var transport = new FakeTransport(responses);
            var config = ResolvedConfig.Resolve(new PaylinkClientOptions
            {
                PublicToken = "p",
                HashToken = "s",
                MaxRetries = maxRetries,
                Transport = transport,
            });
            config.DelayAsync = (delay, cancellationToken) => Task.CompletedTask;
            config.NextRandom = () => 0.0;

            return (new PaylinkClient(config), transport);
        }

        private static PaylinkHttpResponse Ok(string data)
        {
            return new PaylinkHttpResponse(200, "{\"success\":true,\"data\":" + data + "}");
        }

        private static PaylinkHttpResponse Err(int status, string message)
        {
            return new PaylinkHttpResponse(status, "{\"success\":false,\"message\":\"" + message + "\"}");
        }

        private static CreateInvoiceParams Invoice()
        {
            return new CreateInvoiceParams
            {
                FirstName = "J",
                LastName = "D",
                Email = "j@e.com",
                OrderTitle = "X",
                OrderAmount = "10.00",
                Currency = "USD",
            };
        }

        private static VccChargeParams Card()
        {
            return new VccChargeParams
            {
                FirstName = "S",
                LastName = "S",
                CurrencyId = 1,
                Price = "10",
                Product = "W",
                CardNumber = "4111111111111111",
                CardExpiryMonth = "12",
                CardExpiryYear = "2030",
                Country = "EG",
                Address = "A",
                City = "C",
            };
        }
    }

    internal sealed class FakeTransport : IPaylinkTransport
    {
        private readonly Queue<object> _responses;

        public FakeTransport(params object[] responses)
        {
            _responses = new Queue<object>(responses);
        }

        public int Calls { get; private set; }

        public List<IReadOnlyDictionary<string, string>> Headers { get; } = new List<IReadOnlyDictionary<string, string>>();

        public Task<PaylinkHttpResponse> SendAsync(
            string method,
            string url,
            IReadOnlyDictionary<string, string> headers,
            byte[]? body,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            Calls++;
            Headers.Add(new Dictionary<string, string>(headers));

            var item = _responses.Dequeue();
            if (item is Exception error)
            {
                return Task.FromException<PaylinkHttpResponse>(error);
            }

            return Task.FromResult((PaylinkHttpResponse)item);
        }
    }
}
