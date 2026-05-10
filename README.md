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

## CI

GitHub Actions workflow: `.github/workflows/ci.yml`

It runs deterministic baseline checks:

1. Restore
2. Build (Release, no-restore)
3. Test (Release, no-build)
