# Phase 1 Multi-AI Audit Reconciliation

Date: 2026-10-18 (Asia/Dubai)

Authority used for this register: local source and executable evidence first; the ChatGPT review and Claude Revision 2 next; Gemini and the earlier Claude report as investigation inputs. No audit is treated as an instruction source by itself.

## Source-protection checkpoint

- Original commit: `b42222d58bfd2989373bcd67285c166c28a9056b`
- Phase 2 baseline: `79e79e3cb7562dcb49d8fb92488a6e978cddfcb1`
- Repair branch: `codex/phase3-functional-stabilization`
- External source backup: `C:/Users/Lenovo/OneDrive/Downloads/ScrewAndCreate_Phase2_PreRepair_Backup_2026-10-18.zip`
- Backup SHA-256: `8B319F587550F46B19077D0CAFEC2D89145527373881378E98F0A3533BEC5A07`
- Backup verification: 239 entries, including the gameplay scene, TEMP prefab, package manifest, project version, six EditMode test sources, one PlayMode test source, and 90 `.meta` files.
- Excluded from baseline: Unity caches, logs, test/build outputs, local archives, and regenerated `Assets/Resources/BillingMode.json` residue from the unused Purchasing package.
- Remote state: untouched; no Phase 1 or Phase 2 commit pushed.

## Consolidated defect register

| ID | Sources | Actual-source result | Phase 1 disposition | Required evidence |
|---|---|---|---|---|
| F01 | Claude V-02; ChatGPT E01 | **Confirmed.** `RestoreScrewTransforms` restores position but forces `Quaternion.identity`; authored screw pose is approximately 90 degrees about local X. `RemoveSequence` replaces rotation with a world-independent Y Euler rotation. | Fix home position and rotation capture, compose animation from home pose around an explicit local mechanical axis, reconcile on undo/restart/load/interruption. | Pre/post PlayMode pose tests and runtime capture. |
| F02 | Claude V-01; ChatGPT E02; earlier Claude P-01/P-11 | **Confirmed.** Tray targets sit at world y=-4 beneath an opaque screen-space panel. The panel is a raycast target. | Put occupied parts in a camera-visible tray world region derived from the viewport; decorative UI must not block gameplay. | Bounds/render visibility assertions at four ratios plus runtime captures. |
| F03 | Claude V-01/V-15; ChatGPT E02/E07 | **Confirmed.** Existing assembly tests call `SelectTrayPart` directly; they do not prove the pointer route. `ProcessPointerPress` exits when the EventSystem reports UI coverage. | Add a genuine Input System pointer test at the rendered tray part's screen position, including UI raycast evidence and empty-space behavior. | PlayMode pointer-path test. |
| F04 | ChatGPT E03; Claude V-05 | **Confirmed and narrower than V-05.** Both pointer paths raycast all layers and include triggers; generated decorative primitives retain colliders. The complete replacement of primitives is visual-production work. | Filter hits to explicit interaction views, ignore triggers/decorative colliders, and use deterministic hit priority. Defer final prefabs. | Overlapping decoration/trigger PlayMode test. |
| F05 | Claude V-03; ChatGPT gate G1; Gemini aspect-ratio risk | **Confirmed.** Camera orthographic size is fixed at 5.7; no essential-playfield bounds calculation exists. Screenshots alone do not prove interaction bounds. | Add reusable playfield fitting from essential bounds, viewport/aspect, and UI reservations. | 9:16, 9:19.5, 9:21, and 3:4 tests. |
| F06 | Claude V-09 | **Confirmed.** `defaultScreenOrientation` is Auto Rotation and all four autorotation flags are enabled. | Configure portrait deliberately and disable unwanted rotation. | Serialized settings audit and Android build. |
| F07 | Claude V-11; earlier Claude P-06/P-08; Gemini debug UI | **Confirmed.** SAVE, LOAD, and `DEV` are on the ordinary HUD; status text occupies the playfield. | Gate diagnostics with `Debug.isDebugBuild`, preserve save/load methods, and reserve a non-overlapping player feedback region. | Release-configuration PlayMode test and runtime capture. |
| F08 | ChatGPT E04 | **Confirmed and missing from Claude's primary P0 list.** `RotateSelected` changes the part transform, but every `RefreshVisuals` restores the tray target rotation while `_selectedRotation` remains changed. | Reconcile a selected tray part from its chosen rotation; keep visual and requested rotation aligned. | Rotate, refresh/hint, save/load/undo tests. |
| F09 | Claude V-13; ChatGPT E05 | **Confirmed.** Screw visuals animate before `TryRemoveScrew`, so pause or interruption can leave a moved visual with unchanged state. | Validate and commit the move before animation, then reconcile the view from the authoritative snapshot on interruption. | Pause/disable during removal test and single-commit assertions. |
| F10 | Claude V-12 | **Confirmed.** Each part owns `trayTargets[i % 3]`, so P1 and P4 share a target independent of current tray index. | Position parts by snapshot tray index, not permanent part assignment. | Multiple occupancy, undo, placement, and reload tests. |
| F11 | Claude V-15; ChatGPT E07 | **Confirmed.** `CaptureScreenshotsFromCommandLine` opens the scene without PlayMode; previous images cannot prove Awake/Start state. | Add a PlayMode evidence capture route for initial, removed, inventory, selected, and reconciled states. | Labeled Editor PlayMode screenshots. |
| F12 | Claude V-04; earlier Claude P-05; Gemini material/lighting findings | **Confirmed visual limitation.** All TEMP materials use URP Unlit, so scene lights do not shade them. | Defer the production material reconstruction under the Phase 1 scope boundary. No visual parity claim. | Future device screenshot gate. |
| F13 | Claude V-05/V-06; earlier Claude P-03/P-04; Gemini asset findings | **Confirmed TEMP implementation.** The editor builder uses `CreatePrimitive` and recreates the scene. | Preserve the committed graybox; do not begin asset replacement. Add an explicit warning/freeze guard if needed so later art is not overwritten. | Baseline commit and later production-pipeline gate. |
| F14 | Claude V-07/V-08; ChatGPT E08 | **Confirmed.** Puzzle/HUD/save identity and presentation geometry are hard-coded. | Defer LevelPresentation and separated board/windmill architecture to the approved next design phase. | Future approved data schema. |
| F15 | Claude V-10 | **Partly confirmed.** Package/company identity remain template defaults and belong to the release gate. Purchasing was unintentionally introduced during the Android attempt; the tracked manifest was restored, while generated billing residue remains excluded. | Keep identity deferred; do not re-add Purchasing. | Clean manifest and staged-file review. |
| F16 | Claude V-14 | **Confirmed placeholders.** Audio/haptic settings have no output service; legacy Text is used. | Defer production audio/haptics/text-system work; keep controls functional and honest. | Future device/service tests. |
| F17 | Claude V-16 | **Confirmed configuration values; performance impact unproven.** Mobile URP settings need device profiling. | Do not tune by guesswork in Phase 1. | Named-device profiler capture later. |
| F18 | Claude V-17 | **Confirmed stale evidence.** The Phase 2 report says Android modules are missing and overstates visual matching; logs show installed SDK/NDK/JDK and a successful earlier APK build. | Correct documentation with exact build/runtime provenance and limitations. | Build log, APK hash, and revised report. |
| F19 | Claude V-18 | **Partly confirmed latent risk.** Undo revision semantics are internal Core behavior, and no failing presentation cache exists. Hint highlight can remain after state changes. | Clear transient presentation state during reconciliation. Leave protected Core unchanged without a failing case. | Presentation test; Core unchanged. |
| F20 | Earlier Claude P-12/P-13 | **Resolved or superseded.** Build output is ignored; asset ledger has TEMP/reference rows; baseline commit now exists. | Maintain explicit staging and provenance. | Git state and manifest review. |

