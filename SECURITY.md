# Security Policy

## Supported versions

Only the latest release receives security fixes.

## Reporting a vulnerability

Please **do not open a public issue** for security problems.

Use GitHub's private reporting instead:
**Security tab → [Report a vulnerability](https://github.com/bilal8492/upinstall/security/advisories/new)**.

Include what you found, steps to reproduce, and the UpInstall version. You can expect an initial response within a week.

## Security notes

- UpInstall makes no network requests of its own. It only runs the local `winget` executable.
- Package ids are validated (`^\w[\w.+\-]*$`) before being written to the PowerShell script that "Run in terminal" opens.
- That script is written to a unique file under `%LOCALAPPDATA%\UpInstall\runs\` (per-user) rather than a shared, predictable
  location, because it can be run elevated.
- Release binaries are built by GitHub Actions from the tagged commit and published with SHA-256 checksums and
  build-provenance attestations.
