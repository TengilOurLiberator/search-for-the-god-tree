# Audio bible

Status: Approved by the owner, 2026-10-09 (decisions/0008).

## Principles

1. **Sound is combat feedback.** In physics melee the ear confirms what the hands did. Every impact tells the player what hit what, and how hard. This comes before music and atmosphere.
2. **Warm and acoustic.** The world sounds hand-made: wood, string, breath, skin drums. Synthetic sound is reserved for motes and weaving, so magic always sounds like nothing else.
3. **Quiet has value.** The wilderness has room to breathe. Dungeons are sparse and close. Music steps back so that a boss theme arriving means something.

## Music

- Palette: strings, woodwinds, plucked instruments, hand percussion, wordless voices. No electric guitar, no dubstep, no modern drum kit.
- **The Godtree motif:** one short rising phrase that belongs to the tree. It is hinted at in every zone and heard in full only when the tree fills the sky. The motif itself is not written yet; it will be a golden reference in the style kit.
- Each zone has its own theme and one lead instrument. Towns are melodic and settled; wilderness is light and open; dungeons are mostly texture; bosses are the only place the full ensemble plays.
- Music is written in loops with layers that can be added or removed by the game, never as one fixed track.

## Sound effects

- Impacts are built from a material matrix: what struck (blade, blunt, shaft, hand, shield) against what was struck (wood, stone, metal, flesh, bark, husk), at three strengths. A brief for impact sounds always names its cell in the matrix.
- Enemy attacks have a distinct warning sound before they land (pillar 4).
- Weaving sounds are bright, glassy and tonal, and tuned to the zone's music where practical.
- No sound may be painful at full strength in a headset. Avoid sudden extreme peaks.

## Voice

- No full voice acting. Dialogue is text.
- The Rooted speak in short wordless vocal sounds (a hum, a laugh, a "hm?") to give them warmth. Generated voices are used for these only.
- No voice may imitate a real person.

## Technical

- Shipped audio is Ogg Vorbis at 48 kHz. Size limits by type are in [data/budgets.json](../data/budgets.json).
- Positional sounds are mono; music and ambience beds are stereo.
- Loudness targets are to be set when the first sounds are in the engine.
