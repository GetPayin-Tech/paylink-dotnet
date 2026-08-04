using System;
using System.Net.Http;
using Paylink;
using Paylink.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>Registers the Paylink client and its services in the DI container.</summary>
    public static class PaylinkServiceCollectionExtensions
    {
        /// <summary>The named <see cref="IHttpClientFactory"/> client the transport resolves.</summary>
        public const string HttpClientName = "Paylink";

        /// <summary>
        /// Registers <see cref="IPaylinkClient"/> (and each service interface — <see cref="IInvoices"/>,
        /// <see cref="IPayments"/>, <see cref="IVcc"/>, <see cref="ICards"/>, <see cref="IRecurring"/>,
        /// <see cref="IWebhooks"/>) as singletons. Unless a transport is set in
        /// <paramref name="configure"/>, requests go through an <see cref="IHttpClientFactory"/>-backed
        /// transport so handler lifetimes are managed by the container.
        /// </summary>
        public static IServiceCollection AddPaylink(
            this IServiceCollection services,
            Action<PaylinkClientOptions> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            services.AddHttpClient(HttpClientName);

            services.AddSingleton<IPaylinkClient>(provider =>
            {
                var options = new PaylinkClientOptions();
                configure(options);

                if (options.Transport == null)
                {
                    var factory = provider.GetRequiredService<IHttpClientFactory>();
                    options.Transport = new HttpClientFactoryTransport(factory, HttpClientName);
                }

                return new PaylinkClient(options);
            });

            services.AddSingleton<IInvoices>(provider => provider.GetRequiredService<IPaylinkClient>().Invoices);
            services.AddSingleton<IPayments>(provider => provider.GetRequiredService<IPaylinkClient>().Payments);
            services.AddSingleton<IVcc>(provider => provider.GetRequiredService<IPaylinkClient>().Vcc);
            services.AddSingleton<ICards>(provider => provider.GetRequiredService<IPaylinkClient>().Cards);
            services.AddSingleton<IRecurring>(provider => provider.GetRequiredService<IPaylinkClient>().Recurring);
            services.AddSingleton<IWebhooks>(provider => provider.GetRequiredService<IPaylinkClient>().Webhooks);

            return services;
        }
    }
}
