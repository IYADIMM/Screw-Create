# First Disassembly Vertical Slice — Workshop Latch

Status: **PROPOSED, hand-validated against the specification; not implemented or solver-verified**
Level ID: `DS-INTRO-01`
Goal: prove screw transfer, covered fastener exposure, deterministic component release, undo/restart, hints, save/resume, and portrait interaction.

## 1. Why this level

Workshop Latch is deliberately smaller than the existing windmill. Three layered wooden components and three screws isolate the new rules without production-art complexity. The geometry can be authored from simple beveled boards while still reading as a real latch assembly.

## 2. Board contents

### Persistent base

| ID | Type | Description |
|---|---|---|
| `BASE-01` | Baseboard | Does not detach; owns all service holes |
| `H-SVC-A` | Standard service hole | Exposed initially; accepts standard screws |
| `H-SVC-B` | Universal service hole | Covered initially by `C-SIDE`; exposed when `C-SIDE` releases |
| `R-01` | Universal rack slot | Persistent workbench slot, exposed initially |

### Components

| ID | Type/layer | Secured by | Covers | Release dependencies | Release path | Required |
|---|---|---|---|---|---|---|
| `C-TOP` | Top wooden brace, front layer | `H-FAST-1` | `S-CORE` | none | `SlideLeft` | yes |
| `C-SIDE` | Side latch, front layer | `H-FAST-2` | `H-SVC-B` | none | `Drop` | yes |
| `C-CORE` | Central panel, rear layer | `H-FAST-3` | none | `C-TOP` released | `SlideRight` | yes |

### Structural holes and screws

| Hole | Initial screw | Screw compatibility | Structural effect | Initial exposure |
|---|---|---|---|---|
| `H-FAST-1` | `S-TOP` | standard | Secures `C-TOP` | exposed |
| `H-FAST-2` | `S-SIDE` | broad | Secures `C-SIDE` | exposed |
| `H-FAST-3` | `S-CORE` | standard | Secures `C-CORE` | covered by `C-TOP` |

All three structural holes cease securing their component when vacated. Vacated structural holes are not legal storage destinations in the launch rules.

## 3. Initial state

```text
Screw locations:
  S-TOP  -> H-FAST-1
  S-SIDE -> H-FAST-2
  S-CORE -> H-FAST-3

Storage:
  H-SVC-A empty and exposed
  H-SVC-B empty and covered
  R-01    empty and exposed

Components:
  C-TOP  attached
  C-SIDE attached
  C-CORE attached

Accessible screws: S-TOP, S-SIDE
Covered screw: S-CORE
Phase: Active
```

## 4. Allowed actions

- `S-TOP` may move to `H-SVC-A` or `R-01`.
- `S-SIDE` is broad and may move to `R-01`; it may move to `H-SVC-B` only after that hole is exposed, which cannot happen before `C-SIDE` releases. Therefore its initial legal destination is `R-01`.
- `S-CORE` becomes accessible only after `C-TOP` releases and may move to either exposed compatible service hole or rack slot.
- Stored standard screws may be moved between exposed compatible storage positions to recover from an inconvenient choice.

## 5. Invalid actions

- Selecting covered `S-CORE`.
- Moving `S-SIDE` to standard-only `H-SVC-A`.
- Moving any screw to occupied or covered `H-SVC-B`.
- Moving a screw to its source or to any vacated structural hole.
- Tapping wood, the empty area, or a hidden destination as if it were interactive.
- Starting a second transaction while a committed transfer/release animation is reconciling.

Every invalid action leaves the snapshot and undo history unchanged.

## 6. Deterministic release behavior

- Vacating `H-FAST-1` immediately releases `C-TOP`, exposes `S-CORE`, and leaves `C-CORE` attached because `H-FAST-3` is occupied.
- Vacating `H-FAST-2` immediately releases `C-SIDE` and exposes `H-SVC-B`.
- Vacating `H-FAST-3` releases `C-CORE` only after `C-TOP` is already released; this dependency should already be satisfied because `S-CORE` was covered by `C-TOP`.
- Completion occurs when `C-TOP`, `C-SIDE`, and `C-CORE` are all released/cleared.

## 7. Hand-validated solution A

