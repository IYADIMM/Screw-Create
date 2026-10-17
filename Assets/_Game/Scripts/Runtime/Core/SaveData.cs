using System;
using System.IO;
using UnityEngine;

namespace ScrewAndCreate.Core
{
    [Serializable]
    public sealed class GameSaveData
    {
        public const int CurrentVersion = 1;
        public int SaveVersion = CurrentVersion;
        public int GeneratorVersion = DeterministicGenerator.Version;
        public string ContentCatalogVersion = "dev-1";
        public string[] CompletedLevelIds = Array.Empty<string>();
        public int[] BestStars = Array.Empty<int>();
        public string[] UnlockedFamilies = Array.Empty<string>();
        public bool TutorialComplete;
        public bool HapticsEnabled = true;
        public bool ReducedCameraMotion;
        public float MasterVolume = 1f;
        public float MusicVolume = 0.8f;
        public float SfxVolume = 1f;
        public PuzzleSnapshot ActivePuzzle;
        public string ActiveSeedCode = string.Empty;
        public string DailyDate = string.Empty;
        public string DailyTimezoneId = string.Empty;
        public bool DailyCompleted;
    }

    public interface ISaveMigration
    {
        int FromVersion { get; }
        int ToVersion { get; }
        GameSaveData Migrate(GameSaveData source);
    }

    public interface ISaveService
    {
        void Save(GameSaveData data);
        bool TryLoad(out GameSaveData data, out bool recoveredPrevious);
    }

    /// <summary>Writes a same-volume temporary file and retains the replaced file as recovery data.</summary>
    public sealed class AtomicJsonSaveService : ISaveService
    {
        readonly string _path;
        string TempPath => _path + ".tmp";
        string PreviousPath => _path + ".previous";

        public AtomicJsonSaveService(string path) => _path = path ?? throw new ArgumentNullException(nameof(path));

        public void Save(GameSaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(TempPath, JsonUtility.ToJson(data, true));
            if (File.Exists(_path)) File.Replace(TempPath, _path, PreviousPath, true);
            else File.Move(TempPath, _path);
        }

        public bool TryLoad(out GameSaveData data, out bool recoveredPrevious)
        {
            recoveredPrevious = false;
            if (TryRead(_path, out data)) return true;
            if (TryRead(PreviousPath, out data)) { recoveredPrevious = true; return true; }
            data = null;
            return false;
        }

        static bool TryRead(string path, out GameSaveData data)
        {
            data = null;
            if (!File.Exists(path)) return false;
            try
            {
                data = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(path));
                return data != null && data.SaveVersion > 0 && data.SaveVersion <= GameSaveData.CurrentVersion;
            }
            catch (Exception) { return false; }
        }
    }
}
