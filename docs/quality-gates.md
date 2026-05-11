# CI Quality Gates

This document describes the CI quality gates enforced by .github/workflows/ci.yml.

Gates enforced:

1. Build succeeds (Release configuration)
2. Unit & integration tests pass (`dotnet test`)
3. Dependency vulnerability scan (`dotnet list package --vulnerable`) — pipeline fails if vulnerabilities are reported
4. Quick static analysis: Re-run build to ensure no transient warnings (warnings-as-errors enforced by Directory.Build.props)

How to run locally:

- dotnet restore PlayPair.sln
- dotnet build PlayPair.sln -c Release --no-restore
- dotnet test PlayPair.sln -c Release --no-build
- dotnet list PlayPair.sln package --vulnerable

If the vulnerability scan reports issues, address package upgrades or create a risk-acceptance ticket.
