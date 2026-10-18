using System;
using System.IO;
using NUnit.Framework;

namespace ScrewAndCreate.Core.Tests
{
    public sealed class SaveDataTests
    {
        string _directory;

        [SetUp]
        public void SetUp() => _directory = Path.Combine(Path.GetTempPath(), "ScrewAndCreateTests", Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }

        [Test]
        public void AtomicSaveRetainsRecoverablePreviousCopy()
        {
            var path = Path.Combine(_directory, "save.json");
            var service = new AtomicJsonSaveService(path);
            service.Save(new GameSaveData { DailyDate = "2026-10-17" });
            service.Save(new GameSaveData { DailyDate = "2026-10-18" });
            File.WriteAllText(path, "corrupt");
            Assert.That(service.TryLoad(out var recovered, out var usedPrevious), Is.True);
            Assert.That(usedPrevious, Is.True);
            Assert.That(recovered.DailyDate, Is.EqualTo("2026-10-17"));
        }

        [Test]
        public void VersionOneProgressMigratesToValidatedRecords()
        {
            var path = Path.Combine(_directory, "save.json");
            Directory.CreateDirectory(_directory);
            var old = new GameSaveData
            {
                SaveVersion = 1,
                CompletedLevelIds = new[] { "MW-01", "MW-02" },
                BestStars = new[] { 3, 2 }
            };
            File.WriteAllText(path, UnityEngine.JsonUtility.ToJson(old));
            var service = new AtomicJsonSaveService(path);
            Assert.That(service.TryLoad(out var loaded, out var recovered), Is.True);
            Assert.That(recovered, Is.False);
            Assert.That(loaded.SaveVersion, Is.EqualTo(2));
            Assert.That(loaded.LevelProgress[1].LevelId, Is.EqualTo("MW-02"));
            Assert.That(loaded.LevelProgress[1].BestStars, Is.EqualTo(2));
        }

        [Test]
        public void AndroidStyleReplaceFailureUsesSafeFallback()
        {
            var path = Path.Combine(_directory, "save.json");
            var service = new AtomicJsonSaveService(path, replace: (_, __, ___) => throw new PlatformNotSupportedException());
            service.Save(new GameSaveData { DailyDate = "first" });
            service.Save(new GameSaveData { DailyDate = "second" });
            Assert.That(service.TryLoad(out var loaded, out var recovered), Is.True);
            Assert.That(recovered, Is.False);
            Assert.That(loaded.DailyDate, Is.EqualTo("second"));
            File.WriteAllText(path, "corrupt");
            Assert.That(service.TryLoad(out loaded, out recovered), Is.True);
            Assert.That(recovered, Is.True);
            Assert.That(loaded.DailyDate, Is.EqualTo("first"));
        }

        [Test]
        public void IntegrityMismatchRejectsAlteredPayload()
        {
            var path = Path.Combine(_directory, "save.json");
            var service = new AtomicJsonSaveService(path);
            service.Save(new GameSaveData { DailyDate = "2026-10-18" });
            var json = File.ReadAllText(path).Replace("2026-10-18", "2026-10-19");
            File.WriteAllText(path, json);
            Assert.That(service.TryLoad(out _, out _), Is.False);
        }
    }
}
