using System;
using System.Globalization;

namespace Paylink.Signing
{
    /// <summary>
    /// Canonical value → string coercion, used for BOTH the signature concatenation
    /// and the JSON request body, so the bytes signed are exactly the bytes sent.
    /// <c>null</c> becomes an empty string, matching PHP's <c>implode</c>. All numeric
    /// formatting is invariant-culture so a machine's locale never shifts a signature.
    /// </summary>
    internal static class Coerce
    {
        public static string ToWire(object? value)
        {
            switch (value)
            {
                case null:
                    return "";
                case bool b:
                    return b ? "1" : "0";
                case string s:
                    return s;
                case int i:
                    return i.ToString(CultureInfo.InvariantCulture);
                case long l:
                    return l.ToString(CultureInfo.InvariantCulture);
                case decimal m:
                    return m.ToString(CultureInfo.InvariantCulture);
                case double d:
                    return FormatFloating(d);
                case float f:
                    return FormatFloating(f);
                default:
                    throw new PaylinkException(
                        "Cannot serialize value of type " + value.GetType().Name + ".");
            }
        }

        /// <summary>
        /// Formats a floating-point amount with invariant culture, rendering an
        /// integer-valued float without a trailing decimal so it matches a plain
        /// integer amount on the wire (and the other SDKs). Throws on non-finite values.
        /// </summary>
        private static string FormatFloating(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new PaylinkException("Cannot serialize non-finite number: " + value);
            }

            if (value == Math.Floor(value) && Math.Abs(value) < 9.2e18)
            {
                return ((long)value).ToString(CultureInfo.InvariantCulture);
            }

            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
