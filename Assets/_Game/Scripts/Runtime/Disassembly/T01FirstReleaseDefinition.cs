using System;

namespace ScrewAndCreate.Disassembly
{
    public static class T01FirstReleaseDefinition
    {
        public static DisassemblyDefinition Create() => new DisassemblyDefinition
        {
            Id = "T01",
            Screws = new[]
            {
                new DisassemblyScrewDefinition { Id = "S_1", InitialHoleId = "H_C1" }
            },
            Holes = new[]
            {
                new DisassemblyHoleDefinition
                {
                    Id = "H_C1",
                    OwnerKind = HoleOwnerKind.Baseboard,
                    OwnerComponentId = string.Empty,
                    Persistent = true,
                    Role = HoleRole.Structural,
                    SecuredComponentIds = new[] { "C_PLANK" },
                    CoveredByComponentIds = Array.Empty<string>()
                },
                new DisassemblyHoleDefinition
                {
                    Id = "H_P1",
                    OwnerKind = HoleOwnerKind.Baseboard,
                    OwnerComponentId = string.Empty,
                    Persistent = true,
                    Role = HoleRole.Parking,
                    SecuredComponentIds = Array.Empty<string>(),
                    CoveredByComponentIds = Array.Empty<string>()
                }
            },
            Components = new[]
            {
                new DisassemblyComponentDefinition
                {
                    Id = "C_PLANK",
                    SecuringHoleIds = new[] { "H_C1" },
                    ReleaseDependencyComponentIds = Array.Empty<string>()
                }
            },
            RequiredComponentIds = new[] { "C_PLANK" }
        };
    }
}
