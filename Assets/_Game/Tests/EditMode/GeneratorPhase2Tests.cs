using System;
using System.Linq;
using NUnit.Framework;

namespace ScrewAndCreate.Core.Tests
{
    public sealed class GeneratorPhase2Tests
    {
        static readonly ulong[] CuratedSeeds =
        {
            0UL, 1UL, 2UL, 3UL, 5UL, 8UL, 13UL, 21UL, 34UL, 55UL,
            89UL, 144UL, 233UL, 377UL, 610UL, 987UL, 1597UL, 2584UL, 4181UL, 6765UL,
            10946UL, 17711UL, 28657UL, 46368UL, 75025UL, 121393UL, 196418UL, 317811UL, 514229UL, ulong.MaxValue
        };

        [Test]
        public void EveryDifficultyBandStaysInsideItsProfile()
        {
            var generator = new DeterministicGenerator();
            foreach (DifficultyBand band in Enum.GetValues(typeof(DifficultyBand)))
            {
                var profile = DeterministicGenerator.GetProfile(band);
                for (ulong seed = 0; seed < 20; seed++)
                {
                    var generated = generator.Generate(seed, CreationFamily.MeadowWindmill, band);
                    Assert.That(generated.Verification.Solved, Is.True, $"{band}/{seed}: {generated.Verification.FailureReason}");
                    Assert.That(generated.Definition.Parts.Length, Is.InRange(profile.MinParts, profile.MaxParts));
                    Assert.That(generated.Definition.TrayCapacity, Is.EqualTo(3));
                    Assert.That(generated.DifficultyScore, Is.GreaterThan(0));
                }
            }
        }

        [Test]
        public void ThirtyCuratedSeedsCoverEveryFamilyAndBand()
        {
            var generator = new DeterministicGenerator();
            for (var index = 0; index < CuratedSeeds.Length; index++)
            {
                var family = (CreationFamily)(index % 3);
                var band = (DifficultyBand)(index % 5);
                var first = generator.Generate(CuratedSeeds[index], family, band);
                var second = generator.Generate(CuratedSeeds[index], family, band);
                Assert.That(first.Verification.Solved, Is.True, $"curated {index}");
                Assert.That(Signature(first), Is.EqualTo(Signature(second)), $"curated {index}");
            }
        }

        [Test]
        public void OneThousandSeedDevelopmentBatchIsSolvedAndReproducible()
        {
            var generator = new DeterministicGenerator();
            var maxStates = 0; long totalStates = 0;
            for (ulong seed = 0; seed < 1000; seed++)
            {
                var family = (CreationFamily)(seed % 3);
                var band = (DifficultyBand)((seed / 3) % 5);
                var first = generator.Generate(seed, family, band);
                var second = generator.Generate(seed, family, band);
                Assert.That(first.Verification.Solved, Is.True, $"seed {seed}: {first.Verification.FailureReason}");
                Assert.That(Signature(first), Is.EqualTo(Signature(second)), $"seed {seed} reproducibility");
                totalStates += first.Verification.StatesVisited;
                maxStates = Math.Max(maxStates, first.Verification.StatesVisited);
            }
            TestContext.Progress.WriteLine($"PHASE2_BATCH accepted=1000 rejected=0 failures=0 averageStates={(double)totalStates / 1000:0.000} maxStates={maxStates} reproducibilityFailures=0");
        }

        [Test]
        public void DifferentSeedsChangeStructureBeyondIdentity()
        {
            var generator = new DeterministicGenerator();
            var signatures = Enumerable.Range(0, 12).Select(x => StructuralSignature(generator.Generate((ulong)x, CreationFamily.MeadowWindmill, DifficultyBand.Engineer))).Distinct().Count();
            Assert.That(signatures, Is.GreaterThanOrEqualTo(4));
        }

        static string Signature(GeneratedPuzzle puzzle) => puzzle.Definition.Id + ":" + StructuralSignature(puzzle);

        static string StructuralSignature(GeneratedPuzzle puzzle) => string.Join("|", puzzle.Definition.Parts.Select((p, i) =>
            $"{p.AssemblyDependencyPartIds.FirstOrDefault()}:{string.Join(",", p.AllowedRotations)}:{p.CompatibilityGroup}:{string.Join(",", puzzle.Definition.Screws[i].RequiredRemovedScrewIds)}"));
    }
}
