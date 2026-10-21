using System.Linq;
using NUnit.Framework;
using ScrewAndCreate.Disassembly;
using UnityEngine;

namespace ScrewAndCreate.DisassemblyPresentation.Tests
{
    public sealed class DisassemblyPresentationMappingTests
    {
        [Test]
        public void ExactT01ViewMapping_IsAccepted()
        {
            var root = new GameObject("MappingTest");
            try
            {
                var screw = root.AddComponent<DisassemblyScrewView>(); screw.Id = "S_1";
                var sourceObject = new GameObject("Source"); sourceObject.transform.SetParent(root.transform); var source = sourceObject.AddComponent<DisassemblyHoleView>(); source.Id = "H_C1"; source.ScrewAnchor = source.transform;
                var parkingObject = new GameObject("Parking"); parkingObject.transform.SetParent(root.transform); var parking = parkingObject.AddComponent<DisassemblyHoleView>(); parking.Id = "H_P1"; parking.ScrewAnchor = parking.transform;
                var componentObject = new GameObject("Component"); componentObject.transform.SetParent(root.transform); var component = componentObject.AddComponent<DisassemblyComponentView>(); component.Id = "C_PLANK"; component.ReleasedPose = new GameObject("Released").transform; component.ReleasedPose.SetParent(root.transform);

                var errors = DisassemblyT01Controller.ValidateMappings(
                    T01FirstReleaseDefinition.Create(), new[] { screw }, new[] { source, parking }, new[] { component });

                Assert.That(errors, Is.Empty);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void DuplicateAndMissingIds_ReturnActionableErrors()
        {
            var root = new GameObject("BadMappingTest");
            try
            {
                var first = root.AddComponent<DisassemblyScrewView>(); first.Id = "S_1";
                var secondObject = new GameObject("Duplicate"); secondObject.transform.SetParent(root.transform); var second = secondObject.AddComponent<DisassemblyScrewView>(); second.Id = "S_1";
                var errors = DisassemblyT01Controller.ValidateMappings(
                    T01FirstReleaseDefinition.Create(), new[] { first, second }, new DisassemblyHoleView[0], new DisassemblyComponentView[0]);
                Assert.That(errors.Any(x => x.Contains("Duplicate screw view ID S_1")), Is.True);
                Assert.That(errors.Any(x => x.Contains("Missing hole view ID H_C1")), Is.True);
                Assert.That(errors.Any(x => x.Contains("Missing component view ID C_PLANK")), Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
