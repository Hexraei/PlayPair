# PlayPair

Production-grade .NET repository skeleton and CI baseline.

## Prerequisites

- .NET SDK 8.0+

## Repository Structure

- `PlayPair.sln`
- `src/PlayPair.Client.AppShell`
- `src/PlayPair.Client.MediaSession`
- `src/PlayPair.Client.Sync`
- `src/PlayPair.Client.Transport`
- `src/PlayPair.Contracts`
- `src/PlayPair.Server`
- `tests/PlayPair.Tests`

## Local Development Commands

```bash
dotnet restore PlayPair.sln
dotnet build PlayPair.sln --configuration Release --no-restore
dotnet test PlayPair.sln --configuration Release --no-build
```

## Cloud Deployment (Render.com)

We have configured a Render Blueprint (`render.yaml`) to make cloud deployment simple.

1. **Push Code**: Push this codebase to your own GitHub repository.
2. **Deploy on Render**:
   - Go to [Render Blueprints](https://dashboard.render.com/blueprints).
   - Click **New Blueprint Instance**.
   - Connect your GitHub repository.
   - Render will automatically read the `render.yaml` file and configure the Web Service to build using `src/PlayPair.Server/Dockerfile`.
   - Click **Approve** to start the build and deployment.
3. **Verify Health**: Once deployed, navigate to your public Render URL (e.g. `https://your-app-name.onrender.com/health`) in your browser to verify it returns `status: "ok"`.

## Connecting Standalone Client

Once the cloud server is live, start the standalone Windows client:

```powershell
$env:PLAYPAIR_SERVER_URL = "https://your-app-name.onrender.com"
.\dist\PlayPair.exe
```

The application will automatically connect to `${PLAYPAIR_SERVER_URL}/hubs/room`.

## Documentation

- **User Guide**: [docs/user-guide.md](docs/user-guide.md) — How to use PlayPair
- **Quality Gates**: [docs/quality-gates.md](docs/quality-gates.md)
- **Release & Operations**: [docs/release-and-ops.md](docs/release-and-ops.md)
- **Release Notes**: [docs/release-notes-v0.1.0.md](docs/release-notes-v0.1.0.md)

## CI

GitHub Actions workflow: `.github/workflows/ci.yml`

It runs deterministic baseline checks:

1. Restore
2. Build (Release, no-restore)
3. Test (Release, no-build)
