using System.Linq;
using NUnit.Framework;

namespace ScrewAndCreate.Disassembly.Tests
{
    public sealed class DisassemblyTransitionTests
    {
        [Test]
        public void T01_MoveToParking_ReleasesPlankAndCompletes()
        {
            var session = new DisassemblySession(T01FirstReleaseDefinition.Create());
            CollectionAssert.AreEqual(new[] { "S_1" }, session.GetAccessibleScrewIds());
            CollectionAssert.AreEqual(new[] { "H_P1" }, session.GetLegalDestinationIds("S_1"));

            var result = session.TryTransferScrew("S_1", "H_P1");

            Assert.That(result.Success, Is.True, result.Reason);
            CollectionAssert.AreEqual(new[] { "C_PLANK" }, result.ReleasedComponentIds);
            Assert.That(session.Snapshot.Phase, Is.EqualTo(DisassemblyPhase.Complete));
            Assert.That(session.Snapshot.ActionCount, Is.EqualTo(1));
            Assert.That(session.Snapshot.Revision, Is.EqualTo(1));
            Assert.That(session.Snapshot.ScrewLocations.Single().HoleId, Is.EqualTo("H_P1"));
        }

        [Test]
        public void InvalidMoves_DoNotMutateStateOrUndoHistory()
        {
            var session = new DisassemblySession(T01FirstReleaseDefinition.Create());
            var before = session.Snapshot.CanonicalKey();
            Assert.That(session.TryTransferScrew("MISSING", "H_P1").Success, Is.False);
            Assert.That(session.TryTransferScrew("S_1", "H_C1").Success, Is.False);
            Assert.That(session.TryTransferScrew("S_1", "MISSING").Success, Is.False);
            Assert.That(session.Snapshot.CanonicalKey(), Is.EqualTo(before));
            Assert.That(session.UndoCount, Is.Zero);
        }

        [Test]
        public void Undo_RestoresExactAuthoritativeSnapshot()
        {
            var session = new DisassemblySession(T01FirstReleaseDefinition.Create());
            var before = session.Snapshot.CanonicalKey();
            Assert.That(session.TryTransferScrew("S_1", "H_P1").Success, Is.True);
            Assert.That(session.UndoCount, Is.EqualTo(1));
            Assert.That(session.TryUndo(), Is.True);
            Assert.That(session.Snapshot.CanonicalKey(), Is.EqualTo(before));
            Assert.That(session.UndoCount, Is.Zero);
            Assert.That(session.TryUndo(), Is.False);
        }

        [Test]
        public void Component_RemainsAttachedUntilEverySecuringHoleIsEmpty()
        {
            var session = new DisassemblySession(DisassemblyTestDefinitions.MultipleFasteners());
            var first = session.TryTransferScrew("S_1", "P_1");
            Assert.That(first.Success, Is.True, first.Reason);
            Assert.That(first.ReleasedComponentIds, Is.Empty);
            Assert.That(session.Snapshot.ReleasedComponentIds, Is.Empty);

            var second = session.TryTransferScrew("S_2", "P_2");
            Assert.That(second.Success, Is.True, second.Reason);
            CollectionAssert.AreEqual(new[] { "C" }, second.ReleasedComponentIds);
        }

        [Test]
        public void CoveredScrewAndDestination_BecomeAvailableAfterCoverReleases()
        {
            var session = new DisassemblySession(DisassemblyTestDefinitions.CoveredContent());
            CollectionAssert.AreEqual(new[] { "S_TOP" }, session.GetAccessibleScrewIds());
            CollectionAssert.AreEqual(new[] { "P_1" }, session.GetLegalDestinationIds("S_TOP"));

            Assert.That(session.TryTransferScrew("S_TOP", "P_1").Success, Is.True);

            CollectionAssert.Contains(session.GetAccessibleScrewIds().ToArray(), "S_BOTTOM");
            CollectionAssert.Contains(session.GetLegalDestinationIds("S_BOTTOM").ToArray(), "P_2");
        }

