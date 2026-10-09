# 0004: Repository layout and recipes

- **Date:** 2026-10-09
- **Decided by:** Lead

**Decision.**
- The Unity project lives in `unity/`, so the repository root stays readable.
- Our content sits under `unity/Assets/Godtree/` (`Art`, `Audio`, `Scripts`, `Tests`, `Prefabs`, `Scenes`).
- Each asset's recipe is `recipes/<asset-id>/recipe.json`, with its workflow files beside it, outside the Unity project. A recipe lists the files it produced; every asset file must be listed by exactly one recipe.
- Models are submitted as `.glb` (or `.vrm`) where possible, because the checks can count their triangles. `.fbx` must declare its count. The final interchange format is settled in milestone 3.
- `.blend` files, executables and scripts are not accepted from contributors.

**Reason.** Recipes outside the Unity project keep workflow files from being imported by the engine and let path rules separate "how it was made" from "what ships". Blender files can run embedded scripts when opened.
