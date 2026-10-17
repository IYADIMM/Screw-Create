using System;
using System.Globalization;
using System.Text;

namespace ScrewAndCreate.Core
{
    public readonly struct SeedIdentity
    {
        public SeedIdentity(int generatorVersion, ulong seed, CreationFamily family, DifficultyBand band)
        {
            GeneratorVersion = generatorVersion;
            Seed = seed;
            Family = family;
            Band = band;
        }
        public int GeneratorVersion { get; }
        public ulong Seed { get; }
        public CreationFamily Family { get; }
        public DifficultyBand Band { get; }
    }

    public static class SeedCode
    {
        public static string Encode(SeedIdentity identity)
        {
            var payload = $"SC-{identity.GeneratorVersion}-{identity.Seed:X16}-{(int)identity.Family}-{(int)identity.Band}-0";
            return $"{payload}-{Crc32.Compute(Encoding.ASCII.GetBytes(payload)):X8}";
        }

        public static bool TryDecode(string code, out SeedIdentity identity)
        {
            identity = default;
            if (string.IsNullOrWhiteSpace(code)) return false;
            var parts = code.Trim().ToUpperInvariant().Split('-');
            if (parts.Length != 7 || parts[0] != "SC") return false;
            var payload = string.Join("-", parts, 0, 6);
            if (!uint.TryParse(parts[6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var checksum)) return false;
            if (Crc32.Compute(Encoding.ASCII.GetBytes(payload)) != checksum) return false;
            if (!int.TryParse(parts[1], out var version) || version < 1) return false;
            if (!ulong.TryParse(parts[2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var seed)) return false;
            if (!int.TryParse(parts[3], out var familyValue) || !Enum.IsDefined(typeof(CreationFamily), familyValue)) return false;
            if (!int.TryParse(parts[4], out var bandValue) || !Enum.IsDefined(typeof(DifficultyBand), bandValue)) return false;
            // The fixed payload has six sections: SC, version, seed, family, band. A reserved section protects future expansion.
            if (parts[5] != "0") return false;
            identity = new SeedIdentity(version, seed, (CreationFamily)familyValue, (DifficultyBand)bandValue);
            return true;
        }

        public static string EncodeVersion1(ulong seed, CreationFamily family, DifficultyBand band)
        {
            var payload = $"SC-1-{seed:X16}-{(int)family}-{(int)band}-0";
            return $"{payload}-{Crc32.Compute(Encoding.ASCII.GetBytes(payload)):X8}";
        }
    }

    public static class Crc32
    {
        public static uint Compute(byte[] bytes)
        {
            uint crc = 0xFFFFFFFFU;
            foreach (var value in bytes)
            {
                crc ^= value;
                for (var bit = 0; bit < 8; bit++) crc = (crc >> 1) ^ (0xEDB88320U & unchecked((uint)-(int)(crc & 1U)));
            }
            return ~crc;
        }
    }
}
