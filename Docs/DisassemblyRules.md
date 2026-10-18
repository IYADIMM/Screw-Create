# Screw & Create — Disassembly Rules

Status: **PHASE 2.1 CORRECTED SPECIFICATION — not implemented in Unity**

Rule set: `phase-2.1-baseboard-relocation-v1`

## 1. Authoritative logical state

A stable snapshot contains:

- a distinct location for every screw ID;
- the released component-ID set;
- phase, action count, hint count and revision;
- no transforms, colliders, renderer flags, animation time, touch coordinates or UI state.

Occupancy, exposure, available destination state, legal moves and completion derive from the immutable definition plus the snapshot. Screw identities remain distinct even when screws share appearance or compatibility.

## 2. Hole definitions and ownership

| Property | Meaning | Launch behavior |
|---|---|---|
| Baseboard owner | The hole physically persists for the entire puzzle | May become a destination when all other conditions pass |
| Component owner | The hole exists only while its owning component remains attached | Never a launch destination; ceases to exist on release |
| Structural role | The hole is assigned to one component’s fastening set | Reject as destination while that component is attached |
| Active structural hole | Structural hole currently occupied while its component is attached | Its occupant contributes to fastening |
| Vacated active structural hole | Empty structural hole whose component remains attached | Still rejects parking; this prevents re-fastening ambiguity |
| Retired structural hole | Persistent baseboard structural hole whose component has released | Legal universal parking destination when exposed and empty |
| Dedicated parking role | Baseboard hole with no fastening effect | Legal universal parking destination when exposed and empty |
| Covered hole | At least one declared covering component remains attached | Cannot be selected or used as a destination |

A hole is not legal merely because it is empty. Ownership, persistence, coverage, occupancy and fastening state all apply.

## 3. Accessibility

Logical screw accessibility requires:

1. the puzzle is active;
2. the screw occupies an existing hole;
3. every component declared to cover that hole is released.

The logical rule does not query a camera, collider, GameObject, renderer, modal, animation or pointer position. The presentation may temporarily suppress input during reconciliation, but that does not change the engine’s legal-move set.

## 4. Legal destination

For snapshot `s`, hole `h` is a legal parking destination when all conditions hold:

1. `h` is owned by the persistent baseboard;
2. `h` is empty;
3. `h` is exposed under the authored coverage relation;
4. `h` is either a dedicated parking hole or a structural hole assigned to a released component;
5. `h` differs from the screw’s source.

The introductory rule set has one universal screw family. Compatibility checks are therefore true for every intro screw and are not presented to the player.

## 5. Transfer transaction

`TransferScrew(screwId, sourceHoleId, destinationHoleId)` is the sole launch action.

The engine validates the complete command against one snapshot. On success it:

1. stores one pre-move snapshot in history;
2. changes the screw location from source to destination;
3. increments action count and revision once;
4. resolves deterministic release closure;
5. derives exposure and destination state from the result;
6. evaluates completion and whether the current snapshot has any legal moves;
7. publishes one stable result for presentation.

Validation failure is atomic and changes no logical data or history.

Parked screws may be transferred again to another legal destination. Moving a parked screw never reattaches a component and never changes a retired structural hole back to an active fastener.

## 6. Component release and closure

A component becomes release-eligible when:

- every structural hole in its `securedBy` set is empty; and
- every authored release dependency is already released.

After a transfer, repeatedly release all eligible components in ordinal component-ID order until no further release is possible. Release is an immediate logical transition. It can:

- retire persistent structural holes belonging to the released component;
- remove component-owned holes from the board;
- uncover holes and screw heads;
- satisfy another component’s release dependency.

Visual falling, sliding, hinging and clearing occur afterward. Completion is evaluated from the stable logical closure and never waits for animation.

## 7. Coverage

Coverage is an authored relation from an attached component to a hole ID. A covered hole blocks both its occupant’s selection and its use as a destination. Geometry, raycasts and renderer visibility do not create or remove logical coverage.

Definitions with unknown IDs, impossible ownership, duplicate occupancy, invalid securing references, components that release during initialization, or unsupported cyclic dependencies must fail validation before play.

## 8. Storage-capacity invariant

Define `E(s)` as the number of exposed, empty, legal destination holes in stable snapshot `s`.

- A structural transfer consumes one destination before closure.
- Releasing a component can make its empty persistent structural holes legal and can uncover parking holes.
- Moving a parked screw changes which hole is empty but does not create capacity.
- A component needing `k` additional transfers cannot release along a route that exposes fewer than `k` destinations before that release.

The verifier’s `NC01` fixture has three occupied structural holes, two parking holes and no intermediate release. Exhaustive enumeration proves no solution. No rack, screw disappearance or hidden destination may be assumed to repair such a level.

## 9. Completion, no-move state and recovery

The puzzle is complete when every required component is released. Screw locations do not otherwise affect completion.

`NoCurrentlyLegalMoves` means only that the present snapshot has no legal transfer. It is not the same as a solver proof of no solution, and undo availability is a separate player-history property. The launch UX may offer undo and restart in a no-move state; it must not manufacture a loss to sell recovery.

Undo restores the exact prior snapshot, including screw locations, released components, counters and derived legal moves. Presentation reconciles to that snapshot.

## 10. Solver result contract

The production solver must return one explicit outcome:

- `ProvenSolution` with an action sequence;
- `ProvenNoSolution` only after exhaustive search of the reachable graph;
- `ResourceLimitReached` when a state, time or memory limit interrupts search;
- `Cancelled` when the caller cancels.

`NoCurrentlyLegalMoves` is a state query, not a solver outcome. Resource exhaustion and cancellation must never be reported as a deadlock or proof of no solution.

The solver must use:

1. the same transfer and release functions as gameplay and hints;
2. a canonical state containing each distinct screw’s location plus released components;
3. stable successors ordered by screw ID and then destination ID;
4. visited-state pruning;
5. explicit state/time limits and cancellation;
6. exhaustive certification for small tutorials;
7. background or incremental execution for searches large enough to block Unity’s main thread.

Symmetry reduction is allowed only after a proof that the merged screws have identical source behavior, coverage, destination behavior and structural consequences.

## 11. Presentation contract

The engine owns definition validation, state, history, transactions, legality, release closure, completion and solver behavior. The presentation owns hit testing, animation, audio, haptics, camera fitting and UI.

The presentation asks the engine for selectable screws and legal destinations. It commits first, then animates. On pause, focus change, disable, undo, restart or load, it cancels transient motion and reconstructs every view from the authoritative snapshot.

## 12. Evidence boundary

The Python verifier exhaustively validates the five design fixtures and one negative control against this rule model. It does not verify a Unity implementation because no production disassembly engine exists yet.
