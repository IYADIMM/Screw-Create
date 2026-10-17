using NUnit.Framework;

namespace ScrewAndCreate.Core.Tests
{
    public sealed class SolverValidationTests
    {
        [Test]
        public void SolverFindsFullRemoveAndAssemblePath()
        {
            var result = new PuzzleSolver().Solve(PuzzleEngineTests.TwoPartFixture());
            Assert.That(result.Solved, Is.True, result.FailureReason);
            Assert.That(result.Actions, Has.Length.EqualTo(4));
            Assert.That(result.Actions[0].ItemId, Is.EqualTo("S2"));
        }

        [Test]
        public void ValidationRejectsDependencyCycle()
        {
            var fixture = PuzzleEngineTests.TwoPartFixture();
            fixture.Screws[0].RequiredRemovedScrewIds = new[] { "S2" };
            fixture.Screws[1].RequiredRemovedScrewIds = new[] { "S1" };
            var errors = fixture.Validate();
            Assert.That(errors, Has.Some.Contains("Cycle"));
        }

        [Test]
        public void DevelopmentSeedBatchIsReproducibleAndSolvable()
        {
            var generator = new DeterministicGenerator();
            for (ulong seed = 0; seed < 250; seed++)
            {
                var a = generator.Generate(seed, CreationFamily.WoodlandBridge, DifficultyBand.Craftsperson);
                var b = generator.Generate(seed, CreationFamily.WoodlandBridge, DifficultyBand.Craftsperson);
                Assert.That(a.Verification.Solved, Is.True, $"Seed {seed}: {a.Verification.FailureReason}");
                Assert.That(a.Definition.Parts[0].AllowedRotations[0], Is.EqualTo(b.Definition.Parts[0].AllowedRotations[0]));
            }
        }
    }
}
