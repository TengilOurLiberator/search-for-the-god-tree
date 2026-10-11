# For artists and animators

You make things by hand: models, rigs, animations, textures, concept art, sound, music. This page is how to give them to the game. You do not need to use AI, write code, or know Unity.

Everything in the game is released as **CC0 1.0 (public domain)**. By contributing you give your work away on those terms, for anyone to use for anything. You will be credited by name in [CREDITS.md](../CREDITS.md) unless you ask not to be.

## What is wanted most

Things the AI pipeline cannot do, or does badly. A person who can do any of these is worth more to the game than another thousand generated props.

| Wanted | Why | Notes |
|---|---|---|
| **Four-legged creature rigs and animation** | The Burrling, the Thornback and the root crawler are statues. No free tool generates this | Models are in `unity/Assets/Godtree/Art/Enemies/`. Idle, walk, attack, hit, death |
| **Hands with fingers that bend** | Every rigged person has mitten hands | The human skeleton is inside `unity/Assets/Godtree/Art/Animations/anm_human_basic.glb` |
| **Combat animation for people and man-shaped creatures** | The set in use is general-purpose; attacks must change the silhouette before they land | Same skeleton. See rule 1 of the [art bible](../canon/art-bible.md) |
| **Faces: mouths that move, expressions** | Townsfolk talk with still faces | Blend shapes on the existing character models |
| **Hollow and thin things** | Helms, hoods, a chain shirt, a robe, coats and hems all shatter in 3D generation | Budget class `prop_small` or `character_npc` |
| **Glass, glow and flame shapes** | The lantern and the mote vial are placeholders | Model the solid parts; glow is added in the engine |
| **Clean-up of generated models** | Ragged hems, rough small props, a hunched Barkbound | Take any model in `Art/` and make it better |
| **Concept art that moves the look away from its influences** | The characters sit close to a famous hand-drawn film look | See [canon/art-bible.md](../canon/art-bible.md) and `stylekit/golden/` |
| **Music and sound** | Nobody has yet listened to the generated set critically | `unity/Assets/Godtree/Audio/` |

