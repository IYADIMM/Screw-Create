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

