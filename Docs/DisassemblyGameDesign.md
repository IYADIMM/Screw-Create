# Screw & Create — Disassembly Game Design

Status: **PROPOSED — approval required before implementation**
Date: 2026-10-18
Product direction: premium portrait Android screw-removal and object-disassembly puzzle

## 1. Product definition

The primary game starts with an assembled wooden object. The player studies its layers, moves accessible screws to legal destinations, releases structural components, and clears the required object. Completion ends at disassembly; rebuilding is not required.

The product promise is a readable mechanical puzzle with deterministic rules, satisfying material response, reversible mistakes, and levels that are proven solvable. It excludes assumed timers, lives, energy, color sorting, paid boosters, forced advertisements, and arbitrary move penalties.

Current-code facts are identified as **VERIFIED**. New rules in this document are **PROPOSED** until implemented and tested.

## 2. Reference-pattern review

The two cited store listings show common category patterns: visible layered boards, exposed and covered fasteners, order-dependent removal, components that shift or fall, small portrait HUDs, undo/tool controls, and escalating visual density. One listing explicitly describes choosing bolts in order so boards shift, fall, and expose paths; the other combines screw planning with match-three storage and meta progression. Store reviews also repeatedly criticize forced ads, premature “no moves” states, randomness, and pay-or-restart pressure.

