using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Paylink;
using Paylink.Http;
using Xunit;

namespace Paylink.Tests
{
    public sealed class DiRegistrationTests
    {
        [Fact]
        public void AddPaylinkRegistersClientAndServicesAsSingletons()
        {
            var provider = BuildProvider();

            Assert.NotNull(provider.GetService<IPaylinkClient>());
            Assert.NotNull(provider.GetService<IInvoices>());
            Assert.NotNull(provider.GetService<IPayments>());
            Assert.NotNull(provider.GetService<IWebhooks>());
            Assert.Same(
                provider.GetRequiredService<IPaylinkClient>(),
                provider.GetRequiredService<IPaylinkClient>());
        }

        [Fact]
        public async Task ResolvedClientMakesCalls()
        {
            var client = BuildProvider().GetRequiredService<IPaylinkClient>();

            var result = await client.Invoices.CreateAsync(new CreateInvoiceParams
            {
                FirstName = "J",
                LastName = "D",
                Email = "j@e.com",
                OrderTitle = "X",
                OrderAmount = "10.00",
                Currency = "USD",
            });

            Assert.Equal(1L, result.InvoiceId);
        }

        private static IServiceProvider BuildProvider()
        {
            var services = new ServiceCollection();
            services.AddPaylink(options =>
            {
                options.PublicToken = "p";
                options.HashToken = "s";
                options.Transport = new StubTransport();
            });

            return services.BuildServiceProvider();
        }
    }

    internal sealed class StubTransport : IPaylinkTransport
    {
        public Task<PaylinkHttpResponse> SendAsync(
            string method,
            string url,
            IReadOnlyDictionary<string, string> headers,
            byte[]? body,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            const string body200 = "{\"success\":true,\"data\":{\"checkout_url\":\"https://x/y\",\"invoice_id\":1,\"expires_at\":\"t\"}}";
            return Task.FromResult(new PaylinkHttpResponse(200, body200));
        }
    }
}
