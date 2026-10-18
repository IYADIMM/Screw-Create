# Phase 2 Implementation Checklist

| Order | Work item | Source files | Verification | Requirements | Status |
|---:|---|---|---|---|---|
| 1 | Record five passes, branch, reference, and zero-credit scope | `Docs/Phase2ReviewLog.md`, `References/` | Hashes, Git status | REQ-PROTOCOL-004–008, REQ-GATE-001 | Complete |
| 2 | Correct mixed remove/assemble loop and exact undo | Core model/engine/solver | EditMode mixed-loop tests | REQ-GAME-005, REQ-GAME-007–010 | Complete |
| 3 | Add socket catalog and compatibility enforcement | `PuzzleModel.cs`, `PuzzleEngine.cs` | Positive/negative compatibility tests | REQ-GAME-007–008 | Complete |
| 4 | Harden definitions and snapshot restoration | Core model/engine | malformed/null/corruption tests | REQ-SAVE-001–002, REQ-TEST-002 | Complete |
| 5 | Upgrade save schema, migrations, and Android-safe writes | `SaveData.cs` | migration/recovery/fallback tests | REQ-SAVE-001–006 | Complete |
| 6 | Expand seed identity and validation | `SeedCode.cs` | boundary/version/catalog tests | REQ-SEED-001–003 | Complete |
| 7 | Add data-driven five-band generator and rejection metrics | generator/solver | boundaries, 30 curated seeds, 1,000-seed batch | REQ-SEED-004–018 | Complete for Phase 2 |
| 8 | Build idempotent TEMP_ windmill scene and assets | editor builder, presentation, services | scene generation and hierarchy audit | REQ-GAME-001–014, REQ-ART-001–012 | Complete for TEMP_ slice |
| 9 | Implement input, tray, assembly, HUD, controls, save/reload, completion | presentation/services/UI | PlayMode tests and manual editor run | REQ-FEEL-001–008, REQ-SCREEN-001–006 | Complete for TEMP_ slice |
| 10 | Capture portrait screenshots and compare to reference | scene, `Docs/Phase2VerificationReport.md` | three screenshot dimensions and checklist | REQ-ART-001–012 | Complete with documented gaps |
| 11 | Compile, run all tests, strict verification, document evidence | tests/docs | clean Unity results | REQ-TEST-001–009 | Complete |
| 12 | Commit and push review branch without merging | Git | remote SHA comparison | REQ-PROTOCOL-008 | In progress |