Open tasks with a full brief are [issues labelled `task`](https://github.com/TengilOurLiberator/search-for-the-god-tree/issues?q=is%3Aissue+is%3Aopen+label%3Atask). Something not on the list is welcome too: offer it.

## Before you start

- Read [canon/art-bible.md](../canon/art-bible.md) (the look) and [canon/pillars.md](../canon/pillars.md) (what the game is). Look at the pictures in `stylekit/golden/` and `docs/images/`.
- **The world is original.** Nothing from an existing game, film, comic or show: no characters, designs, logos or names.
- **It must be yours to give.** Your own work, or built only from CC0 material that you name. No kit-bashed store assets, no ripped models, no motion capture you do not own, no Mixamo clips (their terms do not allow redistribution).
- **Any tool is fine.** Blender, Maya, ZBrush, Substance, Krita, a pencil: use what you are licensed to use. Only the files you deliver have to be in open formats.
- **If AI helped**, say so. Then the generator rules apply to that part: it must be a free local tool on the [approved list](../data/approved-generators.json), with its prompt and settings in the recipe. See [recipes.md](recipes.md).

## What to deliver

| Kind | Format | Limits |
|---|---|---|
| Models | `.glb` (preferred; it can be measured automatically) or `.fbx` | Triangle and texture limits by budget class, below |
| Animation | `.glb` holding the skeleton and the clips, with or without a mesh | Clips that repeat must start and end on the same pose |
| Textures, concept art | `.png` | Sides are powers of two, 2048 at most (concept art up to 4096, any shape) |
| Sound | `.ogg`, 48 kHz | 1 MB per sound effect or voice line, 10 MB per music track |

- **Scale and placement:** real size in metres, standing on the ground at the origin. Weapons have their origin at the grip.
- **No file over 50 MB**, and no `.blend`, `.ma`, `.ztl` or other working files: they can carry scripts and they are large. Keep your source files; link to them if you want others to have them.
- **Names:** `<category>_<name>.<ext>`, lowercase with underscores, for example `enm_burrling_rig.glb` or `anm_burrling_basic.glb`. Categories: `chr` characters, `wpn` weapons, `enm` enemies, `env` environment, `prp` props, `anm` animation, `vfx` effects, `ui` interface, `con` concept art, `sfx`, `mus`, `vox`.

| Budget class | Triangles (max) | Texture side (max) |
|---|---|---|
| `character_player` | 40,000 | 2048 |
| `character_npc` | 25,000 | 2048 |
| `enemy_common` | 20,000 | 2048 |
| `enemy_boss` | 60,000 | 2048 |
| `weapon` | 5,000 | 1024 |
| `env_modular` | 8,000 | 2048 |
| `prop_small` | 2,000 | 1024 |
| `prop_hero` | 10,000 | 2048 |

The enforced numbers are in [data/budgets.json](../data/budgets.json).

## Two ways to send it

### The easy way: offer it

1. Open an issue with the **[Offer art or animation](https://github.com/TengilOurLiberator/search-for-the-god-tree/issues/new?template=art-offer.yml)** form.
2. Attach pictures or a short video of the work, and the files as a `.zip` (GitHub takes up to 25 MB per attachment), or give a link to where they can be downloaded.
3. The lead looks at the preview, answers on the issue, and if the work fits brings the files into the repository with a recipe and your credit. Changes are asked for on the issue.

Nothing you attach is opened automatically. Files are brought in by hand, in a working session, after the preview has been looked at.

### The direct way: a pull request

For people at home with Git. You need [Git LFS](https://git-lfs.com) (`git lfs install` once).

1. Open an offer issue as above, or comment on a `task` issue to claim it. The pull request must name that issue.
2. Fork, branch, and add your files under `unity/Assets/Godtree/Art/<Characters|Enemies|Weapons|Props|Environment|Animations>/` or `.../Audio/<Sfx|Music>/`.
3. Add a recipe folder `recipes/<asset-id>/` with a `recipe.json` (below) and a `preview.png`.
4. Open the pull request with the template. Task type is `asset`. Under **Sponsor** put your own name and GitHub login: a person is their own sponsor.
5. Automated checks measure sizes and names. Then the lead reviews, usually within the hour while the owner's PC is on. The owner merges.

A recipe for hand-made work is short. It says what the thing is, who made it and how:

```json
{
  "id": "enm_burrling_rig",
  "title": "Burrling, rigged",
  "origin": "handmade",
  "licence": "CC0-1.0",
  "budget_class": "enemy_common",
  "steps": [],
  "cleanup": [
    "Retopologised the generated Burrling by hand to 6,400 triangles",
    "Rigged with 28 bones; weights painted by hand"
  ],
  "sources": [],
  "outputs": [
    { "path": "unity/Assets/Godtree/Art/Enemies/enm_burrling_rig.glb" }
  ],
  "notes": "Made by Your Name (your-github-login) in Blender 4.5. Credit as: Your Name."
}
```

- `origin` is `handmade` for your own work, or `mixed` if you started from CC0 material or a generated model. For `mixed`, list each starting point under `sources` with its name, link, author and licence. Models already in this repository count as CC0 sources.
- An animation-only file uses the budget class `prop_small`.
- Full field list: [recipes.md](recipes.md).

## How it is judged

- **Fit with the look**, against the art bible and the golden pictures.
- **Readable silhouette** at ten metres, and for attacks, a pose that changes before the blow lands.
- **Within budget**, clean and tidy: one object per file, sensible origin, no stray parts.
- **Honest provenance.** What you made, what you started from, what a tool did.

The reviewer is an AI working for the owner. It can look at your preview pictures but cannot open a 3D model, watch an animation or hear a sound, so it says so and a person checks those before anything is merged. How the review is protected against manipulation is in [review.md](review.md).

Feedback is specific and says what is good as well as what to change. A request for changes is not a no.

## Questions

Ask on your issue, or start a thread in [Discussions](https://github.com/TengilOurLiberator/search-for-the-god-tree/discussions). If something on this page is unclear or wrong, saying so is a contribution.
