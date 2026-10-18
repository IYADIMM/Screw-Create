# Phase 1 Functional Stabilization Verification Report

Date: 2026-10-18 (Asia/Dubai)  
Unity: 6000.6.6f1  
Repository: `IYADIMM/Screw-Create`

## A. Executive outcome

**PARTIALLY COMPLETE.** All feasible local Phase 1 source protection, confirmed functional repairs, automated tests, Editor PlayMode screenshots, static project audits, and Android APK compilation passed. A physical Android device was not connected, so installation, real touchscreen input, pause/background lifecycle, safe-area/cutout behavior, and device performance remain **BLOCKED** rather than inferred from Editor evidence. No Phase 2 visual reconstruction was started.

## B. Source protection

- Original commit: `b42222d58bfd2989373bcd67285c166c28a9056b`.
- Preserved Phase 2 baseline: `79e79e3cb7562dcb49d8fb92488a6e978cddfcb1` (`Baseline Phase 2 playable windmill slice`).
- Repair branch: `codex/phase3-functional-stabilization`.
- Implementation commit: `44061db` (`Fix Phase 1 presentation and input defects`).
- Verified external backup: `C:/Users/Lenovo/OneDrive/Downloads/ScrewAndCreate_Phase2_PreRepair_Backup_2026-10-18.zip`.
- Backup SHA-256: `8B319F587550F46B19077D0CAFEC2D89145527373881378E98F0A3533BEC5A07`; 239 entries checked.
- Preserved and excluded: Unity caches/logs/test results/build outputs, local archives, generated Purchasing residue under `Assets/Resources`, and Unity's untracked `Assets/InitTestScene*.unity` test-runner artifact.
- Remote state remained untouched. Nothing was pushed.

## C. Defect register

| ID | Source file | Root cause | Exact correction | Test/evidence | Status |
|---|---|---|---|---|---|
| F01 | `WindmillVerticalSliceController.cs` | Restore forced `Quaternion.identity`; animation replaced authored rotation. | Capture local position/rotation, compose local-axis animation from home pose, restore through reconciliation. | Initial/axis/undo/restart/load/repeat PlayMode tests. | PASS |
| F02 | Controller, builder, tray HUD | Recovered world parts were behind the opaque screen-space tray. | Position occupied parts by snapshot tray index in a reserved visible viewport band; keep tray decoration nonblocking. | Four-ratio bounds tests and captures 02–04. | PASS |
| F03 | Controller input path | Old tests bypassed pointer/UI routing. | Explicit EventSystem hit review plus real Input System pointer test at rendered item coordinates. | `RecoveredPartIsVisibleAndSelectableThroughPointerPath`. | PASS (simulated pointer) |
| F04 | Controller, builder, `TagManager.asset` | Raycasts hit all layers and triggers; decorative primitive colliders could win. | Dedicated `GameplayInteraction` layer, trigger exclusion, ordered typed hits. | Decoration overlap and empty-space test. | PASS |
| F05 | New `PlayfieldFitter.cs` | Fixed orthographic size could clip narrow screens. | Fit essential renderer bounds into a UI-reserved viewport region whenever aspect changes. | 9:16, 9:19.5, 9:21, and 3:4 assertions. | PASS |
| F06 | `ProjectSettings.asset` | Auto orientation and both landscapes were enabled. | Set portrait default; disable upside-down and landscape orientations. | Serialized settings audit and APK build. | PASS |
| F07 | Builder/HUD | SAVE, LOAD, DEV, and status shared the ordinary HUD/playfield. | Gate diagnostics beneath inactive `DeveloperControls`; move feedback above essential playfield. | Developer-control PlayMode assertion and screenshots. | PASS |
| F08 | Controller | Refresh reset a selected part's visual rotation. | Reapply `_selectedRotation` to the selected tray part during snapshot refresh. | Rotate then hint/refresh regression. | PASS |
| F09 | Controller | Screw animated before authoritative state commit. | Commit `TryRemoveScrew` before animation; reconcile from snapshot on pause, focus, or disable. | Disable/pause interruption and action-count tests. | PASS |
| F10 | Controller | Parts permanently reused `trayTargets[i % 3]`. | Derive position from current snapshot tray index. | Occupancy/undo/original integration tests. | PASS |
| F11 | PlayMode evidence fixture | Previous captures did not prove initialized runtime state. | Load the gameplay scene in PlayMode and render five labeled runtime states. | `Docs/Phase1Screenshots/`. | PASS (Editor PlayMode) |
| F12 | TEMP materials | TEMP materials remain URP Unlit. | Kept unchanged because material reconstruction is outside Phase 1. | Source inspection; no parity claim. | DEFERRED |
| F13 | TEMP builder/geometry | Scene is generated from primitives and builder-owned. | Baseline and backup preserved; only the existing TEMP scene was regenerated. | Baseline commit and Git history. | PASS for preservation; production art deferred |
| F14 | Core/presentation data | Level identity and much presentation geometry remain hard-coded. | No architecture rewrite in Phase 1. | Source inspection. | DEFERRED |
| F15 | Project identity/packages | Template identity remains; earlier Purchasing residue was untracked. | Kept package manifests unchanged and excluded generated billing residue. | Package diff empty. | PASS for Phase 1 scope |
| F16 | Audio/haptics/text | Output services and final typography are placeholders. | Preserved temporary controls; no speculative dependency added. | Source inspection. | DEFERRED |
| F17 | URP/device performance | Audit performance prescriptions were unmeasured. | No guessed tuning; device profiling remains required. | No device connected. | BLOCKED |
| F18 | Documentation | Phase 2 report incorrectly said Android modules were absent. | Corrected environment/build records and removed unsupported final-visual implications. | Successful APK and hash below. | PASS |
| F19 | Presentation transient state | Hint/selection state could survive reconciliation. | Clear transient state on undo/load/restart and render from snapshot. Core unchanged. | Lifecycle and refresh tests. | PASS |
| F20 | Repository hygiene | Build/asset provenance needed explicit review. | Explicit staging, backup, baseline, credit ledger, metadata and Git audits. | 0 missing meta, 0 duplicate GUIDs, no push. | PASS |

