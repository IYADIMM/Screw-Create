using NUnit.Framework;

namespace ScrewAndCreate.Core.Tests
{
    public sealed class PuzzleEngineTests
    {
        [Test]
        public void CoveredScrewBecomesRemovableAfterCoverPartReleases()
        {
            var engine = new PuzzleEngine(TwoPartFixture());
            Assert.That(engine.CanRemoveScrew("S1", out var reason), Is.False);
            Assert.That(reason, Does.Contain("covered"));
            Assert.That(engine.TryRemoveScrew("S2").Success, Is.True);
            Assert.That(engine.CanRemoveScrew("S1", out _), Is.True);
        }

        [Test]
        public void MoveThatWouldOverflowTrayIsRejectedAtomically()
        {
            var fixture = TwoPartFixture();
            fixture.TrayCapacity = 1;
            var engine = new PuzzleEngine(fixture);
            Assert.That(engine.TryRemoveScrew("S2").Success, Is.True);
            var before = engine.CaptureSnapshot().CanonicalKey();
            var result = engine.TryRemoveScrew("S1");
            Assert.That(result.Success, Is.False);
            Assert.That(result.Reason, Does.Contain("overflow"));
            Assert.That(engine.CaptureSnapshot().CanonicalKey(), Is.EqualTo(before));
        }

        [Test]
        public void UndoRestoresCoverageTrayCountersAndPhase()
        {
            var engine = new PuzzleEngine(TwoPartFixture());
            var original = engine.CaptureSnapshot().CanonicalKey();
            engine.TryRemoveScrew("S2");
            Assert.That(engine.TryUndo(), Is.True);
            Assert.That(engine.CaptureSnapshot().CanonicalKey(), Is.EqualTo(original));
            Assert.That(engine.ActionCount, Is.Zero);
            Assert.That(engine.Phase, Is.EqualTo(PuzzlePhase.Disassembly));
        }

        [Test]
        public void AssemblyEnforcesDependenciesSocketAndRotation()
        {
            var engine = new PuzzleEngine(TwoPartFixture());
            engine.TryRemoveScrew("S2");
            engine.TryRemoveScrew("S1");
            Assert.That(engine.TryAssemblePart("P2", "Socket-P2", 90).Success, Is.False);
            Assert.That(engine.TryAssemblePart("P1", "Wrong", 0).Success, Is.False);
            Assert.That(engine.TryAssemblePart("P1", "Socket-P1", 45).Success, Is.False);
            Assert.That(engine.TryAssemblePart("P1", "Socket-P1", 0).Success, Is.True);
            Assert.That(engine.TryAssemblePart("P2", "Socket-P2", 90).Success, Is.True);
            Assert.That(engine.Phase, Is.EqualTo(PuzzlePhase.Complete));
        }

        internal static PuzzleDefinition TwoPartFixture() => new PuzzleDefinition
        {
            Id = "fixture-two-part",
            TrayCapacity = 2,
            Screws = new[]
            {
                new ScrewDefinition { Id = "S1", CoveringPartIds = new[] { "P2" } },
                new ScrewDefinition { Id = "S2" }
            },
            Parts = new[]
            {
                new PartDefinition { Id = "P1", CompatibilityGroup = "wood", TargetSocketId = "Socket-P1", SecuringScrewIds = new[] { "S1" }, AllowedRotations = new[] { 0 } },
                new PartDefinition { Id = "P2", CompatibilityGroup = "wood", TargetSocketId = "Socket-P2", SecuringScrewIds = new[] { "S2" }, AssemblyDependencyPartIds = new[] { "P1" }, AllowedRotations = new[] { 90 } }
            }
        };
    }
}
