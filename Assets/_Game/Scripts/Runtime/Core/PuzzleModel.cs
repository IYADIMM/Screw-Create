using System;
using System.Collections.Generic;
using System.Linq;

namespace ScrewAndCreate.Core
{
    public enum PuzzlePhase { Disassembly, Assembly, Complete, Failed }
    public enum DifficultyBand { Apprentice, Builder, Craftsperson, Engineer, Master }
    public enum CreationFamily { LittleRover, MeadowWindmill, WoodlandBridge }
    public enum PuzzleActionKind { RemoveScrew, AssemblePart }

    [Serializable]
    public sealed class ScrewDefinition
    {
        public string Id = string.Empty;
        public bool AuthoredLocked;
        public string[] RequiredRemovedScrewIds = Array.Empty<string>();
        public string[] CoveringPartIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class PartDefinition
    {
        public string Id = string.Empty;
        public string CompatibilityGroup = string.Empty;
        public string TargetSocketId = string.Empty;
        public string[] SecuringScrewIds = Array.Empty<string>();
        public string[] AssemblyDependencyPartIds = Array.Empty<string>();
        public int[] AllowedRotations = new[] { 0 };
        public float RotationToleranceDegrees = 8f;
    }

    [Serializable]
    public sealed class PuzzleDefinition
    {
        public string Id = string.Empty;
        public int TrayCapacity = 3;
        public ScrewDefinition[] Screws = Array.Empty<ScrewDefinition>();
        public PartDefinition[] Parts = Array.Empty<PartDefinition>();

        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(Id)) errors.Add("Puzzle ID is required.");
            if (TrayCapacity < 1) errors.Add("Tray capacity must be positive.");

            ValidateUniqueIds(Screws.Select(x => x.Id), "screw", errors);
            ValidateUniqueIds(Parts.Select(x => x.Id), "part", errors);
            var screwIds = new HashSet<string>(Screws.Select(x => x.Id), StringComparer.Ordinal);
            var partIds = new HashSet<string>(Parts.Select(x => x.Id), StringComparer.Ordinal);

            foreach (var screw in Screws)
            {
                CheckReferences(screw.RequiredRemovedScrewIds, screwIds, $"Screw {screw.Id} prerequisite", errors);
                CheckReferences(screw.CoveringPartIds, partIds, $"Screw {screw.Id} covering part", errors);
            }

            foreach (var part in Parts)
            {
                CheckReferences(part.SecuringScrewIds, screwIds, $"Part {part.Id} securing screw", errors);
                CheckReferences(part.AssemblyDependencyPartIds, partIds, $"Part {part.Id} assembly dependency", errors);
                if (string.IsNullOrWhiteSpace(part.TargetSocketId)) errors.Add($"Part {part.Id} needs a target socket.");
                if (string.IsNullOrWhiteSpace(part.CompatibilityGroup)) errors.Add($"Part {part.Id} needs a compatibility group.");
                if (part.AllowedRotations == null || part.AllowedRotations.Length == 0) errors.Add($"Part {part.Id} needs an allowed rotation.");
            }

            DetectCycle(Screws.ToDictionary(x => x.Id, x => x.RequiredRemovedScrewIds), "screw", errors);
            DetectCycle(Parts.ToDictionary(x => x.Id, x => x.AssemblyDependencyPartIds), "assembly", errors);
            return errors;
        }

        static void ValidateUniqueIds(IEnumerable<string> ids, string kind, ICollection<string> errors)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in ids)
            {
                if (string.IsNullOrWhiteSpace(id)) errors.Add($"A {kind} ID is empty.");
                else if (!seen.Add(id)) errors.Add($"Duplicate {kind} ID: {id}.");
            }
        }

        static void CheckReferences(IEnumerable<string> references, HashSet<string> known, string label, ICollection<string> errors)
        {
            foreach (var reference in references ?? Array.Empty<string>())
                if (!known.Contains(reference)) errors.Add($"{label} references missing ID {reference}.");
        }

        static void DetectCycle(IReadOnlyDictionary<string, string[]> graph, string label, ICollection<string> errors)
        {
            var visiting = new HashSet<string>(StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            foreach (var node in graph.Keys)
                if (Visit(node, graph, visiting, visited))
                {
                    errors.Add($"Cycle detected in {label} dependency graph.");
                    return;
                }
        }

        static bool Visit(string node, IReadOnlyDictionary<string, string[]> graph, HashSet<string> visiting, HashSet<string> visited)
        {
            if (visited.Contains(node)) return false;
            if (!visiting.Add(node)) return true;
            if (graph.TryGetValue(node, out var edges))
                foreach (var edge in edges ?? Array.Empty<string>())
                    if (graph.ContainsKey(edge) && Visit(edge, graph, visiting, visited)) return true;
            visiting.Remove(node);
            visited.Add(node);
            return false;
        }
    }

    [Serializable]
    public sealed class PuzzleSnapshot
    {
        public PuzzlePhase Phase;
        public string[] RemovedScrewIds = Array.Empty<string>();
        public string[] ReleasedPartIds = Array.Empty<string>();
        public string[] TrayPartIds = Array.Empty<string>();
        public string[] AssembledPartIds = Array.Empty<string>();
        public int ActionCount;
        public int HintCount;
        public int Revision;

        public string CanonicalKey() => string.Join(";", new[]
        {
            ((int)Phase).ToString(),
            string.Join(",", RemovedScrewIds.OrderBy(x => x, StringComparer.Ordinal)),
            string.Join(",", ReleasedPartIds.OrderBy(x => x, StringComparer.Ordinal)),
            string.Join(",", TrayPartIds),
            string.Join(",", AssembledPartIds.OrderBy(x => x, StringComparer.Ordinal))
        });
    }

    public readonly struct PuzzleAction
    {
        public PuzzleAction(PuzzleActionKind kind, string itemId, string targetId = "", int rotation = 0)
        {
            Kind = kind;
            ItemId = itemId;
            TargetId = targetId;
            Rotation = rotation;
        }

        public PuzzleActionKind Kind { get; }
        public string ItemId { get; }
        public string TargetId { get; }
        public int Rotation { get; }
        public override string ToString() => $"{Kind}:{ItemId}:{TargetId}:{Rotation}";
    }

    public readonly struct MoveResult
    {
        public MoveResult(bool success, string reason) { Success = success; Reason = reason; }
        public bool Success { get; }
        public string Reason { get; }
        public static MoveResult Ok() => new MoveResult(true, string.Empty);
        public static MoveResult Reject(string reason) => new MoveResult(false, reason);
    }
}
