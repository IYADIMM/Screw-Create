using System;
using System.Globalization;
using System.Text;

namespace ScrewAndCreate.Core
{
    public readonly struct SeedIdentity
    {
        public SeedIdentity(int generatorVersion, ulong seed, CreationFamily family, DifficultyBand band, uint catalogHash = 0)
        { GeneratorVersion = generatorVersion; Seed = seed; Family = family; Band = band; CatalogHash = catalogHash; }
        public int GeneratorVersion { get; }
        public ulong Seed { get; }
        public CreationFamily Family { get; }
        public DifficultyBand Band { get; }
        public uint CatalogHash { get; }
    }

    public static class SeedCode
    {
        public const int LatestSupportedVersion = 2;

        public static string Encode(SeedIdentity identity)
        {
            if (identity.GeneratorVersion < 1 || identity.GeneratorVersion > LatestSupportedVersion) throw new ArgumentOutOfRangeException(nameof(identity));
            if (!Enum.IsDefined(typeof(CreationFamily), identity.Family) || !Enum.IsDefined(typeof(DifficultyBand), identity.Band)) throw new ArgumentOutOfRangeException(nameof(identity));
            if (identity.GeneratorVersion >= 2 && identity.CatalogHash == 0) throw new ArgumentException("Version-two seed identity requires a content catalog hash.", nameof(identity));
            var catalog = identity.GeneratorVersion == 1 ? "0" : identity.CatalogHash.ToString("X8", CultureInfo.InvariantCulture);
            var payload = $"SC-{identity.GeneratorVersion}-{identity.Seed:X16}-{(int)identity.Family}-{(int)identity.Band}-{catalog}";
            return $"{payload}-{Crc32.Compute(Encoding.ASCII.GetBytes(payload)):X8}";
        }

        public static bool TryDecode(string code, out SeedIdentity identity)
        {
            identity = default;
            if (string.IsNullOrWhiteSpace(code)) return false;
            var parts = code.Trim().ToUpperInvariant().Split('-');
            if (parts.Length != 7 || parts[0] != "SC") return false;
            var payload = string.Join("-", parts, 0, 6);
            if (!uint.TryParse(parts[6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var checksum) || Crc32.Compute(Encoding.ASCII.GetBytes(payload)) != checksum) return false;
            if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var version) || version < 1 || version > LatestSupportedVersion) return false;
            if (!ulong.TryParse(parts[2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var seed)) return false;
            if (!int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out var familyValue) || !Enum.IsDefined(typeof(CreationFamily), familyValue)) return false;
            if (!int.TryParse(parts[4], NumberStyles.None, CultureInfo.InvariantCulture, out var bandValue) || !Enum.IsDefined(typeof(DifficultyBand), bandValue)) return false;
            uint catalogHash;
            if (version == 1) { if (parts[5] != "0") return false; catalogHash = 0; }
            else if (!uint.TryParse(parts[5], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out catalogHash) || catalogHash == 0) return false;
            identity = new SeedIdentity(version, seed, (CreationFamily)familyValue, (DifficultyBand)bandValue, catalogHash);
            return true;
        }

        public static uint CatalogHashFor(string catalogId)
        {
            if (string.IsNullOrWhiteSpace(catalogId)) throw new ArgumentException("Catalog ID is required.", nameof(catalogId));
            var value = Crc32.Compute(Encoding.UTF8.GetBytes(catalogId));
            return value == 0 ? 1U : value;
        }

        public static string EncodeVersion1(ulong seed, CreationFamily family, DifficultyBand band) => Encode(new SeedIdentity(1, seed, family, band));
    }

    public static class Crc32
    {
        public static uint Compute(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
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
