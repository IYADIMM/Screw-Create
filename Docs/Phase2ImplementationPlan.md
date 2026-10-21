# Phase 2 Disassembly Implementation Plan

Status: **REVISED AFTER PHASE 2.1 FEASIBILITY GATE — implementation approval pending**

## M0 — Rule approval and protected baseline

Approval must name:

- staged persistent-baseboard relocation rules;
- no rack and universal screws for the first five levels;
- no re-fastening in launch rules;
- parallel stateful disassembly session with pure shared transitions;
- separate versioned disassembly save envelope;
- T01 as the first production slice.

Before implementation, record branch/HEAD/status, confirm the Phase 2.1 verifier passes, preserve existing untracked Unity files, and confirm no protected Core diff.

**Gate:** explicit user approval and recoverable source state.

## M1 — Smallest production milestone: T01 pure domain

Add a separate assembly under `Assets/_Game/Scripts/Runtime/Disassembly/` containing only:

- immutable `DisassemblyDefinition`, `ComponentDefinition`, `HoleDefinition` and `ScrewDefinition` data;
- serializable `DisassemblySnapshot`;
- definition and snapshot validators;
- pure occupancy, exposure, destination, transfer and deterministic release-closure functions;
- canonical key preserving distinct screw IDs;
- `DisassemblySession` owning current snapshot and exact undo history;
- explicit transaction results and no-currently-legal-moves query.

Encode only T01 First Release as a test fixture. Do not create or modify a Unity scene in this milestone.

Tests must prove:

1. all T01 definition and initial-state invariants;
2. exact legal screw and destination sets;
3. atomic rejection with no state/history mutation;
4. one transfer releases the component and completes before presentation;
5. undo restores exact state;
6. canonical key stability;
7. parity with the Python T01 state count and solution;
8. every existing EditMode test still passes.

**Gate:** pure-domain T01 parity and zero existing regressions. Stop for review.

## M2 — Explicit-outcome solver and five-fixture parity

Add a solver that calls the same transition function as `DisassemblySession` and returns `ProvenSolution`, `ProvenNoSolution`, `ResourceLimitReached` or `Cancelled`.

Requirements:

- stable successor order by screw ID then destination ID;
- visited-state pruning using the canonical key;
- explicit state/time limits and cancellation;
- exhaustive mode for small fixtures;
- incremental or worker execution for larger searches;
- no screw symmetry reduction without behavioral-equivalence proof.

Port T01–T05 and NC01 as C# fixtures. Assert exact reachable states, minimum lengths and deadlock counts from `Phase21FeasibilityReport.json`.

**Gate:** exact Python/C# parity and explicit resource-limit tests.

## M3 — Separate save envelope

Add a namespaced version-1 disassembly save without changing `Assets/_Game/Scripts/Runtime/Core/SaveData.cs`.

Test:

- snapshot round trip and definition/content identity;
- malformed occupancy and unknown IDs;
- checksum failure and previous-copy recovery if the existing atomic service pattern is reused;
- current construction save and v1-to-v2 migration regression.

**Gate:** construction saves remain byte-for-behavior compatible.

## M4 — T01 data asset and authoring validation

Create a serialized T01 asset outside controllers. Validate unique IDs, view-to-logic mapping, hole ownership, structural assignments, cover references, recorded screw axes, colliders, interaction layers and screen-space target size.

**Gate:** serialized asset produces the same canonical initial state and one-step proof as the test fixture.

## M5 — Separate T01 graybox presentation

Create a new disassembly scene. Do not overwrite the Windmill graybox. Reuse safe-area and playfield fitting utilities where suitable.

The controller must:

- query logical accessibility and destinations;
- commit before animation;
- animate toward the committed snapshot;
- reconcile on undo, restart, load, disable, pause and focus change;
- expose no construction tray, sockets, rotate control or developer save/load panel.

**Gate:** pointer-path PlayMode tests and interruption reconciliation pass.

## M6 — Tutorial sequence and device-facing HUD

Add T02–T05 one at a time, preserving one new concept per level. Use component-clear progress, undo, restart, hint and pause. Use shape and motion cues without color-matching semantics.

Test 9:16, 9:19.5, 9:21 and 3:4 for fit, safe area, touch targets and text. Hints must come from a proven solution path. A no-move state offers undo/restart and is never inferred from solver resource exhaustion.

**Gate:** all five C# exhaustive metrics match the Phase 2.1 report; PlayMode evidence exists for each concept.

## M7 — Android development verification

Build a development APK only after compilation and Unity suites pass. Record commit, size, SHA-256, build log and screenshot provenance. On a physical device verify install, portrait framing, taps, destination selection, release, undo/restart/hint/pause, lifecycle recovery and save/resume.

**Gate:** device report identifies phone, Android version, APK hash and unresolved limitations.

## M8 — Mandatory visual-quality milestone

After the gameplay slice is validated, replace visible TEMP primitives with original beveled wood components, recognizable mechanically aligned screws, URP Lit materials, wood texture treatment, differentiated holes, contact shadows, professional lighting and functional portrait UI. Keep developer controls hidden and touch targets reliable.

No paid asset, download or external-generation credit is authorized by this plan. Obtain approval for any exact asset batch or credit use before acquisition.

**Gate:** device screenshots and play evidence; a successful compile alone is insufficient.

## M9 — Later mechanics decision

Only after playtesting the base rule set, separately evaluate compatibility groups, optional racks, component-owned temporary holes, hinges, procedural generation and construction-mode integration. Each addition needs a player-facing purpose, transition semantics and solver proof.

## Risk controls

| Risk | Control |
|---|---|
| Breaking construction | Parallel assembly/domain/save; protected Core unchanged |
| Rule divergence | One pure transition function for session, solver, hints and tests |
| Storage contradiction | Validator diagnostics plus exhaustive fixture certification |
| False deadlock | Separate no-move query from solver outcome and undo state |
| Main-thread stalls | Limits, cancellation and nonblocking large searches |
| Scene loss | New scene path; preserve Windmill graybox and builder |
| Visual overclaim | Require runtime and physical-device evidence |
| Credit use | No paid or external generation without exact user approval |

## Stop condition

Phase 2.1 ends with corrected documents and executable feasibility evidence. Do not begin M1 until the user explicitly approves the selected rules and architecture. Do not push or merge this correction branch without explicit permission.
