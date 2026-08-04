# Changelog

All notable changes to this project are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and
this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
