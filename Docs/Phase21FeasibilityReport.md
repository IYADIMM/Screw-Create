# Phase 2.1 Disassembly Design Feasibility Report

Status: **EXECUTED DESIGN VERIFICATION — not production gameplay verification**

The dependency-free verifier exhaustively enumerated every stable logical state reachable in five tutorial fixtures and one deliberately unsatisfiable capacity control under the documented Phase 2.1 transfer and release rules. Completed states are terminal. Screw identities are preserved in canonical states.

Reproduction command: `python Tools/DesignVerification/verify_disassembly_design.py --json-out Docs/Phase21FeasibilityReport.json --markdown-out Docs/Phase21FeasibilityReport.md`

## Results

| Fixture | Category | Outcome | Exhaustive | Reachable states | Minimum moves | Reachable deadlocks |
|---|---|---|---:|---:|---:|---:|
| T01 — First Release | tutorial | PROVEN_SOLUTION | yes | 2 | 1 | 0 |
| T02 — Multiple Fasteners | tutorial | PROVEN_SOLUTION | yes | 7 | 2 | 0 |
| T03 — Overlapping Components | tutorial | PROVEN_SOLUTION | yes | 10 | 2 | 0 |
| T04 — Strategic Scarcity | tutorial | PROVEN_SOLUTION | yes | 9 | 2 | 0 |
| T05 — Recovering a Mistake | tutorial | PROVEN_SOLUTION | yes | 149 | 4 | 6 |
| NC01 — Three Transfers Into Two Fixed Holes | negative_control | PROVEN_NO_SOLUTION | yes | 13 | n/a | 6 |

## Per-tutorial proof

### T01 — First Release

Concept: Move one accessible screw to one persistent baseboard parking hole and observe immediate logical release.

Outcome: **PROVEN_SOLUTION**. Enumeration was exhaustive. The graph contains 2 reachable stable states and 0 reachable non-complete states with no currently legal moves.

Shortest legal solution:

1. `S_1: H_C1 -> H_P1`

Capacity transition on that solution:

- Step 1: `S_1: H_C1 -> H_P1`; empty legal destinations 1 -> 1; released C_PLANK; newly legal empty holes H_C1.

### T02 — Multiple Fasteners

Concept: A component remains attached until both of its structural holes are empty.

Outcome: **PROVEN_SOLUTION**. Enumeration was exhaustive. The graph contains 7 reachable stable states and 0 reachable non-complete states with no currently legal moves.

Shortest legal solution:

1. `S_1: H_C1 -> H_P1`
2. `S_2: H_C2 -> H_P2`

Capacity transition on that solution:

- Step 1: `S_1: H_C1 -> H_P1`; empty legal destinations 2 -> 1; released none; newly legal empty holes none.
- Step 2: `S_2: H_C2 -> H_P2`; empty legal destinations 1 -> 2; released C_PLANK; newly legal empty holes H_C1, H_C2.

### T03 — Overlapping Components

Concept: Release the top plank to expose the lower screw and an additional parking hole.

Outcome: **PROVEN_SOLUTION**. Enumeration was exhaustive. The graph contains 10 reachable stable states and 0 reachable non-complete states with no currently legal moves.

Shortest legal solution:

1. `S_TOP: H_TOP -> H_P1`
2. `S_BOTTOM: H_BOTTOM -> H_P2`

Capacity transition on that solution:

- Step 1: `S_TOP: H_TOP -> H_P1`; empty legal destinations 1 -> 2; released C_TOP; newly legal empty holes H_P2, H_TOP.
- Step 2: `S_BOTTOM: H_BOTTOM -> H_P2`; empty legal destinations 2 -> 2; released C_BOTTOM; newly legal empty holes H_BOTTOM.

### T04 — Strategic Scarcity

Concept: One initially available parking hole is reused through retired persistent structural holes.

Outcome: **PROVEN_SOLUTION**. Enumeration was exhaustive. The graph contains 9 reachable stable states and 0 reachable non-complete states with no currently legal moves.

Shortest legal solution:

1. `S_LEFT: H_LEFT -> H_P1`
2. `S_RIGHT: H_RIGHT -> H_LEFT`

Capacity transition on that solution:

- Step 1: `S_LEFT: H_LEFT -> H_P1`; empty legal destinations 1 -> 1; released C_LEFT; newly legal empty holes H_LEFT.
- Step 2: `S_RIGHT: H_RIGHT -> H_LEFT`; empty legal destinations 1 -> 1; released C_RIGHT; newly legal empty holes H_RIGHT.

### T05 — Recovering a Mistake

Concept: A tempting partial removal consumes all visible capacity; undo restores a state from which the key plank can expose enough capacity.

Outcome: **PROVEN_SOLUTION**. Enumeration was exhaustive. The graph contains 149 reachable stable states and 6 reachable non-complete states with no currently legal moves.

Shortest legal solution:

1. `S_KEY: H_KEY -> H_P1`
2. `S_T1: H_T1 -> H_KEY`
3. `S_T2: H_T2 -> H_P2`
4. `S_T3: H_T3 -> H_P3`

Capacity transition on that solution:

- Step 1: `S_KEY: H_KEY -> H_P1`; empty legal destinations 2 -> 3; released C_KEY; newly legal empty holes H_KEY, H_P3.
- Step 2: `S_T1: H_T1 -> H_KEY`; empty legal destinations 3 -> 2; released none; newly legal empty holes none.
- Step 3: `S_T2: H_T2 -> H_P2`; empty legal destinations 2 -> 1; released none; newly legal empty holes none.
- Step 4: `S_T3: H_T3 -> H_P3`; empty legal destinations 1 -> 3; released C_TRIPLE; newly legal empty holes H_T1, H_T2, H_T3.

Shortest recorded deadlock path:

1. `S_T1: H_T1 -> H_P1`
2. `S_T2: H_T2 -> H_P2`
The snapshot has no currently legal move. Player undo remains a separate recovery property.

### NC01 — Three Transfers Into Two Fixed Holes

Concept: Prove the storage-capacity contradiction when three fasteners must move before release but only two persistent parking holes exist.

Outcome: **PROVEN_NO_SOLUTION**. Enumeration was exhaustive. The graph contains 13 reachable stable states and 6 reachable non-complete states with no currently legal moves.

Shortest legal solution:

No solution was found.

Shortest recorded deadlock path:

1. `S_1: H_C1 -> H_P1`
2. `S_2: H_C2 -> H_P2`
The snapshot has no currently legal move. Player undo remains a separate recovery property.

## Interpretation limits

This report proves that the five tutorial fixtures are solvable and the negative control is unsolvable under this verifier's documented rules, with every reachable stable state enumerated within the configured budget. It does not prove that a future Unity implementation matches these rules, that the tutorials are fun, that their presentation is readable, or that an Android build works. Production acceptance requires shared-rule engine tests, Unity integration tests, playtesting and device evidence.
