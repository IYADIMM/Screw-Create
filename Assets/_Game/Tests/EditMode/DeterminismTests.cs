using NUnit.Framework;

namespace ScrewAndCreate.Core.Tests
{
    public sealed class DeterminismTests
    {
        [Test]
        public void PcgSequenceIsStable()
        {
            var rng = new DeterministicRng(42UL, 54UL);
            var golden = new uint[]
            {
                2707161783U, 2068313097U, 3122475824U, 2211639955U,
                3215226955U, 3421331566U, 3217466285U, 2167406445U
            };
            foreach (var expected in golden) Assert.That(rng.NextUInt(), Is.EqualTo(expected));
        }

        [Test]
        public void SeedCodeRoundTripsAndRejectsMutation()
        {
            var code = SeedCode.Encode(new SeedIdentity(1, 0x123456789ABCDEF0UL, CreationFamily.MeadowWindmill, DifficultyBand.Engineer));
            Assert.That(SeedCode.TryDecode(code, out var decoded), Is.True);
            Assert.That(decoded.Seed, Is.EqualTo(0x123456789ABCDEF0UL));
            Assert.That(decoded.Family, Is.EqualTo(CreationFamily.MeadowWindmill));
            Assert.That(decoded.Band, Is.EqualTo(DifficultyBand.Engineer));
            var mutated = code.Substring(0, code.Length - 1) + (code.EndsWith("0") ? "1" : "0");
            Assert.That(SeedCode.TryDecode(mutated, out _), Is.False);
        }

        [Test]
        public void GeneratorReproducesCanonicalDefinition()
        {
            var generator = new DeterministicGenerator();
            var a = generator.Generate(99UL, CreationFamily.LittleRover, DifficultyBand.Builder);
            var b = generator.Generate(99UL, CreationFamily.LittleRover, DifficultyBand.Builder);
            Assert.That(a.Definition.Id, Is.EqualTo(b.Definition.Id));
            Assert.That(a.Definition.Parts[0].AllowedRotations[0], Is.EqualTo(b.Definition.Parts[0].AllowedRotations[0]));
            Assert.That(a.Verification.Solved, Is.True);
            Assert.That(b.Verification.Actions, Has.Length.EqualTo(a.Verification.Actions.Length));
        }
    }
}
