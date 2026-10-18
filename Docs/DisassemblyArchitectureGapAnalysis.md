# Disassembly Architecture Gap Analysis

Status: **PHASE 2.1 SOURCE-VERIFIED DESIGN ANALYSIS — no production implementation**

Reviewed baseline: branch `codex/phase3-functional-stabilization`, commit `e0169a168c3b0e52d0fb3a659bbf8e5b08f5c95e`

Correction branch: `codex/phase2-1-gameplay-reconciliation`

## 1. Source-grounded findings

### `Assets/_Game/Scripts/Runtime/Core/PuzzleModel.cs` — mixed-workbench definitions and snapshots

The current model represents screws that are removed, parts that are released into a tray, and parts assembled into target sockets. Its snapshot does not contain a screw-to-hole location map, hole ownership, destination occupancy or retired structural-hole state. Extending it in place would change established construction semantics.

### `Assets/_Game/Scripts/Runtime/Core/PuzzleEngine.cs` — `PuzzleEngine`

The engine is stateful. It owns mutable removed/released/tray/assembled sets, counters, phase and a private `Stack<PuzzleSnapshot>` undo history. `TryRemoveScrew` and `TryAssemblePart` mutate that state after validation. `CaptureSnapshot`, `TryRestoreSnapshot` and `TryUndo` expose controlled state transitions.

The class therefore cannot accurately be described as a stateless functional engine. It is a stateful transaction owner with serializable snapshots. Its current rules remove screws from play and transfer released parts to a capacity-limited tray, which does not model screw relocation.

### `Assets/_Game/Scripts/Runtime/Core/PuzzleSolver.cs` — `PuzzleSolver`

The existing solver searches by calling the same current engine operations and uses a canonical `SolverKey`, visited-state pruning and a hard state budget. `SolverResult` exposes `Solved` and `ResourceLimitReached`, so budget exhaustion is distinguishable from the ordinary failure string. It has no cancellation contract and targets the construction state space. The disassembly solver should retain shared transition use while adopting an explicit result enum and cancellation.

### `Assets/_Game/Scripts/Runtime/Core/SaveData.cs` — `GameSaveData` and `AtomicJsonSaveService`

The current save schema is version 2, stores a construction `PuzzleSnapshot`, supports v1-to-v2 migration, wraps JSON with CRC32, and falls back to a previous copy. Reinterpreting `ActivePuzzle` as a relocation snapshot would risk old saves and migration behavior. Phase 2.1 makes no change to this file.

### `Assets/_Game/Scripts/Runtime/Presentation/WindmillVerticalSliceController.cs` — current slice controller

The controller presents the preserved Windmill graybox and construction loop. Its interaction and reconciliation work remains useful evidence, but its part tray, socket placement and construction completion are not the primary disassembly game.

### `Assets/_Game/Scripts/Runtime/Presentation/PlayfieldFitter.cs` and `SafeAreaFitter.cs`

These presentation utilities remain reusable for portrait framing and safe-area handling. They must not participate in logical accessibility or legality.

## 2. Decision: parallel stateful disassembly domain

Add a separate pure-C# domain only after approval. Use one consistent architecture:

- `DisassemblyDefinition` is immutable authored data.
- `DisassemblySnapshot` is a complete serializable value: distinct screw locations, released components, counters, revision and phase.
- Pure transition helpers derive occupancy, exposure, legal destinations, release closure, completion and canonical keys from definition plus snapshot.
- `DisassemblySession` owns the current snapshot and undo/redo history, validates commands, calls the pure transition helpers, and publishes committed results.
- `DisassemblySolver` owns only search-local data and calls the same pure transition function used by `DisassemblySession`.
- Presentation owns no gameplay state and reconciles views from snapshots.

This keeps mutation and history in one session object while making rule evaluation reproducible and directly shareable by gameplay, solver, tests and hints.

## 3. Proposed data ownership

| Concern | Owner | Serialized |
|---|---|---:|
| Components, holes, covers, dependencies, completion targets | `DisassemblyDefinition` | authored asset/data |
| Screw locations, released set, phase, counters, revision | `DisassemblySnapshot` | yes |
| Current snapshot and undo/redo stacks | `DisassemblySession` | current snapshot and optional command history |
| Legality and release closure | pure transition module | no internal state |
| Search frontier, visited set, limits, cancellation | `DisassemblySolver` | no |
| Selection, animation, transforms, audio, haptics | presentation controller/views | no |

## 4. Canonicalization and solver requirements

The canonical key must include, in stable ID order:

1. each distinct screw ID and its current hole ID;
2. the released component bitset or sorted IDs;
3. any future rule-relevant flags.

Action count, hint count, animation progress, selection and undo depth do not affect future legal moves and stay outside the solver key. Screw IDs may not be collapsed solely because they use the same compatibility group. A symmetry reduction needs a proof that source coverage, structural role, allowed destinations and every future consequence are identical.

Successors are ordered by screw ID then destination ID. The solver returns `ProvenSolution`, `ProvenNoSolution`, `ResourceLimitReached` or `Cancelled`. A no-legal-moves snapshot is recorded as a reachable deadlock candidate; it is not a solver result and says nothing about player undo history.

Small tutorials require exhaustive enumeration. Larger searches require explicit state/time budgets, cancellation and background or incremental execution so Unity’s main thread is not blocked.

## 5. Save strategy

Preserve `GameSaveData` and all existing migrations unchanged during the first production slice. Introduce a separate versioned disassembly envelope, for example:

```text
DisassemblySaveEnvelope v1
  contentVersion
  levelId
  definitionHash
  currentSnapshot
  optional moveHistory
  checksum
```

Use a separate file or namespaced save key. A future unified profile may reference both construction and disassembly saves only after explicit migration design and regression tests. Do not modify `Assets/_Game/Scripts/Runtime/Core/SaveData.cs` merely to start the slice.

## 6. Preserved systems

- `Assets/_Game/Scripts/Runtime/Core/` remains unchanged.
- The Windmill graybox scene and builder remain a historical, functional reference.
- Existing construction definitions, solver, tray, sockets, save migration and tests remain available for a later construction game.
- `PlayfieldFitter`, `SafeAreaFitter`, input-layer discipline, home-pose capture and snapshot reconciliation can be reused at the presentation boundary.

## 7. Missing production systems

- Disassembly definition and validator.
- Screw-location snapshot and canonical key.
- Pure transfer/release transition module.
- Stateful session with exact undo.
- Explicit-outcome solver with cancellation.
- Separate disassembly save envelope.
- Data assets for the five tutorials.
- Disassembly presentation, authoring validator and device tests.

## 8. Risk controls

| Risk | Control |
|---|---|
| Construction regression | Parallel assembly; no edits to protected Core; rerun existing Unity tests after integration |
| Gameplay/solver divergence | One pure transition function used by session, solver, hints and tests |
| False deadlock on search exhaustion | Explicit solver outcomes; no-move query kept separate |
| Save corruption | Separate versioned envelope; do not reinterpret construction snapshots |
| Visual state changing legality | Logical coverage only; presentation reconciles after commit |
| Main-thread stalls | Exhaustive search only for small fixtures; limits, cancellation and worker/incremental search for larger levels |

## 9. Verification boundary

Phase 2.1 changed documentation and an isolated Python verifier only. No Unity runtime, scene, save or Core file was changed. Existing Unity tests were not made unnecessary; they must be rerun when the approved production domain is added.
