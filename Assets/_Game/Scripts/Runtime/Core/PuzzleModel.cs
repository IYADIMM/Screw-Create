using System;
using System.Collections.Generic;
using System.Linq;

namespace ScrewAndCreate.Core
{
    public enum PuzzlePhase { Disassembly, Assembly, Complete, Failed }
    public enum InteractionMode { Disassembly, Assembly }
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
    public sealed class SocketDefinition
    {
        public string Id = string.Empty;
        public string VisualTargetId = string.Empty;
        public string[] AcceptedCompatibilityGroups = Array.Empty<string>();
        public string[] AllowedPartIds = Array.Empty<string>();
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
        public SocketDefinition[] Sockets = Array.Empty<SocketDefinition>();

        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(Id)) errors.Add("Puzzle ID is required.");
            if (TrayCapacity < 1) errors.Add("Tray capacity must be positive.");
            if (Screws == null) errors.Add("Screws collection is null.");
            if (Parts == null) errors.Add("Parts collection is null.");
            if (Sockets == null) errors.Add("Sockets collection is null.");
            var screws = (Screws ?? Array.Empty<ScrewDefinition>()).Where(x => x != null).ToArray();
            var parts = (Parts ?? Array.Empty<PartDefinition>()).Where(x => x != null).ToArray();
            var sockets = (Sockets ?? Array.Empty<SocketDefinition>()).Where(x => x != null).ToArray();
            if (screws.Length != (Screws ?? Array.Empty<ScrewDefinition>()).Length) errors.Add("Screws contains a null entry.");
            if (parts.Length != (Parts ?? Array.Empty<PartDefinition>()).Length) errors.Add("Parts contains a null entry.");
            if (sockets.Length != (Sockets ?? Array.Empty<SocketDefinition>()).Length) errors.Add("Sockets contains a null entry.");

            ValidateUniqueIds(screws.Select(x => x.Id), "screw", errors);
            ValidateUniqueIds(parts.Select(x => x.Id), "part", errors);
            ValidateUniqueIds(sockets.Select(x => x.Id), "socket", errors);
            var screwIds = new HashSet<string>(screws.Select(x => x.Id).Where(ValidId), StringComparer.Ordinal);
            var partIds = new HashSet<string>(parts.Select(x => x.Id).Where(ValidId), StringComparer.Ordinal);
            var socketIds = new HashSet<string>(sockets.Select(x => x.Id).Where(ValidId), StringComparer.Ordinal);

            foreach (var screw in screws)
            {
                CheckArray(screw.RequiredRemovedScrewIds, $"Screw {screw.Id} prerequisites", errors);
                CheckArray(screw.CoveringPartIds, $"Screw {screw.Id} covering parts", errors);
                CheckReferences(screw.RequiredRemovedScrewIds, screwIds, $"Screw {screw.Id} prerequisite", errors);
                CheckReferences(screw.CoveringPartIds, partIds, $"Screw {screw.Id} covering part", errors);
            }

            foreach (var part in parts)
            {
                CheckArray(part.SecuringScrewIds, $"Part {part.Id} securing screws", errors);
                CheckArray(part.AssemblyDependencyPartIds, $"Part {part.Id} assembly dependencies", errors);
                CheckArray(part.AllowedRotations, $"Part {part.Id} allowed rotations", errors);
                CheckReferences(part.SecuringScrewIds, screwIds, $"Part {part.Id} securing screw", errors);
                CheckReferences(part.AssemblyDependencyPartIds, partIds, $"Part {part.Id} assembly dependency", errors);
                if (string.IsNullOrWhiteSpace(part.TargetSocketId) || !socketIds.Contains(part.TargetSocketId)) errors.Add($"Part {part.Id} references an invalid target socket.");
                if (string.IsNullOrWhiteSpace(part.CompatibilityGroup)) errors.Add($"Part {part.Id} needs a compatibility group.");
                ValidateRotationSet(part.AllowedRotations, part.RotationToleranceDegrees, $"Part {part.Id}", errors);
                if ((part.SecuringScrewIds ?? Array.Empty<string>()).Any(id => screws.Any(s => s.Id == id && s.AuthoredLocked)))
                    errors.Add($"Part {part.Id} requires a permanently authored-locked screw.");
            }

