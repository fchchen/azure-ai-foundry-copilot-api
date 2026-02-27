# Changelog

## 2026-02-27

### Added
- Microsoft 365 inbox triage feature: `POST /api/ai-foundry/unread-email-summary`.
- Graph mail integration services (real HTTP + mock provider).
- Application-layer orchestration service for AI/chat and unread-email summary use cases.
- Copilot package OAuth stamping script: `scripts/configure-apppackage-oauth.sh`.
- Privacy and terms docs: `PRIVACY.md`, `TERMS.md`.
- New tests for security config, unread-email summary, OpenAPI parity, orchestration, and blob selection logic.

### Changed
- Copilot plugin/auth metadata now uses OAuth and runtime-configured tenant/client/scope/API base URL.
- Copilot metadata endpoints now support in-memory caching, `ETag`, and `304 Not Modified`.
- OpenAPI contracts tightened with required response fields.
- Security posture hardened:
  - mock auth blocked outside Development/Testing
  - CORS deny-by-default when origins are not configured
  - anonymous health endpoint no longer leaks infrastructure flags outside Development
  - AI key resolution now supports secret-provider fallback
- Blob recent-conversation listing optimized to select top N by metadata before downloading content.
- Copilot declarative instructions and conversation starters moved to configuration.
- CI now collects and publishes coverage summary/artifacts.

### Notes
- Full test suite passing on latest run: `70 passed, 0 failed`.
