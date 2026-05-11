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
