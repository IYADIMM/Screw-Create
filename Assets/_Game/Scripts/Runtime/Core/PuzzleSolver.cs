using System;
using System.Collections.Generic;
using System.Linq;

namespace ScrewAndCreate.Core
{
    public sealed class SolverResult
    {
        public bool Solved;
        public bool ResourceLimitReached;
        public int StatesVisited;
        public PuzzleAction[] Actions = Array.Empty<PuzzleAction>();
        public string FailureReason = string.Empty;
    }

    /// <summary>Deterministic depth-first verifier with canonical-state deduplication and a hard state budget.</summary>
    public sealed class PuzzleSolver
    {
        public SolverResult Solve(PuzzleDefinition definition, int maxStates = 10000)
        {
            if (maxStates < 1) throw new ArgumentOutOfRangeException(nameof(maxStates));
            var validation = definition.Validate();
            if (validation.Count > 0) return new SolverResult { FailureReason = string.Join(" ", validation) };

            var engine = new PuzzleEngine(definition);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var path = new List<PuzzleAction>();
            var result = new SolverResult();
            result.Solved = Search(engine, visited, path, maxStates, result);
            result.Actions = result.Solved ? path.ToArray() : Array.Empty<PuzzleAction>();
            if (!result.Solved && string.IsNullOrEmpty(result.FailureReason))
                result.FailureReason = result.ResourceLimitReached ? "Solver state budget exhausted." : "No legal solution found.";
            return result;
        }

        static bool Search(PuzzleEngine engine, HashSet<string> visited, List<PuzzleAction> path, int maxStates, SolverResult result)
        {
            if (engine.Phase == PuzzlePhase.Complete) return true;
            if (result.StatesVisited >= maxStates) { result.ResourceLimitReached = true; return false; }
            var key = engine.CaptureSnapshot().CanonicalKey();
            if (!visited.Add(key)) return false;
            result.StatesVisited++;

            if (engine.Phase == PuzzlePhase.Disassembly)
            {
                foreach (var screwId in engine.GetRemovableScrews().ToArray())
                {
                    if (!engine.TryRemoveScrew(screwId).Success) continue;
                    path.Add(new PuzzleAction(PuzzleActionKind.RemoveScrew, screwId));
                    if (Search(engine, visited, path, maxStates, result)) return true;
                    path.RemoveAt(path.Count - 1);
                    engine.TryUndo();
                }
            }
            else if (engine.Phase == PuzzlePhase.Assembly)
            {
                foreach (var part in engine.GetAssemblableParts().ToArray())
                {
                    var rotation = part.AllowedRotations[0];
                    if (!engine.TryAssemblePart(part.Id, part.TargetSocketId, rotation).Success) continue;
                    path.Add(new PuzzleAction(PuzzleActionKind.AssemblePart, part.Id, part.TargetSocketId, rotation));
                    if (Search(engine, visited, path, maxStates, result)) return true;
                    path.RemoveAt(path.Count - 1);
                    engine.TryUndo();
                }
            }
            return false;
        }
    }
}
