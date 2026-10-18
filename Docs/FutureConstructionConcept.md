# Future Construction Concept Preservation

Status: **DEFERRED — preserved, not player-facing**

## 1. Purpose

The mixed recover → inventory → build concept is no longer the primary game. Its implemented capabilities remain useful for a possible later construction mode and must not be deleted, silently repurposed, or exposed as an unfinished menu option.

## 2. Existing reusable systems

| Capability | Current source | Preservation value |
|---|---|---|
| Part definitions and compatibility groups | `PuzzleModel.cs` | Typed part/socket compatibility |
| Target sockets and allowed part IDs | `PuzzleModel.cs` | Authored placement targets |
| Rotation sets and tolerance | `PuzzleModel.cs`, `PuzzleEngine.cs` | Snap validation and accessible discrete rotation |
| Assembly dependencies | Part and socket definitions | Ordered construction sequences |
| Tray capacity/order | `PuzzleEngine` and `PuzzleSnapshot` | Small construction inventory |
| Mixed remove/assemble solver | `PuzzleSolver.cs` | Verified interleaving and hints |
| Exact undo and snapshot restore | `PuzzleEngine.cs` | Reliable reversible construction actions |
| Save schema and migration | `SaveData.cs` | Active construction-state persistence |
| Part/socket presentation | `PartView.cs`, `SocketView.cs` | Existing tray, blueprint, drag, rotate, and snap prototype |
| Phase 1 tray/input fixes | `WindmillVerticalSliceController.cs` | Visible/selectable inventory and pointer path |
| Construction tests | `PuzzleEngineTests`, `MixedWorkbenchTests`, `WindmillVerticalSlicePlayModeTests` | Regression contract for preserved behavior |

## 3. Current limitations

- Presentation and level data are hard-coded in `WindmillVerticalSliceController.CreateDefinition()`.
- Parts share TEMP primitive geometry and Unlit materials.
- Construction completion is coupled to the current mixed-workbench engine.
- Tray capacity is mechanically central and can force remove/assemble interleaving.
- Drag/rotate/snap behavior has Editor PlayMode evidence but no physical-device evidence.
- Save data has one active puzzle payload and no explicit product-mode discriminator beyond `InteractionMode`.
- There is no production home-screen flow, content catalogue, authored level asset, or final construction UX.

## 4. Separation requirements

1. Keep `PuzzleEngine`, mixed models, solver, generator, and their tests intact.
2. Keep the current graybox scene and TEMP prefab as a reproducible reference.
3. Place new disassembly rules in a parallel domain and namespace.
4. Give disassembly and construction separate level-definition types and snapshot payloads.
5. Use shared utilities only where semantics are identical: deterministic RNG, CRC, atomic file handling, camera fitting, safe areas, and input-layer conventions.
6. Do not make construction-only terms (`TrayPartIds`, `AssembledPartIds`, sockets, rotation) part of the disassembly public API.
7. A future mode selector requires a fully approved product decision and complete construction flow; no placeholder button should be shipped.

## 5. Future reactivation requirements

Before construction returns, require:

- a product role distinct from the primary disassembly campaign;
- data-driven authored levels;
- production part/socket assets and visual feedback;
- a versioned save-mode discriminator and migration plan;
- physical-device drag, rotation, target, safe-area, and lifecycle tests;
- progression and tutorial design that does not confuse disassembly rules;
- a complete feature, not a partially accessible prototype.

## 6. Preservation tests

During disassembly implementation:

- Continue running all existing Core tests, including mixed interleaving, compatibility, rotation, tray overflow, hints, snapshots, generation, and save migration.
- Continue running the current Windmill PlayMode tests until a deliberate archival milestone is approved.
- Treat any change to `PuzzleEngine` completion, tray, socket, rotation, or assembly behavior as a regression unless separately authorized.
