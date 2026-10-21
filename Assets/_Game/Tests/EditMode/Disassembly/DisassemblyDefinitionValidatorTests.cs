using System;
using System.Linq;
using NUnit.Framework;

namespace ScrewAndCreate.Disassembly.Tests
{
    public sealed class DisassemblyDefinitionValidatorTests
    {
        [Test]
        public void T01Definition_IsValid()
        {
            Assert.That(DisassemblyValidator.ValidateDefinition(T01FirstReleaseDefinition.Create()), Is.Empty);
        }

        [Test]
        public void T01InitialSnapshot_PreservesExactIdentityAndCounters()
        {
            var snapshot = DisassemblyValidator.CreateInitialSnapshot(T01FirstReleaseDefinition.Create());
            Assert.That(snapshot.Phase, Is.EqualTo(DisassemblyPhase.Active));
            Assert.That(snapshot.ActionCount, Is.Zero);
            Assert.That(snapshot.Revision, Is.Zero);
            Assert.That(snapshot.ReleasedComponentIds, Is.Empty);
            Assert.That(snapshot.ScrewLocations, Has.Length.EqualTo(1));
            Assert.That(snapshot.ScrewLocations[0].ScrewId, Is.EqualTo("S_1"));
            Assert.That(snapshot.ScrewLocations[0].HoleId, Is.EqualTo("H_C1"));
            Assert.That(snapshot.SolverKey(), Is.EqualTo("S_1=H_C1|released="));
        }

        [Test]
        public void DuplicateComponentIds_AreReportedWithoutThrowing()
        {
            var definition = T01FirstReleaseDefinition.Create();
            definition.Components = new[] { definition.Components[0], definition.Components[0] };
            Assert.That(DisassemblyValidator.ValidateDefinition(definition).Any(x => x.Contains("Duplicate component ID")), Is.True);
        }

        [Test]
        public void MissingInitialHole_IsRejected()
        {
            var definition = T01FirstReleaseDefinition.Create();
            definition.Screws[0].InitialHoleId = "MISSING";
            Assert.That(DisassemblyValidator.ValidateDefinition(definition).Any(x => x.Contains("invalid initial hole")), Is.True);
        }

        [Test]
        public void DuplicateInitialOccupancy_IsRejected()
        {
            var definition = DisassemblyTestDefinitions.MultipleFasteners();
            definition.Screws[1].InitialHoleId = "H_1";
            Assert.That(DisassemblyValidator.ValidateDefinition(definition).Any(x => x.Contains("Multiple screws occupy")), Is.True);
        }

        [Test]
        public void BaseboardHole_MustBePersistentAndOwnerless()
        {
            var definition = T01FirstReleaseDefinition.Create();
            definition.Holes[1].Persistent = false;
            definition.Holes[1].OwnerComponentId = "C_PLANK";
            var errors = DisassemblyValidator.ValidateDefinition(definition);
            Assert.That(errors.Any(x => x.Contains("cannot name an owner")), Is.True);
            Assert.That(errors.Any(x => x.Contains("must be persistent")), Is.True);
        }

        [Test]
        public void ComponentOwnedHole_MustHaveValidOwnerAndBeNonPersistent()
        {
            var definition = DisassemblyTestDefinitions.ComponentOwnedHole();
            definition.Holes[0].OwnerComponentId = "MISSING";
            definition.Holes[0].Persistent = true;
            var errors = DisassemblyValidator.ValidateDefinition(definition);
            Assert.That(errors.Any(x => x.Contains("invalid owner")), Is.True);
            Assert.That(errors.Any(x => x.Contains("cannot be persistent")), Is.True);
        }

        [Test]
        public void FasteningRelationships_MustBeReciprocal()
        {
            var definition = T01FirstReleaseDefinition.Create();
            definition.Holes[0].SecuredComponentIds = Array.Empty<string>();
            Assert.That(DisassemblyValidator.ValidateDefinition(definition).Any(x => x.Contains("inconsistent fastening")), Is.True);
        }

        [Test]
        public void CoveredByReference_MustExist()
        {
            var definition = T01FirstReleaseDefinition.Create();
            definition.Holes[1].CoveredByComponentIds = new[] { "MISSING" };
            Assert.That(DisassemblyValidator.ValidateDefinition(definition).Any(x => x.Contains("covering component references missing")), Is.True);
        }

        [Test]
        public void ReleaseDependencyCycle_IsRejected()
        {
            var definition = DisassemblyTestDefinitions.TransitiveRelease();
            definition.Components[0].ReleaseDependencyComponentIds = new[] { "C_B" };
            Assert.That(DisassemblyValidator.ValidateDefinition(definition).Any(x => x.Contains("Cycle detected")), Is.True);
        }

        [Test]
        public void InitiallyEligibleRelease_IsRejectedAsUnresolvedClosure()
        {
            var definition = T01FirstReleaseDefinition.Create();
            definition.Screws[0].InitialHoleId = "H_P1";
            Assert.That(DisassemblyValidator.ValidateDefinition(definition).Any(x => x.Contains("unresolved release closure")), Is.True);
        }

        [Test]
        public void OccupiedComponentHole_MustSecureItsOwner()
        {
            var definition = DisassemblyTestDefinitions.ComponentOwnedHole();
            definition.Components[0].SecuringHoleIds = new[] { "P_1" };
            definition.Holes[0].SecuredComponentIds = Array.Empty<string>();
            definition.Holes[0].Role = HoleRole.Parking;
            definition.Holes[1].Role = HoleRole.Structural;
            definition.Holes[1].SecuredComponentIds = new[] { "C_A" };
            Assert.That(DisassemblyValidator.ValidateDefinition(definition).Any(x => x.Contains("must secure its owner")), Is.True);
        }

        [Test]
        public void Snapshot_RejectsMissingScrewLocationAndPhaseMismatch()
        {
            var definition = T01FirstReleaseDefinition.Create();
            var snapshot = DisassemblyValidator.CreateInitialSnapshot(definition);
            snapshot.ScrewLocations = Array.Empty<ScrewLocationState>();
            snapshot.Phase = DisassemblyPhase.Complete;
            var errors = DisassemblyValidator.ValidateSnapshot(definition, snapshot, false);
            Assert.That(errors.Any(x => x.Contains("exactly one location")), Is.True);
            Assert.That(errors.Any(x => x.Contains("phase does not match")), Is.True);
        }

        [Test]
        public void Snapshot_RejectsScrewInReleasedOwnersVanishedHole()
        {
            var definition = DisassemblyTestDefinitions.ComponentOwnedHole();
            var snapshot = DisassemblyValidator.CreateInitialSnapshot(definition);
            snapshot.ReleasedComponentIds = new[] { "C_A" };
            snapshot.Phase = DisassemblyPhase.Complete;
            Assert.That(DisassemblyValidator.ValidateSnapshot(definition, snapshot, false)
                .Any(x => x.Contains("vanished component-owned hole")), Is.True);
        }
    }
}
