using System;
using System.Collections.Generic;
using System.Linq;

namespace ScrewAndCreate.Disassembly
{
    public static class DisassemblyRules
    {
        public static IReadOnlyList<string> GetAccessibleScrewIds(
            DisassemblyDefinition definition,
            DisassemblySnapshot snapshot)
        {
            EnsureValidState(definition, snapshot);
            if (snapshot.Phase != DisassemblyPhase.Active) return Array.Empty<string>();
            return snapshot.ScrewLocations
                .Where(x => HoleExists(definition, snapshot, x.HoleId) && IsHoleExposed(definition, snapshot, x.HoleId))
                .Select(x => x.ScrewId)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();
        }

        public static IReadOnlyList<string> GetLegalDestinationIds(
            DisassemblyDefinition definition,
            DisassemblySnapshot snapshot,
            string screwId)
        {
            EnsureValidState(definition, snapshot);
            if (snapshot.Phase != DisassemblyPhase.Active) return Array.Empty<string>();
            var source = snapshot.ScrewLocations.FirstOrDefault(x => string.Equals(x.ScrewId, screwId, StringComparison.Ordinal));
            if (source == null || !IsScrewAccessibleUnchecked(definition, snapshot, source)) return Array.Empty<string>();
            var occupied = new HashSet<string>(snapshot.ScrewLocations.Select(x => x.HoleId), StringComparer.Ordinal);
            return definition.Holes
                .Where(h => !string.Equals(h.Id, source.HoleId, StringComparison.Ordinal))
                .Where(h => IsLegalDestinationUnchecked(definition, snapshot, h, occupied))
                .Select(h => h.Id)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();
        }

        public static IReadOnlyList<DisassemblyMove> GetLegalMoves(
            DisassemblyDefinition definition,
            DisassemblySnapshot snapshot)
        {
            EnsureValidState(definition, snapshot);
            if (snapshot.Phase != DisassemblyPhase.Active) return Array.Empty<DisassemblyMove>();
            var moves = new List<DisassemblyMove>();
            foreach (var screwId in GetAccessibleScrewIds(definition, snapshot))
            {
                var source = snapshot.ScrewLocations.First(x => string.Equals(x.ScrewId, screwId, StringComparison.Ordinal));
                foreach (var destinationId in GetLegalDestinationIds(definition, snapshot, screwId))
                    moves.Add(new DisassemblyMove(screwId, source.HoleId, destinationId));
            }
            return moves;
        }

        public static bool HasAnyLegalMove(DisassemblyDefinition definition, DisassemblySnapshot snapshot) =>
            GetLegalMoves(definition, snapshot).Count > 0;

        public static DisassemblyTransitionResult TryTransferScrew(
            DisassemblyDefinition definition,
            DisassemblySnapshot snapshot,
            string screwId,
            string destinationHoleId)
        {
            if (definition == null) return DisassemblyTransitionResult.Reject("Definition is null.");
            if (snapshot == null) return DisassemblyTransitionResult.Reject("Snapshot is null.");
            var definitionErrors = DisassemblyValidator.ValidateDefinition(definition);
            if (definitionErrors.Count > 0)
                return DisassemblyTransitionResult.Reject($"Definition is invalid: {string.Join(" ", definitionErrors)}");
            var snapshotErrors = DisassemblyValidator.ValidateSnapshot(definition, snapshot);
            if (snapshotErrors.Count > 0)
                return DisassemblyTransitionResult.Reject($"Snapshot is invalid: {string.Join(" ", snapshotErrors)}");
            if (snapshot.Phase != DisassemblyPhase.Active)
                return DisassemblyTransitionResult.Reject("Puzzle is not active.");
            if (string.IsNullOrWhiteSpace(screwId))
                return DisassemblyTransitionResult.Reject("Screw ID is required.");
            var source = snapshot.ScrewLocations.FirstOrDefault(x => string.Equals(x.ScrewId, screwId, StringComparison.Ordinal));
            if (source == null) return DisassemblyTransitionResult.Reject("Unknown screw.");
            if (!IsScrewAccessibleUnchecked(definition, snapshot, source))
                return DisassemblyTransitionResult.Reject("Screw is not accessible.");
            if (string.Equals(source.HoleId, destinationHoleId, StringComparison.Ordinal))
                return DisassemblyTransitionResult.Reject("Destination must differ from source.");
            var destination = definition.Holes.FirstOrDefault(x => string.Equals(x.Id, destinationHoleId, StringComparison.Ordinal));
            if (destination == null) return DisassemblyTransitionResult.Reject("Unknown destination hole.");
            var occupied = new HashSet<string>(snapshot.ScrewLocations.Select(x => x.HoleId), StringComparer.Ordinal);
            if (!IsLegalDestinationUnchecked(definition, snapshot, destination, occupied))
                return DisassemblyTransitionResult.Reject("Destination is not an exposed empty persistent parking location.");

            var next = snapshot.Clone();
            next.ScrewLocations.First(x => string.Equals(x.ScrewId, screwId, StringComparison.Ordinal)).HoleId = destinationHoleId;
            next.ActionCount++;
            next.Revision++;
            var previouslyReleased = new HashSet<string>(next.ReleasedComponentIds, StringComparer.Ordinal);
            ResolveReleaseClosure(definition, next);
            var released = new HashSet<string>(next.ReleasedComponentIds, StringComparer.Ordinal);
            next.Phase = definition.RequiredComponentIds.All(released.Contains)
                ? DisassemblyPhase.Complete
                : DisassemblyPhase.Active;
            var resultErrors = DisassemblyValidator.ValidateSnapshot(definition, next);
            if (resultErrors.Count > 0)
                return DisassemblyTransitionResult.Reject($"Resulting snapshot is invalid: {string.Join(" ", resultErrors)}");
            var newlyReleased = released.Except(previouslyReleased).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            return DisassemblyTransitionResult.Accept(next, newlyReleased);
        }

