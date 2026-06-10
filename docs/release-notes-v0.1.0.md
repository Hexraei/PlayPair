# Release v0.1.0 — PlayPair MVP

Release summary:

- Initial MVP release containing:
  - Windows client (tray + overlay + media session integration)
  - ASP.NET Core SignalR server (room lifecycle, relay, in-memory state)
  - Shared contracts package
  - CI pipeline with tests and vulnerability scan
  - Dockerfile and packaging script for server
  - Release workflow for GitHub Actions
  - Quality gates and operational runbook (docs/release-and-ops.md)

Notes:
- This is a privacy-first sync tool; no media files are uploaded.
- Server uses ephemeral in-memory room state for MVP.

Next steps:
1. Push tag `v0.1.0` to origin (this script attempts to push; may require credentials).
2. Run GitHub Actions release workflow to publish server artifact.
3. Deploy server image to staging and perform smoke E2E tests with two clients.
