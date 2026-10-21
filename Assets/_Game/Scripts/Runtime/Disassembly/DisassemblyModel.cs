using System;
using System.Collections.Generic;
using System.Linq;

namespace ScrewAndCreate.Disassembly
{
    public enum DisassemblyPhase { Active, Complete }
    public enum HoleOwnerKind { Baseboard, Component }
    public enum HoleRole { Parking, Structural }

    [Serializable]
    public sealed class DisassemblyScrewDefinition
    {
        public string Id = string.Empty;
        public string InitialHoleId = string.Empty;
    }

    [Serializable]
    public sealed class DisassemblyHoleDefinition
    {
        public string Id = string.Empty;
        public HoleOwnerKind OwnerKind = HoleOwnerKind.Baseboard;
        public string OwnerComponentId = string.Empty;
        public bool Persistent = true;
        public HoleRole Role = HoleRole.Parking;
        public string[] SecuredComponentIds = Array.Empty<string>();
        public string[] CoveredByComponentIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class DisassemblyComponentDefinition
    {
        public string Id = string.Empty;
        public string[] SecuringHoleIds = Array.Empty<string>();
        public string[] ReleaseDependencyComponentIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class DisassemblyDefinition
    {
        public string Id = string.Empty;
        public DisassemblyScrewDefinition[] Screws = Array.Empty<DisassemblyScrewDefinition>();
        public DisassemblyHoleDefinition[] Holes = Array.Empty<DisassemblyHoleDefinition>();
        public DisassemblyComponentDefinition[] Components = Array.Empty<DisassemblyComponentDefinition>();
        public string[] RequiredComponentIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class ScrewLocationState
    {
        public string ScrewId = string.Empty;
        public string HoleId = string.Empty;

        public ScrewLocationState Clone() => new ScrewLocationState { ScrewId = ScrewId, HoleId = HoleId };
    }

    [Serializable]
    public sealed class DisassemblySnapshot
    {
        public DisassemblyPhase Phase = DisassemblyPhase.Active;
        public ScrewLocationState[] ScrewLocations = Array.Empty<ScrewLocationState>();
        public string[] ReleasedComponentIds = Array.Empty<string>();
        public int ActionCount;
        public int Revision;

        public DisassemblySnapshot Clone() => new DisassemblySnapshot
        {
            Phase = Phase,
            ScrewLocations = (ScrewLocations ?? Array.Empty<ScrewLocationState>())
                .Select(x => x?.Clone()).ToArray(),
            ReleasedComponentIds = (ReleasedComponentIds ?? Array.Empty<string>()).ToArray(),
            ActionCount = ActionCount,
            Revision = Revision
        };

        public string SolverKey()
        {
            var locations = (ScrewLocations ?? Array.Empty<ScrewLocationState>())
                .Where(x => x != null)
                .OrderBy(x => x.ScrewId, StringComparer.Ordinal)
                .Select(x => $"{x.ScrewId}={x.HoleId}");
            var released = (ReleasedComponentIds ?? Array.Empty<string>())
                .OrderBy(x => x, StringComparer.Ordinal);
            return $"{string.Join(";", locations)}|released={string.Join(",", released)}";
        }

        public string CanonicalKey() =>
            $"{(int)Phase}|{SolverKey()}|actions={ActionCount}|revision={Revision}";
    }

    public readonly struct DisassemblyMove
    {
        public DisassemblyMove(string screwId, string sourceHoleId, string destinationHoleId)
        {
            ScrewId = screwId;
            SourceHoleId = sourceHoleId;
            DestinationHoleId = destinationHoleId;
        }

        public string ScrewId { get; }
        public string SourceHoleId { get; }
        public string DestinationHoleId { get; }
        public override string ToString() => $"{ScrewId}:{SourceHoleId}->{DestinationHoleId}";
    }

    public sealed class DisassemblyTransitionResult
    {
        DisassemblyTransitionResult(bool success, string reason, DisassemblySnapshot snapshot, string[] released)
        {
            Success = success;
            Reason = reason ?? string.Empty;
            Snapshot = snapshot?.Clone();
            ReleasedComponentIds = released ?? Array.Empty<string>();
        }

        public bool Success { get; }
        public string Reason { get; }
        public DisassemblySnapshot Snapshot { get; }
        public IReadOnlyList<string> ReleasedComponentIds { get; }

        public static DisassemblyTransitionResult Accept(DisassemblySnapshot snapshot, IEnumerable<string> released) =>
            new DisassemblyTransitionResult(true, string.Empty, snapshot, (released ?? Array.Empty<string>()).ToArray());

        public static DisassemblyTransitionResult Reject(string reason) =>
            new DisassemblyTransitionResult(false, reason, null, Array.Empty<string>());
    }
}