        public static IReadOnlyList<string> FindEligibleComponents(
            DisassemblyDefinition definition,
            DisassemblySnapshot snapshot)
        {
            var occupied = new HashSet<string>(
                (snapshot.ScrewLocations ?? Array.Empty<ScrewLocationState>())
                .Where(x => x != null)
                .Select(x => x.HoleId), StringComparer.Ordinal);
            var released = new HashSet<string>(snapshot.ReleasedComponentIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            return (definition.Components ?? Array.Empty<DisassemblyComponentDefinition>())
                .Where(x => x != null && !released.Contains(x.Id))
                .Where(x => (x.ReleaseDependencyComponentIds ?? Array.Empty<string>()).All(released.Contains))
                .Where(x => (x.SecuringHoleIds ?? Array.Empty<string>()).All(holeId => !occupied.Contains(holeId)))
                .Select(x => x.Id)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();
        }

        public static bool HoleExists(
            DisassemblyDefinition definition,
            DisassemblySnapshot snapshot,
            string holeId)
        {
            var hole = definition.Holes.FirstOrDefault(x => string.Equals(x.Id, holeId, StringComparison.Ordinal));
            if (hole == null) return false;
            if (hole.OwnerKind == HoleOwnerKind.Baseboard) return hole.Persistent;
            return !(snapshot.ReleasedComponentIds ?? Array.Empty<string>())
                .Contains(hole.OwnerComponentId, StringComparer.Ordinal);
        }

        public static bool IsHoleExposed(
            DisassemblyDefinition definition,
            DisassemblySnapshot snapshot,
            string holeId)
        {
            var hole = definition.Holes.FirstOrDefault(x => string.Equals(x.Id, holeId, StringComparison.Ordinal));
            if (hole == null || !HoleExists(definition, snapshot, holeId)) return false;
            var released = new HashSet<string>(snapshot.ReleasedComponentIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            return (hole.CoveredByComponentIds ?? Array.Empty<string>()).All(released.Contains);
        }

        static void ResolveReleaseClosure(DisassemblyDefinition definition, DisassemblySnapshot snapshot)
        {
            var released = new HashSet<string>(snapshot.ReleasedComponentIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            while (true)
            {
                snapshot.ReleasedComponentIds = released.OrderBy(x => x, StringComparer.Ordinal).ToArray();
                var eligible = FindEligibleComponents(definition, snapshot);
                if (eligible.Count == 0) break;
                foreach (var componentId in eligible) released.Add(componentId);
            }
            snapshot.ReleasedComponentIds = released.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        }

        static bool IsScrewAccessibleUnchecked(
            DisassemblyDefinition definition,
            DisassemblySnapshot snapshot,
            ScrewLocationState location) =>
            HoleExists(definition, snapshot, location.HoleId) &&
            IsHoleExposed(definition, snapshot, location.HoleId);

        static bool IsLegalDestinationUnchecked(
            DisassemblyDefinition definition,
            DisassemblySnapshot snapshot,
            DisassemblyHoleDefinition hole,
            ISet<string> occupied)
        {
            if (hole.OwnerKind != HoleOwnerKind.Baseboard || !hole.Persistent) return false;
            if (occupied.Contains(hole.Id) || !IsHoleExposed(definition, snapshot, hole.Id)) return false;
            if (hole.Role == HoleRole.Parking) return true;
            var released = new HashSet<string>(snapshot.ReleasedComponentIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            return (hole.SecuredComponentIds ?? Array.Empty<string>()).All(released.Contains);
        }

        static void EnsureValidState(DisassemblyDefinition definition, DisassemblySnapshot snapshot)
        {
            var definitionErrors = DisassemblyValidator.ValidateDefinition(definition);
            if (definitionErrors.Count > 0) throw new ArgumentException(string.Join(" ", definitionErrors), nameof(definition));
            var snapshotErrors = DisassemblyValidator.ValidateSnapshot(definition, snapshot);
            if (snapshotErrors.Count > 0) throw new ArgumentException(string.Join(" ", snapshotErrors), nameof(snapshot));
        }
    }
}
