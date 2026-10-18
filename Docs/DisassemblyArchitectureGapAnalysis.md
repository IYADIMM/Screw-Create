# Disassembly Architecture Gap Analysis

Status: **VERIFIED baseline + PROPOSED architecture; no gameplay code changed**
Date: 2026-10-18

## 1. Verified baseline

- Branch: `codex/phase3-functional-stabilization`.
- HEAD at analysis start: `e1c9ea79b0417f4a0d82a568fba6450a89bc93b3`.
- Working tree contained only the preserved untracked Unity `InitTestScene…` scene and `.meta` before this documentation work.
- Phase 2 baseline `79e79e3`, Phase 1 implementation `44061db`, and evidence `e1c9ea7` exist.
- Protected `Assets/_Game/Scripts/Runtime/Core/` and `Packages/` have no diff from the Phase 2 baseline.
- Existing result XML confirms 27/27 EditMode, 16/16 PlayMode, 10/10 post-build regressions, and 1/1 runtime evidence test passed.
- APK exists at `Builds/Phase2/ScrewAndCreate-Phase2.apk`, 55,766,198 bytes, SHA-256 `31D4EB2CF67D1985D89CBE33691A96A41436A445AAE267A78B1E2382786D4168`.
- Physical Android verification remains pending.

These facts prove the stabilized mixed-workbench prototype. They do not prove the proposed disassembly rules.

## 2. Capability matrix

| Capability | Implemented | Suitable for disassembly | Gap | Recommended action |
|---|---|---|---|---|
| Unique IDs and reference validation | Yes, `PuzzleDefinition.Validate()` | Partly | No holes, screw locations, release paths, or disassembly completion validation | **REUSE pattern; EXTEND** in a disassembly model |
| Screw prerequisites | Yes, `ScrewDefinition.RequiredRemovedScrewIds` | Yes for authored ordering | Models removed/not removed, not transfer destinations | **REUSE concept** |
| Covered screws | Yes, `CoveringPartIds` and `CanRemoveScrew()` | Yes | Coverage depends on released parts but no general exposed holes | **ADAPT/EXTEND** |
| Screw removal | Yes, `TryRemoveScrew()` | Partly | Removed screws disappear logically and trigger part tray insertion | **REPLACE in current presentation** with atomic transfer |
| Screw relocation/hole occupancy | No | Required | No hole model or screw location map | **ADD** |
| Component fastening/release | Yes, `SecuringScrewIds` and release closure after removal | Partly | Released parts always enter a part tray; no release path/state | **ADAPT** |
| Structural support graph | Basic explicit securing/dependency graph | Partly | No support modes, deterministic motion path, or stable release ordering | **EXTEND** |
| Part inventory | Yes, three-slot tray | No for primary mode | Forces reassembly to free capacity | **REMOVE FROM CURRENT PLAYER PRESENTATION; preserve code** |
| Assembly sockets/rotation | Yes | No for primary mode | Completion depends on all parts assembled | **DORMANT FOR FUTURE** |
| Completion | Yes | No | `UpdateCompletion()` requires all screws removed and all parts assembled | **ADD disassembly completion predicate** |
| Undo | Yes, full snapshots | Strong pattern | Snapshot lacks screw-location/hole/component-release state | **REUSE pattern; EXTEND state** |
| Snapshot validation | Yes | Strong pattern | Current invariant requires every released part in tray or assembled | **ADD disassembly validator** |
| Solver | Deterministic DFS over removal and assembly | Strong foundation | Successor set and completion are wrong; new state may contain cycles | **ADAPT algorithm; add shortest-path/bounded search as needed** |
| Hints | First solver action | Strong pattern | Hint cannot describe source/destination transfer | **EXTEND action data** |
| Deterministic generator | Yes, v2 mixed-loop candidates | Later | Generates one screw per part plus assembly graph; no holes/layers | **DORMANT initially; redesign after authored slice** |
| PRNG/seed codes | Yes | Yes | Generator/content version must change | **KEEP/REUSE** |
| Atomic save/migration | Yes | Yes | `GameSaveData` has only `PuzzleSnapshot`; migration needed | **REUSE service; EXTEND schema later** |
| Input System route | Yes, mouse/touch to explicit layer | Yes | Controller assumes screw removal and part assembly | **KEEP infrastructure; ADAPT commands** |
| Camera fitting | Yes, essential bounds at four ratios | Yes | Bounds must include service holes and dynamic board, exclude cleared parts | **KEEP/EXTEND** |
| Safe area | Yes | Yes | Physical cutout verification missing | **KEEP; device verify** |
| Animation reconciliation | Yes for screw removal and part tray | Yes as pattern | No transfer/insertion or release-path views | **REUSE principle; REPLACE sequences** |
| HUD/pause/restart/undo/hint | Yes | Mostly | Tray count, rotate, blueprint, and completion copy are construction-specific | **KEEP shell; replace mode-specific elements** |
| Android portrait/build | Yes | Yes | Device testing absent; package ID still template identity | **KEEP; device verify; identity later** |

## 3. Source-grounded findings

### Current Core

