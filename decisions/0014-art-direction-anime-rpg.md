# 0014: Art direction is the painterly anime action-RPG look

- **Date:** 2026-10-10
- **Decided by:** Owner

**Decision.** The game's look changes from flat storybook cel shading to a **painterly anime action-RPG look**: stylised 3D with painterly textures, soft cel shading with warm bounce light and cool rim light, realistic proportions with expressive anime faces, detailed ornate equipment (engraved metal, layered cloth and leather), lush bright colour with luminous highlights, atmospheric depth.

The owner asked for "realistic anime vibes", named the polished look of current anime action RPGs as the reference, and chose variant **F** from a six-way style test (`stylekit/jobs/style-test-realistic-anime.json`; sheets in `docs/review/session-1/style-test-*.jpg`).

**What this replaces.** Rules 2 and 6 of the first art bible (flat two-tone colour; outlines on characters only) and prompt fragments version 0. Readable silhouettes, coloured shadows, colour carrying meaning and the palette all stay.

**What does not change.** The world is original. No existing game, studio or artist is named in any prompt, and no design is copied; the look is described in words. The forbidden-names check still applies.

**Consequences.**
- `stylekit/prompt-fragments.json` is now version 1; version 0's style line is kept in the file for the record.
- Everything generated in the old style (50 golden concepts, about 30 models) is superseded. It stays in the repository as placeholders until its replacement passes review, then it is removed.
- This also settles the earlier "style distance" worry: the new look is further from any single hand-drawn film studio's style than the old one was.
- Pillar 4 ("Bright world, real danger") is unchanged; its test of reading an enemy from silhouette and colour still applies to more detailed designs.
