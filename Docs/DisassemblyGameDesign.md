# Screw & Create — Disassembly Game Design

Status: **PHASE 2.1 CORRECTED DESIGN — production implementation requires approval**

Date: 2026-10-18

Product: premium portrait Android screw-removal and object-disassembly puzzle

## 1. Product decision

The launch game starts with a recognizable wooden object. The player transfers accessible screws into legal holes on the persistent baseboard, releases overlapping components, reveals covered screws and holes, and clears all required components. Release is deterministic and immediate in logical state; motion and falling are presentation that follow the committed state.

There is no mandatory rebuilding stage. The existing construction systems and saves remain preserved for a possible later mode.

The launch experience excludes timers, lives, energy, advertisements, purchases, paid recovery, color matching, screw purchases, compulsory racks and arbitrary move penalties. The first five levels use one universal screw family.

## 2. Reconciled option matrix

| Criterion | A. Controlled hybrid | B. Universal physical relocation | C. Staged onboarding | Selected staged baseboard relocation |
|---|---|---|---|---|
| Learnability | Medium: service holes, rack and compatibility arrive together | Medium: “any empty hole” is simple but ambiguous around fastening | High: adds one concept at a time | High: universal screws, no rack, explicit baseboard destination states |
| Strategic depth | High through several interacting constraints | High through occupancy and hole reuse | Grows gradually but does not by itself define a complete rule system | High enough through coverage, occupancy, retired holes and scarce capacity |
| Gameplay coherence | Mechanically coherent but the rack is less physical | Strong if ownership and fastening effects are explicit | Strong progression, incomplete as a rules engine | Strong: every screw remains in a real persistent baseboard hole |
| Implementation effort | High | Medium–high | Depends on the underlying rules | Medium; one transfer action and one release closure |
| Deadlock risk | Medium; rack can mask capacity defects | High if “empty” ignores ownership or fastening | Low early, unknown later | Measurable; authored levels require exhaustive certification |
| Content scalability | High, with authoring burden | High | High for teaching sequence | High; compatibility and optional storage can be added after proof |
| Differentiation | Service/rack logistics | Physical board manipulation | Player-respectful teaching | Physical cause-and-effect, verified fairness and premium presentation |

## 3. Selected launch rule system

Adopt **staged persistent-baseboard relocation** as one coherent launch system:

1. The only gameplay command is `TransferScrew(screwId, sourceHoleId, destinationHoleId)`.
2. A legal destination is an exposed, empty, persistent baseboard hole whose current state permits parking.
3. A dedicated baseboard parking hole always permits parking when exposed and empty.
4. A persistent structural hole permits parking only after the component it secured has released. It is then a retired structural hole and does not re-fasten the component.
5. A structural hole for an attached component never accepts a screw. This avoids re-fastening in the launch rules.
6. A hole owned by a detachable component is never a launch destination and disappears when that component releases.
7. Parked screws may move again between legal destinations, preserving recovery options.
8. All introductory screws are universally compatible. Size or shape compatibility is deferred.
9. There is no compulsory rack. Optional off-board storage is deferred until a level demonstrates a design need that physical holes cannot satisfy.
10. After each committed transfer, the engine resolves all newly eligible component releases in stable component-ID order, derives exposure again, then checks completion and the legal-move set.

This adopts Gemini’s physical relocation idea only after adding ownership, persistence and fastening-state constraints. It adopts Claude’s staged teaching order without treating onboarding advice as a complete rules specification. It removes the original Codex rack and compatibility burden from the introductory system.

## 4. Why the alternatives were adapted

The original controlled hybrid created too many introductory concepts and allowed weak level designs to rely on an abstract rack. Its deterministic transactions, release closure, undo and solver requirements remain valuable.

Universal relocation is coherent only when “empty hole” is qualified. An empty hole on a detachable component can disappear, a covered hole cannot be selected, and a structural hole for an attached component has a fastening effect. The selected system therefore does not allow every empty hole.

Staged onboarding correctly separates movement, multiple fasteners, overlap, scarcity and recovery. It required a complete destination policy and executable fixtures before it could establish feasibility.

## 5. Storage-capacity rule

Let `E(s)` be the number of exposed, empty legal parking destinations in stable state `s`. Moving a screw from a structural hole consumes one destination. If that move releases a component, each exposed empty persistent structural hole assigned to that released component becomes a legal destination, and any parking hole uncovered by the release can also increase capacity.

If a component requires `k` more structural transfers before it releases and no intervening release can expose at least `k` destinations, that route cannot complete. Moving parked screws among legal holes does not increase total capacity by itself.

The negative control `NC01` proves the earlier contradiction: three fasteners, two fixed parking holes, no earlier release and no screw disappearance yields `PROVEN_NO_SOLUTION`, 13 reachable states and 6 deadlocks.

## 6. First five levels

| Level | New concept | Initial empty legal destinations | Minimum transfers | Reachable states | Reachable deadlocks |
|---|---|---:|---:|---:|---:|
| T01 First Release | Select, relocate, release | 1 | 1 | 2 | 0 |
| T02 Multiple Fasteners | All fasteners must leave | 2 | 2 | 7 | 0 |
| T03 Overlapping Components | Coverage and reveal | 1 | 2 | 10 | 0 |
| T04 Strategic Scarcity | Reuse a retired structural hole | 1 | 2 | 9 | 0 |
| T05 Recovering a Mistake | Recognize and undo a capacity deadlock | 2 | 4 | 149 | 6 |

These figures were produced by exhaustive enumeration in `Tools/DesignVerification/verify_disassembly_design.py`. The complete fixtures and acceptance tests are in `DisassemblyVerticalSliceSpec.md`; executable results are in `Phase21FeasibilityReport.md` and `.json`.

Workshop Latch is retained as a later candidate. Its former three-screw/two-service-hole/no-reuse proposal is not a valid first tutorial and must be redesigned under these rules before production use.

## 7. Player experience

1. The player taps an exposed screw.
2. The presentation highlights only legal destination holes returned by the engine.
3. The player taps a destination; the engine validates and commits one atomic transfer.
4. The engine resolves component release immediately and publishes the new snapshot.
5. The presentation animates extraction, travel, insertion and deterministic component motion toward that snapshot.
6. Undo restores the prior snapshot even when animation was interrupted.
7. The level completes when every required component is logically released. Completion does not wait for animation.

## 8. Progression after the introduction

Later authored levels may add deeper overlap, release dependencies, branching capacity choices and recognizable mechanisms. Screw compatibility, optional racks, hinged pieces and efficiency goals require separate approval and solver evidence. Procedural generation remains deferred until authored content establishes reliable difficulty metrics.

## 9. Visual quality gate

The current TEMP primitive Android presentation is a preserved functional reference, not the visual target. After one production gameplay slice is verified, the next major milestone must replace visible primitives with original beveled wood components, recognizable mechanically aligned screws, URP Lit materials, differentiated holes, contact shadows, professional lighting, reliable portrait UI and device-tested touch targets. Final artwork is outside Phase 2.1.

## 10. Approval gate

Production implementation remains blocked until the user approves this exact rule system, the five-level progression, the parallel architecture and the separate disassembly save strategy. Approval does not authorize paid assets, external-generation credits, visual reconstruction or a GitHub push.