Screw & Create will retain the readable mechanical cause-and-effect while rejecting copied layouts, branding, character art, match-three/color-box rules, forced interruptions, and monetized recovery. Reference observations were taken from the official [Wood Screw listing](https://play.google.com/store/apps/details?id=com.game.screwpuzzle) and [Woodle Screw listing](https://play.google.com/store/apps/details?id=com.wood.bolt.wordle.screw.nuts.puzzle), accessed 2026-10-18.

## 3. Rule-system comparison

| Criterion | A. Direct removal | B. Relocation/hole management | C. Controlled hybrid disassembly |
|---|---|---|---|
| Core action | Remove an eligible screw from play | Move screws between holes | Transfer one accessible screw from a structural fastener to a compatible service hole or rack slot, then resolve releases |
| Strategic depth | Low–medium; depends heavily on authored dependencies | High; destination scarcity creates planning | High; combines structural order with visible, bounded destination planning |
| New-player clarity | Highest | Medium | Medium-high with a short tutorial and explicit destination highlights |
| Existing-engine compatibility | Highest: current engine already removes screws | Low: no screw-location or hole state exists | Medium: reuses dependency/undo/solver patterns but needs a new disassembly domain |
| Solvability verification | Simple monotonic search | Larger cyclic search | Bounded cyclic search with deterministic release closure |
| Trivial-game risk | High if every legal screw can simply disappear | Low | Low if hole compatibility and exposure are authored carefully |
| Unwinnable-state risk | Low unless structural motion can trap access | High | Controlled; required pre-publication solver proof and recoverable tutorial levels |
| Touch complexity | Low | Medium | Medium; tap screw, then highlighted destination |
| Generation | Easy but may feel repetitive | Difficult | Feasible after authored rules and solver are stable |
| Level flexibility | Moderate | High | Highest: layers, covers, hole exposure, compatibility, and release motion |
| Animation burden | Extraction and release | Extraction, travel, insertion | Extraction, transfer/insertion, release, and reconciliation |
| Mobile usability | Strong | Requires generous holes and selection affordances | Strong if destination count is small and targets meet touch-size gates |
| Production effort | Lowest | High | High but incremental and reusable |
| Originality opportunity | Mostly presentation | Limited by crowded market pattern | Stronger through physical service-hole logic, structural consequences, and premium fairness |

## 4. Recommendation

Adopt **C. Controlled hybrid disassembly** as one coherent rule system.

Every gameplay action is an atomic `TransferScrew` operation. The player selects an accessible screw seated in a structural fastener, service hole, or rack slot, then selects one compatible empty destination. The engine commits the transfer, evaluates which components have lost all structural fastening, resolves a deterministic release closure, and exposes any newly uncovered screws or service holes.

The launch rule set uses:

- Structural fastener holes that secure declared components.
- Persistent, visibly distinct service holes on the baseboard that store screws without securing components.
- A small persistent workbench rack as a fallback destination.
- Physical compatibility cues such as shaft diameter or head engraving, not color matching.
- Deterministic component-release dependencies and presentation-only fall/slide paths.
- Undo, restart, hints, save/resume, and solver validation.

The existing three-slot **part** inventory is removed from the current player presentation. Its code and tests remain preserved for the deferred construction concept. A future screw rack may reuse its layout concepts but must have disassembly-specific state and semantics.

## 5. Essential, optional, and deferred mechanics

### Launch-essential

- Accessible/covered screw rules.
- Atomic screw transfer to service holes or rack slots.
- Destination compatibility and occupancy.
- Structural component release and layer exposure.
- Deterministic state, undo, restart, hint, save/resume, completion, deadlock detection, and solver proof.
- Clear portrait framing, reliable touch targets, and state-to-presentation reconciliation.

### Optional after the first validated slice

- Multiple physical screw families with shape-based cues.
- Hinged or guided components with authored release paths.
- Optional efficiency stars based on undo-free or near-minimum solutions.
- Accessibility modes that reinforce physical compatibility with patterns and labels.

### Deferred

- Procedural level generation until authored disassembly rules and solver metrics are stable.
- Volatile storage holes on falling components.
- Simultaneous physics simulation as puzzle authority.
- Daily challenges, meta progression, cosmetic collections, and construction mode.
- Monetization, ads, IAP, currencies, timers, lives, and paid recovery.

## 6. Player experience loop

1. Read the assembled object and highlighted tutorial cue when applicable.
2. Tap an accessible screw; legal destinations illuminate and the selected fastener receives immediate visual/audio feedback.
3. Tap a destination; the logical transfer commits once.
4. Watch the screw withdraw, travel, and settle. Newly unsecured components slide, hinge, or fall along authored paths.
5. Reassess exposed screws and holes. Use undo or restart without penalty when needed.
6. Clear every required detachable component to complete the level.

Logical state always precedes animation. Animation may be interrupted and rebuilt from the authoritative snapshot.

## 7. Progression and difficulty

| Band | Learning focus | Typical structure |
|---|---|---|
| Introductory | Select, transfer, release, undo | One layer, universal screws, two visible destinations, no dead ends |
| Developing | Covered screws and newly exposed service holes | Two layers, shallow dependencies, recoverable destination choices |
| Intermediate | Compatibility and branching | Three or more layers, asymmetric screws, several useful solution paths |
| Advanced | Coupled structural choices | Deep dependencies, constrained destinations, deliberate recoverable traps, recognizable machines |

Difficulty must be measured with several values rather than screw count alone:

- legal opening actions and average legal actions per state;
- dependency and cover depth;
- number and timing of exposed service holes;
- screw-to-destination compatibility tightness;
- shortest solution length and solver states visited;
- number of distinct useful solution paths;
- distance from a recoverable mistake to progress;
- simultaneous visual layers and touch-target separation.

No numerical difficulty label is considered accurate until measured from implemented rules and playtests.

## 8. Product differentiators

| Rank | Differentiator | Player benefit | Cost | Complexity | Reuse | Launch necessity |
|---:|---|---|---|---|---|---|
| 1 | Solver-verified fairness and honest deadlock detection | Trust; no fabricated losses | Medium | High | Very high | Required |
| 2 | Structural consequences with authored release paths | Every screw action feels meaningful | Medium | Medium | Very high | Required |
| 3 | Premium mechanical feel: local-axis withdrawal, insertion, wood response | Tactile satisfaction | Medium | Medium | Very high | Required |
| 4 | Clear shape-based compatibility and accessibility reinforcement | Reduces guessing and color dependence | Low–medium | Medium | High | Required |
| 5 | Recognizable objects across families | Variety and emotional payoff | High | Medium | High | Required after slice |
| 6 | Fast portrait UX with no forced interruption | Comfortable short sessions | Medium | Medium | Very high | Required |
| 7 | Optional efficiency mastery | Replay value without punishment | Low | Low–medium | High | Optional |
| 8 | Large procedural catalogue | Longevity | High | High | High | Deferred |

Feature count is not a differentiator. The launch slice should prove the first six qualities before adding breadth.

## 9. Visual and audio direction

- Original warm workbench setting with clean depth separation.
- Real-scale wooden layers with bevels, readable contact shadows, and physically plausible fasteners.
- Brass and steel variants used for material storytelling rather than color sorting.
- Compact HUD: level/progress, undo, restart, hint, and pause. No part tray or assembly blueprint in the primary mode.
- Short extraction, insertion, release, and completion sounds; restrained haptics with a reduced-motion and haptics toggle.
- Detached components move along authored paths; physics may embellish after state commitment but never decides puzzle outcome.

## 10. Acceptance gate

This design becomes implementation-authorized only after approval of:

1. the controlled-hybrid rule system;
2. service-hole and rack semantics;
3. the first vertical-slice level;
4. the proposed parallel disassembly architecture;
5. the rule that the part inventory and assembly HUD are dormant in the primary game.
