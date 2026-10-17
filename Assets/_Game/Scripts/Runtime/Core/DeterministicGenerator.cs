using System;

namespace ScrewAndCreate.Core
{
    public sealed class GeneratedPuzzle
    {
        public int GeneratorVersion;
        public ulong Seed;
        public CreationFamily Family;
        public DifficultyBand Band;
        public PuzzleDefinition Definition = new PuzzleDefinition();
        public SolverResult Verification = new SolverResult();
    }

    /// <summary>Version-one generator skeleton: builds a legal reverse chain and independently solves it.</summary>
    public sealed class DeterministicGenerator
    {
        public const int Version = 1;

        public GeneratedPuzzle Generate(ulong seed, CreationFamily family, DifficultyBand band)
        {
            var rng = new DeterministicRng(seed, (ulong)family + 1UL);
            var partCount = 2 + Math.Min((int)band, 2);
            var screws = new ScrewDefinition[partCount];
            var parts = new PartDefinition[partCount];
            for (var index = 0; index < partCount; index++)
            {
                var screwId = $"S{index + 1:D2}";
                var partId = $"P{index + 1:D2}";
                screws[index] = new ScrewDefinition
                {
                    Id = screwId,
                    CoveringPartIds = index + 1 < partCount ? new[] { $"P{index + 2:D2}" } : Array.Empty<string>()
                };
                parts[index] = new PartDefinition
                {
                    Id = partId,
                    CompatibilityGroup = $"{family}-wood",
                    TargetSocketId = $"Socket-{partId}",
                    SecuringScrewIds = new[] { screwId },
                    AssemblyDependencyPartIds = index == 0 ? Array.Empty<string>() : new[] { $"P{index:D2}" },
                    AllowedRotations = new[] { rng.NextInt(4) * 90 },
                    RotationToleranceDegrees = 8f
                };
            }

            var definition = new PuzzleDefinition
            {
                Id = $"GEN-V{Version}-{family}-{band}-{seed:X16}",
                TrayCapacity = partCount,
                Screws = screws,
                Parts = parts
            };
            return new GeneratedPuzzle
            {
                GeneratorVersion = Version,
                Seed = seed,
                Family = family,
                Band = band,
                Definition = definition,
                Verification = new PuzzleSolver().Solve(definition)
            };
        }
    }
}
