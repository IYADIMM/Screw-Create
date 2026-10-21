# Decisions

| Date | Decision | Scope and reason | Requirements | Status/evidence |
|---|---|---|---|---|
| 2026-10-18 | Use `C:/Users/Lenovo/OneDrive/Desktop/Screw & Create` as the project root. | User-provided writable workspace; initially empty. | REQ-PROTOCOL-004, REQ-TECH-005 | Active |
| 2026-10-18 | Pin Unity Editor `6000.6.6f1`. | Exact installed Unity 6.6 version. | REQ-TECH-001 | Verified from editor executable and Unity CLI |
| 2026-10-18 | Use installed `com.unity.template.urp-blank` template. | Matches the required mobile URP 3D architecture. | REQ-TECH-002 | Installed template v17.2.6 |
| 2026-10-18 | Initialize local Git with Unity ignores. | Offline, reversible source-control baseline; remote provider remains undecided. | REQ-PROTOCOL-008 | Implemented on `main`; generated caches and build outputs ignored |
| 2026-10-18 | Remove optional IDE and Unity collaboration packages from the generated template. | Package resolution was blocked by signed-in-only optional packages; the game does not require them. Keeping the minimal runtime packages makes headless compilation repeatable. | REQ-TECH-004 | Verified by package resolution, compilation, and tests |
| 2026-10-18 | Spend zero external-generation credits. | User explicitly prohibited paid/credit-consuming generation without exact-batch approval. | REQ-ASSET-003–004, REQ-GATE-001 | Active; see CreditLedger.md |
| 2026-10-18 | Use clearly labeled development identity values until the publisher gate. | Production publisher/package identity requires approval. | REQ-GATE-001 | Active |
| 2026-10-18 | Make screw removal and structural disassembly the primary product direction. | Authoritative Phase 2 directive cancels mandatory recover/inventory/build as the current main game. | Phase 2 specification | Active; see `DisassemblyGameDesign.md` |
| 2026-10-18 | Preserve construction systems for a possible later mode. | Existing assembly, socket, tray, rotation, solver, save, and tests remain valuable but must not appear as an unfinished primary flow. | Phase 2 specification | Active; see `FutureConstructionConcept.md` |
| 2026-10-18 | Propose controlled hybrid disassembly for approval. | Atomic screw transfer to compatible persistent service holes/rack plus deterministic structural release offers more depth than direct removal while avoiding match-three/color sorting. | Phase 2 specification | Proposed; implementation blocked pending user approval |
| 2026-10-18 | Supersede the controlled-hybrid launch proposal with staged persistent-baseboard relocation. | Phase 2.1 reconciliation found that a compulsory rack and early compatibility added complexity and could mask invalid capacity. | Phase 2.1 specification | Corrected design; implementation approval pending |
| 2026-10-18 | Permit persistent structural holes as storage only after their component releases. | This creates physically coherent reusable capacity without re-fastening; active and merely vacated structural holes remain unavailable. | Phase 2.1 hole policy | Executably modeled in T01–T05 |
| 2026-10-18 | Use universal screws and no compulsory rack in the first five levels. | Teaches movement, fasteners, coverage, scarcity and recovery one concept at a time. | Phase 2.1 onboarding | Executably verified; later mechanics deferred |
| 2026-10-18 | Use a parallel stateful disassembly session with pure shared transitions and a separate save envelope. | Current Core is a stateful construction engine with construction-specific snapshots and migrations. | Phase 2.1 architecture | Approved design recommendation; implementation pending |
| 2026-10-18 | Replace Workshop Latch as tutorial one with T01 First Release. | Workshop Latch introduced rack, compatibility and coverage before the base transfer rule was proven. | Phase 2.1 tutorial sequence | Workshop Latch preserved as a later redesign candidate |

