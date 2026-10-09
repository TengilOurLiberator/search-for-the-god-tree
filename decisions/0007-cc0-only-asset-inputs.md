# 0007: Only CC0 third-party material in assets

- **Date:** 2026-10-09
- **Decided by:** Lead

**Decision.** Third-party material folded into an asset (a texture, a base mesh, an animation clip) must be CC0. Attribution licences such as CC BY 4.0 are not accepted for now.

**Reason.** The asset tree is CC0 (decision 0002). Material under an attribution licence cannot be relicensed CC0, so accepting it would mean a mixed tree, per-file licence tracking and a credits file that must never fall out of date. The approved sources (Poly Haven, ambientCG, Quaternius, Kenney, KayKit) are all CC0, so little is lost.

**Exception route.** Open an issue. If the lead agrees a specific item is worth it, a new decision adds it and the bookkeeping it needs.

**Affects.** `data/licences.json` (`accepted_asset_input_licences`), enforced by the checks.