            foreach (var socket in sockets)
            {
                CheckArray(socket.AcceptedCompatibilityGroups, $"Socket {socket.Id} compatibility groups", errors);
                CheckArray(socket.AllowedPartIds, $"Socket {socket.Id} allowed parts", errors);
                CheckArray(socket.AssemblyDependencyPartIds, $"Socket {socket.Id} dependencies", errors);
                CheckArray(socket.AllowedRotations, $"Socket {socket.Id} allowed rotations", errors);
                if ((socket.AcceptedCompatibilityGroups ?? Array.Empty<string>()).Length == 0) errors.Add($"Socket {socket.Id} accepts no compatibility group.");
                if ((socket.AcceptedCompatibilityGroups ?? Array.Empty<string>()).Any(string.IsNullOrWhiteSpace)) errors.Add($"Socket {socket.Id} has an empty compatibility group.");
                CheckReferences(socket.AllowedPartIds, partIds, $"Socket {socket.Id} allowed part", errors);
                CheckReferences(socket.AssemblyDependencyPartIds, partIds, $"Socket {socket.Id} dependency", errors);
                ValidateRotationSet(socket.AllowedRotations, socket.RotationToleranceDegrees, $"Socket {socket.Id}", errors);
            }

            DetectCycle(screws.ToDictionary(x => x.Id, x => x.RequiredRemovedScrewIds ?? Array.Empty<string>()), "screw", errors);
            DetectCycle(parts.ToDictionary(x => x.Id, x => x.AssemblyDependencyPartIds ?? Array.Empty<string>()), "part assembly", errors);
            return errors;
        }

        static bool ValidId(string id) => !string.IsNullOrWhiteSpace(id);

        static void ValidateUniqueIds(IEnumerable<string> ids, string kind, ICollection<string> errors)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in ids)
            {
                if (string.IsNullOrWhiteSpace(id)) errors.Add($"A {kind} ID is empty.");
                else if (!seen.Add(id)) errors.Add($"Duplicate {kind} ID: {id}.");
            }
        }

        static void CheckArray<T>(T[] values, string label, ICollection<string> errors)
        { if (values == null) errors.Add($"{label} collection is null."); }

        static void CheckReferences(IEnumerable<string> references, HashSet<string> known, string label, ICollection<string> errors)
        {
            foreach (var reference in references ?? Array.Empty<string>())
                if (string.IsNullOrWhiteSpace(reference) || !known.Contains(reference)) errors.Add($"{label} references missing ID {reference ?? "<null>"}.");
        }

        static void ValidateRotationSet(int[] rotations, float tolerance, string label, ICollection<string> errors)
        {
            if (float.IsNaN(tolerance) || float.IsInfinity(tolerance) || tolerance < 0f || tolerance > 180f) errors.Add($"{label} has invalid rotation tolerance.");
            if (rotations == null || rotations.Length == 0) { errors.Add($"{label} needs an allowed rotation."); return; }
            if (rotations.Any(x => x < 0 || x >= 360)) errors.Add($"{label} rotations must be normalized to 0–359 degrees.");
            if (rotations.Distinct().Count() != rotations.Length) errors.Add($"{label} has duplicate rotations.");
        }

        static void DetectCycle(IReadOnlyDictionary<string, string[]> graph, string label, ICollection<string> errors)
        {
            var visiting = new HashSet<string>(StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            foreach (var node in graph.Keys)
                if (Visit(node, graph, visiting, visited)) { errors.Add($"Cycle detected in {label} dependency graph."); return; }
        }

        static bool Visit(string node, IReadOnlyDictionary<string, string[]> graph, HashSet<string> visiting, HashSet<string> visited)
        {
            if (visited.Contains(node)) return false;
            if (!visiting.Add(node)) return true;
            if (graph.TryGetValue(node, out var edges))
                foreach (var edge in edges ?? Array.Empty<string>())
                    if (graph.ContainsKey(edge) && Visit(edge, graph, visiting, visited)) return true;
            visiting.Remove(node); visited.Add(node); return false;
        }
    }

    [Serializable]
    public sealed class PuzzleSnapshot
    {
        public PuzzlePhase Phase;
        public InteractionMode InteractionMode;
        public string[] RemovedScrewIds = Array.Empty<string>();
        public string[] ReleasedPartIds = Array.Empty<string>();
        public string[] TrayPartIds = Array.Empty<string>();
        public string[] AssembledPartIds = Array.Empty<string>();
        public int ActionCount;
        public int HintCount;
        public int Revision;

        public string SolverKey() => string.Join(";", new[]
        {
            string.Join(",", (RemovedScrewIds ?? Array.Empty<string>()).OrderBy(x => x, StringComparer.Ordinal)),
            string.Join(",", (ReleasedPartIds ?? Array.Empty<string>()).OrderBy(x => x, StringComparer.Ordinal)),
            string.Join(",", TrayPartIds ?? Array.Empty<string>()),
            string.Join(",", (AssembledPartIds ?? Array.Empty<string>()).OrderBy(x => x, StringComparer.Ordinal))
        });

        public string CanonicalKey() => $"{(int)Phase};{(int)InteractionMode};{SolverKey()};{ActionCount};{HintCount};{Revision}";
    }

    public readonly struct PuzzleAction
    {
        public PuzzleAction(PuzzleActionKind kind, string itemId, string targetId = "", int rotation = 0)
        { Kind = kind; ItemId = itemId; TargetId = targetId; Rotation = rotation; }
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
