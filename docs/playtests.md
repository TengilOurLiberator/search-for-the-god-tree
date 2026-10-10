# Playtest log

What each recorded session in the sandbox showed, and what was changed because of it. Recordings live outside the repository in `D:\godtree-work\recordings\<date-time>\` (`samples.csv`, `events.csv`, `summary.json`, `frames/`).

"Twist" is the angle between where the controller wants the hand and where the physics hand is. Empty hands sit at 1 to 2 degrees.

## 2026-10-10, owner, Meta headset over Link

### Before recording

- **Could not pick anything up.** Cause: the sword's grab line was inside its grip collider, which the rig ranks as ungrabbable; the other weapons were display models with no physics; nothing said that grabbing is the grip button. Fixed, instruction board added, regression test added.

### Session 1 (5.5 minutes)

| Held | Twist, typical | Spin, typical | Spin, worst |
|---|---|---|---|
| nothing | 1 to 2 deg | | |
| dagger, vial, lantern | 1 to 6 deg | | |
| sword | 110 deg | 380 to 460 deg/s | 4,650 deg/s |
| spear, bow, axe | 80 to 150 deg | | |

- Owner: weapons "don't really follow your hand movements, they kind of go crazy".
- Cause: long held objects collided with the holder's own body, and the sword's fixed grab pose fought the hand.
- Change: held objects ignore the holder's body; sword switched to grab-anywhere; weapons lighter.

### Session 2 (77 seconds)

- Sword twist down to 24 to 44 degrees. Spear still 105 degrees when held in both hands. One hand was flung metres away after a release.
- Causes: two rigid grips on one object fight each other; collisions were switched back on while the object still overlapped the hand.
- Change: the second hand's grip holds position but not angle; collisions return only once the object is clear.

### Session 3 (7.5 minutes; 33 arrows, 52 mote bolts, 12 push knots, 197 hits)

| Held | Twist, typical |
|---|---|
| bow | 2 to 5 deg |
| axe | 3 to 8 deg |
| dagger | 12 deg |
| spear | 19 to 22 deg |
| sword | 18 to 28 deg |

- Bow draws were consistent (0.42 to 0.58 m, 25 to 35 m/s).
- Owner: "having to pick it up the right way makes the items difficult to handle"; wants point-and-pull with a snapped hold.
- Change: `DistanceGrab`, `GripPose`, `SnappedGrab`; hand turn drive five times stiffer while holding; sword and spear lighter.

### Not yet answered by the owner

How the sword feels now (lag, wrong angle or wobble); whether bow aiming is natural; whether the two spells are fun and the right strength.

## Checks made without a headset (2026-10-10, afternoon)

The owner was away, so these were done by rendering:

- **Grip check** (`docs/images/grip_check.png`): each weapon from above with its palm point and pointing direction drawn on. It showed the dagger, greatsword and rapier would be held by the blade (their concept art was drawn point-down) and the buckler was treated as a blade. Holds are now taken from each model's own long axis and where it is widest. All twenty look right in the render; none has been held yet.
- **Menu preview** (`docs/images/menu_preview.png`): the mirror showed the menu itself and only the player's legs. Fixed; the board is now opaque.
- **Shader**: the first version drew everything black. Cause: it borrowed passes from Unity's standard shader, whose material layout differs. It now has its own passes.

## Changes made without a headset (2026-10-10, evening)

None of these has been seen or felt in a headset yet.

- **Training dummy.** The plain cylinders are replaced by the generated sack-and-helmet model, drawn as three pieces (stand, sack, head) that each rock on their own spring. Its springs are stiffer and better damped than before, because with the old values the taller sack swayed for six seconds after one blow. To judge in the headset: does it feel solid, and does it settle quickly enough.
- **Ground.** The repeating pattern in the grass is gone, a dirt road runs from the spawn through the arch, and hills ring the meadow.
- **Flickering grey patches on the floor** around the plinth were the base scene's own floor showing through the meadow. It is hidden now.
- **Layout.** Scenery is arranged as a road, an arch with walls and banners, a camp on the right and a house, well and cart on the left.
