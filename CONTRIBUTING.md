# Contributing

[AGENTS.md](AGENTS.md) is the briefing and the rules. This file is the practical how-to.

> Open for assets, documentation and proposals from anyone. Gameplay code needs trust tier 1. Artists and animators: [docs/artists.md](docs/artists.md) is written for you and needs no Git. Ideas and votes: [docs/voting.md](docs/voting.md).

Pull requests are read on a schedule by the lead, an AI reviewer that works through a guarded script; how it is protected, and what it can and cannot do, is in [docs/review.md](docs/review.md).

## Set-up

- Git and Git LFS (`git lfs install` once per machine).
- Unity 6000.3 LTS through Unity Hub. The Unity project will live in `unity/` (it does not exist yet).
- PowerShell to run the checks: Windows PowerShell 5.1 is built into Windows; PowerShell 7 (`pwsh`) is free on Linux and macOS.
- For asset work: the generator named in your brief, installed locally. Each recipe states the video memory it needs.

## The loop

1. Pick an open issue with no claim and comment to claim it. Read the whole brief.
2. Branch in your fork: `task/<issue-number>-<short-name>`.
3. Do the task and nothing else. Other things you notice become new issues.
4. Run the checks (below).
5. Open a pull request with the template, every section filled in.

## Running the checks

```bash
git diff --name-status origin/main...HEAD > changed.txt
```

Save your pull request description as `body.md`, then:

```bash
pwsh -File tools/checks/Run-Checks.ps1 -ChangedFilesPath changed.txt -PrBodyPath body.md -Author your-github-login
```

On Windows without PowerShell 7, use `powershell -ExecutionPolicy Bypass -File ...` instead of `pwsh -File ...`. To validate every recipe and asset in the repository:

```bash
pwsh -File tools/checks/Run-Checks.ps1 -All
```

The checks verify: the description is complete and names a sponsor; files are only in paths your task type and trust tier allow; every asset has a recipe; generators are approved; licences are declared and accepted; names, texture sizes, triangle counts and file sizes are within [data/budgets.json](data/budgets.json); no names from existing works.

If a check is wrong, open an issue. Never work around one.

## Conventions

- **Text files:** UTF-8, LF line endings, final newline ([.editorconfig](.editorconfig)).
- **C#:** four-space indent, `Godtree.*` namespaces, one type per file, no editor-only code outside `Editor/` folders. Code conventions will be expanded when the Unity project exists.
- **Assets:** named per [canon/art-bible.md](canon/art-bible.md); models as `.glb` where possible so the checks can measure them; textures as `.png`; shipped audio as `.ogg`.
- **Never commit:** third-party packages or SDK binaries, `.blend` files, executables, scripts outside your task, anything over 50 MB, the Unity `Library/` folder.
- **Commits:** short imperative subject, issue number in the body.

## Task types

| Type | Paths | Minimum tier |
|---|---|---|
| `asset` | `recipes/`, `unity/Assets/Godtree/Art/`, `unity/Assets/Godtree/Audio/` | 0 |
| `docs` | `docs/` | 0 |
| `proposal` | `proposals/` | 0 |
| `code` | `unity/Assets/Godtree/Scripts/`, `Tests/`, `Prefabs/`, `Scenes/` | 1 |
| `restricted` | anything | 2, plus the owner's approval |

The enforced list is [data/path-rules.json](data/path-rules.json).
