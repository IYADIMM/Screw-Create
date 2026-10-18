using System;
using System.Collections.Generic;
using System.Linq;

namespace ScrewAndCreate.Core
{
    [Serializable]
    public sealed class DifficultyProfile
    {
        public DifficultyBand Band;
        public int MinParts;
        public int MaxParts;
        public int MinLayers;
        public int MaxLayers;
        public int MaxDependencyDepth;
        public int MaxBranching;
        public int MinOpeningMoves;
        public int MaxOpeningMoves;
        public int RotationChoiceCount;
    }

    public sealed class GeneratedPuzzle
    {
        public int GeneratorVersion;
        public string ContentCatalogId = string.Empty;
        public ulong Seed;
        public CreationFamily Family;
        public DifficultyBand Band;
        public int DifficultyScore;
        public int Attempts;
        public string[] RejectionReasons = Array.Empty<string>();
        public PuzzleDefinition Definition = new PuzzleDefinition();
        public SolverResult Verification = new SolverResult();
    }

    /// <summary>Version-two generator with bounded profiles, mixed-loop tray pressure, and independent solver verification.</summary>
    public sealed class DeterministicGenerator
    {
        public const int Version = 2;
        public const string ContentCatalogId = "local-v2";
        public const int MaxAttempts = 12;

        static readonly DifficultyProfile[] Profiles =
        {
            new DifficultyProfile { Band=DifficultyBand.Apprentice, MinParts=2, MaxParts=3, MinLayers=1, MaxLayers=2, MaxDependencyDepth=1, MaxBranching=2, MinOpeningMoves=1, MaxOpeningMoves=2, RotationChoiceCount=1 },
            new DifficultyProfile { Band=DifficultyBand.Builder, MinParts=3, MaxParts=4, MinLayers=2, MaxLayers=3, MaxDependencyDepth=2, MaxBranching=2, MinOpeningMoves=1, MaxOpeningMoves=2, RotationChoiceCount=2 },
            new DifficultyProfile { Band=DifficultyBand.Craftsperson, MinParts=4, MaxParts=5, MinLayers=2, MaxLayers=4, MaxDependencyDepth=3, MaxBranching=3, MinOpeningMoves=1, MaxOpeningMoves=2, RotationChoiceCount=2 },
            new DifficultyProfile { Band=DifficultyBand.Engineer, MinParts=5, MaxParts=6, MinLayers=3, MaxLayers=5, MaxDependencyDepth=4, MaxBranching=3, MinOpeningMoves=1, MaxOpeningMoves=2, RotationChoiceCount=3 },
            new DifficultyProfile { Band=DifficultyBand.Master, MinParts=6, MaxParts=7, MinLayers=4, MaxLayers=6, MaxDependencyDepth=5, MaxBranching=3, MinOpeningMoves=1, MaxOpeningMoves=2, RotationChoiceCount=4 }
        };

        public static DifficultyProfile GetProfile(DifficultyBand band) => Profiles.Single(x => x.Band == band);

        public GeneratedPuzzle Generate(ulong seed, CreationFamily family, DifficultyBand band)
        {
            if (!Enum.IsDefined(typeof(CreationFamily), family)) throw new ArgumentOutOfRangeException(nameof(family));
            if (!Enum.IsDefined(typeof(DifficultyBand), band)) throw new ArgumentOutOfRangeException(nameof(band));
            var rejections = new List<string>();
            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var attemptSeed = seed + unchecked((ulong)attempt * 0x9E3779B97F4A7C15UL);
                var definition = BuildCandidate(attemptSeed, family, band);
                var errors = definition.Validate();
                if (errors.Count > 0) { rejections.Add("validation:" + errors[0]); continue; }
                var verification = new PuzzleSolver().Solve(definition, 50000);
                if (!verification.Solved) { rejections.Add(verification.ResourceLimitReached ? "solver-budget" : "unsolved"); continue; }
                return new GeneratedPuzzle
                {
                    GeneratorVersion = Version, ContentCatalogId = ContentCatalogId, Seed = seed, Family = family, Band = band,
                    DifficultyScore = Score(definition, verification), Attempts = attempt + 1,
                    RejectionReasons = rejections.ToArray(), Definition = definition, Verification = verification
                };
            }
            return new GeneratedPuzzle
            {
                GeneratorVersion = Version, ContentCatalogId = ContentCatalogId, Seed = seed, Family = family, Band = band,
                Attempts = MaxAttempts, RejectionReasons = rejections.ToArray(),
                Verification = new SolverResult { FailureReason = "Generator attempt limit exhausted." }
            };
        }

