# Screw & Create — M2 First Playable Verification Report

Date: 2026-10-19  
Status: **COMPLETE — STOPPED AT M2**

## Baseline and scope

- Starting commit: `b2e3c59b1a4065e6c9778b977dc216b561e4bd71`
- Branch: `codex/m2-first-playable-disassembly`
- Unity: `6000.6.6f1`
- Playable scene: `Assets/_Game/Scenes/Gameplay_Disassembly_T01.unity`
- Board prefab: `Assets/_Game/Prefabs/Disassembly/T01_Board.prefab`
- No paid asset, downloaded asset, or external-generation credit was used.
- The existing construction Core, Windmill scene and prefab, construction saves, solver, tests and artwork remain preserved.
- Nothing was pushed or merged.

## Playable T01 integration

`DisassemblyT01Controller` creates a `DisassemblySession` from the production `T01FirstReleaseDefinition`. The session is the only gameplay authority. A tap selects `S_1`, queries the session for legal destinations, highlights `H_P1`, and sends the chosen transfer back to the session. The successful logical transfer commits before presentation animation begins. The controller reads the resulting authoritative snapshot and release IDs to animate the screw and `C_PLANK`; it does not contain a second legality, release or completion rule.

Scene-authored arrays map the exact screw, hole and component IDs to dedicated view components. Startup validation reports missing, blank, duplicate and unknown IDs, missing screw anchors and missing release poses. Input uses Unity's Input System, rejects modal interception, extra touches, invalid holes, rapid duplicate commits and input while presentation is busy.

The mechanical sequence rotates and withdraws the screw, follows an authored arc, aligns and settles it into `H_P1`, then swings and drops the released plank. Undo, restart, pause, disable and lifecycle reconciliation stop presentation coroutines and reconstruct every view from the authoritative snapshot. The actual session completion phase controls the completion panel and progress.

The portrait HUD provides level identity, component progress, instruction, Undo, confirmed Restart, Pause and completion actions. Safe-area fitting and camera framing cover 9:16, 9:19.5, 9:21 and 3:4. The reusable visual kit contains editor-generated rounded/beveled wood geometry, a lathed screw, modeled holes, procedural wood texture, URP Lit wood and brass materials, contact shadows, and workshop lighting. No runtime primitive is used as player-facing art.

## Verification evidence

| Suite | Discovered | Executed | Passed | Failed | Skipped |
|---|---:|---:|---:|---:|---:|
| EditMode | 55 | 55 | 55 | 0 | 0 |
| PlayMode | 24 | 24 | 24 | 0 | 0 |

The 55 EditMode tests include the preserved M1 and construction coverage plus M2 mapping validation. The 24 PlayMode tests include all 16 preserved tests and eight M2 tests covering initialization, exact mapping, visible pointer targeting, invalid destination rejection, successful relocation and completion, rapid duplicate taps, interruption reconstruction, exact Undo and Restart, camera framing, safe-area constraints and real rendered evidence capture.

Local result files:

- `TestResults/m2-editmode.xml`
- `TestResults/m2-playmode.xml`
- `TestResults/m2-scene-build.log`
- `TestResults/m2-android-build.log`

These local evidence files are intentionally ignored by source control.

## Rendered screenshots

All images below were captured from the running Unity scene with graphics enabled:

- `Docs/M2Screenshots/initial-9x16.png`
- `Docs/M2Screenshots/initial-9x19_5.png`
- `Docs/M2Screenshots/initial-9x21.png`
- `Docs/M2Screenshots/initial-3x4.png`
- `Docs/M2Screenshots/selected-9x16.png`
- `Docs/M2Screenshots/transferred-9x16.png`
- `Docs/M2Screenshots/released-9x16.png`
- `Docs/M2Screenshots/completion-9x16.png`
- `Docs/M2Screenshots/undo-9x16.png`

## Android development build

- Result: `Succeeded`
- APK: `Builds/M2/ScrewAndCreate-M2-T01.apk`
- File size: `40,686,385` bytes
- SHA-256: `2ABBE5325020E2402A5DDC6986C7F1EDDD9E10537FE7E1A8289261E1795DB092`
- Package: `com.iyadimm.screwandcreate`
- Product: `Screw & Create`
- Version: `0.2.0-m2` (`versionCode` 2)
- Unity: `6000.6.6f1`
- Build date: 2026-10-19
- Tested source: the local M2 commit containing this report; its exact hash is recorded in the completion response because a commit cannot contain its own hash.

`adb devices` reported no attached device.

**ANDROID DEVICE VERIFICATION PENDING.**

Editor PlayMode coverage does not establish physical touchscreen, install, resume, thermal or device-performance behavior.

## Known visual and presentation limits

M2 is a coherent first playable rather than final premium art. It uses one procedural wood treatment, one board composition and simple authored transform animation. Audio was not added; the scene includes an `AudioHooks` root for later integration. Haptics are deferred. The controller exposes reduced-motion behavior, but no player settings screen is part of M2. Final art polish, broader material variation, sound, haptics, device-specific tuning and production animation belong in later reviewed milestones.

## Minimal next milestone

M3 should begin only after review. Its minimum useful scope is physical-device install and lifecycle verification of T01, targeted correction of any touchscreen or framing defects found there, and a focused audiovisual polish pass using the existing authoritative integration. It should not add campaign levels until T01 device behavior is accepted.
