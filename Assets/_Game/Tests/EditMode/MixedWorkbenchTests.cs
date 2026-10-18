using System;
using System.Linq;
using NUnit.Framework;

namespace ScrewAndCreate.Core.Tests
{
    public sealed class MixedWorkbenchTests
    {
        [Test]
        public void FourPartsSolveThroughThreeSlotInterleaving()
        {
            var definition = FourPartFixture();
            var result = new PuzzleSolver().Solve(definition);
            Assert.That(result.Solved, Is.True, result.FailureReason);
            var firstAssembly = Array.FindIndex(result.Actions, x => x.Kind == PuzzleActionKind.AssemblePart);
            var lastRemoval = Array.FindLastIndex(result.Actions, x => x.Kind == PuzzleActionKind.RemoveScrew);
            Assert.That(firstAssembly, Is.LessThan(lastRemoval));
        }

        [Test]
        public void SocketDependencyCanMakeRequiredSlotImpossibleToFree()
        {
            var definition = FourPartFixture();
            definition.Parts[0].AssemblyDependencyPartIds = new[] { "P4" };
            definition.Sockets[0].AssemblyDependencyPartIds = new[] { "P4" };
            Assert.That(new PuzzleSolver().Solve(definition).Solved, Is.False);
        }

        [Test]
        public void OverflowRejectionChangesNoFullState()
        {
            var definition = FourPartFixture();
            definition.TrayCapacity = 1;
            var engine = new PuzzleEngine(definition);
            Assert.That(engine.TryRemoveScrew("S1").Success, Is.True);
            var before = engine.CaptureSnapshot().CanonicalKey();
            Assert.That(engine.TryRemoveScrew("S2").Success, Is.False);
            Assert.That(engine.CaptureSnapshot().CanonicalKey(), Is.EqualTo(before));
        }

        [Test]
        public void UndoRemovalAndAssemblyRestoreExactPriorStateWhileRetainingHintUse()
        {
            var engine = new PuzzleEngine(FourPartFixture());
            var start = engine.CaptureSnapshot().CanonicalKey();
            engine.TryRemoveScrew("S1");
            Assert.That(engine.TryUndo(), Is.True);
            Assert.That(engine.CaptureSnapshot().SolverKey(), Is.EqualTo(new PuzzleEngine(FourPartFixture()).CaptureSnapshot().SolverKey()));
            engine.TryRemoveScrew("S1");
            engine.SetInteractionMode(InteractionMode.Assembly);
            var beforeAssembly = engine.CaptureSnapshot();
            engine.RegisterHintUse();
            engine.TryAssemblePart("P1", "Socket-P1", 0);
            Assert.That(engine.TryUndo(), Is.True);
            Assert.That(engine.CaptureSnapshot().SolverKey(), Is.EqualTo(beforeAssembly.SolverKey()));
            Assert.That(engine.HintCount, Is.EqualTo(1));
            Assert.That(start, Is.Not.Empty);
        }

        [Test]
        public void CompletionRequiresEveryReleasedPartAssembled()
        {
            var engine = new PuzzleEngine(FourPartFixture());
            for (var i = 1; i <= 3; i++) engine.TryRemoveScrew($"S{i}");
            for (var i = 1; i <= 3; i++) engine.TryAssemblePart($"P{i}", $"Socket-P{i}", 0);
            engine.TryRemoveScrew("S4");
            Assert.That(engine.Phase, Is.Not.EqualTo(PuzzlePhase.Complete));
            engine.TryAssemblePart("P4", "Socket-P4", 0);
            Assert.That(engine.Phase, Is.EqualTo(PuzzlePhase.Complete));
        }

        [Test]
        public void SolverHintIsLegalFromMixedState()
        {
            var definition = FourPartFixture();
            var engine = new PuzzleEngine(definition);
            engine.TryRemoveScrew("S1");
            engine.TryRemoveScrew("S2");
            Assert.That(new PuzzleSolver().TryGetHint(definition, engine.CaptureSnapshot(), out var hint), Is.True);
            var legal = hint.Kind == PuzzleActionKind.RemoveScrew
                ? engine.CanRemoveScrew(hint.ItemId, out _)
                : engine.CanAssemblePart(hint.ItemId, hint.TargetId, hint.Rotation, out _);
            Assert.That(legal, Is.True);
        }

        [Test]
        public void CompatibilityAndAllowedPartAreBothEnforced()
        {
            var definition = FourPartFixture();
            var engine = new PuzzleEngine(definition);
            engine.TryRemoveScrew("S1");
            definition = FourPartFixture();
            definition.Sockets[0].AcceptedCompatibilityGroups = new[] { "metal" };
            var incompatible = new PuzzleEngine(definition);
            incompatible.TryRemoveScrew("S1");
            Assert.That(incompatible.TryAssemblePart("P1", "Socket-P1", 0).Reason, Does.Contain("Compatibility"));
            Assert.That(engine.TryAssemblePart("P1", "Socket-P1", 0).Success, Is.True);
        }

        [Test]
        public void CorruptedSnapshotIsRejectedWithoutChangingState()
        {
            var engine = new PuzzleEngine(FourPartFixture());
            var before = engine.CaptureSnapshot().CanonicalKey();
            var corrupt = engine.CaptureSnapshot();
            corrupt.TrayPartIds = new[] { "UNKNOWN", "UNKNOWN" };
            Assert.That(engine.TryRestoreSnapshot(corrupt, out var reason), Is.False);
            Assert.That(reason, Does.Contain("unknown"));
            Assert.That(engine.CaptureSnapshot().CanonicalKey(), Is.EqualTo(before));
        }

        internal static PuzzleDefinition FourPartFixture()
        {
            var screws = Enumerable.Range(1, 4).Select(i => new ScrewDefinition { Id = $"S{i}" }).ToArray();
            var parts = Enumerable.Range(1, 4).Select(i => new PartDefinition
            {
                Id = $"P{i}", CompatibilityGroup = "wood", TargetSocketId = $"Socket-P{i}", SecuringScrewIds = new[] { $"S{i}" },
                AssemblyDependencyPartIds = i == 1 ? Array.Empty<string>() : new[] { $"P{i - 1}" }, AllowedRotations = new[] { 0 }
            }).ToArray();
            var sockets = Enumerable.Range(1, 4).Select(i => new SocketDefinition
            {
                Id = $"Socket-P{i}", VisualTargetId = $"Ghost-P{i}", AcceptedCompatibilityGroups = new[] { "wood" }, AllowedPartIds = new[] { $"P{i}" },
                AssemblyDependencyPartIds = i == 1 ? Array.Empty<string>() : new[] { $"P{i - 1}" }, AllowedRotations = new[] { 0 }
            }).ToArray();
            return new PuzzleDefinition { Id = "four-part-mixed", TrayCapacity = 3, Screws = screws, Parts = parts, Sockets = sockets };
        }
    }
}
