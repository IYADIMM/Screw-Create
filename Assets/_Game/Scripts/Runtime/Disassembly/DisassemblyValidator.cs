using System;
using System.Collections.Generic;
using System.Linq;

namespace ScrewAndCreate.Disassembly
{
    public static class DisassemblyValidator
    {
        public static IReadOnlyList<string> ValidateDefinition(DisassemblyDefinition definition)
        {
            var errors = new List<string>();
            if (definition == null) { errors.Add("Definition is null."); return errors; }
            if (string.IsNullOrWhiteSpace(definition.Id)) errors.Add("Definition ID is required.");
            if (definition.Screws == null) errors.Add("Screws collection is null.");
            if (definition.Holes == null) errors.Add("Holes collection is null.");
            if (definition.Components == null) errors.Add("Components collection is null.");
            if (definition.RequiredComponentIds == null) errors.Add("Required components collection is null.");

            var screws = NonNull(definition.Screws, "Screws", errors);
            var holes = NonNull(definition.Holes, "Holes", errors);
            var components = NonNull(definition.Components, "Components", errors);
            ValidateUniqueIds(screws.Select(x => x.Id), "screw", errors);
            ValidateUniqueIds(holes.Select(x => x.Id), "hole", errors);
            ValidateUniqueIds(components.Select(x => x.Id), "component", errors);

            var holeIds = IdSet(holes.Select(x => x.Id));
            var componentIds = IdSet(components.Select(x => x.Id));
            var holeById = holes.Where(x => ValidId(x.Id)).GroupBy(x => x.Id, StringComparer.Ordinal)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
            var componentById = components.Where(x => ValidId(x.Id)).GroupBy(x => x.Id, StringComparer.Ordinal)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);

            var occupied = new HashSet<string>(StringComparer.Ordinal);
            foreach (var screw in screws)
            {
                if (string.IsNullOrWhiteSpace(screw.InitialHoleId) || !holeIds.Contains(screw.InitialHoleId))
                    errors.Add($"Screw {screw.Id} references invalid initial hole {screw.InitialHoleId ?? "<null>"}.");
                else if (!occupied.Add(screw.InitialHoleId))
                    errors.Add($"Multiple screws occupy initial hole {screw.InitialHoleId}.");
            }

            foreach (var hole in holes)
            {
                if (!Enum.IsDefined(typeof(HoleOwnerKind), hole.OwnerKind))
                    errors.Add($"Hole {hole.Id} has an invalid owner kind.");
                if (!Enum.IsDefined(typeof(HoleRole), hole.Role))
                    errors.Add($"Hole {hole.Id} has an invalid role.");
                CheckArray(hole.SecuredComponentIds, $"Hole {hole.Id} secured components", errors);
                CheckArray(hole.CoveredByComponentIds, $"Hole {hole.Id} covering components", errors);
                CheckDistinct(hole.SecuredComponentIds, $"Hole {hole.Id} secured components", errors);
                CheckDistinct(hole.CoveredByComponentIds, $"Hole {hole.Id} covering components", errors);
                CheckReferences(hole.SecuredComponentIds, componentIds, $"Hole {hole.Id} secured component", errors);
                CheckReferences(hole.CoveredByComponentIds, componentIds, $"Hole {hole.Id} covering component", errors);

                if (hole.OwnerKind == HoleOwnerKind.Baseboard)
                {
                    if (!string.IsNullOrEmpty(hole.OwnerComponentId)) errors.Add($"Baseboard hole {hole.Id} cannot name an owner component.");
                    if (!hole.Persistent) errors.Add($"Baseboard hole {hole.Id} must be persistent.");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(hole.OwnerComponentId) || !componentIds.Contains(hole.OwnerComponentId))
                        errors.Add($"Component-owned hole {hole.Id} references an invalid owner.");
                    if (hole.Persistent) errors.Add($"Component-owned hole {hole.Id} cannot be persistent.");
                    if (occupied.Contains(hole.Id) && componentById.TryGetValue(hole.OwnerComponentId ?? string.Empty, out var owner) &&
                        !(owner.SecuringHoleIds ?? Array.Empty<string>()).Contains(hole.Id, StringComparer.Ordinal))
                        errors.Add($"Occupied component-owned hole {hole.Id} must secure its owner so no screw disappears on release.");
                }

                if (hole.Role == HoleRole.Parking && (hole.SecuredComponentIds ?? Array.Empty<string>()).Length > 0)
                    errors.Add($"Parking hole {hole.Id} cannot secure components.");
                if (hole.Role == HoleRole.Structural && (hole.SecuredComponentIds ?? Array.Empty<string>()).Length == 0)
                    errors.Add($"Structural hole {hole.Id} must secure at least one component.");
            }

