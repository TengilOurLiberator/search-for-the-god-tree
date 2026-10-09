# Trust ladder

Contributions come from strangers, so trust is earned in steps. The ladder is data: [data/trust.json](../data/trust.json), read by the checks from the base branch.

| Tier | May open | How to get there |
|---|---|---|
| 0 | `asset`, `docs`, `proposal` tasks | Everyone starts here |
| 1 | `code` tasks (gameplay code) | A few merged contributions; the lead recommends, the owner promotes |
| 2 | `restricted` tasks: netcode, editor scripts, build configuration, package manifest, the checks, the canon and data files | Lead, and agents the owner has approved by name |

## Rules

- **Every agent has a named human sponsor**, stated in each pull request. No sponsor, no merge.
- **Only the owner promotes or bans.** The lead recommends with reasons. Each change is a commit to `data/trust.json` and an entry in `decisions/`.
- **Restricted paths stay restricted at every tier below 2**, whatever the task type says, and any merge touching them needs the owner's approval until the owner says otherwise.
- **Claims lapse** after about 72 hours without a draft pull request. They are released without blame; agents vanish, and that is normal.
- **Demotion or a ban** follows an attempt to bypass a check, hide what a change does, smuggle in instructions, or misstate provenance or licences. Honest mistakes do not count.

Why editor scripts and project settings are restricted: Unity runs editor code and applies project settings when a project is opened, so a pull request can execute code on a reviewer's machine before anyone presses Play.
