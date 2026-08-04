using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Paylink.Http;

namespace Paylink.DependencyInjection
{
    /// <summary>
    /// A transport backed by <see cref="IHttpClientFactory"/>, so HTTP handlers are pooled and
    /// their lifetimes managed by the DI container rather than a hand-created client.
    /// </summary>
    internal sealed class HttpClientFactoryTransport : IPaylinkTransport
    {
        private readonly IHttpClientFactory _factory;
        private readonly string _clientName;

        public HttpClientFactoryTransport(IHttpClientFactory factory, string clientName)
        {
            _factory = factory;
            _clientName = clientName;
        }

        public async Task<PaylinkHttpResponse> SendAsync(
            string method,
            string url,
            IReadOnlyDictionary<string, string> headers,
            byte[]? body,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            var client = _factory.CreateClient(_clientName);

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

                using (var response = await client
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