            foreach (var component in components)
            {
                CheckArray(component.SecuringHoleIds, $"Component {component.Id} securing holes", errors);
                CheckArray(component.ReleaseDependencyComponentIds, $"Component {component.Id} release dependencies", errors);
                CheckDistinct(component.SecuringHoleIds, $"Component {component.Id} securing holes", errors);
                CheckDistinct(component.ReleaseDependencyComponentIds, $"Component {component.Id} release dependencies", errors);
                if ((component.SecuringHoleIds ?? Array.Empty<string>()).Length == 0)
                    errors.Add($"Component {component.Id} needs at least one securing hole.");
                CheckReferences(component.SecuringHoleIds, holeIds, $"Component {component.Id} securing hole", errors);
                CheckReferences(component.ReleaseDependencyComponentIds, componentIds, $"Component {component.Id} release dependency", errors);
                if ((component.ReleaseDependencyComponentIds ?? Array.Empty<string>()).Contains(component.Id, StringComparer.Ordinal))
                    errors.Add($"Component {component.Id} cannot depend on itself.");

                foreach (var holeId in component.SecuringHoleIds ?? Array.Empty<string>())
                {
                    if (!holeById.TryGetValue(holeId, out var hole)) continue;
                    if (hole.Role != HoleRole.Structural || !(hole.SecuredComponentIds ?? Array.Empty<string>()).Contains(component.Id, StringComparer.Ordinal))
                        errors.Add($"Component {component.Id} and hole {holeId} have inconsistent fastening relationships.");
                }
            }

            foreach (var hole in holes.Where(x => x.Role == HoleRole.Structural))
                foreach (var componentId in hole.SecuredComponentIds ?? Array.Empty<string>())
                    if (componentById.TryGetValue(componentId, out var component) &&
                        !(component.SecuringHoleIds ?? Array.Empty<string>()).Contains(hole.Id, StringComparer.Ordinal))
                        errors.Add($"Hole {hole.Id} and component {componentId} have inconsistent fastening relationships.");

            CheckDistinct(definition.RequiredComponentIds, "Required components", errors);
            CheckReferences(definition.RequiredComponentIds, componentIds, "Required component", errors);
            if ((definition.RequiredComponentIds ?? Array.Empty<string>()).Length == 0)
                errors.Add("At least one required component is needed.");

            var dependencyGraph = components
                .Where(x => ValidId(x.Id))
                .GroupBy(x => x.Id, StringComparer.Ordinal)
                .ToDictionary(
                    x => x.Key,
                    x => x.First().ReleaseDependencyComponentIds ?? Array.Empty<string>(),
                    StringComparer.Ordinal);
            DetectCycle(dependencyGraph, errors);

