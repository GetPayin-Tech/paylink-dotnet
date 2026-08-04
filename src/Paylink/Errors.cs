using System;
using System.Collections.Generic;

namespace Paylink
{
    /// <summary>Base class for every error raised by the SDK.</summary>
    public class PaylinkException : Exception
    {
        public PaylinkException(string message) : base(message) { }

        public PaylinkException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    /// <summary>
    /// Raised when the client is constructed with missing or invalid configuration
    /// (e.g. an empty public/hash token, or a non-positive timeout).
    /// </summary>
    public class PaylinkConfigurationException : PaylinkException
    {
        public PaylinkConfigurationException(string message) : base(message) { }
    }

    /// <summary>
    /// Raised when the API responds with a non-success envelope
    /// (<c>{ "success": false }</c>) or a non-2xx status. Carries the HTTP
    /// <see cref="Status"/>, the server-provided per-field <see cref="Errors"/>, and
    /// the <see cref="Raw"/> response body for inspection.
    /// </summary>
    public class PaylinkApiException : PaylinkException
    {
        public PaylinkApiException(
            string message,
            int status,
            IReadOnlyDictionary<string, object?>? errors = null,
            object? raw = null,
            TimeSpan? retryAfter = null)
            : base(message)
        {
            Status = status;
            Errors = errors;
            Raw = raw;
            RetryAfter = retryAfter;
        }

        /// <summary>The HTTP status code returned by the API.</summary>
        public int Status { get; }

        /// <summary>The server-provided validation errors, keyed by field, if any.</summary>
        public IReadOnlyDictionary<string, object?>? Errors { get; }

        /// <summary>The raw response body (parsed, or the raw text if not JSON).</summary>
        public object? Raw { get; }

        /// <summary>
        /// How long the server asked us to wait before retrying, parsed from the
        /// <c>Retry-After</c> header (present on 429s). The SDK already honors this for
        /// replay-safe requests; it is exposed so callers can schedule their own retry
        /// of a request the SDK will not replay on its own.
        /// </summary>
        public TimeSpan? RetryAfter { get; }

        /// <summary>True when the API rejected the request as a duplicate (HTTP 409).</summary>
        public bool IsIdempotencyConflict => Status == 409;

        /// <summary>True when the request was rate limited (HTTP 429).</summary>
        public bool IsRateLimited => Status == 429;

        /// <summary>
        /// True when the API forbade the request (HTTP 403) — typically because the
        /// relevant product (card tokenization, recurring payments) is not enabled for
        /// the integration's account.
        /// </summary>
        public bool IsForbidden => Status == 403;
    }

    /// <summary>Raised by <c>Webhooks.Verify</c> when a signature does not match.</summary>
    public class PaylinkSignatureException : PaylinkException
    {
        public PaylinkSignatureException(string message = "Webhook signature verification failed.")
            : base(message) { }
    }

    /// <summary>
    /// Raised when the request never produced an HTTP response — a network failure,
    /// DNS error, or the configured timeout elapsing before the server replied.
    /// </summary>
    public class PaylinkConnectionException : PaylinkException
    {
        public PaylinkConnectionException(string message) : base(message) { }

        public PaylinkConnectionException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
