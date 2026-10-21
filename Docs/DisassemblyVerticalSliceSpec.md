# First Five Disassembly Tutorials

Status: **PHASE 2.1 EXECUTABLY VERIFIED DESIGN FIXTURES — not production-ready**

Canonical fixture data: `Tools/DesignVerification/tutorial_fixtures.json`

Rule set: `phase-2.1-baseboard-relocation-v1`

## 1. Shared fixture rules

- Every listed hole is owned by the persistent baseboard; no tutorial uses a component-owned destination.
- `H_P*` holes are dedicated parking holes.
- `H_*` structural holes secure their declared component while it remains attached and reject incoming screws.
- When a component releases, its empty persistent structural holes become retired legal parking holes.
- A `covered_by` component blocks both the hole and any screw occupying it until that component releases.
- All screws are universally compatible.
- Completion means every listed component is released.
- Completed states are terminal for enumeration.
- Parked screws may be moved to other legal holes.
- Undo is player history and is not part of the canonical solver state.

## 2. T01 — First Release

**New concept:** select one screw, move it to one suitable empty destination, and observe release.

### Initial state

| Item | Type/owner | Initial occupancy or state | Coverage/effect |
|---|---|---|---|
| `C_PLANK` | detachable component | attached | secured by `H_C1` |
| `H_C1` | baseboard structural | `S_1` | secures `C_PLANK`; exposed |
| `H_P1` | baseboard parking | empty | exposed |

### Rules and proof

- Legal initial move: `S_1: H_C1 -> H_P1`.
- Illegal: source equals destination; occupied, covered, component-owned or active structural destination; unknown IDs.
- Release dependency: none. Vacating `H_C1` immediately releases `C_PLANK` and retires `H_C1`.
- Exact solution: `S_1: H_C1 -> H_P1`.
- Proven minimum: 1 transfer.
- Reachable stable states: 2.
- Reachable deadlocks: 0.
- Recovery: undo returns the only completed state to the initial state.

### Acceptance tests

1. Initial accessible screws are exactly `{S_1}` and destinations are exactly `{H_P1}`.
2. The legal transfer releases `C_PLANK` in the same transaction.
3. `H_C1` is a legal empty retired hole only after release.
4. Completion is true immediately after closure, before animation.
5. Every invalid command leaves state and history unchanged.

## 3. T02 — Multiple Fasteners

**New concept:** one plank remains attached until both fasteners leave.

### Initial state

| Item | Type/owner | Initial occupancy or state | Coverage/effect |
|---|---|---|---|
| `C_PLANK` | detachable component | attached | secured by `H_C1`, `H_C2` |
| `H_C1` | baseboard structural | `S_1` | secures `C_PLANK`; exposed |
| `H_C2` | baseboard structural | `S_2` | secures `C_PLANK`; exposed |
| `H_P1` | baseboard parking | empty | exposed |
| `H_P2` | baseboard parking | empty | exposed |

### Rules and proof

- Legal initial moves: either structural screw to either parking hole.
- After one transfer, the moved screw may relocate between the two parking holes; the vacated structural hole remains unavailable because `C_PLANK` is still attached.
- Illegal: placing any screw into `H_C1` or `H_C2` before release; moving the second screw into the occupied parking hole.
- Release dependency: none. `C_PLANK` releases only when both `H_C1` and `H_C2` are empty.
- Exact shortest solution: `S_1: H_C1 -> H_P1`; `S_2: H_C2 -> H_P2`.
- Proven minimum: 2 transfers.
- Reachable stable states: 7.
- Reachable deadlocks: 0.
- Recovery: a redundant relocation of the first stored screw can be reversed or followed by moving the second screw into the newly empty parking hole.

### Acceptance tests

1. Either one-screw move leaves `C_PLANK` attached.
2. A vacated active structural hole rejects parking.
3. The second structural transfer releases the plank and retires both structural holes.
4. Both screw orders and both destination permutations solve.
5. Exhaustive enumeration returns 7 states, minimum 2, no deadlocks.

