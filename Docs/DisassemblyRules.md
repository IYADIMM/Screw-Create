# Screw & Create — Disassembly Rules

Status: **PROPOSED — not implemented**
Rule set: Controlled hybrid disassembly

## 1. Authoritative state

The logical state contains only deterministic puzzle data:

- `ScrewLocation[screwId]`: one structural hole, one service hole, one rack slot, or `RemovedWithReleasedComponent` for a future explicitly authored exception.
- `ComponentState[componentId]`: `Attached`, `Released`, or `Cleared`.
- `ExposedScrewIds` and `ExposedHoleIds`, derived from attached component layers rather than stored independently where practical.
- `ActionCount`, `HintCount`, `Revision`, and an undo stack of complete snapshots.
- `Phase`: `Active`, `Complete`, or `FailedDeadlock`.

Transforms, animation progress, particle state, selection highlight, and sound are presentation data and never determine legality or completion.

## 2. Definitions

- **Structural hole:** an authored fastener site. An occupied structural hole secures its declared attached components.
- **Service hole:** a persistent baseboard hole used only to park a screw. It secures no component and has an authored compatibility set.
- **Rack slot:** a persistent off-board storage location, also with compatibility rules. The first slice uses one slot.
- **Accessible screw:** a screw whose head is exposed, whose location is selectable, and whose containing object is not already cleared.
- **Covered:** a screw or hole hidden by at least one attached covering component.
- **Release dependency:** an authored component that must already be released or cleared before another component may release.
- **Release closure:** the deterministic repeated release of every newly eligible component after a committed screw transfer.

## 3. Puzzle initialization

1. Validate unique IDs and every reference.
2. Validate that each screw has exactly one initial location.
3. Validate one occupant per hole or rack slot.
4. Validate destination compatibility and nonempty destination sets.
5. Validate acyclic cover and release-dependency graphs for the launch rule set.
6. Build the initial snapshot from authored data.
7. Derive exposure from visual layers and attached covering components.
8. Run the solver from the initial snapshot before the level is accepted for play.
9. Reconcile every view from the accepted snapshot.

## 4. Screw accessibility

A screw is selectable when all conditions hold:

1. The puzzle phase is `Active`.
2. The screw occupies a structural hole, service hole, or rack slot.
3. Its head is not covered by an attached component.
4. Its view is active, on the interaction layer, and inside the fitted playfield or rack region.
5. No modal UI blocks the pointer.
6. No screw-transfer animation is already resolving.

Accessibility is recalculated after every release closure, undo, restart, load, and lifecycle reconciliation.

## 5. Covered or blocked screws

- An attached covering component blocks every screw ID listed in its cover relationship.
- A covered screw cannot be selected even when its collider is geometrically reachable.
- A screw can also be blocked because no compatible empty destination exists. It remains selectable for explanation/highlight, but committing a move is unavailable.
- Invalid taps produce concise feedback and no state mutation.

## 6. Screw selection

Selecting an accessible screw creates a transient presentation selection. The engine returns all currently legal destinations. Those destinations illuminate using shape/pattern as well as color. Tapping the selected screw again cancels selection without consuming an action.

## 7. Legal screw actions

The sole launch action is:

`TransferScrew(screwId, sourceLocationId, destinationLocationId)`

It is legal only when:

- the source still contains that screw;
- the screw is accessible;
- the destination is a different service hole or rack slot;
- the destination is exposed, persistent, and empty;
- the destination accepts the screw's authored physical compatibility group;
- the destination remains present after the deterministic release closure caused by vacating the source;
- the resulting snapshot passes invariants.

The engine validates the complete transaction before mutation. On success it pushes one undo snapshot, changes the screw location, increments action/revision counters once, resolves releases, and checks completion/deadlock. Visual animation begins only after this commit.

The first release does not allow placing a screw into another structural hole. That capability is deferred because it changes which components are secured and substantially enlarges the solver/state model.

## 8. Invalid screw actions

The following reject atomically:

- unknown or covered screw;
- stale source location;
- occupied, covered, incompatible, nonpersistent, or identical destination;
- source or destination belonging to a cleared component;
- move during modal pause or active transaction;
- move that would violate a state invariant.

Rejected actions do not change counters, undo history, component state, screw positions, or selection except for presentation feedback.

## 9. Free holes

- Only authored service holes are legal launch destinations.
- Structural holes vacated by screws remain visually present but are not storage destinations in the launch rules.
- Service holes belong to the persistent baseboard in the first slice; they never disappear during release.
- A covered service hole becomes available immediately after its last covering component releases.

## 10. Screw storage and relocation

- A service hole stores one compatible screw.
- A rack slot stores one compatible screw and remains accessible throughout the level.
- A stored screw may be moved again to another legal service hole or rack slot. This supports recovery and lets the solver escape poor earlier placement.
- Storage has no match-three clearing, color box, automatic deletion, payment gate, or random behavior.
- Screw location is always explicit; there is no transient logical “in hand” state.

