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
- **Player body.** The grey mannequin is replaced by a generated character bound to the rig's skeleton; the character menu's mirror shows it, head included (`docs/images/menu_preview.png`). To judge in the headset: do the hands sit where your hands are, do elbows and shoulders look acceptable when you swing, is anything of your own body in the way when you look down.
- **Smaller things.** Sparks where hits land; mote bolts glow and leave a trail; clouds; trees, bushes and banners move in the wind; a lit campfire with sound; the mirror is no longer blocked by whatever stands in front of you.

### Session 4 (2026-10-10, evening, about 12 minutes)

- Owner: "This is okay." Asked for: staffs and wands that cast; arrows "get stuck when shooting"; animated NPCs and monsters; a conversation with every NPC; and a way for the lead to playtest alone.
- Four arrows were loosed at 26 to 31 m/s. Cause of the sticking: each arrow began inside the bow's own shape, struck it and stuck. Fixed.

### Session 5 (2026-10-11, morning, about 7 minutes)

The first time the owner saw the night's work: the town, the animated people and creatures, staffs, dismemberment.

- Owner: "It seems to work pretty okay"; "they look pretty good".
- **The bow "still doesn't do any damage".** The record agrees: 17 arrows at 25 to 34 m/s, and not one counted against a creature. Two causes: a creature judged every blow by speed times weight, and an arrow weighs 80 grams, so every arrow fell under the threshold; and an arrow that lands freezes itself in place first, after which the creature ignored it as a thing that does not move. Arrows are now judged by speed alone (about one health per metre per second; the robot's arrow at 32 m/s took 30). A robot routine now checks this; none did before, which is why the night's testing missed it.
- **Spells from a staff went "up", not where the controller faces.** They flew along the staff, which leans in the hand. Owner's rule: magic, arrows "and stuff like that" go where the controller points, like the pointer arrows in the headset's own menus. Now: spells leave the staff's tip toward what the controller's pointing line meets; arrows fly where the bow hand's controller points (the pull sets only the speed); a mote bolt always flies where the controller points.
- Not yet clear: whether thrown objects should also be steered toward where the controller points. Asked.
- The creatures died to weapons: a Barkbound and the brute, one limb severed. 43 spells and 42 mote bolts were cast.

## Robot playtests (from 2026-10-10, night)

The robot player stands in for a person: it drives the rig's head, hands and buttons through the real input path, walks with the stick, and saves pictures. It shows whether a thing works. It says nothing about comfort or fun. Routines are in `unity/Assets/Godtree/Tests/PlayMode/RobotPlaytests.cs`; pictures in `D:\godtree-work\robot\`.

| Routine | What it showed |
|---|---|
| Buttons reach the rig | Grip and trigger arrive; the physics hand follows the controller within 2 mm |
| Sword and dummy | Picks the sword up, walks to the dummy, lands hits of about 3 N s (weak; its swing is short) |
| Bow | Reaching onto the crowded table took the halberd by mistake and knocked the bow off, so it uses point-and-pull. Arrows: 5 of 5 stuck in the bow before the fix, 5 of 5 in the dummy after. The line from palm to palm is 9 degrees off the line between the controllers; a person corrects by eye and so does the robot |
| Spells | All eight casts gave the right spell and reached the dummy. A fireball moves 13 loose things; a thunderclap 25 |
| Townsfolk | Idle, turn to face the robot, greet, then listen |
| Fight | The Barkbound came from 6 m, struck 5 times (player health 100 to 35), and died to the twelfth sword stroke, losing its head. Faults found on the way: it struck through a wall; the gate was too narrow to walk through |
| Talk | The innkeeper answers in about half a second and is heard in about two. Asked what is in the Godtree: "No one knows what lies within the Godtree... I cannot tell you what the god is like." Asked for an email: "I have never seen a computer or an email, little one." A generated spoken question was understood and answered: "My name is Tobin, and you should try my stew before you go anywhere." |
