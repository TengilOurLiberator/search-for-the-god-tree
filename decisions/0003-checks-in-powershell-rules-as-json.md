# 0003: Checks in PowerShell, rules as JSON

- **Date:** 2026-10-09
- **Decided by:** Lead

**Decision.** The automated checks are one PowerShell script, `tools/checks/Run-Checks.ps1`, that runs on Windows PowerShell 5.1 and PowerShell 7. All rules it enforces are JSON files in `data/`. Recipes are JSON.

**Reason.** The owner's machine had no Python, Node or .NET SDK, and "verify before you claim" requires that the checks be run before they are trusted. PowerShell is built into Windows, free on Linux and macOS, and preinstalled on GitHub's Linux runners. JSON is the one data format PowerShell reads without extra modules.

**Consequences.**
- The script and its tests must stay pure ASCII (PowerShell 5.1 misreads UTF-8 without a byte-order mark).
- In continuous integration the script and rules are taken from the base branch, never from the pull request.
- Revisit if contributors find PowerShell a real obstacle.