This trace is verified against the written state-transition rules only. It is **not** a solver result because the disassembly engine does not yet exist.

| Step | Action | Release closure | Resulting useful state |
|---:|---|---|---|
| 0 | Initial | none | `H-SVC-A` and `R-01` empty; `S-TOP`, `S-SIDE` accessible |
| 1 | Transfer `S-SIDE`: `H-FAST-2` → `R-01` | `C-SIDE` drops | `H-SVC-B` becomes exposed; rack occupied by broad screw |
| 2 | Transfer `S-TOP`: `H-FAST-1` → `H-SVC-A` | `C-TOP` slides left | `S-CORE` becomes accessible |
| 3 | Transfer `S-CORE`: `H-FAST-3` → `H-SVC-B` | `C-CORE` slides right | All required components released; Complete |

All destination compatibility and occupancy invariants hold after each step.

## 8. Alternative solution B

| Step | Action | Purpose |
|---:|---|---|
| 1 | `S-TOP`: `H-FAST-1` → `H-SVC-A` | Release top and expose core screw |
| 2 | `S-SIDE`: `H-FAST-2` → `R-01` | Release side and expose universal service hole |
| 3 | `S-CORE`: `H-FAST-3` → `H-SVC-B` | Release core and complete |

The first two moves commute, so the tutorial demonstrates meaningful freedom without a dead end.

## 9. Recovery example

If the player moves `S-TOP` to `R-01`, `S-SIDE` temporarily has no destination. This is not declared a loss because the player can transfer `S-TOP` from `R-01` to `H-SVC-A`, freeing `R-01`, then continue. The hint system should recommend that recovery move. Undo provides an additional recovery path.

## 10. Expected interaction sequence

1. Initial instruction: “Move a screw to an open service hole.” Highlight `S-TOP` and `H-SVC-A` without forcing the action.
2. Tapping a screw raises it slightly and highlights legal destinations.
3. Destination tap commits state, then the screw withdraws, travels, and inserts.
4. The unsecured component follows its authored path; new screw/hole exposure receives a brief outline.
5. HUD displays progress `0/3`, `1/3`, etc., plus undo, restart, hint, and pause.
6. Final component clears and a restrained completion panel appears.

## 11. Undo, restart, save/resume

- Undo after any step restores the source screw, destination vacancy, component attachment, coverage, progress, and camera framing.
- Restart returns exactly to step 0 after confirmation.
- Saving after step 1 or 2 must restore the same screw locations, attached/released components, exposed destinations, and legal-action set.
- Pause/focus interruption during an animation reconciles to the already committed snapshot.

## 12. Solver validation plan

After implementation, encode the level as a `DisassemblyDefinition` and require:

- solver reports solved;
- shortest solution length is three transfers;
- both orderings above are accepted;
- the recovery state with `S-TOP` in `R-01` remains solvable;
- `S-SIDE` → `H-SVC-A` is rejected atomically;
- every state reachable within a bounded search is either solvable or has a one-step undo path;
- deterministic action order produces a stable golden solution for test purposes.

Until those tests exist, the level status remains **Proposed / awaiting engine validation**.

## 13. Representative tests

### EditMode

1. Initial accessibility returns only `S-TOP` and `S-SIDE`.
2. Destination query enforces broad/standard compatibility.
3. Moving `S-TOP` releases `C-TOP` and exposes `S-CORE` in one transaction.
4. Moving `S-SIDE` releases `C-SIDE` and exposes `H-SVC-B`.
5. Invalid occupied/covered/incompatible destinations do not mutate canonical state.
6. Undo restores exact prior screw locations, component state, counters, and exposure.
7. Solver accepts solution A and B and the recovery state.
8. Completion ignores final screw storage positions and requires all three components released.

### PlayMode

1. Actual Input System tap selects the correct screw and destination through the UI.
2. Covered `S-CORE` has no reachable hit action until `C-TOP` releases.
3. State commits before extraction animation and survives disable/pause.
4. Release path ends in the snapshot-defined cleared pose.
5. Undo during/after motion restores every home pose.
6. All screws, destinations, buttons, and feedback remain usable at 9:16, 9:19.5, 9:21, and 3:4.