## 4. T03 — Overlapping Components

**New concept:** a top plank covers a lower screw and a parking hole.

### Initial state

| Item | Type/owner | Initial occupancy or state | Coverage/effect |
|---|---|---|---|
| `C_TOP` | detachable component | attached | secured by `H_TOP`; covers `H_BOTTOM` and `H_P2` |
| `C_BOTTOM` | detachable component | attached | secured by `H_BOTTOM` |
| `H_TOP` | baseboard structural | `S_TOP` | exposed; secures `C_TOP` |
| `H_BOTTOM` | baseboard structural | `S_BOTTOM` | covered by `C_TOP`; secures `C_BOTTOM` |
| `H_P1` | baseboard parking | empty | exposed |
| `H_P2` | baseboard parking | empty | covered by `C_TOP` |

### Rules and proof

- Only legal initial move: `S_TOP: H_TOP -> H_P1`.
- Illegal initially: selecting `S_BOTTOM`; using `H_BOTTOM`, `H_TOP` or covered `H_P2` as a destination.
- Release dependency: none. Moving `S_TOP` releases `C_TOP`, uncovers `S_BOTTOM` and `H_P2`, and retires `H_TOP`.
- Exact shortest solution: `S_TOP: H_TOP -> H_P1`; `S_BOTTOM: H_BOTTOM -> H_P2`.
- The second move may instead use retired `H_TOP`.
- Proven minimum: 2 transfers.
- Reachable stable states: 10.
- Reachable deadlocks: 0.
- Recovery: after the top releases, the parked top screw may be repositioned among legal holes without preventing completion.

### Acceptance tests

1. Initial accessibility is exactly `{S_TOP}` and initial destination is exactly `{H_P1}`.
2. Geometry or collider visibility cannot make `S_BOTTOM` logically accessible early.
3. Top release simultaneously exposes `H_BOTTOM`, exposes `H_P2`, and retires `H_TOP`.
4. Bottom transfer to `H_P2` or `H_TOP` completes.
5. Exhaustive enumeration returns 10 states, minimum 2, no deadlocks.

## 5. T04 — Strategic Scarcity

**New concept:** one initial parking hole is enough because each single-fastener release retires a structural hole for reuse.

### Initial state

| Item | Type/owner | Initial occupancy or state | Coverage/effect |
|---|---|---|---|
| `C_LEFT` | detachable component | attached | secured by `H_LEFT` |
| `C_RIGHT` | detachable component | attached | secured by `H_RIGHT` |
| `H_LEFT` | baseboard structural | `S_LEFT` | exposed; secures `C_LEFT` |
| `H_RIGHT` | baseboard structural | `S_RIGHT` | exposed; secures `C_RIGHT` |
| `H_P1` | baseboard parking | empty | the only initial legal destination |

### Rules and proof

- Legal initial moves: either screw to `H_P1`.
- Whichever component releases first retires its source structural hole, creating the next empty legal destination.
- Illegal: moving the other screw into the still occupied `H_P1`; moving into either active structural hole.
- Release dependencies: none.
- Exact shortest solution: `S_LEFT: H_LEFT -> H_P1`; `S_RIGHT: H_RIGHT -> H_LEFT`.
- The mirrored right-first solution is also valid.
- Proven minimum: 2 transfers.
- Reachable stable states: 9.
- Reachable deadlocks: 0.
- Recovery: either first choice is safe; parked-screw relocation can be undone without losing the retired hole.

### Capacity transition

Before step 1, `E=1`. The transfer consumes `H_P1`, then release retires `H_LEFT`, returning `E=1`. Step 2 consumes that hole and releases `C_RIGHT`, retiring `H_RIGHT`, so completion occurs with `E=1`. Capacity is reused, not invented and no screw disappears.

### Acceptance tests

1. Initial legal destination count is exactly one.
2. Either first structural transfer releases exactly its component.
3. The vacated source changes from active structural to retired parking only after release.
4. The opposite screw can use that retired hole and complete.
5. Exhaustive enumeration returns 9 states, minimum 2, no deadlocks.

