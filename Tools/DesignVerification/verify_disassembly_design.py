#!/usr/bin/env python3
"""Exhaustively verify the small Phase 2.1 tutorial fixtures.

This is a design feasibility tool, not the production Unity engine. It has no
third-party dependencies and intentionally keeps screws distinct in canonical
states.
"""

from __future__ import annotations

import argparse
import json
from collections import deque
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, Iterable, List, Optional, Sequence, Tuple


State = Tuple[Tuple[str, ...], Tuple[str, ...]]
Move = Tuple[str, str, str]


@dataclass(frozen=True)
class SearchResult:
    outcome: str
    exhaustive: bool
    reachable_states: int
    reachable_deadlocks: int
    minimum_solution_length: Optional[int]
    solution: Tuple[Move, ...]
    deadlock_examples: Tuple[Tuple[Move, ...], ...]
    capacity_trace: Tuple[dict, ...]


class Puzzle:
    def __init__(self, data: dict):
        self.data = data
        self.level_id = data["id"]
        self.components = {item["id"]: item for item in data["components"]}
        self.holes = {item["id"]: item for item in data["holes"]}
        self.screws = {item["id"]: item for item in data["screws"]}
        self.component_ids = tuple(sorted(self.components))
        self.screw_ids = tuple(sorted(self.screws))
        self.hole_ids = tuple(sorted(self.holes))
        self.validate_definition()
        initial_locations = tuple(self.screws[sid]["hole"] for sid in self.screw_ids)
        initial_released = self.release_closure(initial_locations, ())
        if initial_released:
            raise ValueError(f"{self.level_id}: components release during initialization")
        self.initial_state: State = (initial_locations, initial_released)

    def validate_definition(self) -> None:
        if len(self.components) != len(self.data["components"]):
            raise ValueError(f"{self.level_id}: duplicate component id")
        if len(self.holes) != len(self.data["holes"]):
            raise ValueError(f"{self.level_id}: duplicate hole id")
        if len(self.screws) != len(self.data["screws"]):
            raise ValueError(f"{self.level_id}: duplicate screw id")

        occupied = set()
        for sid, screw in self.screws.items():
            hole_id = screw["hole"]
            if hole_id not in self.holes:
                raise ValueError(f"{self.level_id}: {sid} uses unknown hole {hole_id}")
            if hole_id in occupied:
                raise ValueError(f"{self.level_id}: multiple screws occupy {hole_id}")
            occupied.add(hole_id)

        for hid, hole in self.holes.items():
            if hole["owner"] != "baseboard" and hole["owner"] not in self.components:
                raise ValueError(f"{self.level_id}: {hid} has unknown owner {hole['owner']}")
            if hole["role"] not in ("parking", "structural"):
                raise ValueError(f"{self.level_id}: {hid} has invalid role")
            secures = hole.get("secures")
            if hole["role"] == "structural" and secures not in self.components:
                raise ValueError(f"{self.level_id}: {hid} must secure a component")
            if hole["role"] == "parking" and secures is not None:
                raise ValueError(f"{self.level_id}: parking hole {hid} cannot secure a component")
            for cover in hole.get("covered_by", []):
                if cover not in self.components:
                    raise ValueError(f"{self.level_id}: {hid} has unknown cover {cover}")

        for cid, component in self.components.items():
            securing = component["secured_by"]
            if not securing:
                raise ValueError(f"{self.level_id}: {cid} needs at least one structural hole")
            for hid in securing:
                hole = self.holes.get(hid)
                if hole is None or hole["role"] != "structural" or hole.get("secures") != cid:
                    raise ValueError(f"{self.level_id}: {cid} has invalid securing hole {hid}")
            for dependency in component.get("release_requires", []):
                if dependency not in self.components or dependency == cid:
                    raise ValueError(f"{self.level_id}: {cid} has invalid release dependency")
            if not any(hid in occupied for hid in securing):
                raise ValueError(f"{self.level_id}: {cid} starts with no occupied fastener")

    def occupancy(self, locations: Sequence[str]) -> Dict[str, str]:
        return {hid: sid for sid, hid in zip(self.screw_ids, locations)}

    def hole_exists(self, hole_id: str, released: Iterable[str]) -> bool:
        owner = self.holes[hole_id]["owner"]
        return owner == "baseboard" or owner not in set(released)

    def hole_exposed(self, hole_id: str, released: Iterable[str]) -> bool:
        released_set = set(released)
        return all(cover in released_set for cover in self.holes[hole_id].get("covered_by", []))

    def is_legal_destination(self, hole_id: str, locations: Sequence[str], released: Iterable[str]) -> bool:
        hole = self.holes[hole_id]
        released_set = set(released)
        if hole["owner"] != "baseboard":
            return False
        if hole_id in locations or not self.hole_exposed(hole_id, released_set):
            return False
        if hole["role"] == "parking":
            return True
        return hole.get("secures") in released_set

    def screw_accessible(self, screw_id: str, state: State) -> bool:
        locations, released = state
        hole_id = locations[self.screw_ids.index(screw_id)]
        return self.hole_exists(hole_id, released) and self.hole_exposed(hole_id, released)

    def legal_moves(self, state: State) -> Tuple[Move, ...]:
        if self.is_complete(state):
            return ()
        locations, released = state
        destinations = [
            hid for hid in self.hole_ids
            if self.is_legal_destination(hid, locations, released)
        ]
        moves: List[Move] = []
        for index, screw_id in enumerate(self.screw_ids):
            if not self.screw_accessible(screw_id, state):
                continue
            source = locations[index]
            for destination in destinations:
                if destination != source:
                    moves.append((screw_id, source, destination))
        return tuple(moves)

    def apply(self, state: State, move: Move) -> State:
        if move not in self.legal_moves(state):
            raise ValueError(f"{self.level_id}: illegal move {move}")
        locations, released = state
        screw_id, source, destination = move
        index = self.screw_ids.index(screw_id)
        if locations[index] != source:
            raise ValueError(f"{self.level_id}: stale source in {move}")
        next_locations = list(locations)
        next_locations[index] = destination
        next_released = self.release_closure(tuple(next_locations), released)
        return (tuple(next_locations), next_released)

    def release_closure(self, locations: Sequence[str], released: Iterable[str]) -> Tuple[str, ...]:
        occupancy = self.occupancy(locations)
        released_set = set(released)
        changed = True
        while changed:
            changed = False
            for component_id in self.component_ids:
                if component_id in released_set:
                    continue
                component = self.components[component_id]
                if not set(component.get("release_requires", [])).issubset(released_set):
                    continue
                if all(hole_id not in occupancy for hole_id in component["secured_by"]):
                    released_set.add(component_id)
                    changed = True
        return tuple(sorted(released_set))

    def is_complete(self, state: State) -> bool:
        return state[1] == self.component_ids

    def canonical_key(self, state: State) -> str:
        locations, released = state
        location_part = ";".join(f"{sid}={hid}" for sid, hid in zip(self.screw_ids, locations))
        return f"{location_part}|released={','.join(released)}"

    def available_capacity(self, state: State) -> int:
        locations, released = state
        return sum(self.is_legal_destination(hid, locations, released) for hid in self.hole_ids)

    def capacity_trace_for(self, moves: Sequence[Move]) -> Tuple[dict, ...]:
        state = self.initial_state
        trace = []
        for number, move in enumerate(moves, start=1):
            before_released = set(state[1])
            before_capacity = self.available_capacity(state)
            next_state = self.apply(state, move)
            newly_released = sorted(set(next_state[1]) - before_released)
            newly_legal = sorted(
                hid for hid in self.hole_ids
                if self.is_legal_destination(hid, next_state[0], next_state[1])
                and not self.is_legal_destination(hid, state[0], state[1])
            )
            trace.append({
                "step": number,
                "move": {"screw": move[0], "from": move[1], "to": move[2]},
                "capacity_before": before_capacity,
                "capacity_after": self.available_capacity(next_state),
                "released": newly_released,
                "newly_legal_empty_holes": newly_legal,
            })
            state = next_state
        return tuple(trace)


