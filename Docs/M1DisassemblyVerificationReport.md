# Screw & Create — M1 Disassembly Verification Report

Date: 2026-10-19
Status: **COMPLETE — STOPPED AT M1**

## Scope and source protection

- Branch: `codex/m1-disassembly-domain`
- Required base commit: `a1b304faf4ed0137d271b33d096a4dcbd88dd733`
- Runtime assembly: `ScrewAndCreate.Disassembly`
- Runtime namespace and source root: `ScrewAndCreate.Disassembly` under `Assets/_Game/Scripts/Runtime/Disassembly/`
- Test root: `Assets/_Game/Tests/EditMode/Disassembly/`
- No existing Core source, construction rules, construction tests, save schema, scene, artwork or production screenshot is changed.
- `SaveData.cs` is unchanged.
- The pre-existing untracked Unity test scene and `.codex-remote-attachments` directory remain untouched.
- No dependency, paid asset or external-generation credit was used.
- Nothing was pushed or merged.

## Implemented M1 behavior

The new assembly is pure C# and has no Unity engine reference. It defines level, screw, hole and component data; validates identifiers, ownership, persistence, occupancy, fastening, coverage, dependencies and initial invariants; stores authoritative snapshots with distinct screw locations; exposes deterministic legality queries and atomic transfers; resolves iterative release closure; derives completion; and provides one-snapshot-per-transfer undo.

T01 uses the approved IDs and relationships: component `C_PLANK`, structural hole `H_C1`, parking hole `H_P1` and screw `S_1`. The only initial move transfers `S_1` from `H_C1` to `H_P1`, releases `C_PLANK`, increments action and revision once, and completes the level. Undo restores the byte-equivalent logical fields represented by the canonical snapshot key.

Focused rule fixtures verify multiple fasteners, authored coverage, component-owned hole removal, shared structural-hole retirement, no re-fastening, transitive release dependencies, deterministic transitions and exact undo. The test-only T01 enumerator reuses the production legality and transition functions; no production solver was added.

## Guardrail reconciliation

The final guardrail says a persistent structural baseboard hole becomes a parking destination only after **every** component it secures has released. The earlier Phase 2.1 rules describe a structural hole in singular terms as assigned to one component. M1 represents a structural hole's secured components as an explicit set and requires all of them to be released before retirement. This applies the newer guardrail without changing T01 or the existing Phase 2.1 fixtures, which each assign one component per structural hole.

No other conflict was found between the final guardrails and the verified Phase 2.1 rule system.

## Verification evidence

### Static/source review

- Runtime assembly definition sets `noEngineReferences: true`.
- Runtime sources contain no `UnityEngine`, `MonoBehaviour`, camera, collider, physics, animation, input or UI dependency.
- Snapshot and solver keys preserve each screw ID and its location; no symmetry reduction is present.
- Released component IDs only grow during forward transitions. Parking a screw in a retired structural hole does not reattach a component.
- Transfers clone first, validate the result, and update session state/history only after success.
- Release closure repeats in ordinal component-ID order until no component is eligible, including dependency-only transitive releases when occupancy no longer changes.

### Executed tests

Unity version: `6000.6.6f1`.

| Suite | Discovered | Executed | Passed | Failed | Skipped |
|---|---:|---:|---:|---:|---:|
| EditMode after M1 | 53 | 53 | 53 | 0 | 0 |
| Existing PlayMode regression | 16 | 16 | 16 | 0 | 0 |

The EditMode total comprises the existing 27 tests plus 26 new disassembly tests. The final EditMode result is recorded in `TestResults/m1-editmode.xml`; the PlayMode result is recorded in `TestResults/m1-playmode.xml`. Test result files are local evidence and remain excluded from source control.

The Python verifier was executed independently from `Tools/DesignVerification/verify_disassembly_design.py`:

| Fixture | Reachable states | Minimum solution | Reachable deadlocks | Outcome |
|---|---:|---:|---:|---|
| T01 | 2 | 1 | 0 | `PROVEN_SOLUTION` |
| T05 | 149 | 4 | 6 | `PROVEN_SOLUTION` |

The C# T01 enumerator matches the Python result exactly: 2 states, minimum 1, 0 deadlocks.

The six reported T05 deadlocks are **no-legal-action states**: the exhaustive verifier records a non-complete state as a deadlock only when its legal-move list is empty. They are not silent unsolvable states with remaining legal actions. The T05 fixture was not changed.

## Future disassembly save envelope

M1 intentionally does not alter the construction save schema. A future separately versioned disassembly envelope will need:

- envelope/schema version;
- level and immutable definition identity, with a content version or hash;
- current authoritative snapshot: phase, distinct screw ID-to-hole locations, released component IDs, action count and revision;
- optional undo history stored separately from the canonical solver state;
- integrity/checksum data and atomic previous-save recovery;
- explicit migration or rejection behavior when the definition version is incompatible.

## Deferred work

M1 contains no board controller, scene integration, camera, collider, HUD, animation, physics, sound, haptics, hold-to-preview, move-count stars, tutorial redesign, production solver, save migration or Android interaction. Production visuals and real-device interaction remain unimplemented and unverified. M2 and the full T01–T05 campaign have not started.