        [Test]
        public void ComponentOwnedHole_CeasesToExistWhenOwnerReleases()
        {
            var definition = DisassemblyTestDefinitions.ComponentOwnedHole();
            var session = new DisassemblySession(definition);
            Assert.That(session.TryTransferScrew("S_A", "P_1").Success, Is.True);
            Assert.That(DisassemblyRules.HoleExists(definition, session.Snapshot, "H_A"), Is.False);
        }

        [Test]
        public void SharedStructuralHole_RetiresOnlyAfterEverySecuredComponentReleases()
        {
            var session = new DisassemblySession(DisassemblyTestDefinitions.SharedStructuralHole());
            Assert.That(session.TryTransferScrew("S_SHARED", "P_1").Success, Is.True);
            CollectionAssert.AreEqual(new[] { "C_A" }, session.Snapshot.ReleasedComponentIds);
            CollectionAssert.DoesNotContain(session.GetLegalDestinationIds("S_B").ToArray(), "H_SHARED");

            Assert.That(session.TryTransferScrew("S_B", "P_2").Success, Is.True);
            CollectionAssert.AreEqual(new[] { "C_A", "C_B" }, session.Snapshot.ReleasedComponentIds);
            CollectionAssert.Contains(session.GetLegalDestinationIds("S_SHARED").ToArray(), "H_SHARED");
        }

        [Test]
        public void ParkingInRetiredStructuralHole_DoesNotRefastenReleasedComponents()
        {
            var session = new DisassemblySession(DisassemblyTestDefinitions.SharedStructuralHole());
            Assert.That(session.TryTransferScrew("S_SHARED", "P_1").Success, Is.True);
            Assert.That(session.TryTransferScrew("S_B", "P_2").Success, Is.True);
            var moveBack = session.TryTransferScrew("S_SHARED", "H_SHARED");
            Assert.That(moveBack.Success, Is.True, moveBack.Reason);
            CollectionAssert.AreEqual(new[] { "C_A", "C_B" }, session.Snapshot.ReleasedComponentIds);
            Assert.That(session.Snapshot.Phase, Is.EqualTo(DisassemblyPhase.Active));
        }

        [Test]
        public void OneTransfer_ResolvesTransitiveReleaseClosureInOneUndoStep()
        {
            var session = new DisassemblySession(DisassemblyTestDefinitions.TransitiveRelease());
            var before = session.Snapshot.CanonicalKey();
            var result = session.TryTransferScrew("S_A", "P_1");
            Assert.That(result.Success, Is.True, result.Reason);
            CollectionAssert.AreEqual(new[] { "C_A", "C_B" }, result.ReleasedComponentIds);
            Assert.That(session.UndoCount, Is.EqualTo(1));
            Assert.That(session.TryUndo(), Is.True);
            Assert.That(session.Snapshot.CanonicalKey(), Is.EqualTo(before));
        }

        [Test]
        public void SameInputStateAndAction_ProduceIdenticalSnapshot()
        {
            var definition = DisassemblyTestDefinitions.TransitiveRelease();
            var initial = DisassemblyValidator.CreateInitialSnapshot(definition);
            var left = DisassemblyRules.TryTransferScrew(definition, initial, "S_A", "P_1");
            var right = DisassemblyRules.TryTransferScrew(definition, initial, "S_A", "P_1");
            Assert.That(left.Success, Is.True, left.Reason);
            Assert.That(right.Success, Is.True, right.Reason);
            Assert.That(left.Snapshot.CanonicalKey(), Is.EqualTo(right.Snapshot.CanonicalKey()));
        }

        [Test]
        public void SolverKey_PreservesIndividualScrewIdentity()
        {
            var definition = DisassemblyTestDefinitions.MultipleFasteners();
            var left = DisassemblyValidator.CreateInitialSnapshot(definition);
            var right = left.Clone();
            right.ScrewLocations.Single(x => x.ScrewId == "S_1").HoleId = "H_2";
            right.ScrewLocations.Single(x => x.ScrewId == "S_2").HoleId = "H_1";
            Assert.That(left.SolverKey(), Is.Not.EqualTo(right.SolverKey()));
        }
    }
}
