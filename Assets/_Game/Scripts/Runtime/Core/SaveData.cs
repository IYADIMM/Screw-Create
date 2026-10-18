using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ScrewAndCreate.Core
{
    [Serializable]
    public sealed class LevelProgressRecord
    {
        public string LevelId = string.Empty;
        public int BestStars;
    }

    [Serializable]
    public sealed class PuzzleMoveRecord
    {
        public PuzzleActionKind Kind;
        public string ItemId = string.Empty;
        public string TargetId = string.Empty;
        public int Rotation;
    }

    [Serializable]
    public sealed class GameSaveData
    {
        public const int CurrentVersion = 2;
        public int SaveVersion = CurrentVersion;
        public int GeneratorVersion = DeterministicGenerator.Version;
        public string ContentCatalogVersion = DeterministicGenerator.ContentCatalogId;
        public LevelProgressRecord[] LevelProgress = Array.Empty<LevelProgressRecord>();
        public string[] UnlockedFamilies = Array.Empty<string>();
        public bool TutorialComplete;
        public bool HapticsEnabled = true;
        public bool ReducedCameraMotion;
        public float MasterVolume = 1f;
        public float MusicVolume = 0.8f;
        public float SfxVolume = 1f;
        public string ActiveAuthoredLevelId = string.Empty;
        public string ActivePuzzleIdentity = string.Empty;
        public DifficultyBand ActiveDifficulty;
        public InteractionMode ActiveInteractionMode;
        public PuzzleSnapshot ActivePuzzle;
        public PuzzleMoveRecord[] ActiveMoveHistory = Array.Empty<PuzzleMoveRecord>();
        public string ActiveSeedCode = string.Empty;
        public string DailyDate = string.Empty;
        public string DailyTimezoneId = string.Empty;
        public bool DailyCompleted;

        // Retained only so version-one JSON can be migrated safely.
        public string[] CompletedLevelIds = Array.Empty<string>();
        public int[] BestStars = Array.Empty<int>();

        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            if (SaveVersion < 1 || SaveVersion > CurrentVersion) errors.Add("Unsupported save version.");
            if (GeneratorVersion < 1 || GeneratorVersion > DeterministicGenerator.Version) errors.Add("Unsupported generator version.");
            if (string.IsNullOrWhiteSpace(ContentCatalogVersion)) errors.Add("Content catalog identity is required.");
            if (LevelProgress == null || LevelProgress.Any(x => x == null || string.IsNullOrWhiteSpace(x.LevelId) || x.BestStars < 0 || x.BestStars > 3)) errors.Add("Level progress is malformed.");
            if (LevelProgress != null && LevelProgress.Select(x => x?.LevelId).Where(x => x != null).Distinct(StringComparer.Ordinal).Count() != LevelProgress.Count(x => x != null)) errors.Add("Level progress contains duplicate IDs.");
            if (UnlockedFamilies == null || ActiveMoveHistory == null) errors.Add("Save collections cannot be null.");
            if (!InUnitRange(MasterVolume) || !InUnitRange(MusicVolume) || !InUnitRange(SfxVolume)) errors.Add("Volume values must be finite and between zero and one.");
            return errors;
        }

        static bool InUnitRange(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f && value <= 1f;
    }

    public interface ISaveMigration
    {
        int FromVersion { get; }
        int ToVersion { get; }
        GameSaveData Migrate(GameSaveData source);
    }

    public sealed class SaveV1ToV2Migration : ISaveMigration
    {
        public int FromVersion => 1;
        public int ToVersion => 2;

        public GameSaveData Migrate(GameSaveData source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var ids = source.CompletedLevelIds ?? Array.Empty<string>();
            var stars = source.BestStars ?? Array.Empty<int>();
            source.LevelProgress = ids.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal)
                .Select(id => new LevelProgressRecord { LevelId = id, BestStars = Math.Max(0, Math.Min(3, Array.IndexOf(ids, id) < stars.Length ? stars[Array.IndexOf(ids, id)] : 0)) }).ToArray();
            source.CompletedLevelIds = Array.Empty<string>(); source.BestStars = Array.Empty<int>();
            source.ContentCatalogVersion = string.IsNullOrWhiteSpace(source.ContentCatalogVersion) ? "dev-1" : source.ContentCatalogVersion;
            source.SaveVersion = ToVersion;
            return source;
        }
    }

    public sealed class SaveMigrationPipeline
    {
        readonly Dictionary<int, ISaveMigration> _steps;
        public SaveMigrationPipeline(IEnumerable<ISaveMigration> steps = null)
        { _steps = (steps ?? new ISaveMigration[] { new SaveV1ToV2Migration() }).ToDictionary(x => x.FromVersion); }

        public bool TryMigrate(GameSaveData source, out GameSaveData migrated)
        {
            migrated = source;
            if (source == null || source.SaveVersion < 1 || source.SaveVersion > GameSaveData.CurrentVersion) return false;
            while (migrated.SaveVersion < GameSaveData.CurrentVersion)
            {
                if (!_steps.TryGetValue(migrated.SaveVersion, out var step) || step.ToVersion <= step.FromVersion) return false;
                migrated = step.Migrate(migrated);
            }
            return migrated.Validate().Count == 0;
        }
    }

    public interface ISaveService
    {
        void Save(GameSaveData data);
        bool TryLoad(out GameSaveData data, out bool recoveredPrevious);
    }

    [Serializable]
    sealed class SaveEnvelope
    {
        public int FormatVersion = 1;
        public string Payload = string.Empty;
        public uint PayloadCrc32;
    }

    /// <summary>Same-volume atomic save with integrity detection, previous-copy recovery, and a File.Replace fallback used by Android filesystems.</summary>
    public sealed class AtomicJsonSaveService : ISaveService
    {
        readonly string _path;
        readonly SaveMigrationPipeline _migrations;
        readonly Action<string, string, string> _replace;
        string TempPath => _path + ".tmp";
        string PreviousPath => _path + ".previous";

        public AtomicJsonSaveService(string path, SaveMigrationPipeline migrations = null, Action<string, string, string> replace = null)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
            _migrations = migrations ?? new SaveMigrationPipeline();
            _replace = replace ?? ((source, destination, backup) => File.Replace(source, destination, backup, true));
        }

        public void Save(GameSaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            data.SaveVersion = GameSaveData.CurrentVersion;
            var validation = data.Validate();
            if (validation.Count > 0) throw new ArgumentException(string.Join(" ", validation), nameof(data));
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            var payload = JsonUtility.ToJson(data, true);
            var envelope = new SaveEnvelope { Payload = payload, PayloadCrc32 = Crc32.Compute(Encoding.UTF8.GetBytes(payload)) };
            File.WriteAllText(TempPath, JsonUtility.ToJson(envelope, true));
            if (File.Exists(_path)) ReplaceWithFallback(); else File.Move(TempPath, _path);
        }

        public bool TryLoad(out GameSaveData data, out bool recoveredPrevious)
        {
            recoveredPrevious = false;
            if (TryRead(_path, out data)) return true;
            if (TryRead(PreviousPath, out data)) { recoveredPrevious = true; return true; }
            data = null; return false;
        }

        void ReplaceWithFallback()
        {
            try { _replace(TempPath, _path, PreviousPath); }
            catch (Exception exception) when (exception is PlatformNotSupportedException || exception is IOException || exception is UnauthorizedAccessException)
            {
                if (File.Exists(PreviousPath)) File.Delete(PreviousPath);
                File.Copy(_path, PreviousPath, true);
                File.Delete(_path);
                File.Move(TempPath, _path);
            }
        }

        bool TryRead(string path, out GameSaveData data)
        {
            data = null;
            if (!File.Exists(path)) return false;
            try
            {
                var json = File.ReadAllText(path);
                string payload;
                var envelope = JsonUtility.FromJson<SaveEnvelope>(json);
                if (envelope != null && !string.IsNullOrEmpty(envelope.Payload))
                {
                    if (Crc32.Compute(Encoding.UTF8.GetBytes(envelope.Payload)) != envelope.PayloadCrc32) return false;
                    payload = envelope.Payload;
                }
                else payload = json; // Backward-compatible version-one raw JSON.
                var parsed = JsonUtility.FromJson<GameSaveData>(payload);
                return _migrations.TryMigrate(parsed, out data);
            }
            catch (Exception) { data = null; return false; }
        }
    }
}
