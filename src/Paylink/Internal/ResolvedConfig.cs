using System;
using System.Threading;
using System.Threading.Tasks;
using Paylink.Http;

namespace Paylink.Internal
{
    /// <summary>Validated, internal configuration passed to every service.</summary>
    internal sealed class ResolvedConfig
    {
        private static readonly Random SharedRandom = new Random();

        private ResolvedConfig(
            string publicToken,
            string hashToken,
            string baseUrl,
            TimeSpan timeout,
            int maxRetries,
            IPaylinkTransport transport)
        {
            PublicToken = publicToken;
            HashToken = hashToken;
            BaseUrl = baseUrl;
            Timeout = timeout;
            MaxRetries = maxRetries;
            Transport = transport;
        }

        public string PublicToken { get; }

        public string HashToken { get; }

        public string BaseUrl { get; }

        public TimeSpan Timeout { get; }

        public int MaxRetries { get; }

        public IPaylinkTransport Transport { get; }

        /// <summary>Base delay for exponential backoff. Internal; overridden in tests.</summary>
        public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromMilliseconds(250);

        /// <summary>Delay function used between retries. Internal; overridden in tests.</summary>
        public Func<TimeSpan, CancellationToken, Task> DelayAsync { get; set; } =
            (delay, cancellationToken) => Task.Delay(delay, cancellationToken);

        /// <summary>Jitter source in [0,1). Internal; overridden in tests.</summary>
        public Func<double> NextRandom { get; set; } = DefaultRandom;

        /// <summary>Validates <paramref name="options"/> and fills defaults.</summary>
        public static ResolvedConfig Resolve(PaylinkClientOptions options)
        {
            if (options == null)
            {
                throw new PaylinkConfigurationException("options is required.");
            }

            var publicToken = RequireNonEmpty(options.PublicToken, "PublicToken");
            var hashToken = RequireNonEmpty(options.HashToken, "HashToken");

            var timeout = options.Timeout ?? TimeSpan.FromSeconds(30);
            if (timeout <= TimeSpan.Zero)
            {
                throw new PaylinkConfigurationException("Timeout must be a positive duration.");
            }

            var maxRetries = options.MaxRetries ?? 2;
            if (maxRetries < 0)
            {
                throw new PaylinkConfigurationException("MaxRetries must be a non-negative integer.");
            }

            var baseUrl = (options.BaseUrl ?? "https://pay.getpayin.com").TrimEnd('/');
            var transport = options.Transport ?? new HttpClientTransport();

            return new ResolvedConfig(publicToken, hashToken, baseUrl, timeout, maxRetries, transport);
        }

        private static string RequireNonEmpty(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new PaylinkConfigurationException(name + " is required and must be a non-empty string.");
            }

            return value;
        }

        private static double DefaultRandom()
        {
            lock (SharedRandom)
            {
                return SharedRandom.NextDouble();
            }
        }
    }
}
