# Phase 1 Requirements-to-Evidence Checklist

| Gate | Requirement | Evidence target | Status |
|---|---|---|---|
| G0 | External source backup verified | ZIP manifest and SHA-256 in `Phase1AuditReconciliation.md` | PASS |
| G0 | Phase 2 baseline recoverable | Commit `79e79e3cb7562dcb49d8fb92488a6e978cddfcb1` | PASS |
| G0 | Dedicated repair branch; no push | `codex/phase3-functional-stabilization` | PASS |
| G0 | Consolidated source-verified defect register | `Phase1AuditReconciliation.md` | PASS |
| G1 | Screw home pose and mechanical axis | 10-test focused PlayMode suite | PASS |
| G1 | Visible inventory by tray index | Four-ratio tests and runtime captures 02–04 | PASS |
| G1 | Genuine pointer path and UI separation | Input System pointer test at item screen position | PASS (simulated pointer) |
| G1 | Deterministic interactive hit filtering | Decoration overlap and empty-space test | PASS |
| G1 | Responsive playfield and safe areas | 9:16, 9:19.5, 9:21, 3:4 tests | PASS |
| G1 | Android portrait-only settings | Serialized project audit and successful APK build | PASS |
| G1 | Release HUD excludes diagnostics | PlayMode assertion and screenshots | PASS |
| G2 | State commits before animation | Interrupted-removal tests | PASS |
| G2 | View reconciles from snapshot | Undo/restart/load/pause/disable tests | PASS |
| G2 | Selected rotation remains synchronized | Rotate then hint/refresh test | PASS |
| Evidence | PlayMode screenshots from initialized runtime states | Five PNGs in `Phase1Screenshots` | PASS |
| Regression | Existing Core behavior preserved | 27/27 EditMode; no Core diff | PASS |
| Regression | Existing scene/integration behavior preserved | 16/16 full PlayMode | PASS |
| Build | Clean compile and Android APK where available | Build log; APK hash in report | PASS |
| Device | Physical install, real touch, lifecycle, cutout and performance | `adb devices` returned no device | BLOCKED |
| Scope | No Core edits, paid assets, generators, DOTween, URP migration, monetization, or push | Final diff/ledger/Git audit | PASS |
