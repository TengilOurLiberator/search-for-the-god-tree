# 0005: Stack changes after verification

- **Date:** 2026-10-09
- **Decided by:** Lead (technical reasons; no cost or licence change). Evidence: THIRD_PARTY_LICENCES.md.

| Change | Reason |
|---|---|
| Target Unity **6000.3 LTS**, not 6000.0 | 6000.0 LTS support ends October 2026; 6000.3 is supported to December 2027 |
| **UltimateXR dropped** as fallback rig | No OpenXR integration; needs vendor SDKs. No replacement chosen; if BIMOS fails, evaluate Unity's XR Interaction Toolkit with in-house physics hands |
| **UnInventory dropped**; inventory in-house | Abandoned since 2020, built for Unity 2019 flat-screen drag-and-drop. A VR inventory is physical (holsters, pouches) and belongs to pillar 1 |
| **VRM 1.0** only | The only MToon shader for URP is MToon10, which requires VRM 1.0 |
| **FishNet, Steamworks.NET, Unity Behavior referenced in the manifest, never vendored** | FishNet's custom licence has no distribution grant; Valve's binaries may ship only with a build; Unity Behavior is under Unity's terms |
| **Enemy AI behind our own interface** | Unity Behavior is in maintenance mode |
| **Active ragdoll implementation open** | BIMOS-Active-Ragdolls has no licence. Decide in milestone 2 once the owner answers whether it is theirs |

**Not yet tested.** BIMOS on 6000.3; FishySteamworks against current FishNet.
