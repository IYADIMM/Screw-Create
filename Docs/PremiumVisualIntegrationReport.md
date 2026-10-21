# Screw & Create — Premium Visual Integration Report

Date: 2026-10-21  
Branch: `codex/premium-asset-integration`  
Starting commit: `9e0b8836b68ac111c188955e8d0c1edc6a7252c2`  
Unity: `6000.6.6f1` / URP `17.6.0`

## Scope and protection

The premium pass is implemented as a separate playable scene:

- `Assets/_Game/Scenes/Gameplay_Disassembly_T01_Premium.unity`
- `Assets/_Game/Prefabs/Disassembly/Premium/T01_Premium_Board.prefab`
- `Assets/_Game/Prefabs/Disassembly/Premium/T01_Premium_Workshop.prefab`

The original `Gameplay_Disassembly_T01.unity`, Windmill scene, Windmill prefab, construction Core, disassembly domain, T01 definition, saves, rules, and solver were not changed. The existing logical IDs remain `S_1`, `H_C1`, `H_P1`, and `C_PLANK`. Existing colliders and authoritative session behavior remain the source of selection, legality, transfer, release, completion, Undo, and Restart.

## Visual and audio result

The premium scene replaces the saturated orange/red presentation with an oak work surface, walnut backing, honey-birch detachable plank, a modeled brass Phillips screw, and a modeled brass-rim destination hole. URP Lit materials use imported base color and normal maps plus generated smoothness data derived from the supplied roughness maps. The empty hole includes a dark recessed depth surface, while the legal destination uses a restrained teal selection ring.

Lighting now uses a modest warm directional key with soft shadows, a low-intensity cool fill, a warm bench bounce, and neutral ambient light. The workshop background is a calm solid finish so the playable mechanism remains the focus. The camera fitter gives the mechanism a larger useful viewport while keeping all renderers visible at 9:16, 9:19.5, 9:21, and 3:4.

The HUD uses compact neutral panels, muted brass outlines, imported Undo/Restart/Pause/Check icons, smaller controls, and a reduced visual footprint. Five subtle sound cues are synchronized with successful authoritative events: valid selection, unscrewing, insertion, component release, and completion.

## Verification

| Suite | Total | Passed | Failed | Skipped |
|---|---:|---:|---:|---:|
| EditMode | 55 | 55 | 0 | 0 |
| PlayMode | 28 | 28 | 0 | 0 |

The expanded PlayMode suite verifies premium asset binding, exact logical mappings, successful relocation and completion, Undo, Restart, audio references, safe-area bounds, camera framing, and real rendered output across all four required aspect ratios. The original M2 interaction and regression tests remain in the same full run.

Local test evidence:

- `TestResults/premium-editmode.xml`
- `TestResults/premium-playmode.xml`
- `TestResults/premium-scene-build.log`
- `TestResults/premium-android-build.log`

These generated test outputs are intentionally ignored by Git.

## Screenshots

Before:

- `Docs/M2Screenshots/initial-9x16.png`

After:

- `Docs/PremiumIntegrationScreenshots/after-initial-9x16.png`
- `Docs/PremiumIntegrationScreenshots/after-initial-9x19_5.png`
- `Docs/PremiumIntegrationScreenshots/after-initial-9x21.png`
- `Docs/PremiumIntegrationScreenshots/after-initial-3x4.png`
- `Docs/PremiumIntegrationScreenshots/after-selected-9x16.png`
- `Docs/PremiumIntegrationScreenshots/after-completion-9x16.png`
- `Docs/PremiumIntegrationScreenshots/after-undo-9x16.png`
- `Docs/PremiumIntegrationScreenshots/before-after-9x16.png`

All after images were rendered by the Unity PlayMode test from the premium scene. The comparison image only places the existing genuine M2 capture beside the genuine premium capture and adds labels.

## Android development build

- APK: `Builds/Premium/ScrewAndCreate-Premium-T01.apk`
- Size: `51,548,819` bytes
- SHA-256: `489AD57A574CC9F8666AF639F8A1E36FCD3FF5BE865B7F86C021553BD55271AA`
- Package: `com.iyadimm.screwandcreate`
- Version: `0.3.0-premium` (`versionCode` 3)
- Unity build result: `Succeeded`
- ADB result: no attached devices

**PHYSICAL ANDROID DEVICE VERIFICATION PENDING.** The APK was built successfully but was not installed or tested on a real device.

## Remaining visual limitations

- The included procedural grain repeats more visibly than a studio-grade scanned material.
- The included screw recess is a surface insert rather than a true machined cavity.
- Audio balance has been verified by file analysis and event wiring, but still needs listening on a physical phone speaker.
- Haptics, a sound toggle, broader tutorial redesign, advanced physics, new mechanics, boosters, advertisements, and extra menus remain outside this pass.

No paid generation credits were used. No external or competing-game asset was copied. The work stops at this local premium-integration commit for review.