## 6. T05 — Recovering a Mistake

**New concept:** a tempting partial removal can consume visible capacity; undo restores a solvable route.

### Initial state

| Item | Type/owner | Initial occupancy or state | Coverage/effect |
|---|---|---|---|
| `C_KEY` | detachable component | attached | secured by `H_KEY`; covers `H_P3` |
| `C_TRIPLE` | detachable component | attached | secured by `H_T1`, `H_T2`, `H_T3` |
| `H_KEY` | baseboard structural | `S_KEY` | exposed; secures `C_KEY` |
| `H_T1` | baseboard structural | `S_T1` | exposed; secures `C_TRIPLE` |
| `H_T2` | baseboard structural | `S_T2` | exposed; secures `C_TRIPLE` |
| `H_T3` | baseboard structural | `S_T3` | exposed; secures `C_TRIPLE` |
| `H_P1` | baseboard parking | empty | exposed |
| `H_P2` | baseboard parking | empty | exposed |
| `H_P3` | baseboard parking | empty | covered by `C_KEY` |

### Rules and proof

- Initially any accessible screw may move to `H_P1` or `H_P2`.
- Moving `S_KEY` releases `C_KEY`, retires `H_KEY`, and uncovers `H_P3`. This raises empty legal capacity from 2 before the move to 3 after closure.
- Moving two `C_TRIPLE` screws into `H_P1` and `H_P2` first leaves `C_TRIPLE` attached, `C_KEY` attached, and no legal destination. That snapshot has no currently legal moves.
- Exact shortest solution: `S_KEY: H_KEY -> H_P1`; `S_T1: H_T1 -> H_KEY`; `S_T2: H_T2 -> H_P2`; `S_T3: H_T3 -> H_P3`.
- Proven minimum: 4 transfers.
- Reachable stable states: 149.
- Reachable deadlocks: 6.
- Shortest deadlock example: `S_T1: H_T1 -> H_P1`; `S_T2: H_T2 -> H_P2`.
- Recovery: undo the second transfer, move `S_KEY` into the freed parking hole, then finish the three-fastener component. Restart is also available. The no-move snapshot is not a solver timeout and is not monetized.

### Acceptance tests

1. `S_KEY` release exposes exactly `H_KEY` and `H_P3` as new empty legal destinations.
2. All three `C_TRIPLE` structural holes must be empty before release.
3. The documented two-move mistake yields no legal move and is not complete.
4. Undo restores the exact one-mistake snapshot and a legal route remains.
5. Solver reports `ProvenSolution` with minimum 4 from the initial state.
6. Exhaustive enumeration returns 149 states and exactly 6 no-move deadlocks.

## 7. Negative control NC01 — Three Transfers Into Two Fixed Holes

This is not a playable tutorial. One component has three occupied structural holes and only two exposed persistent parking holes. It cannot release until all three fasteners move. Structural holes do not retire before release and parked-screw relocation cannot increase capacity.

The verifier exhaustively returns `PROVEN_NO_SOLUTION`, 13 reachable states and 6 no-move deadlocks. This directly rejects the prior three-transfer/two-position proposal when no rack, disappearance or intermediate capacity is present.

## 8. Shared presentation acceptance

For every tutorial, Unity production tests must also prove:

- input uses the actual pointer/touch path while logical legality remains independent of hit testing;
- state commits before animation and completion does not wait for it;
- disable, pause, focus loss, undo, restart and load reconcile from the snapshot;
- all screws, holes and controls remain reachable at 9:16, 9:19.5, 9:21 and 3:4;
- no developer controls appear in the player HUD;
- physical Android evidence is required before claiming touch or visual acceptance.

## 9. Workshop Latch disposition

`DS-INTRO-01 Workshop Latch` is no longer the first tutorial. Preserve it as a later candidate. Before reuse, rewrite its destinations under the selected no-rack, universal-screw rules and certify the resulting JSON fixture with the same verifier and production engine.
