# Research: hand tracking over Link, and a rigged player body

Date: 2026-10-10. Done by a research agent for the lead; nothing here has been built or tested yet. Sources are linked; items marked "not verified" could not be confirmed from a primary source (Meta's community forum refused automated reading).

## A. Controller-free hand tracking on PC over Link

### What is true

- **Link does provide hand tracking, but only as a developer feature.** Meta documents it as supported "only … in the Unity Editor". It is switched on in the Meta Horizon Link PC app under Settings > Developer > Developer Runtime Features, and needs a Meta developer account. Meta also states that released store apps do not get developer features. <https://developers.meta.com/horizon/documentation/unity/unity-handtracking-overview/>, <https://developers.meta.com/horizon/documentation/unity/unity-link/>
- **A built game may get no hands at all.** Not verified either way; one forum report says a build did not. This must be tested before any real investment.
- **Unity side:** the package is `com.unity.xr.hands` (1.9.0, 2026-08-06), for Unity 6000.0 and later, under the Unity Package Distribution License (a Unity licence, not an open-source one; referenced, not vendored, like our other Unity packages). Enable "Hand Tracking Subsystem" in the OpenXR settings. No Meta SDK is needed. <https://docs.unity3d.com/Packages/com.unity.xr.hands@1.9/manual/index.html>
- **Grip value:** the package offers ready-made grasp and pinch values, but they depend on an extension that users report is missing over Link. The safe route is to compute "how closed is the hand" from the finger joints ourselves.
- **BIMOS reads only controller buttons**, so an adapter is needed that feeds wrist pose and a computed grip value into its input.

### Limits that matter for a sword game

- One hand hiding the other breaks tracking (Meta's own design guidance), which hits two-handed holds directly. <https://developers.meta.com/horizon/design/hands/>
- Fast swings and hands outside the cameras' view lose tracking. Meta's fixes for both (Fast Motion Mode, Wide Motion Mode) and for using hands and controllers together (Multimodal) live in Meta's own SDK, which decision 0005 keeps out.
- No buttons, no thumbsticks, no vibration. Moving around needs a replacement (teleport by pinch, arm swing, or similar).

### Recommendation

Treat hand tracking as an **optional, experimental input mode** next to controllers, best suited to spell gestures and picking things up; keep controllers as the way to fight with weapons. Order of work:

1. Owner switches on Developer Runtime Features in the Link app (needs a free Meta developer account). This is the owner's setting to change.
2. Editor prototype: XR Hands, an adapter into BIMOS, grab by closing the hand. One to two days.
3. Test a built .exe on an account without developer mode. If hands do not appear there, the feature can only ever be for our own testing, and work should stop at step 2.
4. Only then: movement without thumbsticks and handling of lost tracking. About a week.

## B. A real, rigged player body

### What is true

- **BIMOS 1.0.0 (what we use) has full-body IK but no custom avatars**; its README says so. The unreleased 2.0.0 branch adds them through a small editor component (`PlayerModelChanger`) that swaps the model under the character and requires a Unity Humanoid avatar. <https://github.com/KadenZombie8/BIMOS>
- **Upgrading BIMOS to 2.0.0 is not advised now:** unreleased, undocumented, moved to a newer Unity version than ours, and it depends on Meta's OpenXR package.
- **glTFast does not make Humanoid avatars** from GLB files (open request since 2022). A rigged character must either come in as FBX with the Humanoid setting, or have its avatar built by script with `AvatarBuilder.BuildHumanAvatar`. <https://github.com/atteneder/glTFast/issues/391>
- **UniRig** (MIT, approved in decision 0006) predicts a skeleton per model with no fixed humanoid naming, is not supported on Windows, and is fragile on RTX 50-series cards (no matching builds of one dependency; a community fix is unmerged). <https://github.com/VAST-AI-Research/UniRig>
- **Mesh2Motion** (MIT code, CC0 art, approved) runs locally in a browser and fits a chosen skeleton by hand; it has no unattended mode.

### Recommendation

1. Rig **one** character (the starter Woken) in Blender against a fixed humanoid skeleton, scripted so it can be repeated: place the template skeleton, automatic weights, export FBX.
2. Import as Humanoid and swap it into the BIMOS rig the way 2.0.0's `PlayerModelChanger` does, written in-house for 1.0.0.
3. Biggest risk: BIMOS 1.0.0's IK and hand poses are bound to its own skeleton, so the swap may need the constraints re-pointed and hand poses redone.
4. Size: one to two days for the rigged character, three to five including the swap. Skip UniRig unless many characters are needed.

A generated character is one fused mesh in an A-pose with gear modelled on; expect rough deformation at shoulders and hips. That is acceptable for a body seen mostly from the inside and in a mirror.
