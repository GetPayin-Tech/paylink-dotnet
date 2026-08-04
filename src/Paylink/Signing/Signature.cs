using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Paylink.Signing
{
    /// <summary>
    /// HMAC-SHA256 signing primitive, byte-compatible with the server. Mirrors
    /// <c>ExternalPaymentIntegration::buildSignatureString()</c>:
    /// <c>base64_encode(hash_hmac('sha256', implode('', $data), $hashToken, true))</c>.
    /// </summary>
    internal static class Signature
    {
        /// <summary>
        /// HMAC-SHA256 over the empty-string join of the already-ordered values,
        /// base64-encoded. Callers supply values in exact wire order, coerced with
        /// <see cref="Coerce"/>.
        /// </summary>
        public static string Build(IEnumerable<string> orderedValues, string hashToken)
        {
            var concatenated = string.Concat(orderedValues);

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(hashToken)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(concatenated));
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Constant-time string comparison, equivalent to PHP's <c>hash_equals</c>.
        /// Returns false on length mismatch without leaking timing.
        /// </summary>
        public static bool SignaturesEqual(string a, string b)
        {
            var bytesA = Encoding.UTF8.GetBytes(a);
            var bytesB = Encoding.UTF8.GetBytes(b);

            if (bytesA.Length != bytesB.Length)
            {
                return false;
            }

            var diff = 0;
            for (var i = 0; i < bytesA.Length; i++)
            {
                diff |= bytesA[i] ^ bytesB[i];
            }

            return diff == 0;
        }
    }
}
