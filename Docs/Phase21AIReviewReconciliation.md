# Phase 2.1 AI Review Reconciliation

Status: **COMPLETE FOR DESIGN CORRECTION — production implementation pending**

Evidence order: local source, executed verification, current directive, independent review hypotheses

## 1. Reviews reconciled

### Original Codex recommendation

The Phase 2 specification proposed a controlled hybrid: structural fastener holes, distinct service holes, a compulsory fallback rack, physical compatibility groups and deterministic release closure. Workshop Latch used three structural screws, two service holes and one rack.

### Gemini recommendation

Gemini favored physical relocation into compatible empty holes on persistent structures, including reuse of vacated structural holes where valid. This improved physical coherence but needed explicit ownership, coverage and fastening-effect rules; “any empty hole” would be unsafe.

### Claude provisional recommendation

Claude favored staged onboarding: movement, multiple fasteners, overlap, scarcity and recovery, while deferring compatibility and dedicated rack mechanics. Its source-related claims were treated as hypotheses. Earlier Claude material focused mainly on the preserved Windmill construction graybox and did not prove the new relocation rules or tutorial solvability.

## 2. Source findings

| Source path and heading | Verified finding | Design consequence |
|---|---|---|
| `Assets/_Game/Scripts/Runtime/Core/PuzzleModel.cs` — puzzle definitions/snapshot | Current state tracks removed screws, released/tray/assembled parts and sockets; it has no screw-to-hole occupancy model | Use a separate disassembly domain |
| `Assets/_Game/Scripts/Runtime/Core/PuzzleEngine.cs` — `PuzzleEngine` | Stateful mutable engine owns snapshot history and implements remove/assemble transactions | Describe current architecture accurately; preserve it for construction |
| `Assets/_Game/Scripts/Runtime/Core/PuzzleSolver.cs` — `PuzzleSolver` | Solver uses current engine transitions, canonical keys, visited pruning and a state budget | Retain shared-transition search; add explicit outcomes and cancellation for disassembly |
| `Assets/_Game/Scripts/Runtime/Core/SaveData.cs` — `GameSaveData`/migrations | Version-2 save contains the construction snapshot and v1 migration | Do not reinterpret or modify it for the first slice; use a separate envelope |
| `Assets/_Game/Scripts/Runtime/Presentation/WindmillVerticalSliceController.cs` — controller | Current presentation implements preserved Windmill construction behavior | Keep dormant for possible construction; new disassembly presentation is separate |
| `Assets/_Game/Scripts/Runtime/Presentation/PlayfieldFitter.cs` and `SafeAreaFitter.cs` | Portrait fit and safe-area utilities are reusable presentation concerns | Reuse only at presentation boundary; never derive logical legality from them |

No source evidence showed that the current production engine already supports screw relocation, baseboard-hole ownership or retired structural-hole storage.

## 3. Findings accepted

- Accept Gemini’s persistent physical-hole relocation as the basis for coherent screw storage.
- Accept Claude’s five-stage teaching sequence and deferral of compatibility and racks.
- Retain Codex’s atomic transfer, deterministic release closure, commit-before-animation, exact undo, solver validation and state-based presentation reconciliation.
- Accept reuse of a baseboard structural hole only after its component releases and the hole’s fastening role is retired.
- Require executable exhaustive certification for small tutorials.
- Preserve the construction engine, saves, Windmill scene and existing tests.

## 4. Findings adapted

- “Compatible empty hole” becomes “exposed, empty, persistent baseboard hole currently in parking state.” This adds ownership, coverage, persistence and fastening-state checks.
- Vacated structural holes are not immediately usable. A hole remains unavailable while its component remains attached and becomes a non-fastening destination only after release.
- Parked screws may move again, but relocation cannot create capacity by itself.
- Universal compatibility applies only to the first five levels; later physical compatibility remains an optional, separately approved mechanic.
- Claude’s recovery lesson is implemented with an honest capacity deadlock and undo, without a booster or forced failure economy.

