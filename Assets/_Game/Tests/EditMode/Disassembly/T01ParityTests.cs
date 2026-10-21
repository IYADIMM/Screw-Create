using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ScrewAndCreate.Disassembly.Tests
{
    public sealed class T01ParityTests
    {
        [Test]
        public void T01Reachability_MatchesVerifiedPythonOracle()
        {
            var definition = T01FirstReleaseDefinition.Create();
            var initial = DisassemblyValidator.CreateInitialSnapshot(definition);
            var queue = new Queue<(DisassemblySnapshot snapshot, int depth)>();
            var visited = new HashSet<string>(StringComparer.Ordinal) { initial.SolverKey() };
            queue.Enqueue((initial, 0));
            var completedDepth = int.MaxValue;
            var deadlocks = 0;

            while (queue.Count > 0)
            {
                var item = queue.Dequeue();
                if (item.snapshot.Phase == DisassemblyPhase.Complete)
                {
                    completedDepth = Math.Min(completedDepth, item.depth);
                    continue;
                }

                var moves = DisassemblyRules.GetLegalMoves(definition, item.snapshot);
                if (moves.Count == 0) deadlocks++;
                foreach (var move in moves)
                {
                    var result = DisassemblyRules.TryTransferScrew(
                        definition, item.snapshot, move.ScrewId, move.DestinationHoleId);
                    Assert.That(result.Success, Is.True, result.Reason);
                    if (visited.Add(result.Snapshot.SolverKey()))
                        queue.Enqueue((result.Snapshot, item.depth + 1));
                }
            }

            Assert.That(visited.Count, Is.EqualTo(2), "Python oracle reachable-state count");
            Assert.That(completedDepth, Is.EqualTo(1), "Python oracle minimum solution length");
            Assert.That(deadlocks, Is.Zero, "Python oracle reachable non-complete deadlocks");
        }
    }
}
