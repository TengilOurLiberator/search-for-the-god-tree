# Recipes

Every asset ships with the recipe that made it. A recipe is provenance: it lets anyone see how an asset was made and make something like it for free. It is not a promise of identical output; the same seed gives different results on different hardware.

## Where

```
recipes/<asset-id>/
  recipe.json        required
  workflow.json      the ComfyUI (or other) workflow, if the tool has one
  preview.png        optional: a render of the result
  notes.md           optional
```

`<asset-id>` follows the naming rule: `<category>_<name>`, for example `wpn_sword_training`. Only `.json`, `.md`, `.png` and `.jpg` files are allowed in `recipes/`.

## recipe.json

```json
{
  "id": "wpn_sword_training",
  "title": "Training sword",
  "origin": "generated",
  "licence": "CC0-1.0",
  "budget_class": "weapon",
  "steps": [
    {
      "generator": "z-image-turbo",
      "tool": "comfyui",
      "version": "model 1.0, ComfyUI 0.3.x",
      "prompt": "a short steel training sword, cel shaded, flat colours",
      "negative_prompt": "",
      "seed": 1234,
      "vram_gb": 16,
      "workflow": "workflow.json"
    }
  ],
  "cleanup": [
    "Decimated to 4,200 triangles in Blender 4.5",
    "Re-centred the pivot at the grip"
  ],
  "sources": [],
  "outputs": [
    { "path": "unity/Assets/Godtree/Art/Weapons/wpn_sword_training.glb" },
    { "path": "unity/Assets/Godtree/Art/Weapons/wpn_sword_training_albedo.png" }
  ],
  "notes": ""
}
```

| Field | Rule |
|---|---|
| `id` | Equals the folder name |
| `origin` | `generated`, `sourced` (ready-made), `handmade`, or `mixed` |
| `licence` | Always `CC0-1.0` |
| `budget_class` | Required when any output is a model; one of the classes in `data/budgets.json` |
| `steps` | Required for `generated` and `mixed`. One entry per generation step, in order |
| `steps[].generator` | An id with status `approved` in `data/approved-generators.json` |
| `steps[].tool` | Optional; the host tool (for example `comfyui`), also from the approved list |
| `steps[].version` | Tool and model versions as exactly as you can state them |
| `steps[].prompt` | The full prompt. Write `n/a` for tools that take none |
| `steps[].seed` | The seed. For tools that take none (mesh tools, Blender), write `0` and say so in `notes` |
| `steps[].vram_gb` | Video memory the step needed; `0` for CPU-only tools |
| `steps[].workflow` | A file in the recipe folder; required whenever the tool can export one |
| `cleanup` | Every manual step, in order. An empty list means there were none |
| `sources` | Required for `sourced` and `mixed`: `name`, `url`, `author`, `licence` for each. Only CC0 is accepted |
| `outputs` | Every asset file this recipe produced. `.fbx` outputs add `"triangles": <count>` |

## Rules

- Generators run locally. No cloud services, no paid tools.
- Prompts never name an existing work, character or living artist.
- Every asset file is listed by exactly one recipe. Unity `.meta` files need no entry.
