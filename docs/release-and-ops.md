# Release & Operations

This document describes packaging, deployment, and runbook steps for PlayPair.

## Packaging

Server (container):
- Dockerfile located at `src/PlayPair.Server/Dockerfile` builds a linux-based image hosting the ASP.NET Core server.
- Local package script: `scripts/package-server.ps1` publishes the server and produces a zip artifact under `./artifacts`.

Client (Windows):
- Build the WPF client with `dotnet publish` targeting `net8.0-windows` and package using MSIX or Inno Setup.
- For MVP: create an installer that places the executable and registers a Windows Tray background app.

## Deployment

Options:
1. Container-based (recommended for server):
   - Build image and push to a registry (GitHub Container Registry or Docker Hub).
   - Deploy behind a simple load balancer or serverless container host.
   - Use CI to create a GitHub Release with server artifact (see `.github/workflows/release.yml`).

### Quick internet-facing deployment checklist (for friend testing)

1. Deploy `src/PlayPair.Server/Dockerfile` to a public container host.
2. Expose HTTP port `80` in container runtime (Dockerfile already sets `ASPNETCORE_URLS=http://+:80`).
3. Verify endpoints:
   - `GET /health`
   - `GET /health/live`
   - `GET /health/ready`
4. Share the HTTPS base URL with testers.
5. Each tester launches client with:

```powershell
$env:PLAYPAIR_SERVER_URL = "https://your-public-server-url"
dotnet run --project .\src\PlayPair.Client.AppShell\PlayPair.Client.AppShell.csproj
```

2. VM-based:
   - Unzip the server artifact and run as a service (systemd on Linux, or a Windows service if applicable).

## Rollout Strategy

- Use tagged releases for production deployment (semantic tags like `v1.2.3`).
- Deploy to staging first, run smoke tests, then to production.
- For backend changes that may affect protocols, use feature flags and compatibility checks.

## Rollback

- Server (container): redeploy previous image tag.
- Client: revert to previous installer release and instruct users to reinstall.

## Monitoring & Alerts

Track these metrics:
- Error rate (5xx) on hub endpoints
- Command relay latency (95th percentile)
- Reconnect rate and resync frequency
- Duplicate command rate

Alerts:
- High error rate (>1% over 5m)
- Elevated reconnects (>5% of active rooms)
- Repeated resyncs (>10 per minute)

## Runbook — Incident Triage

1. Identify scope (server logs, recent releases)
2. Check health endpoints and queued errors
3. If server-side rollback required, deploy previous image
4. If client bug, create hotfix release and communicate to users
5. Post-incident: write an incident report and update playbook

## Operational notes

- Secrets: use GitHub Secrets for registry credentials and deployment tokens.
- Backups: MVP uses ephemeral in-memory state; for production consider durable storage for rooms or session logs.
