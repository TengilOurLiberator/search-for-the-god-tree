# Style kit

Status: **started (v0).** Prompt fragments, one proven image workflow and 20 golden concept images exist. Golden 3D assets do not yet. Asset contributions open only when they do.

The style kit is what makes assets from many contributors look like one game. When built it will hold, versioned:

- **Approved generators**: the enforced list is [data/approved-generators.json](../data/approved-generators.json).
- **Shared prompt fragments**: the wording every recipe starts from for style, lighting, palette and negatives.
- **Golden reference assets**: one character, one weapon, one enemy and one environment kit, each with its recipe, that define the look.
- **Reference workflows**: ComfyUI workflow files proven on the owner's machine.

## What exists

- [prompt-fragments.json](prompt-fragments.json): shared prompt wording, version 0.
- [workflows/](workflows/): ComfyUI workflows in API format, proven on the owner's machine.
- [jobs/](jobs/): subject lists, expanded job files and run logs.
- [golden/concepts/](golden/concepts/): the chosen reference images.

To generate: `tools/pipeline/New-ConceptJobs.ps1` expands a subjects file into jobs; `tools/pipeline/Invoke-GenerationQueue.ps1` runs them unattended through a local ComfyUI.

Where the kit is silent, [canon/art-bible.md](../canon/art-bible.md) decides.
