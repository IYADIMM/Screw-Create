# Phase 2 Verification Report

Date: 2026-10-18 (Asia/Dubai)  
Unity: 6000.6.6f1  
Branch: `codex/phase2-playable-windmill`  
Base: `b42222d58bfd2989373bcd67285c166c28a9056b`

## Implemented

- Mixed active workbench loop: removal and assembly are available from every active state; assembly frees tray capacity; completion requires every screw removed and every released part assembled.
- Exact logical undo for successful removals and assemblies, with hint use retained.
- Explicit socket catalog with compatibility groups, allowed parts, rotations, dependencies, tolerances, and visual target IDs.
- Null-safe definition validation and validated snapshot restoration with deterministic tray order.
- Version-two saves with level records, active identity/mode/history fields, v1-to-v2 migration, CRC integrity detection, previous-copy recovery, and a tested `File.Replace` fallback.
- Version-two catalog-aware seed codes with version-one decoding and malformed/future-version rejection.
- Data-driven five-band generator with branching dependencies, three-slot pressure, bounded attempts, solver verification, difficulty score, and structured rejection reasons.
- Idempotent Unity editor builder for `Gameplay_Windmill_Graybox.unity`, its TEMP_ prefab/materials, portrait HUD, safe area, Input System mouse/touch path, drag/rotation/snap loop, solver hint, undo, restart confirmation, pause, functional temporary settings, local save/reload, screw particles, and single-fire completion animation.

## Automated evidence

- EditMode: **27/27 passed**, 0 failed, 0 skipped, 3.1354986 seconds.
- PlayMode: **5/5 passed**, 0 failed, 0 skipped, 2.2627213 seconds.
- PlayMode coverage includes required roots, safe area, Input System mouse selection, one-action screw animation commit, released tray part, overflow rejection, assembly slot release, undo, restart, hint, pause, save/reload, and completion firing once.
- Seed batch: **1,000 requested; 1,000 accepted; 0 rejected; 0 failures; 0 reproducibility failures; average 8.84 solver states; maximum 14**. All three families and all five bands were covered. See `Phase2SeedBatchReport.json`.
- Strict project verification: 198 files scanned, 0 errors, 0 warnings, 0 unverifiable findings.
- Unity metadata audit: 58 asset files, 0 missing `.meta` files.

## Visual-reference comparison

Actual Unity renders:

- `Phase2Screenshots/reference-portrait.png` — 540×960 (9:16)
- `Phase2Screenshots/narrow-phone.png` — 450×900
- `Phase2Screenshots/tall-phone.png` — 540×1080

Matched in the TEMP_ slice: portrait object-first composition, large central windmill, framed wooden blades, cream/warm-wood/deep-teal/brass palette, visible screws, top level/star HUD, large bottom tray, teal undo/hint controls, blueprint socket ghosts, warm workshop staging, and clear puzzle/background separation.

Known visual gaps: primitive geometry lacks the reference's final bevel quality, wood grain, brass detail, foliage density, depth-of-field, window lighting, restrained bloom, and authored workshop decoration. The screenshots prove the current temporary direction and layout; they do not prove final production art quality. The first visual gate still requires user review before mass-producing levels or paid art.

## Android status

Superseded by the Phase 1 stabilization evidence: Android Build Support, SDK/NDK Tools, and OpenJDK are installed for Unity 6000.6.6f1. A development APK compiled successfully at `Builds/Phase2/ScrewAndCreate-Phase2.apk` (55,766,198 bytes; SHA-256 `31D4EB2CF67D1985D89CBE33691A96A41436A445AAE267A78B1E2382786D4168`). No Android device was connected, so install, real touch, lifecycle, cutout, and performance results remain unverified. See `Phase1VerificationReport.md`.

## How to open and play

1. Open the repository in Unity Hub with Editor 6000.6.6f1.
2. Open `Assets/_Game/Scenes/Gameplay_Windmill_Graybox.unity`.
3. Press Play using a portrait Game view.
4. Click or tap exposed brass screws. After up to three parts enter the tray, select or drag the eligible tray part toward its teal blueprint. Use **ROTATE** when needed, then place it to free a slot.
5. Use undo, hint, restart, pause, save, load, and temporary settings from the HUD.

External-generation spend: **0 credits / 0 currency**.

