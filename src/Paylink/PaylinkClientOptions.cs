using System;
using Paylink.Http;

namespace Paylink
{
    /// <summary>Configuration for <see cref="PaylinkClient"/>.</summary>
    public sealed class PaylinkClientOptions
    {
        /// <summary>Identifies the integration; sent as <c>token</c> on every request.</summary>
        public string PublicToken { get; set; } = "";

        /// <summary>Secret signing key; server-side only, never expose to a client.</summary>
        public string HashToken { get; set; } = "";

        /// <summary>API base URL; defaults to <c>https://pay.getpayin.com</c>.</summary>
        public string? BaseUrl { get; set; }

        /// <summary>Per-attempt timeout; defaults to 30 seconds. With retries, total wall time can exceed this.</summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>How many times to RETRY a failed replay-safe request; defaults to 2 (0 disables).</summary>
        public int? MaxRetries { get; set; }

        /// <summary>Custom transport; defaults to a shared <see cref="HttpClientTransport"/>.</summary>
        public IPaylinkTransport? Transport { get; set; }
    }
}
