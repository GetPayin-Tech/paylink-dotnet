using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Paylink.Http
{
    /// <summary>
    /// Default transport built on a shared <see cref="System.Net.Http.HttpClient"/>. HTTP
    /// error statuses (4xx/5xx) are returned as a response so the caller can read the error
    /// body; genuine connection failures throw.
    /// </summary>
    public sealed class HttpClientTransport : IPaylinkTransport
    {
        private static readonly HttpClient SharedClient = new HttpClient();

        private readonly HttpClient _client;

        public HttpClientTransport(HttpClient? client = null)
        {
            _client = client ?? SharedClient;
        }

        public async Task<PaylinkHttpResponse> SendAsync(
            string method,
            string url,
            IReadOnlyDictionary<string, string> headers,
            byte[]? body,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage(new HttpMethod(method), url))
            using (var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                timeoutSource.CancelAfter(timeout);

                if (body != null)
                {
                    request.Content = new ByteArrayContent(body);
                }

                foreach (var header in headers)
                {
                    if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value) && request.Content != null)
                    {
                        request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                using (var response = await _client
                    .SendAsync(request, HttpCompletionOption.ResponseContentRead, timeoutSource.Token)
                    .ConfigureAwait(false))
                {
                    var text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    var responseHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var header in response.Headers)
                    {
                        responseHeaders[header.Key] = string.Join(",", header.Value);
                    }

                    return new PaylinkHttpResponse((int)response.StatusCode, text, responseHeaders);
                }
            }
        }
    }
}
