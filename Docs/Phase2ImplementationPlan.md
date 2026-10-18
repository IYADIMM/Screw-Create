# Phase 2 Disassembly Implementation Plan

Status: **READY FOR REVIEW — implementation not authorized**
Dependency order is binding unless new source evidence requires a documented change.

## M0 — Approval and source checkpoint

**Approval required:** controlled-hybrid rules, Workshop Latch slice, parallel domain architecture, dormant part inventory/assembly presentation.

After approval:

1. Record the decision in `Docs/Decisions.md`.
2. Verify branch/HEAD and preserved untracked files.
3. Create a dedicated implementation branch from the current specification commit; do not recreate the Phase 1 baseline.
4. Confirm protected Core changes, if any, are explicitly in the approved scope.

**Gate:** approved design decision and cleanly recoverable source state.

## M1 — Disassembly domain model and validator

Add under `Assets/_Game/Scripts/Runtime/Disassembly/`:

- `ScrewAndCreate.Disassembly.asmdef`
- `DisassemblyModel.cs`
- `DisassemblySnapshot.cs`
- `DisassemblyDefinitionValidator.cs`

Define IDs, structural/service/rack locations, compatibility groups, covers, securing sites, release dependencies, release paths, completion targets, and snapshot canonical key.

Tests under `Assets/_Game/Tests/EditMode/Disassembly/`:

- duplicate/missing IDs and references;
- occupancy and initial-location uniqueness;
- coverage/release cycles;
- invalid destination compatibility;
- disappearing/nonpersistent storage rejection;
- canonical-key stability.

**Gate:** all new validation tests plus existing 27 EditMode tests pass. No scene changes.

## M2 — Authoritative engine and release closure

Add `DisassemblyEngine.cs` with:

- `GetAccessibleScrews()`;
- `GetLegalDestinations(screwId)`;
- `CanTransferScrew(...)`;
- `TryTransferScrew(...)`;
- deterministic release closure;
- completion/deadlock phase transitions;
- capture/restore/validate snapshot;
- exact undo and hint-count policy.

Use one transaction for source vacancy, destination occupancy, release closure, counters, and undo.

Tests:

- Workshop Latch legal sequence and both opening orders;
- covered screw exposure;
- compatibility/occupancy rejection with unchanged state;
- deterministic simultaneous releases;
- one action/one undo entry;
- undo after multi-component closure;
- completion independent of final screw storage position;
- restart-equivalent initial snapshot.

**Gate:** mutation/property invariants pass; existing mixed `PuzzleEngine` tests remain unchanged.

## M3 — Solver, hints, and deadlock proof

Add `DisassemblySolver.cs` using the same engine transaction. Start with deterministic breadth-first search for shortest tutorial paths; permit a bounded alternative only after profiling.

Add:

- stable successor order by screw ID then destination ID;
- canonical visited states;
- solved/unsolved/resource-limit distinction;
- first-action hint;
- shortest solution and reachable-deadlock metrics.

Tests:

- Workshop Latch shortest length three;
- both authored solution orders reachable;
- rack-recovery state solved;
- intentionally incompatible fixture unsolved;
- resource-limit result distinct from unsolved;
- hint always legal and belongs to a solution path;
- deterministic repeated solution.

**Gate:** solver proof for the slice and no false completion/deadlock in exhaustive small-fixture enumeration.

## M4 — Save schema and migration

Preferred future change: extend `Assets/_Game/Scripts/Runtime/Core/SaveData.cs` with an explicit active game-mode discriminator and versioned disassembly payload, incrementing the save version. Do not reinterpret an existing `PuzzleSnapshot` as disassembly state.

Tests:

- existing v1→v2 migration still passes;
- v2 mixed save migrates without losing construction state;
- new disassembly save round trip;
- invalid location/component state rejection;
- CRC corruption and previous-copy recovery;
- pause/focus save and restore parity.

**Gate:** all save tests and old fixtures pass before presentation integration.

## M5 — Data-driven Workshop Latch asset

Add a data asset or serialized authoring component under `Assets/_Game/Data/Disassembly/DS-INTRO-01/`. Avoid hard-coded definitions in a controller.

Add an editor validator that confirms:

- logical IDs match view IDs;
- every view/prefab/path exists;
- colliders and interaction layers are correct;
- initial transforms and local screw axes are recorded;
- service-hole targets meet minimum screen-space size at supported ratios;
- cover/layer order agrees with definition;
- solver accepts the serialized level.

