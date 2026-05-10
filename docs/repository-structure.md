# PlayPair Repository Structure (Foundation)

## Solution Layout

- `PlayPair.sln` — root solution containing all baseline projects.
- `src/PlayPair.Client.AppShell` — client shell entry assembly (placeholder only).
- `src/PlayPair.Client.MediaSession` — media session module (placeholder only).
- `src/PlayPair.Client.Sync` — synchronization module (placeholder only).
- `src/PlayPair.Client.Transport` — transport module (placeholder only).
- `src/PlayPair.Contracts` — shared contracts/types module (placeholder only).
- `src/PlayPair.Server` — ASP.NET Core host with `/health` endpoint.
- `tests/PlayPair.Tests` — baseline xUnit test project.

## Build Baseline

This foundation enforces shared .NET defaults through `Directory.Build.props`:

- `net8.0` target framework
- nullable reference types enabled
- implicit usings enabled
- deterministic builds
- warnings treated as errors