- `PuzzleModel.cs`: `PuzzlePhase`, `InteractionMode`, and `PuzzleActionKind` encode mixed disassembly/assembly. `PuzzleSnapshot` stores removed/released/tray/assembled sets, not screw locations.
- `PuzzleEngine.cs:50–72`: legality checks dependencies, covers, and predicted tray overflow; success marks a screw removed and appends every newly eligible part to the tray.
- `PuzzleEngine.cs:138–167`: snapshot validation and completion explicitly require every released part to be either in the tray or assembled, and completion requires every part assembled.
- `PuzzleSolver.cs:41–63`: deterministic DFS enumerates remove then assemble actions. Its visited-key discipline is reusable, but its action graph is not the proposed game.
- `DeterministicGenerator.cs:82–120`: generation creates one fastener and one assembly socket per part and scores assembly edges/rotations. It must remain dormant until the new authored rules stabilize.
- `SaveData.cs:27–67`: version-two save can identify the active puzzle and store a snapshot, but a future migration must distinguish mixed-workbench and disassembly snapshot schemas.

### Current presentation

- `WindmillVerticalSliceController.cs` commits logical removal before animation and reconciles from snapshots. This state-first principle should be retained.
- `ProcessPointerPress()` and `InteractionHits()` already provide UI-aware, explicit-layer mouse/touch selection.
- `RefreshVisuals()` is coupled to tray and assembly sockets; it cannot simply be relabeled.
- `PlayfieldFitter.cs` and `SafeAreaFitter.cs` are reusable.
- `WindmillVerticalSliceBuilder.cs` produces a builder-owned TEMP scene and must not be used to overwrite future authored production scenes.

## 4. Recommended separation

Avoid rewriting the tested mixed-workbench Core in place. After approval, add a parallel disassembly domain:

```text
Assets/_Game/Scripts/Runtime/Disassembly/
  ScrewAndCreate.Disassembly.asmdef
  DisassemblyModel.cs
  DisassemblyDefinitionValidator.cs
  DisassemblyEngine.cs
  DisassemblySolver.cs
  DisassemblySnapshot.cs
  DisassemblySaveAdapter.cs

Assets/_Game/Scripts/Runtime/Presentation/Disassembly/
  DisassemblyBoardController.cs
  DisassemblyScrewView.cs
  FastenerHoleView.cs
  ComponentView.cs
  ReleasePathView.cs
  ScrewRackView.cs
```

The new runtime assembly may reference `ScrewAndCreate.Core` for stable utilities such as `DeterministicRng`, `SeedCode`, and CRC/save infrastructure where interfaces permit. Existing `PuzzleEngine`, assembly models, controller, scene, and tests remain intact for regression and future construction use.

## 5. Future Core changes requiring explicit implementation approval

| Source location | Proposed change | Justification | Regression strategy |
|---|---|---|---|
| New `Runtime/Disassembly/` files | Add separate definition, state, engine, validator, solver, and action types | Required rule semantics do not exist; parallel domain avoids destabilizing mixed-workbench logic | New EditMode suites plus all existing 27 Core tests |
| `SaveData.cs` | Add a versioned disassembly payload or a typed active-mode envelope; increment save version with migration | Current save accepts only `PuzzleSnapshot` | Golden migration, corruption, previous-copy recovery, mixed-save compatibility |
| `SeedCode.cs` and generator identity only if procedural work begins | Add a disassembly generator/content version | Existing codes describe mixed-workbench generation | Old-code decode tests and new golden round trips |
| No initial change to `PuzzleEngine.cs` | Preserve existing mixed engine | Construction concept remains valuable and tested | Existing tests must remain byte-for-behavior compatible |

## 6. Presentation disposition

| Existing system | Classification | Disassembly use |
|---|---|---|
| `PlayfieldFitter` | KEEP | Fit board, screws, holes, and attached components into the safe play region |
| `SafeAreaFitter` | KEEP | Preserve portrait cutout/navigation handling |
| Interaction layer and EventSystem filtering | KEEP | Use for screws and legal destinations |
| Screw home-pose capture/local-axis animation | REUSE | Extend to extraction, transfer, and insertion |
| Snapshot reconciliation | KEEP | Rebuild all screw/component views after interruptions |
| `PartView`/`SocketView` | DORMANT FOR FUTURE | Construction mode only |
| Three-slot part tray | REMOVE FROM CURRENT PLAYER PRESENTATION | Do not show in primary disassembly HUD |
| Rotate button/blueprint ghosts | REMOVE FROM CURRENT PLAYER PRESENTATION | Construction-specific |
| Undo/restart/hint/pause shell | KEEP | Bind to disassembly controller |
| SAVE/LOAD developer panel | KEEP GATED | Preserve diagnostics outside player HUD |
| TEMP windmill scene | KEEP AS PRESERVED REFERENCE | Do not overwrite; build a separate disassembly slice scene |

## 7. Missing systems

- Screw-location and destination-occupancy model.
- Structural versus service-hole semantics.
- Destination compatibility and legal-destination query.
- Deterministic release-closure engine.
- Component release-path data and presentation.
- Disassembly-only completion and deadlock state.
- Solver successors for screw transfer and cyclic storage moves.
- Disassembly snapshot validation and save migration.
- Authoring validator for visual/logical ID parity, layers, covers, paths, and touch bounds.
- Disassembly-specific HUD and accessibility cues.
- Device-level touch, safe-area, lifecycle, and performance evidence.

## 8. Quality gates

1. No modification to protected Core or presentation until the design approval gate is recorded.
2. Test the new domain independently before scene integration.
3. Use the same engine operation in gameplay, solver, hints, and validation.
4. Run all existing 27 EditMode and 16 PlayMode tests after each integration milestone.
5. Preserve the current scene and add a new scene with a distinct path and IDs.
6. Require solver proof, PlayMode pointer tests, four-ratio layout tests, interruption reconciliation, and a development APK before accepting the slice.
