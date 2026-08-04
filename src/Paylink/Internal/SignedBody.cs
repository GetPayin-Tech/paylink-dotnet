using System;
using System.Collections.Generic;
using Paylink.Signing;

namespace Paylink.Internal
{
    /// <summary>
    /// Turns a snake_case value map into the signed wire body for an endpoint: the
    /// endpoint's fields, the public <c>token</c>, and the computed <c>signature</c>. The
    /// signature is built from the same coerced strings that go into the body, in the
    /// spec's order, skipping fields the caller did not provide — exactly how the server
    /// reconstructs it from <c>Arr::except($request-&gt;validated(), ...)</c>.
    /// </summary>
    internal static class SignedBody
    {
        /// <summary>Builds the signed body for <paramref name="spec"/> from <paramref name="input"/>.</summary>
        public static Dictionary<string, string> Build(
            EndpointSpec spec,
            IReadOnlyDictionary<string, object?> input,
            string publicToken,
            string hashToken)
        {
            var signedValues = new List<string>();
            var body = new Dictionary<string, string>();

            Action<object?, string, bool> append = (value, wireKey, signed) =>
            {
                if (value == null)
                {
                    return;
                }

                var stringValue = Coerce.ToWire(value);
                body[wireKey] = stringValue;

                if (signed)
                {
                    signedValues.Add(stringValue);
                }
            };

            foreach (var field in spec.Fields)
            {
                input.TryGetValue(field.Name, out var value);
                append(value, field.Name, field.Signed);
            }

            if (spec.CountryStateBlock)
            {
                AppendCountryStateBlock(input, append);
            }

            body["token"] = publicToken;
            body["signature"] = Signature.Build(signedValues, hashToken);

            return body;
        }

        /// <summary>
        /// Appends the per-country state fields after the listed fields: US adds
        /// <c>us_state</c> + <c>postal_code</c>; CA adds <c>canada_state</c> + <c>postal_code</c>.
        /// </summary>
        private static void AppendCountryStateBlock(
            IReadOnlyDictionary<string, object?> input,
            Action<object?, string, bool> append)
        {
            input.TryGetValue("country", out var country);
            var code = country as string;

            if (code == "US")
            {
                input.TryGetValue("us_state", out var usState);
                append(usState, "us_state", true);
                input.TryGetValue("postal_code", out var postalCode);
                append(postalCode, "postal_code", true);
                return;
            }

            if (code == "CA")
            {
                input.TryGetValue("canada_state", out var canadaState);
                append(canadaState, "canada_state", true);
                input.TryGetValue("postal_code", out var postalCode);
                append(postalCode, "postal_code", true);
            }
        }
    }
}
