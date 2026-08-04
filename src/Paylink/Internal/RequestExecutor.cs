using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Paylink.Http;

namespace Paylink.Internal
{
    /// <summary>Options for a single API request.</summary>
    internal sealed class RequestOptions
    {
        public string Method { get; set; } = "POST";

        public string Path { get; set; } = "";

        public Dictionary<string, string>? Body { get; set; }

        public Dictionary<string, string>? Query { get; set; }

        public string? IdempotencyKey { get; set; }

        public bool? ReplaySafe { get; set; }

        public CancellationToken CancellationToken { get; set; }
    }

    /// <summary>
    /// Runs a request and returns the <c>data</c> element of the success envelope. Retries
    /// transient failures (429, 5xx, connection errors, timeouts) with exponential backoff
    /// and full jitter, honoring <c>Retry-After</c>. A request is only ever replayed when
    /// doing so cannot double-charge: GETs, requests carrying an <c>Idempotency-Key</c>, and
    /// calls explicitly flagged replay-safe.
    /// </summary>
    internal static class RequestExecutor
    {
        private const double RetryMaxDelaySeconds = 8.0;

        /// <summary>Sent on every request so server-side logs can attribute traffic to a version.</summary>
        public static readonly string UserAgent = "paylink-dotnet/" + PaylinkVersion.Value;

        /// <summary>Executes <paramref name="options"/> against the API with retries.</summary>
        public static async Task<JsonElement> ExecuteAsync(ResolvedConfig config, RequestOptions options)
        {
            var replayable = options.ReplaySafe
                ?? (options.Method == "GET" || options.IdempotencyKey != null);
            var maxAttempts = replayable ? config.MaxRetries + 1 : 1;

            var attempt = 1;
            while (true)
            {
                try
                {
                    return await AttemptAsync(config, options).ConfigureAwait(false);
                }
                catch (PaylinkException error) when (IsRetryable(error))
                {
                    if (attempt >= maxAttempts || !IsTransient(error))
                    {
                        throw;
                    }

                    await config.DelayAsync(BackoffDelay(config, error, attempt), options.CancellationToken)
                        .ConfigureAwait(false);
                    attempt++;
                }
            }
        }

        private static bool IsRetryable(PaylinkException error)
        {
            return error is PaylinkApiException || error is PaylinkConnectionException;
        }

        private static bool IsTransient(PaylinkException error)
        {
            if (error is PaylinkConnectionException)
            {
                return true;
            }

            return error is PaylinkApiException api && (api.Status == 429 || api.Status >= 500);
        }

        private static TimeSpan BackoffDelay(ResolvedConfig config, PaylinkException error, int attempt)
        {
            if (error is PaylinkApiException api && api.RetryAfter.HasValue)
            {
                return TimeSpan.FromSeconds(Math.Min(api.RetryAfter.Value.TotalSeconds, RetryMaxDelaySeconds));
            }

            var ceiling = Math.Min(
                config.RetryBaseDelay.TotalSeconds * Math.Pow(2, attempt - 1),
                RetryMaxDelaySeconds);

            return TimeSpan.FromSeconds(config.NextRandom() * ceiling);
        }

        private static async Task<JsonElement> AttemptAsync(ResolvedConfig config, RequestOptions options)
        {
            var url = BuildUrl(config.BaseUrl, options.Path, options.Query);

            var headers = new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "User-Agent", UserAgent },
            };

            byte[]? body = null;
            if (options.Method != "GET")
            {
                headers["Content-Type"] = "application/json";
                body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(options.Body ?? new Dictionary<string, string>()));
            }

            if (options.IdempotencyKey != null)
            {
                headers["Idempotency-Key"] = options.IdempotencyKey;
            }

            PaylinkHttpResponse response;
            try
            {
                response = await config.Transport
                    .SendAsync(options.Method, url, headers, body, config.Timeout, options.CancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (options.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception error)
            {
                throw new PaylinkConnectionException("Request to " + options.Path + " failed.", error);
            }

            return HandleResponse(response, options.Path);
        }

        private static JsonElement HandleResponse(PaylinkHttpResponse response, string path)
        {
            var root = TryParse(response.Text);
            var ok = response.Status >= 200 && response.Status < 300;

            if (!ok || IsFailureEnvelope(root))
            {
                throw ToApiError(response.Status, root, response.Text, ParseRetryAfter(response.GetHeader("Retry-After")));
            }

            if (root.HasValue
                && root.Value.ValueKind == JsonValueKind.Object
                && root.Value.TryGetProperty("data", out var data))
            {
                return data.Clone();
            }

            return root ?? default;
        }

        private static bool IsFailureEnvelope(JsonElement? root)
        {
            return root.HasValue
                && root.Value.ValueKind == JsonValueKind.Object
                && root.Value.TryGetProperty("success", out var success)
                && success.ValueKind == JsonValueKind.False;
        }

        private static PaylinkApiException ToApiError(int status, JsonElement? root, string rawText, TimeSpan? retryAfter)
        {
            string? message = null;
            IReadOnlyDictionary<string, object?>? errors = null;

            if (root.HasValue && root.Value.ValueKind == JsonValueKind.Object)
            {
                var envelope = root.Value;
                message = Json.StrOrNull(envelope, "message") ?? Json.StrOrNull(Json.Child(envelope, "data"), "message");

                var errorsElement = Json.Child(envelope, "errors");
                if (errorsElement.ValueKind == JsonValueKind.Object)
                {
                    errors = (Dictionary<string, object?>?)Json.ToClr(errorsElement);
                }
            }

            var raw = root.HasValue ? Json.ToClr(root.Value) : rawText;

            return new PaylinkApiException(
                message ?? ("PayLink API request failed with status " + status.ToString(CultureInfo.InvariantCulture) + "."),
                status,
                errors,
                raw,
                retryAfter);
        }

        private static JsonElement? TryParse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            try
            {
                using (var document = JsonDocument.Parse(text))
                {
                    return document.RootElement.Clone();
                }
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string BuildUrl(string baseUrl, string path, Dictionary<string, string>? query)
        {
            var url = baseUrl + path;
            if (query == null || query.Count == 0)
            {
                return url;
            }

            var builder = new StringBuilder(url);
            var first = true;
            foreach (var pair in query)
            {
                builder.Append(first ? '?' : '&');
                first = false;
                builder.Append(Uri.EscapeDataString(pair.Key));
                builder.Append('=');
                builder.Append(Uri.EscapeDataString(pair.Value));
            }

            return builder.ToString();
        }

        private static TimeSpan? ParseRetryAfter(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var seconds))
            {
                return TimeSpan.FromSeconds(Math.Max(0, seconds));
            }

            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var when))
            {
                var delta = when - DateTimeOffset.UtcNow;
                return delta > TimeSpan.Zero ? delta : TimeSpan.Zero;
            }

            return null;
        }
    }
}
