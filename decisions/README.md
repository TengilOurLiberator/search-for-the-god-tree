# Decision log

Every creative or technical ruling, with its reason, so it is not reargued later. One file per decision, numbered, never rewritten: a decision that changes gets a new entry that names the one it replaces.

Each entry states: the date, who decided (owner or lead), the decision, the reason, and what it affects.

| # | Date | Decision | By |
|---|---|---|---|
| [0001](0001-title-spelling.md) | 2026-10-09 | The title is "Search for the Godtree"; "Godtree" is one word | Owner |
| [0002](0002-licences.md) | 2026-10-09 | MIT for code, CC0 1.0 for assets (code half replaced by 0019) | Owner |
| [0003](0003-checks-in-powershell-rules-as-json.md) | 2026-10-09 | Checks are written in PowerShell; rules are JSON data | Lead |
| [0004](0004-repository-layout-and-recipes.md) | 2026-10-09 | Repository layout; recipes live in `recipes/<asset-id>/` | Lead |
| [0005](0005-stack-changes-after-verification.md) | 2026-10-09 | Stack changes forced by licence and compatibility findings | Lead |
| [0006](0006-generator-rulings.md) | 2026-10-09 | First approved, conditional and rejected generator lists | Lead, pending owner on marked items |
| [0007](0007-cc0-only-asset-inputs.md) | 2026-10-09 | Only CC0 third-party material may be folded into assets | Lead |
| [0008](0008-canon-approved.md) | 2026-10-09 | Canon approved as drafted; first push and toolchain installs authorised | Owner |
| [0009](0009-overnight-authorisations.md) | 2026-10-09 | VRoid placeholders, Stable Audio Open and Chatterbox accepted; ragdolls in-house; lead may push own work; pacing | Owner |
| [0010](0010-pipeline-and-sandbox-choices.md) | 2026-10-10 | Tools outside the repository, queue-driven generation, TripoSR and Blender finishing, glTFast, sandbox built by script | Lead |
| [0011](0011-background-removal-incident.md) | 2026-10-10 | A non-commercial background remover was used by mistake; models rebuilt with BiRefNet; rule added | Lead; history purge is the owner's |
| [0012](0012-trellis2-approved.md) | 2026-10-10 | TRELLIS.2 approved, through ComfyUI's built-in nodes only; owner accepted the DINOv3 licence | Owner |
| [0013](0013-sandbox-first.md) | 2026-10-10 | Sandbox first; prototypes ahead of the roadmap; point-and-pull grabbing; generate many models, keep few | Owner |
| [0014](0014-art-direction-anime-rpg.md) | 2026-10-10 | Art direction changes to the painterly anime action-RPG look; fragments version 1 | Owner |
| [0015](0015-player-body.md) | 2026-10-10 | How a generated character becomes the player's body | Lead, one point pending owner |
| [0016](0016-owner-direction-session-4.md) | 2026-10-10 | Owner's direction after the fourth headset session | Owner |
| [0017](0017-scheduled-review-and-voted-ideas.md) | 2026-10-11 | Scheduled review of pull requests and voted ideas, through a guarded script; project stays closed until the owner opens it | Owner; lead for the build |
| [0018](0018-open-for-art-and-hand-made-work.md) | 2026-10-11 | Open for assets, documentation and proposals; hand-made work may use any tool; a route in for artists | Owner |
| [0019](0019-code-to-public-domain.md) | 2026-10-11 | Code moves from MIT to the Unlicense: everything the project owns is public domain | Owner |