## D. Modified files

Implementation commit `44061db` contains:

- `Assets/_Game/Scripts/Runtime/Presentation/WindmillVerticalSliceController.cs` — pose preservation, authoritative commit/reconciliation, filtered input, indexed tray presentation, selected state, and responsive fitting hook.
- `Assets/_Game/Scripts/Runtime/Presentation/PlayfieldFitter.cs` and `.meta` — reusable essential-bounds camera fitting.
- `Assets/_Game/Scripts/Editor/VerticalSlice/WindmillVerticalSliceBuilder.cs` — interaction layers, fitter wiring, nonblocking decorative UI, and gated developer controls.
- `Assets/_Game/Tests/PlayMode/WindmillPhase1RegressionTests.cs` and `.meta` — ten focused functional regressions.
- `Assets/_Game/Tests/PlayMode/Phase1RuntimeEvidenceTests.cs` and `.meta` — five-state runtime capture.
- `Assets/_Game/Scenes/Gameplay_Windmill_Graybox.unity` and `Assets/_Game/Prefabs/TEMP_Windmill/TEMP_Windmill_Mechanism.prefab` — regenerated builder-owned TEMP artifacts.
- `ProjectSettings/ProjectSettings.asset` — portrait-only configuration.
- `ProjectSettings/TagManager.asset` — `GameplayInteraction` layer.

Evidence/documentation adds this report, the reconciliation register, checklist, and five PNGs; it updates the build, Phase 2, progress, traceability, and credit records. No files were intentionally removed.

## E. Test evidence

| Run | Discovered/executed | Passed | Failed | Skipped | Evidence |
|---|---:|---:|---:|---:|---|
| Pre-fix focused regression | 6 | 0 | 6 | 0 | `TestResults/phase1-prefix-playmode.xml`, `Logs/Phase1PreFixPlayMode.log` |
| Expanded focused regression | 10 | 10 | 0 | 0 | `TestResults/phase1-expanded-regression.xml`, `Logs/Phase1ExpandedRegression.log` |
| Full EditMode final | 27 | 27 | 0 | 0 | `TestResults/phase1-editmode-final.xml`, `Logs/Phase1EditModeFinal.log` |
| Full PlayMode final | 16 | 16 | 0 | 0 | `TestResults/phase1-playmode-final.xml`, `Logs/Phase1PlayModeFinal.log` |
| Post-build focused regression | 10 | 10 | 0 | 0 | `TestResults/phase1-postbuild-regression.xml`, `Logs/Phase1PostBuildRegression.log` |
| Runtime evidence fixture | 1 | 1 | 0 | 0 | `TestResults/phase1-runtime-evidence.xml`, `Logs/Phase1RuntimeEvidence.log` |

New tests: 10 focused functional tests plus one screenshot-evidence Unity test. The original five PlayMode tests remain in the 16-test full result. Unity compiled without C# errors.

Representative command pattern:

```powershell
Unity.exe -batchmode -projectPath "<project>" -runTests -testPlatform PlayMode -testResults "<result.xml>" -logFile "<log>"
Unity.exe -batchmode -nographics -quit -projectPath "<project>" -executeMethod ScrewAndCreate.Editor.VerticalSlice.WindmillVerticalSliceBuilder.BuildAndroidDevelopmentFromCommandLine -logFile "<log>"
```

