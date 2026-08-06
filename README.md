# @getpayin-tech/paylink-dotnet

[![CI](https://github.com/GetPayin-Tech/paylink-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/GetPayin-Tech/paylink-dotnet/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Paylink.svg)](https://www.nuget.org/packages/Paylink/)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%20netstandard2.0-512bd4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

Official **server-side** .NET SDK for the PayLink payment integration API.
It wraps every integration endpoint with an idiomatic, typed, **dependency-injection-first**
API and computes the order-sensitive HMAC-SHA256 signatures for you, so you never
have to build them by hand.

- Checkouts (`Invoices.CreateAsync`)
- Payment operations (`Payments.VoidAsync` / `RefundAsync` / `SettleAsync` / `ReverseAuthorizationAsync` / `CheckStatusAsync`)
- Server-to-server card charges (`Vcc.ChargeAsync`)
- Card tokenization (`Cards.TokenizeAsync` / `ChargeAsync` / `RevokeAsync`)
- Recurring mandates (`Recurring.CreateAsync` / `StatusAsync` / `CancelAsync` / `PauseAsync` / `ResumeAsync`)
- Webhook signature verification (`Webhooks.Verify`)

> **Server-side only.** Signing uses your secret `HashToken`. Never ship it to a
> browser, mobile, or desktop client.

## Requirements

- .NET **8.0** or any **.NET Standard 2.0** target (.NET Framework 4.6.2+, .NET Core 2.0+, modern .NET)
- The core `Paylink` package has no third-party dependencies; `Paylink.DependencyInjection` adds the `Microsoft.Extensions.*` DI packages.

## Install

```bash
dotnet add package Paylink
dotnet add package Paylink.DependencyInjection   # DI integration (recommended)
```

## Quick start (dependency injection)

Register the client once; inject `IPaylinkClient` anywhere. The transport is backed by
`IHttpClientFactory`, so handler lifetimes are managed by the container.

```csharp
// Program.cs
builder.Services.AddPaylink(options =>
{
    options.PublicToken = builder.Configuration["Paylink:PublicToken"]!;
    options.HashToken = builder.Configuration["Paylink:HashToken"]!; // secret — server-side only
    // options.BaseUrl defaults to https://pay.getpayin.com
    // options.Timeout defaults to 30s per attempt; options.MaxRetries defaults to 2
});
```

```csharp
public sealed class CheckoutController : ControllerBase
{
    private readonly IPaylinkClient _paylink;

    public CheckoutController(IPaylinkClient paylink) => _paylink = paylink;

    [HttpPost("pay")]
    public async Task<IActionResult> Pay()
    {
        var checkout = await _paylink.Invoices.CreateAsync(new CreateInvoiceParams
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            OrderTitle = "Gold Plan",
            OrderAmount = "250.00", // pass amounts as strings to control the exact wire form
            Currency = "USD",
            RedirectionUrl = "https://shop.example.com/return",
            WebhookUrl = "https://shop.example.com/webhooks/paylink",
        });

        return Redirect(checkout.CheckoutUrl);
    }
}
```

For an **embedded checkout**, set `Iframe = true` and render `checkout.CheckoutUrl`
inside an `<iframe>` instead of redirecting (sent in the request body but excluded
from the signature, like `PaymentMode`):

```csharp
var checkout = await _paylink.Invoices.CreateAsync(new CreateInvoiceParams
{
    FirstName = "John",
    LastName = "Doe",
    Email = "john@example.com",
    OrderTitle = "Gold Plan",
    OrderAmount = "250.00",
    Currency = "USD",
    Iframe = true, // enable embedded (iframe) checkout
});
```

Each service interface (`IInvoices`, `IPayments`, `IVcc`, `ICards`, `IRecurring`,
`IWebhooks`) is registered too, so you can inject just the one you need.

Both credentials are issued in the PayLink dashboard under **Settings → Payment
Integrations**. `PublicToken` is sent on every request; `HashToken` is the secret
used only to sign — it never leaves your server.

### Without a container

```csharp
var paylink = new PaylinkClient(publicToken, hashToken);
var checkout = await paylink.Invoices.CreateAsync(new CreateInvoiceParams { /* ... */ });
```

## Payment operations

```csharp
await paylink.Payments.VoidAsync(invoiceId);
await paylink.Payments.SettleAsync(invoiceId, amount: "50.00");
await paylink.Payments.ReverseAuthorizationAsync(invoiceId);

var status = await paylink.Payments.CheckStatusAsync(invoiceId);
// PaymentResult { InvoiceId, PaidStatus, AuthCode }

// Refunds are idempotent when you pass an idempotency key — safe to retry:
var refund = await paylink.Payments.RefundAsync(invoiceId, "10.50", idempotencyKey: "refund-order-1234");
// RefundResult { ..., RefundAmount }
```

## Card tokenization

```csharp
var vaulted = await paylink.Cards.TokenizeAsync(new TokenizeCardParams
{
    FirstName = "Jane", LastName = "Doe",
    CardNumber = "4111111111111111",
    CardExpiryMonth = "12", CardExpiryYear = "2030", CardCvv = "123",
    Country = "EG", Address = "1 Main St", City = "Cairo",
});

await paylink.Cards.ChargeAsync(new ChargeCardParams
{
    CardToken = vaulted.Token,
    Initiator = "merchant",
    FirstName = "Jane", LastName = "Doe",
    Currency = "USD", Price = "100.00", Product = "Monthly rebill",
    Country = "EG", Address = "1 Main St", City = "Cairo",
});

await paylink.Cards.RevokeAsync(vaulted.Token);
```

For `US` and `CA` billing addresses, also pass the state fields the API requires:
`UsState` + `PostalCode` (US) or `CanadaState` + `PostalCode` (CA).

## Recurring mandates

```csharp
var mandate = await paylink.Recurring.CreateAsync(new CreateRecurringParams
{
    FirstName = "Sam", LastName = "Doe", Email = "sam@example.com",
    OrderTitle = "Gold subscription",
    OrderAmount = "250.00", Currency = "USD",
    CadenceInterval = "month", CadenceCount = 1, TotalCycles = 12,
    ConsentText = "I authorise recurring monthly charges.",
}, idempotencyKey: "sub-signup-42");

await paylink.Recurring.StatusAsync(mandate.MandateId);
await paylink.Recurring.PauseAsync(mandate.MandateId);
await paylink.Recurring.ResumeAsync(mandate.MandateId);
await paylink.Recurring.CancelAsync(mandate.MandateId);
```

## Idempotency

Pass an `idempotencyKey` to make a retried write safe — the SDK sends it as the
`Idempotency-Key` header and the server returns the original result instead of
charging, refunding, or creating a second time. Keys are scoped per integration and
capped at 64 characters. Honored on `Invoices.CreateAsync`, `Vcc.ChargeAsync`,
`Cards.ChargeAsync`, `Payments.RefundAsync`, and `Recurring.CreateAsync`.

Reusing a key with a _different_ request is rejected as a conflict: a
`PaylinkApiException` with `IsIdempotencyConflict` (HTTP 409).

## Verifying webhooks

Pass the raw request body to `Verify`. It recomputes the signature with your
`HashToken` and compares in constant time.

```csharp
[HttpPost("/webhooks/paylink")]
public async Task<IActionResult> Webhook()
{
    using var reader = new StreamReader(Request.Body);
    var body = await reader.ReadToEndAsync();

    try
    {
        var webhookEvent = _paylink.Webhooks.Verify(body);
        // webhookEvent.Event, webhookEvent.InvoiceId, webhookEvent.Success, webhookEvent.Raw, ...
        return Ok();
    }
    catch (PaylinkSignatureException)
    {
        return BadRequest();
    }
}
```

> PayLink webhook signatures carry no timestamp, so verification does not protect
> against replay. Pair it with your own idempotency keyed on `invoice_id`.

## Error handling

Every failure is a subclass of `PaylinkException`:

| Exception                     | When                                                                                                                   |
| ----------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| `PaylinkConfigurationException` | Invalid client configuration (missing tokens, non-positive timeout).                                                 |
| `PaylinkApiException`         | The API returned an error. Carries `Status`, `Errors`, `Raw`, `RetryAfter`, and the `IsIdempotencyConflict` / `IsRateLimited` / `IsForbidden` flags. |
| `PaylinkSignatureException`   | A webhook signature did not verify.                                                                                    |
| `PaylinkConnectionException`  | Network failure or timeout (no HTTP response).                                                                         |

```csharp
try
{
    await paylink.Payments.RefundAsync(invoiceId, "10.00");
}
catch (PaylinkApiException error) when (error.IsIdempotencyConflict)
{
    // a refund with this idempotency key already exists
}
```

## Retries and rate limiting

Every integration endpoint is rate limited server-side, so 429s are an expected
condition under burst traffic. The SDK retries transient failures — **429, 5xx,
connection errors, and timeouts** — with exponential backoff and full jitter,
honoring the server's `Retry-After` header when present.

**A request is only ever replayed when replaying it cannot double-charge:** GETs,
any call you pass an `idempotencyKey` to, and pure reads such as
`Payments.CheckStatusAsync`. A bare `Vcc.ChargeAsync` or `Cards.ChargeAsync` is never
replayed. Tune with `MaxRetries` (set `0` to disable). `Timeout` applies to each
attempt.

## Amounts and precision

Signatures are computed over the exact bytes sent on the wire. To avoid any
floating-point ambiguity, **pass monetary amounts as strings** (e.g. `"10.50"`).

## Custom transport

Implement `IPaylinkTransport` for a proxy-aware client or a mock in tests, and set it
on `PaylinkClientOptions.Transport` (or in `AddPaylink`). The default uses
`IHttpClientFactory` under DI, or a shared `HttpClient` otherwise.

## API reference

The full HTTP API — endpoints, fields, error codes, and test cards — is documented in
the PayLink API reference:
<https://pay.getpayin.com/docs/payment_integration/index.html>

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) — in particular the note on signed-field
ordering, which must stay in lockstep with the server.

Security issues: see [SECURITY.md](SECURITY.md). Please do not open a public issue
for a vulnerability.

## License

MIT
