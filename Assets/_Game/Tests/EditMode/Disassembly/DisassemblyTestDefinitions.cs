using System;

namespace ScrewAndCreate.Disassembly.Tests
{
    static class DisassemblyTestDefinitions
    {
        public static DisassemblyDefinition MultipleFasteners() => new DisassemblyDefinition
        {
            Id = "MULTIPLE_FASTENERS",
            Screws = new[] { Screw("S_1", "H_1"), Screw("S_2", "H_2") },
            Holes = new[]
            {
                Structural("H_1", "C"), Structural("H_2", "C"), Parking("P_1"), Parking("P_2")
            },
            Components = new[] { Component("C", new[] { "H_1", "H_2" }) },
            RequiredComponentIds = new[] { "C" }
        };

        public static DisassemblyDefinition CoveredContent() => new DisassemblyDefinition
        {
            Id = "COVERED_CONTENT",
            Screws = new[] { Screw("S_TOP", "H_TOP"), Screw("S_BOTTOM", "H_BOTTOM") },
            Holes = new[]
            {
                Structural("H_TOP", "C_TOP"),
                Structural("H_BOTTOM", "C_BOTTOM", coveredBy: new[] { "C_TOP" }),
                Parking("P_1"),
                Parking("P_2", new[] { "C_TOP" })
            },
            Components = new[]
            {
                Component("C_TOP", new[] { "H_TOP" }),
                Component("C_BOTTOM", new[] { "H_BOTTOM" })
            },
            RequiredComponentIds = new[] { "C_TOP", "C_BOTTOM" }
        };

        public static DisassemblyDefinition ComponentOwnedHole() => new DisassemblyDefinition
        {
            Id = "COMPONENT_OWNED",
            Screws = new[] { Screw("S_A", "H_A") },
            Holes = new[]
            {
                new DisassemblyHoleDefinition
                {
                    Id = "H_A", OwnerKind = HoleOwnerKind.Component, OwnerComponentId = "C_A",
                    Persistent = false, Role = HoleRole.Structural, SecuredComponentIds = new[] { "C_A" }
                },
                Parking("P_1")
            },
            Components = new[] { Component("C_A", new[] { "H_A" }) },
            RequiredComponentIds = new[] { "C_A" }
        };

        public static DisassemblyDefinition SharedStructuralHole() => new DisassemblyDefinition
        {
            Id = "SHARED_STRUCTURAL_HOLE",
            Screws = new[]
            {
                Screw("S_SHARED", "H_SHARED"), Screw("S_B", "H_B"), Screw("S_C", "H_C")
            },
            Holes = new[]
            {
                Structural("H_SHARED", "C_A", "C_B"), Structural("H_B", "C_B"), Structural("H_C", "C_C"),
                Parking("P_1"), Parking("P_2"), Parking("P_3")
            },
            Components = new[]
            {
                Component("C_A", new[] { "H_SHARED" }),
                Component("C_B", new[] { "H_SHARED", "H_B" }),
                Component("C_C", new[] { "H_C" })
            },
            RequiredComponentIds = new[] { "C_A", "C_B", "C_C" }
        };

        public static DisassemblyDefinition TransitiveRelease() => new DisassemblyDefinition
        {
            Id = "TRANSITIVE_RELEASE",
            Screws = new[] { Screw("S_A", "H_A") },
            Holes = new[] { Structural("H_A", "C_A"), Structural("H_B", "C_B"), Parking("P_1") },
            Components = new[]
            {
                Component("C_A", new[] { "H_A" }),
                Component("C_B", new[] { "H_B" }, new[] { "C_A" })
            },
            RequiredComponentIds = new[] { "C_A", "C_B" }
        };

        public static DisassemblyScrewDefinition Screw(string id, string holeId) =>
            new DisassemblyScrewDefinition { Id = id, InitialHoleId = holeId };

        public static DisassemblyHoleDefinition Parking(string id, string[] coveredBy = null) =>
            new DisassemblyHoleDefinition
            {
                Id = id, OwnerKind = HoleOwnerKind.Baseboard, Persistent = true, Role = HoleRole.Parking,
                SecuredComponentIds = Array.Empty<string>(), CoveredByComponentIds = coveredBy ?? Array.Empty<string>()
            };

        public static DisassemblyHoleDefinition Structural(string id, params string[] secured) =>
            Structural(id, secured, Array.Empty<string>());

        public static DisassemblyHoleDefinition Structural(string id, string secured, string[] coveredBy) =>
            Structural(id, new[] { secured }, coveredBy);

        static DisassemblyHoleDefinition Structural(string id, string[] secured, string[] coveredBy) =>
            new DisassemblyHoleDefinition
            {
                Id = id, OwnerKind = HoleOwnerKind.Baseboard, Persistent = true, Role = HoleRole.Structural,
                SecuredComponentIds = secured, CoveredByComponentIds = coveredBy
            };

        public static DisassemblyComponentDefinition Component(
            string id, string[] securingHoles, string[] dependencies = null) =>
            new DisassemblyComponentDefinition
            {
                Id = id, SecuringHoleIds = securingHoles,
                ReleaseDependencyComponentIds = dependencies ?? Array.Empty<string>()
            };
    }
}
