# 0011: A non-commercial background remover was used by mistake; models rebuilt

- **Date:** 2026-10-10
- **Decided by:** Lead (correction of the lead's own error). One item below is the owner's.

**What happened.** The first 21 placeholder models (commit "Asset pipeline: 50 golden concepts, 21 placeholder models...") were made by TripoSR's own `run.py`. That script removes image backgrounds with the `rembg` library. The installed version (2.0.85) defaults to the **BRIA RMBG** model, which is licensed for non-commercial use only. It was downloaded and used without anyone choosing it. The lead found it on 2026-10-10 while listing downloaded model files.

**Why it matters.** Every tool in a recipe must be on the approved list, and BRIA RMBG could never be. The licence file had already recorded TripoSR's background remover as "not read"; the lead published before reading it. That was the mistake.

**What was done.**
1. The BRIA model file was deleted from the machine.
2. Background removal is now a separate, recorded step using **BiRefNet** (MIT), run in ComfyUI (`stylekit/workflows/birefnet_cutout.api.json`).
3. TripoSR now runs only through `tools/pipeline/triposr_run.py`, which replaces `rembg` with a stub that raises an error and refuses images without transparency.
4. All 21 models were regenerated through the clean path and the published files and recipes replaced. Recipes now list four steps: image, cut-out, mesh, finishing.
5. BiRefNet was added to the approved list under the test in decision 0006 (local, free, MIT, no territorial limit). **Owner: say if you object.**

**Still open (owner's decision).** The earlier versions of the 21 `.glb` files remain in git history and in LFS storage. They were shaped by a mask from the non-commercial model. Removing them means rewriting history, which only the owner may order. Lead's recommendation: leave history alone; the files are superseded, were public for a few hours, and are placeholders.

**Rule added.** Before a tool is used for anything that will be committed, list what it downloads on first run and read the licence of each file. "Licence of a sub-model not read" in the licence file means the tool is not ready to use.