Android development APK: `Builds/Phase2/ScrewAndCreate-Phase2.apk`, 55,766,198 bytes, SHA-256 `31D4EB2CF67D1985D89CBE33691A96A41436A445AAE267A78B1E2382786D4168`. Build result: **Succeeded**. No device was available for installation.

## F. Visual verification

All captures are 540×960 automated **Editor PlayMode** renders with runtime initialization executed:

- `Docs/Phase1Screenshots/01-playmode-initial.png` — authored screw-head orientation, fitted windmill, release HUD.
- `Docs/Phase1Screenshots/02-playmode-screw-removed.png` — removed screw and authoritative state transition.
- `Docs/Phase1Screenshots/03-playmode-part-in-inventory.png` — recovered part visible above the tray and tray count 1/3.
- `Docs/Phase1Screenshots/04-playmode-part-selected.png` — clear cyan selected state and blueprint guidance.
- `Docs/Phase1Screenshots/05-playmode-after-undo.png` — screw restored and inventory cleared.

Visual inspection found no missing-resource magenta, exposed SAVE/LOAD/DEV controls, clipped essential windmill parts, or status overlap with screws. These are graybox functional captures, not physical-device screenshots or production-art acceptance.

## G. Regression review

- Protected Core remained byte-for-byte unchanged from baseline.
- The full 27-test deterministic/save/solver/undo suite passed.
- The original five scene/integration tests passed inside the full 16-test PlayMode run.
- Existing remove, inventory, assemble, undo, restart, hint, pause, save/load, and completion behavior remained covered.
- Package manifests did not change; no third-party dependency was introduced.
- Unity asset audit: 63 asset files, 0 missing `.meta`; 91 metadata GUIDs, 0 duplicates; no merge markers.

## H. Known limitations

- **BLOCKED:** no connected Android device (`adb devices` returned an empty device list). Real touch, cutout/navigation inset behavior, install/launch, background/foreground lifecycle, and device performance are unverified.
- The scene and materials remain builder-owned TEMP graybox content. Unlit material quality, production mesh fidelity, audio/haptics, TMP typography, content identity, and profiler-based tuning are later-phase work.
- The screenshot renderer disables particle renderers during capture to avoid a Unity 6.6 batch-mode URP particle-render crash; gameplay particles were not changed.
- Unity-generated YAML contains trailing spaces in empty serialized fields. This is editor serialization output, not hand-authored logic.
- Untracked Unity test-runner and Purchasing residue files were preserved and excluded rather than deleted.

## I. Future reconstruction readiness

The functional foundation is ready for a separately approved reconstruction phase: source is recoverable, Core is protected, camera fitting is reusable, interaction objects use an explicit layer, inventory positions follow authoritative state, and presentation can reconcile after lifecycle interruptions. Free-asset sourcing, Blender mechanical parts, final windmill reconstruction, a separated interactive board, data-driven presentation, wooden UI, and device optimization remain future tasks and were not started.

## J. Final requirement checklist

| Requirement group | Classification | Evidence |
|---|---|---|
| Phase 2 source preserved, backup verified, baseline and repair branch created | PASS | Backup hash, commits `79e79e3` and `44061db` |
| No work lost, destructive Git operation, remote push, Core edit, package addition, or credit use | PASS | Git/diff/ledger audits |
| Screw initialization, axis, undo, restart, load, and repeated-action pose | PASS | Focused regression tests and captures |
| Inventory visibility, occupancy updates, selection, empty-space behavior, and UI separation | PASS | Pointer/decoration tests and captures 02–05 |
| Responsive essential playfield at four required ratios | PASS | `EssentialInteractionPointsFitSupportedAspectRatios` |
| Android portrait serialized configuration | PASS | Project settings and successful APK compilation |
| Developer controls hidden and player controls preserved | PASS | PlayMode assertion, original integration tests, captures |
| State committed before animation and reconciled on pause/disable/load/undo/restart | PASS | Interruption and pose tests |
| Existing EditMode and PlayMode tests pass | PASS | 27/27 and 16/16 final runs |
| Actual PlayMode screenshot path and five required states | PASS | Runtime evidence fixture and PNGs |
| Android APK compilation | PASS | APK path, size, hash, and build log |
| Physical Android install, real touch, cutout, lifecycle, and performance | BLOCKED | No device listed by ADB |
| Production materials/assets/audio/haptics/TMP/final visual parity | NOT TESTED | Explicitly outside Phase 1 |

## K. Final Git state

- Branch: `codex/phase3-functional-stabilization`.
- Baseline commit: `79e79e3cb7562dcb49d8fb92488a6e978cddfcb1`.
- Implementation commit: `44061db`.
- Documentation/evidence is committed separately from implementation.
- Intentionally untracked/excluded local artifacts remain; none are part of the Phase 1 commits.
- Nothing was pushed to GitHub.

