# Test Plan

## Milestone 1 Edit Mode

- Screw exposure, coverage, authored locks, and dependency graph cycles.
- Part release conditions and required fasteners.
- Tray capacity, ordering, overflow prevention, and last-safe-slot reporting.
- Snapshot/undo restoration of all logical fields.
- Assembly compatibility, dependencies, rotations, and completion.
- PRNG golden sequence and reproducibility.
- Share-code encode/decode/checksum and malformed input rejection.
- Generator determinism, solver correctness, bounded failure, and difficulty bands.
- Versioned save serialization, migration, corruption recovery, and daily timezone logic.

## Milestone 2 Play Mode

Boot/menu, load/unload, tutorial interactions, touch/mouse abstraction, pause/resume, interrupted save restore, disassembly-to-assembly win flow, failure/recovery, rewarded mock outcomes, safe areas, and missing-reference validation.

## Release evidence

Run the deterministic smoke route, curated 30 seeds, at least 10,000 generated seeds, every authored level from a clean save with/without hints, lifecycle/edge cases, Android device tests, profiler captures, build logs, and screenshots. Results go in `Docs/VerificationReport.md`.

