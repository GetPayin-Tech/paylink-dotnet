# Changelog

All notable changes to this project are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and
this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.3.0] - 2026-11-18

### Fixed

- `Webhooks.Verify()` now accepts `card_token.created` and `card_token.failed`. Both carry the
  tokenisation and the card it produced, and the server signs all of those fields; this SDK signed
  none of them, so every card-token webhook failed verification. Both events are on by default for
  every integration, so any integrator saving cards was receiving webhooks it could not verify.

### Added

- `CardTokenCreated` and `CardTokenFailed` on `WebhookEventType`.
- The saved card on `WebhookEvent`: `TokenizationId`, `CardToken`, `CardBrand`, `CardLast4`,
  `CardExpMonth` and `CardExpYear`. They are optional constructor parameters at the end, so source
  that constructs an event positionally keeps compiling.

## [0.2.0]

### Added

- Optional `Iframe` parameter on `CreateInvoiceParams` to enable embedded (iframe)
  checkout. Like `PaymentMode`, it is sent in the request body but excluded from the
  HMAC signature.

## [0.1.0]

### Added

- Initial release: checkouts (`Invoices.CreateAsync`), payment operations
  (`Payments.VoidAsync` / `RefundAsync` / `SettleAsync` / `ReverseAuthorizationAsync` /
  `CheckStatusAsync`), server-to-server card charges (`Vcc.ChargeAsync`), card
  tokenization (`Cards.TokenizeAsync` / `ChargeAsync` / `RevokeAsync`), recurring
  mandates (`Recurring.CreateAsync` / `StatusAsync` / `CancelAsync` / `PauseAsync` /
  `ResumeAsync`), and webhook signature verification (`Webhooks.Verify` / `IsValid`).
- Order-sensitive HMAC-SHA256 request signing, proven byte-identical to the PHP
  server and the JS/Python SDKs by a shared golden-signature fixture.
- **Dependency-injection first:** `IPaylinkClient` and per-service interfaces, plus a
  `Paylink.DependencyInjection` package with `services.AddPaylink(...)` backed by
  `IHttpClientFactory`.
- Retry with exponential backoff and full jitter for transient failures (429, 5xx,
  connection errors, timeouts), honoring `Retry-After`. Only replay-safe requests are
  retried; charges are never replayed.
- Typed exception hierarchy (`PaylinkApiException` with `IsIdempotencyConflict` /
  `IsRateLimited` / `IsForbidden` and `RetryAfter`).
- Multi-target `net8.0` + `netstandard2.0`; pluggable `IPaylinkTransport`.
