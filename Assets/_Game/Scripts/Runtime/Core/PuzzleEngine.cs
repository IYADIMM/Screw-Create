using System;
using System.Collections.Generic;
using System.Linq;

namespace ScrewAndCreate.Core
{
    /// <summary>Pure deterministic puzzle rules. Presentation observes snapshots and move results.</summary>
    public sealed class PuzzleEngine
    {
        readonly PuzzleDefinition _definition;
        readonly Dictionary<string, ScrewDefinition> _screws;
        readonly Dictionary<string, PartDefinition> _parts;
        readonly HashSet<string> _removed = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> _released = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> _assembled = new HashSet<string>(StringComparer.Ordinal);
        readonly List<string> _tray = new List<string>();
        readonly Stack<PuzzleSnapshot> _undo = new Stack<PuzzleSnapshot>();

        public PuzzleEngine(PuzzleDefinition definition)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            var errors = definition.Validate();
            if (errors.Count > 0) throw new ArgumentException(string.Join(" ", errors), nameof(definition));
            _screws = definition.Screws.ToDictionary(x => x.Id, StringComparer.Ordinal);
            _parts = definition.Parts.ToDictionary(x => x.Id, StringComparer.Ordinal);
            Phase = PuzzlePhase.Disassembly;
        }

        public PuzzlePhase Phase { get; private set; }
        public int ActionCount { get; private set; }
        public int HintCount { get; private set; }
        public int Revision { get; private set; }
        public int TrayCount => _tray.Count;
        public int TrayCapacity => _definition.TrayCapacity;
        public bool CanUndo => _undo.Count > 0;

        public bool CanRemoveScrew(string screwId, out string reason)
        {
            if (Phase != PuzzlePhase.Disassembly) return Reject("Not in disassembly phase.", out reason);
            if (!_screws.TryGetValue(screwId, out var screw)) return Reject("Unknown screw.", out reason);
            if (_removed.Contains(screwId)) return Reject("Screw already removed.", out reason);
            if (screw.AuthoredLocked) return Reject("Screw is authored locked.", out reason);
            if (screw.RequiredRemovedScrewIds.Any(x => !_removed.Contains(x))) return Reject("Screw dependency is not satisfied.", out reason);
            if (screw.CoveringPartIds.Any(x => !_released.Contains(x))) return Reject("Screw head is covered.", out reason);

            var newlyReleased = PartsReleasedAfterRemoving(screwId).Count;
            if (_tray.Count + newlyReleased > _definition.TrayCapacity) return Reject("Move would overflow the tray.", out reason);
            reason = string.Empty;
            return true;
        }

        public MoveResult TryRemoveScrew(string screwId)
        {
            if (!CanRemoveScrew(screwId, out var reason)) return MoveResult.Reject(reason);
            _undo.Push(CaptureSnapshot());
            _removed.Add(screwId);
            foreach (var partId in EligibleUnreleasedParts().OrderBy(x => x, StringComparer.Ordinal))
            {
                _released.Add(partId);
                _tray.Add(partId);
            }
            ActionCount++;
            Revision++;
            if (_released.Count == _parts.Count) Phase = PuzzlePhase.Assembly;
            return MoveResult.Ok();
        }

        public MoveResult TryAssemblePart(string partId, string socketId, int rotationDegrees)
        {
            if (Phase != PuzzlePhase.Assembly) return MoveResult.Reject("Not in assembly phase.");
            if (!_parts.TryGetValue(partId, out var part)) return MoveResult.Reject("Unknown part.");
            if (!_tray.Contains(partId)) return MoveResult.Reject("Part is not in the tray.");
            if (!string.Equals(part.TargetSocketId, socketId, StringComparison.Ordinal)) return MoveResult.Reject("Socket is incompatible.");
            if (part.AssemblyDependencyPartIds.Any(x => !_assembled.Contains(x))) return MoveResult.Reject("Assembly dependency is not satisfied.");
            if (!part.AllowedRotations.Any(x => AngularDistance(x, rotationDegrees) <= part.RotationToleranceDegrees))
                return MoveResult.Reject("Rotation is outside tolerance.");

            _undo.Push(CaptureSnapshot());
            _tray.Remove(partId);
            _assembled.Add(partId);
            ActionCount++;
            Revision++;
            if (_assembled.Count == _parts.Count) Phase = PuzzlePhase.Complete;
            return MoveResult.Ok();
        }

        public void RegisterHintUse()
        {
            HintCount++;
            Revision++;
        }

        public bool TryUndo()
        {
            if (_undo.Count == 0) return false;
            RestoreSnapshot(_undo.Pop());
            return true;
        }

        public IEnumerable<string> GetRemovableScrews() => _definition.Screws
            .Select(x => x.Id).Where(x => CanRemoveScrew(x, out _)).OrderBy(x => x, StringComparer.Ordinal);

        public IEnumerable<PartDefinition> GetAssemblableParts() => _definition.Parts
            .Where(x => _tray.Contains(x.Id) && x.AssemblyDependencyPartIds.All(_assembled.Contains))
            .OrderBy(x => x.Id, StringComparer.Ordinal);

        public PuzzleSnapshot CaptureSnapshot() => new PuzzleSnapshot
        {
            Phase = Phase,
            RemovedScrewIds = _removed.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            ReleasedPartIds = _released.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            TrayPartIds = _tray.ToArray(),
            AssembledPartIds = _assembled.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            ActionCount = ActionCount,
            HintCount = HintCount,
            Revision = Revision
        };

        public void RestoreSnapshot(PuzzleSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            Replace(_removed, snapshot.RemovedScrewIds);
            Replace(_released, snapshot.ReleasedPartIds);
            Replace(_assembled, snapshot.AssembledPartIds);
            _tray.Clear();
            _tray.AddRange(snapshot.TrayPartIds ?? Array.Empty<string>());
            Phase = snapshot.Phase;
            ActionCount = snapshot.ActionCount;
            HintCount = snapshot.HintCount;
            Revision = snapshot.Revision;
        }

        List<string> PartsReleasedAfterRemoving(string screwId)
        {
            var simulated = new HashSet<string>(_removed, StringComparer.Ordinal) { screwId };
            return _definition.Parts.Where(p => !_released.Contains(p.Id) && p.SecuringScrewIds.All(simulated.Contains)).Select(p => p.Id).ToList();
        }

        IEnumerable<string> EligibleUnreleasedParts() => _definition.Parts
            .Where(p => !_released.Contains(p.Id) && p.SecuringScrewIds.All(_removed.Contains)).Select(p => p.Id);

        static float AngularDistance(float a, float b)
        {
            var delta = Math.Abs((a - b) % 360f);
            return Math.Min(delta, 360f - delta);
        }

        static void Replace(HashSet<string> target, IEnumerable<string> source)
        {
            target.Clear();
            foreach (var item in source ?? Array.Empty<string>()) target.Add(item);
        }

        static bool Reject(string message, out string reason) { reason = message; return false; }
    }
}
