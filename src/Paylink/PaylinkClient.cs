using Paylink.Internal;

namespace Paylink
{
    /// <summary>
    /// The PayLink integration client. Construct once with your integration's
    /// <c>PublicToken</c> and secret <c>HashToken</c>, then use the service properties:
    /// <see cref="Invoices"/>, <see cref="Payments"/>, <see cref="Vcc"/>, <see cref="Cards"/>,
    /// <see cref="Recurring"/>, and <see cref="Webhooks"/>.
    /// </summary>
    public sealed class PaylinkClient : IPaylinkClient
    {
        private readonly ResolvedConfig _config;

        /// <summary>Creates a client from an options object.</summary>
        public PaylinkClient(PaylinkClientOptions options)
            : this(ResolvedConfig.Resolve(options))
        {
        }

        /// <summary>Creates a client from the two required tokens, with all other options defaulted.</summary>
        public PaylinkClient(string publicToken, string hashToken)
            : this(new PaylinkClientOptions { PublicToken = publicToken, HashToken = hashToken })
        {
        }

        internal PaylinkClient(ResolvedConfig config)
        {
            _config = config;

            Invoices = new Invoices(_config);
            Payments = new Payments(_config);
            Vcc = new Vcc(_config);
            Cards = new Cards(_config);
            Recurring = new Recurring(_config);
            Webhooks = new Webhooks(_config);
        }

        /// <summary>Create checkouts.</summary>
        public IInvoices Invoices { get; }

        /// <summary>Void, refund, settle, reverse authorization, and check status.</summary>
        public IPayments Payments { get; }

        /// <summary>Server-to-server raw-card charges.</summary>
        public IVcc Vcc { get; }

        /// <summary>Tokenize, charge, and revoke stored cards.</summary>
        public ICards Cards { get; }

        /// <summary>Create and manage recurring mandates.</summary>
        public IRecurring Recurring { get; }

        /// <summary>Verify inbound webhook signatures.</summary>
        public IWebhooks Webhooks { get; }

        /// <summary>Never surfaces the signing secret.</summary>
        public override string ToString()
        {
            return "PaylinkClient(PublicToken=" + _config.PublicToken + ", BaseUrl=" + _config.BaseUrl + ")";
        }
    }
}
