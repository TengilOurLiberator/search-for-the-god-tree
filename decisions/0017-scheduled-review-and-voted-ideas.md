# 0017: Scheduled review of pull requests and voted ideas

- **Date:** 2026-10-11
- **Decided by:** Owner (the routine); Lead (how it is built)

**Decision.** The owner asked for the routine their other project (Omnissiah) has: pull requests reviewed on a schedule, and ideas that people vote for reviewed and turned into tasks.

1. **Two scheduled runs on the owner's PC**: pull requests every 15 minutes, ideas once a day. Each run can reach GitHub only through `tools/review/safe.mjs`.
2. **The script is adapted from Omnissiah's, not copied.** Its gate reads this project's own rules from the main branch (`data/path-rules.json`, `data/trust.json`) in place of a hard-coded list, requires the automated checks to be green, and knows what is dangerous in a Unity project (editor scripts, assembly definitions, project settings, C# that starts programs, touches files or the network).
3. **A new switch, `data/review.json`.** While `open_to_outside` is false the reviewer reads and comments but can approve nothing. It is false today. Opening the project is the owner's decision (roadmap milestone 4).
4. **Ideas are voted on in GitHub Discussions, category Ideas.** An accepted idea becomes a task issue labelled `task` and `voted`, written as a brief in the lead's own words. Proposals (`proposals/`) stay as the longer, sponsored route. The daily run cannot change the canon; ideas that need a canon change wait for a working session.
5. **The script is Node, not PowerShell.** Decision 0003 put the contribution checks in PowerShell so contributors need nothing extra. This script is run only by the scheduled reviewer, and reusing four hundred lines that have already been tested against manipulation is safer than rewriting them. Node and the GitHub CLI live in the tools folder outside the repository (decision 0010).
6. **Discussions were switched on** for the repository to make this possible: a settings change, done because the owner asked for the routine.

**Reason.** The owner wants the same loop as on Omnissiah: people vote, agents build, the lead reviews, the owner merges. The protections exist because the reviewer reads text written by strangers.

**Not changed.** An approval is not a merge. The first merge, merges touching restricted paths, promotions and the licence remain the owner's (docs/review.md). The licences stay as decision 0002 set them.

**Affects.** `tools/review/safe.mjs`, `data/review.json`, docs/review.md, docs/voting.md, docs/lead-handoff.md, CONTRIBUTING.md, README.md, ROADMAP.md, proposals/README.md.