**Gate:** serialized asset validates and produces the same golden solver solution in batch mode.

## M6 — Separate presentation slice

Create a new scene, for example:

`Assets/_Game/Scenes/Gameplay_Disassembly_WorkshopLatch_Graybox.unity`

Do not overwrite `Gameplay_Windmill_Graybox.unity`.

Add under `Runtime/Presentation/Disassembly/`:

- `DisassemblyBoardController.cs`
- `DisassemblyScrewView.cs`
- `FastenerHoleView.cs`
- `DisassemblyComponentView.cs`
- `ReleasePathView.cs`
- `ScrewRackView.cs`

Reuse `PlayfieldFitter`, `SafeAreaFitter`, explicit interaction layers, UI-selectable blocking, local screw-axis animation, and snapshot reconciliation.

The controller must:

- ask the engine for accessibility and destinations;
- commit before animation;
- animate extraction, transfer, insertion, and release paths;
- reconcile on disable/pause/focus/load/undo/restart;
- expose no part tray, assembly sockets, blueprint ghosts, or rotate control.

**Gate:** compilation plus controller-focused PlayMode tests.

## M7 — HUD and accessibility

Retain undo, restart, hint, pause, status, and progress. Replace tray count and construction guidance with component-clear progress and selected-screw destination guidance.

Add:

- visible selected screw;
- legal/illegal destination states with shape/pattern reinforcement;
- clear reason text for covered/incompatible/occupied actions;
- reduced motion and haptics settings;
- no debug SAVE/LOAD/DEV exposure.

Tests at 9:16, 9:19.5, 9:21, and 3:4:

- every screw and destination inside reserved bounds;
- buttons inside safe area with no overlap;
- actual Input System pointer route reaches each interactive type;
- decorative UI/geometry cannot intercept input;
- text remains outside the active board.

**Gate:** four-ratio PlayMode suite and screenshot review.

## M8 — Animation, feel, and interruption safety

Implement short state-driven sequences without adding DOTween:

- screw head response and local-axis withdrawal;
- transfer arc or direct motion;
- insertion and settle;
- deterministic component drop/slide/hinge path;
- restrained particles, audio hooks, and haptic interface.

Tests:

- state commits once before animation;
- pause/disable/focus during each animation reconciles correctly;
- repeated undo/restart/load has no pose drift;
- rapid taps and multi-touch noise do not double commit;
- reduced-motion path is semantically identical.

**Gate:** no visual-state drift and no rule logic duplicated in presentation.

## M9 — Complete vertical-slice verification

Run:

- all existing EditMode and PlayMode tests;
- complete new disassembly test suites;
- solver proof and exhaustive small-state checks;
- actual PlayMode screenshots for initial, selection, transfer, release, undo, save/reload, and completion;
- Unity compilation/project validation;
- Android development APK build.

Record discovered/executed/pass/fail/skip counts, logs, screenshot origin, APK size/hash, and limitations.

**Gate:** zero unexplained failures; Workshop Latch is solver-proven and playable without assembly.

## M10 — Physical Android verification

On a representative phone verify install/launch, portrait lock, cutouts, touch targets, selection/destination flow, animation interruption, undo/restart/hint/pause, save/resume, thermal behavior, and frame/memory evidence.

**Gate:** device report with model/OS/build hash and screenshots. Editor evidence cannot satisfy this gate.

## M11 — Asset-production decision

Only after the graybox mechanic is approved:

1. approve exact free candidates or Blender tasks;
2. import one controlled batch;
3. update `AssetManifest.csv` and license evidence;
4. profile before expanding the kit.

No paid or credit-consuming generation is assumed.

## Change-risk review

| Risk | Control |
|---|---|
| Breaking construction logic | Parallel domain; run all current tests; do not change `PuzzleEngine` semantics |
| Save incompatibility | Explicit version/mode discriminator and migration tests |
| Solver/gameplay divergence | Solver invokes the same engine transaction |
| Scene loss | New scene path; preserve Windmill graybox and builder |
| UI/input regressions | Reuse Phase 1 interaction layer and real pointer-path tests |
| Animation state drift | Commit first and reconcile from snapshot |
| Unfair deadlocks | Exhaustive small-level analysis and bounded solver proof |
| Asset/license contamination | Per-asset manifest and approval before import |

## Approval checkpoint

Stop after this plan. The next implementation milestone is **M1: disassembly domain model and validator**, and it must not begin until the user approves the recommended rules and architecture.
