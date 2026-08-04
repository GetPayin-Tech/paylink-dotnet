using System.Collections.Generic;
using System.Text.Json;

namespace Paylink.Internal
{
    /// <summary>Small helpers for reading typed fields out of a response <see cref="JsonElement"/>.</summary>
    internal static class Json
    {
        /// <summary>The string at <paramref name="name"/>, or "" when absent/non-string.</summary>
        public static string Str(JsonElement element, string name)
        {
            return StrOrNull(element, name) ?? "";
        }

        /// <summary>The string at <paramref name="name"/>, or null when absent/non-string.</summary>
        public static string? StrOrNull(JsonElement element, string name)
        {
            if (element.ValueKind == JsonValueKind.Object
                && element.TryGetProperty(name, out var value)
                && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }

            return null;
        }

        /// <summary>The 64-bit integer at <paramref name="name"/>, or 0 when absent/non-number.</summary>
        public static long Int64(JsonElement element, string name)
        {
            return Int64OrNull(element, name) ?? 0L;
        }

        /// <summary>The 64-bit integer at <paramref name="name"/>, or null when absent/non-number.</summary>
        public static long? Int64OrNull(JsonElement element, string name)
        {
            if (element.ValueKind == JsonValueKind.Object
                && element.TryGetProperty(name, out var value)
                && value.ValueKind == JsonValueKind.Number
                && value.TryGetInt64(out var number))
            {
                return number;
            }

            return null;
        }

        /// <summary>The decimal at <paramref name="name"/>, or 0 when absent/non-number.</summary>
        public static decimal Decimal(JsonElement element, string name)
        {
            return DecimalOrNull(element, name) ?? 0m;
        }

        /// <summary>The decimal at <paramref name="name"/>, or null when absent/non-number.</summary>
        public static decimal? DecimalOrNull(JsonElement element, string name)
        {
            if (element.ValueKind == JsonValueKind.Object
                && element.TryGetProperty(name, out var value)
                && value.ValueKind == JsonValueKind.Number
                && value.TryGetDecimal(out var number))
            {
                return number;
            }

            return null;
        }

        /// <summary>The nested object at <paramref name="name"/>, or a JSON null element when absent.</summary>
        public static JsonElement Child(JsonElement element, string name)
        {
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value))
            {
                return value;
            }

            return default;
        }

        /// <summary>Converts a JSON element into a plain CLR value graph for error inspection.</summary>
        public static object? ToClr(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    return element.TryGetInt64(out var l) ? (object)l : element.GetDouble();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Array:
                    var list = new List<object?>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(ToClr(item));
                    }

                    return list;
                case JsonValueKind.Object:
                    var map = new Dictionary<string, object?>();
                    foreach (var property in element.EnumerateObject())
                    {
                        map[property.Name] = ToClr(property.Value);
                    }

                    return map;
                default:
                    return null;
            }
        }
    }
}