## 5. Findings rejected or deferred

- Reject a compulsory rack at launch. It adds an abstract storage system and can conceal bad capacity design.
- Reject compatibility restrictions in the first five tutorials. They add another rule before the base loop is learned.
- Reject the rule “every empty hole is legal.” Component-owned, covered and active structural holes have different behavior.
- Reject launch re-fastening. Inserting into a structural hole for an attached component would need new secure/reattach semantics and a larger state space.
- Reject screw disappearance and hidden storage as solutions to capacity contradictions.
- Reject Workshop Latch as tutorial one. Preserve it for later redesign.
- Defer final art, asset acquisition, scene rebuilding, monetization, construction integration and procedural generation.

## 6. Contradictions identified

The earlier Workshop Latch avoided a three-transfer/two-hole contradiction only by supplying a rack while simultaneously making the rack central to the first tutorial. Claude’s cited three-fastener/two-permanent-position/no-return/no-removal proposal is mathematically impossible. Neither prose nor undo changes that capacity invariant.

The earlier rules also mixed logical accessibility with view state, requiring active views, interaction layers, modal state and animation state. Phase 2.1 separates logical accessibility from whether presentation currently accepts a pointer.

Calling the future engine stateless while assigning it mutable state and undo history was inconsistent. The corrected architecture uses pure transition functions inside a stateful session that owns current state and history.

## 7. Executable verification

Command executed with the bundled Python runtime:

```text
python Tools/DesignVerification/verify_disassembly_design.py --json-out Docs/Phase21FeasibilityReport.json --markdown-out Docs/Phase21FeasibilityReport.md
```

| Fixture | Result | Reachable states | Minimum moves | Deadlocks |
|---|---|---:|---:|---:|
| T01 First Release | `PROVEN_SOLUTION` | 2 | 1 | 0 |
| T02 Multiple Fasteners | `PROVEN_SOLUTION` | 7 | 2 | 0 |
| T03 Overlapping Components | `PROVEN_SOLUTION` | 10 | 2 | 0 |
| T04 Strategic Scarcity | `PROVEN_SOLUTION` | 9 | 2 | 0 |
| T05 Recovering a Mistake | `PROVEN_SOLUTION` | 149 | 4 | 6 |
| NC01 Three Transfers Into Two Fixed Holes | `PROVEN_NO_SOLUTION` | 13 | n/a | 6 |

All enumerations completed below the 100,000-state limit. T05’s deadlocks are reachable player states with no current legal move; undo remains a separate recovery property. NC01 demonstrates the missing-capacity failure directly.

A separate low-budget check ran T05 with a one-state limit and returned `RESOURCE_LIMIT_REACHED`, `exhaustive=false`, rather than `ProvenNoSolution`. This verifies the design tool keeps resource exhaustion distinct from deadlock proof.

## 8. Evidence limits

The verifier is an independent design model with no external dependencies. It proves its JSON fixtures under its implemented rules. It does not prove a future C# engine matches those rules, does not verify Unity presentation or Android input, and does not establish that the levels are fun. Those remain production milestones.

The existing Unity suites were rerun in Unity `6000.6.6f1` after the documentation and verifier changes: EditMode passed 27/27 and PlayMode passed 16/16, with zero failures, skips or inconclusive tests. No Unity source, tracked scene, package or project setting changed, and the protected Core diff remained empty. These suites cover the preserved construction baseline; they do not test the proposed disassembly rules.

## 9. Remaining approval decisions

The user must approve before production implementation:

1. staged persistent-baseboard relocation as the launch rules;
2. no rack, no compatibility and no re-fastening in the first five levels;
3. the five tutorial fixtures and T01 as the first implemented slice;
4. the parallel disassembly domain and separate save envelope;
5. the M1 scope in `Phase2ImplementationPlan.md`.

Separate future approval remains required for paid assets, external-generation credits, asset downloads, visual reconstruction, GitHub push or merge.
