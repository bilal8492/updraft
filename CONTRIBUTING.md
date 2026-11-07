# Contributing to Updraft

Thanks for helping! Updraft is intentionally small. Please keep changes focused.

## Setup

1. Install the [.NET 9 SDK](https://dotnet.microsoft.com/download) (`winget install Microsoft.DotNet.SDK.9`).
2. Fork and clone the repo.
3. `.\build.cmd` then `.\Start-Updraft.cmd`.

You don't need Visual Studio, but it works too: open `Updraft/Updraft.csproj`.

## Pull requests

- Open an issue first for anything bigger than a small fix, so we can agree on the approach.
- One topic per PR. Keep the diff small and explain *why* in the description.
- Make sure `dotnet build -c Release -p:Platform=x64 -r win-x64` succeeds with no warnings. CI also builds ARM64.
- Test against real winget output. `WingetService.Parse` must stay language-independent.
- Never write unvalidated text into the generated PowerShell script. Use `WingetService.IsSafeId`.

## Privacy and secrets

No tokens, certificates, personal paths or e-mail addresses in commits. Use your GitHub `noreply` address if you like.

## Conduct

Be kind. See [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).

By contributing you agree your work is licensed under the [MIT License](LICENSE).