def reconstruct_path(parent: Dict[State, Tuple[Optional[State], Optional[Move]]], state: State) -> Tuple[Move, ...]:
    path: List[Move] = []
    cursor = state
    while parent[cursor][0] is not None:
        previous, move = parent[cursor]
        assert previous is not None and move is not None
        path.append(move)
        cursor = previous
    path.reverse()
    return tuple(path)


def exhaust(puzzle: Puzzle, max_states: int) -> SearchResult:
    queue = deque([puzzle.initial_state])
    parent: Dict[State, Tuple[Optional[State], Optional[Move]]] = {
        puzzle.initial_state: (None, None)
    }
    first_solution: Optional[State] = None
    deadlocks: List[State] = []
    exhaustive = True

    while queue:
        if len(parent) > max_states:
            exhaustive = False
            break
        state = queue.popleft()
        if puzzle.is_complete(state):
            if first_solution is None:
                first_solution = state
            continue
        moves = puzzle.legal_moves(state)
        if not moves:
            deadlocks.append(state)
            continue
        for move in moves:
            successor = puzzle.apply(state, move)
            if successor not in parent:
                parent[successor] = (state, move)
                queue.append(successor)

    if not exhaustive:
        outcome = "RESOURCE_LIMIT_REACHED"
    elif first_solution is not None:
        outcome = "PROVEN_SOLUTION"
    else:
        outcome = "PROVEN_NO_SOLUTION"

    solution = reconstruct_path(parent, first_solution) if first_solution is not None else ()
    deadlock_examples = tuple(reconstruct_path(parent, state) for state in deadlocks[:3])
    return SearchResult(
        outcome=outcome,
        exhaustive=exhaustive,
        reachable_states=len(parent),
        reachable_deadlocks=len(deadlocks),
        minimum_solution_length=len(solution) if first_solution is not None else None,
        solution=solution,
        deadlock_examples=deadlock_examples,
        capacity_trace=puzzle.capacity_trace_for(solution),
    )


