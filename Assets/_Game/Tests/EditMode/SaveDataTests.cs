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
    }
}
