using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Paylink.Http
{
    /// <summary>The minimal response the SDK relies on. Headers are looked up case-insensitively.</summary>
    public sealed class PaylinkHttpResponse
    {
        private readonly IReadOnlyDictionary<string, string> _headers;

        public PaylinkHttpResponse(int status, string text, IReadOnlyDictionary<string, string>? headers = null)
        {
            Status = status;
            Text = text ?? "";
            _headers = headers ?? new Dictionary<string, string>();
        }

        public int Status { get; }

        public string Text { get; }

        public string? GetHeader(string name)
        {
            foreach (var pair in _headers)
            {
                if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
                {
                    return pair.Value;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Sends one request and returns a <see cref="PaylinkHttpResponse"/>. Must throw on a
    /// network failure or timeout (the SDK wraps that in a <see cref="PaylinkConnectionException"/>).
    /// Implement this to use a proxy-aware or pooled HTTP client, or a mock in tests.
    /// </summary>
    public interface IPaylinkTransport
    {
        Task<PaylinkHttpResponse> SendAsync(
            string method,
            string url,
            IReadOnlyDictionary<string, string> headers,
            byte[]? body,
            TimeSpan timeout,
            CancellationToken cancellationToken);
    }
}
