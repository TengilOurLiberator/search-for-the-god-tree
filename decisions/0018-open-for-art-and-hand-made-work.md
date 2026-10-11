# 0018: Open for art, documentation and proposals; hand-made work in any tool

- **Date:** 2026-10-11
- **Decided by:** Owner

**Decisions.**
1. **The project is open to outside contributions of assets, documentation and proposals.** `data/review.json` is set to open. Gameplay code stays at trust tier 1 and restricted areas at tier 2, as the trust ladder already says, so opening changes nothing for code until the owner promotes someone.
2. **Hand-made work may be made in any tool the artist is licensed to use**, paid or free. What is delivered must be in open formats (`.glb` or `.fbx`, `.png`, `.ogg`) and released as CC0. The rule that generators must be free, local and on the approved list applies to generated work, where the point is that anyone can reproduce it; a person's handwork is not reproduced by re-running a tool.
3. **Artists get their own route in**: a guide (`docs/artists.md`), an "Offer art or animation" issue form for people who do not use Git, and credit by name in `CREDITS.md`.

**Reason.** The owner wants real artists and animators to be able to give models, animation and the like. The things the pipeline cannot do (four-legged animation, bending fingers, faces, thin and hollow shapes) are exactly what people can.

**Not changed.** Only CC0 material may be folded into assets (decision 0007). The first merge and merges touching restricted paths remain the owner's. Files offered on an issue are downloaded only in a working session, with the owner's agreement, never by the scheduled reviewer.

**Affects.** `data/review.json`, AGENTS.md, CONTRIBUTING.md, README.md, ROADMAP.md, CREDITS.md, canon/art-bible.md (its closing paragraph was out of date), docs/artists.md, docs/recipes.md, docs/voting.md, `.github/ISSUE_TEMPLATE/art-offer.yml`.
