using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Paylink.Internal
{
    /// <summary>Shared request helpers used by the resource services.</summary>
    internal static class Api
    {
        /// <summary>Signs <paramref name="input"/> for <paramref name="spec"/> and POSTs it.</summary>
        public static Task<JsonElement> PostSignedAsync(
            ResolvedConfig config,
            EndpointSpec spec,
            IReadOnlyDictionary<string, object?> input,
            string? idempotencyKey,
            bool? replaySafe,
            CancellationToken cancellationToken)
        {
            var body = SignedBody.Build(spec, input, config.PublicToken, config.HashToken);

            return RequestExecutor.ExecuteAsync(config, new RequestOptions
            {
                Method = "POST",
                Path = spec.Path,
                Body = body,
                IdempotencyKey = idempotencyKey,
                ReplaySafe = replaySafe,
                CancellationToken = cancellationToken,
            });
        }
    }
}