## 11. Wooden component dependencies

Each component declares:

- structural hole IDs that secure it;
- component IDs that cover it or its fasteners;
- release dependencies that must already be released/cleared;
- one authored visual release mode and path identifier;
- whether it is required for level completion.

Dependencies affect logical eligibility. Geometry and collision do not silently create rules.

## 12. Structural support

A component remains attached while any declared structural hole contains a screw, or while any release dependency is not satisfied. The launch system does not simulate weight, torque, friction, or center of mass as puzzle authority.

Support rules may later add authored hinges or rails, but each must resolve deterministically from data and be represented in the solver.

## 13. Component release

After a valid transfer, repeatedly:

1. Find attached components with zero occupied securing holes and satisfied release dependencies.
2. Order simultaneous releases by authored release order, then stable component ID.
3. Mark them `Released`.
4. Recalculate covers and exposure.
5. Continue until no further component becomes eligible.

The resulting release closure is part of the same player action and undo snapshot.

## 14. Falling or sliding movement

- Every component uses an authored presentation path: `Drop`, `SlideLeft`, `SlideRight`, `HingeClockwise`, `HingeCounterClockwise`, or a future validated path.
- The engine records only the logical release result and path ID.
- Presentation animates from home pose to cleared pose. At animation end the component becomes `Cleared` visually.
- Interruption reconciles directly to the snapshot's required pose/state.
- Physics may add noninteractive secondary motion after the deterministic path, but cannot block or enable a move.

## 15. Layer exposure

Exposure is derived after each release closure. Removing a covering component may expose structural screws and service holes. A lower component does not become selectable merely because its renderer is visible; its authored cover set must be clear.

## 16. Undo

- Undo restores the complete snapshot before the last successful transfer, including every screw location and component state.
- One successful transfer plus its entire release closure is one undo step.
- Hint count may remain cumulative, matching current product policy.
- Ongoing animations stop and presentation reconciles to the restored snapshot.
- Invalid actions create no undo entry.

## 17. Restart

Restart requires confirmation, restores the authored initial snapshot, clears undo and transient presentation state, and preserves settings. It does not regenerate a different puzzle.

## 18. Hint generation

The solver computes a path from the current snapshot. A hint identifies the next screw and one legal destination from that path. Presentation highlights both and gives a short reason. A hint never mutates puzzle state and never invents a move when no solver path exists.

## 19. Save and resume

Save data must include level identity, disassembly rule/version identity, complete snapshot, and settings. Save after each successful transfer and on pause/focus loss. Load validates IDs, occupancy, compatibility, component closure, counters, and completion/deadlock consistency before replacing active state. Presentation is rebuilt from the restored snapshot.

## 20. Completion detection

The level completes when every component marked `RequiredForCompletion` is released or cleared. Screw locations, unused service holes, remaining baseboard, and optional decorative components do not prevent completion. There is no assembly requirement.

## 21. Unwinnable-state detection

An active state is a deadlock when the solver proves that no completion path exists within the validated state space. “No immediately legal move” is sufficient only when no animation or release closure is pending. The game may show restart and undo; it must not require payment or an advertisement to continue.

The first tutorial slice must be authored so every reachable state is recoverable by moving stored screws or undoing. Later levels may allow solver-proven dead ends only when the causal constraint is readable and undo remains available.

## 22. Solvability verification

- Use deterministic graph search over canonical state: screw locations plus component states.
- Exclude presentation values and action counters from the solver key.
- Generate successors in stable screw-ID and destination-ID order.
- Apply the same engine transaction and release-closure code used by gameplay.
- Enforce a per-profile state budget and distinguish `Unsolved` from `ResourceLimitReached`.
- Store at least one solution path and metrics: states visited, shortest known length, opening legal moves, and reachable deadlocks.
- Reject authored or generated levels that lack a solver proof.

## 23. State invariants

1. Every screw has exactly one valid location.
2. Every hole/rack slot contains at most one screw.
3. Every occupied destination accepts its screw.
4. Cleared components do not cover objects or provide storage destinations.
5. A released/cleared component has no occupied structural securing hole.
6. Every attached component with no occupied securing hole is blocked only by an explicit release dependency; otherwise release closure must process it.
7. `Complete` exactly matches the completion predicate.
8. `FailedDeadlock` implies incomplete and solver-proven unsolvable.
9. Successful actions increment action/revision once and create one undo entry.
10. Presentation state never changes these invariants.

## 24. Optional rule proposals

These are excluded from launch rules until separately approved:

- structural-hole relocation that can re-secure components;
- storage holes carried by moving components;
- one-way or consumable fasteners;
- timed challenges or efficiency medals;
- procedural physics constraints;
- cosmetic screw families that do not change compatibility.
