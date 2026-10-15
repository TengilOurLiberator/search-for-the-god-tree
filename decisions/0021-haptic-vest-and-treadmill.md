# 0021: The bHaptics TactSuit Pro and the KAT Walk C2+

- **Date:** 2026-10-16
- **Decided by:** Owner (the devices); Lead (how they are supported)

**Decision.** The owner will play with a bHaptics TactSuit Pro (haptic vest, 32 motors) and a KAT Walk C2+ (omnidirectional treadmill). The game supports both, and plays exactly the same without them.

**How (from research on 2026-10-16; vendor docs read through a summarising tool):**

1. **Haptics are ours; vendors are adapters.** The game raises its own haptic events (struck from a direction, block, parry, weapon impact, bow draw and release, spell charge and cast, heartbeat at low health) through an interface in our code. Controllers rumble through Unity's XR input. The vest is one more output.
2. **The bHaptics SDK stays out of the repository.** Its agreement is proprietary, forbids redistribution and open-source entanglement, and needs a bHaptics developer account with an App ID and API key. No open alternative exists; the old local WebSocket protocol is under the same agreement. So: an adapter in our code finds the SDK at run time by reflection, never compiling against it, and does nothing when it is absent. The owner imports the SDK (Asset Store) into their own copy, in a folder ignored by git, creates their own developer-portal app, and keeps the App ID and key in a git-ignored settings file. Zero-integration fallback: the bHaptics Player's Audio-to-Haptic mode.
3. **The treadmill uses KAT Gateway's thumbstick emulation**, through SteamVR with KAT's plugin; no KAT code in the repository. The game adds a **treadmill mode**: movement direction follows the head, stick turning off by default, stick magnitude maps straight to speed with an adjustable multiplier and no extra dead zone or smoothing. KAT's native SDK is gated behind a support request and is not used.
4. **Testing.** The robot checks the logic (a hit from the left drives the left-side motors; treadmill-style stick input moves the body the right way). Only the owner can confirm how they feel, and whether Gateway works with the owner's OpenXR runtime: the documented path is SteamVR.

**Affects.** A haptics module under `Scripts/`, `.gitignore`, settings, docs/lead-handoff.md, a "Hardware" section for players.