        PuzzleDefinition BuildCandidate(ulong seed, CreationFamily family, DifficultyBand band)
        {
            var profile = GetProfile(band);
            var rng = new DeterministicRng(seed, ((ulong)family + 1UL) * 17UL + (ulong)band);
            var count = profile.MinParts + rng.NextInt(profile.MaxParts - profile.MinParts + 1);
            var openingMoves = Math.Min(count, profile.MinOpeningMoves + rng.NextInt(profile.MaxOpeningMoves - profile.MinOpeningMoves + 1));
            var screws = new ScrewDefinition[count]; var parts = new PartDefinition[count]; var sockets = new SocketDefinition[count];
            for (var i = 0; i < count; i++)
            {
                var screwId = $"S{i + 1:D2}"; var partId = $"P{i + 1:D2}"; var socketId = $"Socket-{partId}";
                var parent = i < openingMoves ? -1 : Math.Max(0, i - 1 - rng.NextInt(Math.Min(profile.MaxBranching, i)));
                var assemblyParent = i == 0 ? -1 : Math.Max(0, i - 1 - rng.NextInt(Math.Min(profile.MaxBranching, i)));
                var rotationCount = Math.Max(1, Math.Min(profile.RotationChoiceCount, 4));
                var rotations = Enumerable.Range(0, rotationCount).Select(x => ((x + rng.NextInt(4)) % 4) * 90).Distinct().OrderBy(x => x).ToArray();
                var group = $"{family}-wood-{(i + (int)(seed & 1UL)) % Math.Min(2, 1 + (int)band)}";
                screws[i] = new ScrewDefinition { Id = screwId, RequiredRemovedScrewIds = parent < 0 ? Array.Empty<string>() : new[] { $"S{parent + 1:D2}" } };
                parts[i] = new PartDefinition
                {
                    Id = partId, CompatibilityGroup = group, TargetSocketId = socketId, SecuringScrewIds = new[] { screwId },
                    AssemblyDependencyPartIds = assemblyParent < 0 ? Array.Empty<string>() : new[] { $"P{assemblyParent + 1:D2}" },
                    AllowedRotations = rotations, RotationToleranceDegrees = 10f
                };
                sockets[i] = new SocketDefinition
                {
                    Id = socketId, VisualTargetId = $"Ghost-{partId}", AcceptedCompatibilityGroups = new[] { group }, AllowedPartIds = new[] { partId },
                    AssemblyDependencyPartIds = assemblyParent < 0 ? Array.Empty<string>() : new[] { $"P{assemblyParent + 1:D2}" },
                    AllowedRotations = rotations, RotationToleranceDegrees = 10f
                };
            }
            return new PuzzleDefinition { Id = $"GEN-V{Version}-{ContentCatalogId}-{family}-{band}-{seed:X16}", TrayCapacity = 3, Screws = screws, Parts = parts, Sockets = sockets };
        }

        static int Score(PuzzleDefinition definition, SolverResult solver)
        {
            var screwEdges = definition.Screws.Sum(x => x.RequiredRemovedScrewIds.Length);
            var assemblyEdges = definition.Parts.Sum(x => x.AssemblyDependencyPartIds.Length);
            var rotationChoices = definition.Parts.Sum(x => x.AllowedRotations.Length - 1);
            return definition.Parts.Length * 10 + screwEdges * 4 + assemblyEdges * 5 + rotationChoices * 2 + Math.Min(20, solver.StatesVisited / 5);
        }
    }
}