def move_text(move: Move) -> str:
    return f"{move[0]}: {move[1]} -> {move[2]}"


def markdown_report(results: Sequence[Tuple[Puzzle, SearchResult]], command: str) -> str:
    lines = [
        "# Phase 2.1 Disassembly Design Feasibility Report",
        "",
        "Status: **EXECUTED DESIGN VERIFICATION — not production gameplay verification**",
        "",
        "The dependency-free verifier exhaustively enumerated every stable logical state reachable in five tutorial fixtures and one deliberately unsatisfiable capacity control under the documented Phase 2.1 transfer and release rules. Completed states are terminal. Screw identities are preserved in canonical states.",
        "",
        f"Reproduction command: `{command}`",
        "",
        "## Results",
        "",
        "| Fixture | Category | Outcome | Exhaustive | Reachable states | Minimum moves | Reachable deadlocks |",
        "|---|---|---|---:|---:|---:|---:|",
    ]
    for puzzle, result in results:
        minimum = "n/a" if result.minimum_solution_length is None else str(result.minimum_solution_length)
        lines.append(
            f"| {puzzle.level_id} — {puzzle.data['title']} | {puzzle.data.get('category', 'fixture')} | {result.outcome} | "
            f"{'yes' if result.exhaustive else 'no'} | {result.reachable_states} | {minimum} | {result.reachable_deadlocks} |"
        )

    lines += ["", "## Per-tutorial proof", ""]
    for puzzle, result in results:
        lines += [
            f"### {puzzle.level_id} — {puzzle.data['title']}",
            "",
            f"Concept: {puzzle.data['concept']}",
            "",
            f"Outcome: **{result.outcome}**. Enumeration was {'exhaustive' if result.exhaustive else 'cut off by the state budget'}. The graph contains {result.reachable_states} reachable stable states and {result.reachable_deadlocks} reachable non-complete states with no currently legal moves.",
            "",
            "Shortest legal solution:",
            "",
        ]
        for index, move in enumerate(result.solution, start=1):
            lines.append(f"{index}. `{move_text(move)}`")
        if not result.solution:
            lines.append("No solution was found.")
        if result.capacity_trace:
            lines += ["", "Capacity transition on that solution:", ""]
            for item in result.capacity_trace:
                move = item["move"]
                release = ", ".join(item["released"]) or "none"
                new_holes = ", ".join(item["newly_legal_empty_holes"]) or "none"
                lines.append(
                    f"- Step {item['step']}: `{move['screw']}: {move['from']} -> {move['to']}`; "
                    f"empty legal destinations {item['capacity_before']} -> {item['capacity_after']}; "
                    f"released {release}; newly legal empty holes {new_holes}."
                )
        if result.deadlock_examples:
            lines += ["", "Shortest recorded deadlock path:", ""]
            for index, move in enumerate(result.deadlock_examples[0], start=1):
                lines.append(f"{index}. `{move_text(move)}`")
            lines.append("The snapshot has no currently legal move. Player undo remains a separate recovery property.")
        lines.append("")

    lines += [
        "## Interpretation limits",
        "",
        "This report proves that the five tutorial fixtures are solvable and the negative control is unsolvable under this verifier's documented rules, with every reachable stable state enumerated within the configured budget. It does not prove that a future Unity implementation matches these rules, that the tutorials are fun, that their presentation is readable, or that an Android build works. Production acceptance requires shared-rule engine tests, Unity integration tests, playtesting and device evidence.",
        "",
    ]
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--fixtures", type=Path, default=Path(__file__).with_name("tutorial_fixtures.json"))
    parser.add_argument("--json-out", type=Path)
    parser.add_argument("--markdown-out", type=Path)
    parser.add_argument("--max-states", type=int, default=100000)
    args = parser.parse_args()

    fixture_data = json.loads(args.fixtures.read_text(encoding="utf-8"))
    results: List[Tuple[Puzzle, SearchResult]] = []
    serializable = {"schema_version": 1, "max_states": args.max_states, "levels": []}
    all_fixtures = [
        *(dict(level, category="tutorial") for level in fixture_data["levels"]),
        *(dict(level, category="negative_control") for level in fixture_data.get("negative_controls", [])),
    ]
    all_expectations_met = True
    for level in all_fixtures:
        puzzle = Puzzle(level)
        puzzle.data["category"] = level["category"]
        result = exhaust(puzzle, args.max_states)
        expected = level.get("expected", {})
        checks = {
            "outcome": result.outcome,
            "reachable_states": result.reachable_states,
            "reachable_deadlocks": result.reachable_deadlocks,
            "minimum_solution_length": result.minimum_solution_length,
        }
        for key, actual in checks.items():
            if key in expected and expected[key] != actual:
                all_expectations_met = False
                raise AssertionError(f"{puzzle.level_id}: expected {key}={expected[key]}, got {actual}")
        results.append((puzzle, result))
        serializable["levels"].append({
            "id": puzzle.level_id,
            "category": level["category"],
            **checks,
            "exhaustive": result.exhaustive,
            "solution": [
                {"screw": move[0], "from": move[1], "to": move[2]}
                for move in result.solution
            ],
            "deadlock_examples": [
                [{"screw": move[0], "from": move[1], "to": move[2]} for move in path]
                for path in result.deadlock_examples
            ],
            "capacity_trace": list(result.capacity_trace),
        })

    command = "python Tools/DesignVerification/verify_disassembly_design.py --json-out Docs/Phase21FeasibilityReport.json --markdown-out Docs/Phase21FeasibilityReport.md"
    if args.json_out:
        args.json_out.parent.mkdir(parents=True, exist_ok=True)
        args.json_out.write_text(json.dumps(serializable, indent=2) + "\n", encoding="utf-8")
    if args.markdown_out:
        args.markdown_out.parent.mkdir(parents=True, exist_ok=True)
        args.markdown_out.write_text(markdown_report(results, command), encoding="utf-8")
    print(json.dumps(serializable, indent=2))
    return 0 if all_expectations_met and all(r.exhaustive for _, r in results) else 1


if __name__ == "__main__":
    raise SystemExit(main())