## Duplicate and contradiction resolution

- Claude V-01/V-15, ChatGPT E02/E07, earlier Claude P-01/P-11, and Gemini's inventory criticism describe two distinct defects: visual occlusion and pointer interception. They require separate assertions.
- Claude V-02 and ChatGPT E01 are the same screw-pose defect and share one fix.
- Claude V-03, Gemini's aspect-ratio warning, and earlier Claude P-01/P-11 converge on responsive fitting; the local fixed camera confirms the need.
- Claude V-11, ChatGPT's release-HUD finding, earlier Claude P-06/P-08, and Gemini's raw debug UI finding are duplicates.
- Gemini's instruction to convert or reinstall URP contradicts the manifest and render settings. URP 17.6.0 is already configured, so that recommendation is rejected.
- Gemini's mandatory DOTween and `Resources.Load` suggestions are unsupported and conflict with the no-dependency and serialized-catalogue direction. Coroutines remain acceptable when state commits and reconciliation are correct.
- Gemini guessed nonexistent classes such as `GridManager`, `ScrewState`, and `LevelManager`. The real boundary is the tested Core assembly plus `WindmillVerticalSliceController`; no architecture rewrite is justified.
- Gemini's fixed physics timestep, triangle, draw-call, texture, FOV, and shadow prescriptions are hypotheses until measured on a named Android device. They are deferred.
- The earlier Claude report's full asset replacement items are valid future visual goals, not Phase 1 functional defects.

## Protected Core decision

No audit demonstrates a new failing case inside `Assets/_Game/Scripts/Runtime/Core/` that requires a Phase 1 change. Phase 2 Core changes are preserved in the baseline commit. Phase 1 will not modify Core implementation.

## Phase 1 execution order

1. Add failing presentation/input/layout regression tests and record the pre-fix result.
2. Fix screw pose and authoritative commit/reconciliation.
3. Fix tray layout, selected rotation, UI interception, and deterministic hit selection.
4. Add responsive playfield fitting and portrait-only settings.
5. Gate developer UI and add PlayMode screenshot evidence.
6. Run the full EditMode/PlayMode suites, compile/build Android, review the final diff, and record remaining device-only checks.

## Final disposition

- Confirmed functional defects F01–F11 and F18–F20 were corrected or resolved within the Phase 1 boundary.
- F12, F14, F16, and production portions of F13/F17 remain explicitly deferred; F17 device measurements are blocked because no Android device was connected.
- Pre-fix evidence: 6 focused PlayMode tests executed, 0 passed, 6 failed.
- Final evidence: 27/27 EditMode, 16/16 full PlayMode, 10/10 post-build focused PlayMode, and 1/1 runtime capture fixture passed.
- Android development APK succeeded at 55,766,198 bytes with SHA-256 `31D4EB2CF67D1985D89CBE33691A96A41436A445AAE267A78B1E2382786D4168`.
- Protected Core and package-manifest diffs are empty. External-generation spend remains zero. Nothing was pushed.