            if (errors.Count == 0)
            {
                var initial = CreateInitialSnapshotUnchecked(definition);
                var snapshotErrors = ValidateSnapshot(definition, initial, requireStableClosure: true);
                errors.AddRange(snapshotErrors.Select(x => $"Initial state: {x}"));
            }
            return errors;
        }

        public static DisassemblySnapshot CreateInitialSnapshot(DisassemblyDefinition definition)
        {
            var errors = ValidateDefinition(definition);
            if (errors.Count > 0) throw new ArgumentException(string.Join(" ", errors), nameof(definition));
            return CreateInitialSnapshotUnchecked(definition);
        }

        public static IReadOnlyList<string> ValidateSnapshot(
            DisassemblyDefinition definition,
            DisassemblySnapshot snapshot,
            bool requireStableClosure = true)
        {
            var errors = new List<string>();
            if (definition == null) { errors.Add("Definition is null."); return errors; }
            if (snapshot == null) { errors.Add("Snapshot is null."); return errors; }
            var definitionErrors = ValidateDefinitionShapeOnly(definition);
            if (definitionErrors.Count > 0) { errors.AddRange(definitionErrors); return errors; }

            var screwIds = IdSet(definition.Screws.Select(x => x.Id));
            var holeIds = IdSet(definition.Holes.Select(x => x.Id));
            var componentIds = IdSet(definition.Components.Select(x => x.Id));
            if (snapshot.ScrewLocations == null) errors.Add("Snapshot screw locations collection is null.");
            if (snapshot.ReleasedComponentIds == null) errors.Add("Snapshot released components collection is null.");
            var locations = NonNull(snapshot.ScrewLocations, "Snapshot screw locations", errors);
            ValidateUniqueIds(locations.Select(x => x.ScrewId), "snapshot screw", errors);
            if (locations.Length != screwIds.Count) errors.Add("Snapshot must contain exactly one location for every screw.");
            var occupied = new HashSet<string>(StringComparer.Ordinal);
            foreach (var location in locations)
            {
                if (!screwIds.Contains(location.ScrewId)) errors.Add($"Snapshot references unknown screw {location.ScrewId}.");
                if (!holeIds.Contains(location.HoleId)) errors.Add($"Snapshot references unknown hole {location.HoleId}.");
                else if (!occupied.Add(location.HoleId)) errors.Add($"Snapshot places multiple screws in hole {location.HoleId}.");
            }

            CheckDistinct(snapshot.ReleasedComponentIds, "Snapshot released components", errors);
            CheckReferences(snapshot.ReleasedComponentIds, componentIds, "Snapshot released component", errors);
            var released = IdSet(snapshot.ReleasedComponentIds);
            foreach (var location in locations)
            {
                var hole = definition.Holes.FirstOrDefault(x => string.Equals(x.Id, location.HoleId, StringComparison.Ordinal));
                if (hole != null && hole.OwnerKind == HoleOwnerKind.Component && released.Contains(hole.OwnerComponentId))
                    errors.Add($"Screw {location.ScrewId} occupies vanished component-owned hole {hole.Id}.");
            }

            if (snapshot.ActionCount < 0) errors.Add("Action count cannot be negative.");
            if (snapshot.Revision < 0) errors.Add("Revision cannot be negative.");
            if (!Enum.IsDefined(typeof(DisassemblyPhase), snapshot.Phase))
                errors.Add("Snapshot phase is invalid.");
            var complete = (definition.RequiredComponentIds ?? Array.Empty<string>()).All(released.Contains);
            if ((snapshot.Phase == DisassemblyPhase.Complete) != complete)
                errors.Add("Snapshot phase does not match required-component completion.");

            if (requireStableClosure && errors.Count == 0)
            {
                var eligible = DisassemblyRules.FindEligibleComponents(definition, snapshot);
                if (eligible.Count > 0)
                    errors.Add($"Snapshot has unresolved release closure: {string.Join(",", eligible)}.");
            }
            return errors;
        }

        static DisassemblySnapshot CreateInitialSnapshotUnchecked(DisassemblyDefinition definition) => new DisassemblySnapshot
        {
            Phase = DisassemblyPhase.Active,
            ScrewLocations = definition.Screws.OrderBy(x => x.Id, StringComparer.Ordinal)
                .Select(x => new ScrewLocationState { ScrewId = x.Id, HoleId = x.InitialHoleId }).ToArray(),
            ReleasedComponentIds = Array.Empty<string>(),
            ActionCount = 0,
            Revision = 0
        };

        static IReadOnlyList<string> ValidateDefinitionShapeOnly(DisassemblyDefinition definition)
        {
            var errors = new List<string>();
            if (definition.Screws == null || definition.Holes == null || definition.Components == null || definition.RequiredComponentIds == null)
                errors.Add("Definition collections cannot be null.");
            if ((definition.Screws ?? Array.Empty<DisassemblyScrewDefinition>()).Any(x => x == null) ||
                (definition.Holes ?? Array.Empty<DisassemblyHoleDefinition>()).Any(x => x == null) ||
                (definition.Components ?? Array.Empty<DisassemblyComponentDefinition>()).Any(x => x == null))
                errors.Add("Definition collections cannot contain null entries.");
            return errors;
        }

        static T[] NonNull<T>(T[] values, string label, ICollection<string> errors) where T : class
        {
            var source = values ?? Array.Empty<T>();
            if (source.Any(x => x == null)) errors.Add($"{label} contains a null entry.");
            return source.Where(x => x != null).ToArray();
        }

        static bool ValidId(string id) => !string.IsNullOrWhiteSpace(id);
        static HashSet<string> IdSet(IEnumerable<string> values) =>
            new HashSet<string>((values ?? Array.Empty<string>()).Where(ValidId), StringComparer.Ordinal);

        static void ValidateUniqueIds(IEnumerable<string> ids, string label, ICollection<string> errors)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in ids ?? Array.Empty<string>())
            {
                if (!ValidId(id)) errors.Add($"A {label} ID is empty.");
                else if (!seen.Add(id)) errors.Add($"Duplicate {label} ID: {id}.");
            }
        }

        static void CheckArray<T>(T[] values, string label, ICollection<string> errors)
        { if (values == null) errors.Add($"{label} collection is null."); }

        static void CheckDistinct(IEnumerable<string> values, string label, ICollection<string> errors)
        {
            if (values == null) return;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in values)
                if (!seen.Add(value ?? string.Empty)) errors.Add($"{label} contains duplicate ID {value ?? "<null>"}.");
        }

        static void CheckReferences(IEnumerable<string> references, HashSet<string> known, string label, ICollection<string> errors)
        {
            foreach (var reference in references ?? Array.Empty<string>())
                if (!ValidId(reference) || !known.Contains(reference)) errors.Add($"{label} references missing ID {reference ?? "<null>"}.");
        }

        static void DetectCycle(IReadOnlyDictionary<string, string[]> graph, ICollection<string> errors)
        {
            var visiting = new HashSet<string>(StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            foreach (var node in graph.Keys)
                if (Visit(node, graph, visiting, visited))
                { errors.Add("Cycle detected in component release dependency graph."); return; }
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
}
