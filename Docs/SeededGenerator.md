# Seeded Generator Design

## Stable identity

A challenge identity contains generator version, content catalog version, unsigned 64-bit seed, construction family, and difficulty band. The share code includes these fields and a checksum.

## Deterministic PRNG

Milestone 1 uses a documented PCG-style 32-bit output over explicit 64-bit state and stream. Only stable integer operations are allowed. UnityEngine.Random is excluded. Golden sequences lock behavior across editor/device updates.

## Generation

Generate by reversing a legal removal/assembly sequence, build a directed acyclic dependency graph, then validate forward with an independent bounded solver. Reject cycles, missing assets, unfair opening states, out-of-band difficulty, inaccessible screws, overlaps, offscreen geometry, blocked sockets, and invalid framing.

## Solver state

Canonical state includes screw removal bits, released part bits, tray ordering/capacity, assembly placement/orientation, move history metadata, score/hint effects, generator/content versions, and declared win/fail state. State equality and hashing exclude presentation values.

## Difficulty bands

Apprentice, Builder, Craftsperson, Engineer, and Master use bounded ranges for screws, layers, graph depth, branching, tray pressure, held parts, part similarity, rotations, fair distractors, and mechanism mix.

## Validation evidence

Edit Mode tests cover PRNG golden sequences, seed code round trips/checksum rejection, DAG validation, exposure/release/tray rules, snapshots, solver validity, determinism, and difficulty boundaries. Pre-release batch testing covers at least 10,000 seeds and exports a machine-readable report plus a curated 30-seed regression set.

