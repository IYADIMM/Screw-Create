using System;
using System.Collections.Generic;
using System.Linq;

namespace ScrewAndCreate.Core
{
    /// <summary>Pure mixed-workbench rules. Interaction mode is presentation intent and never gates legal logic.</summary>
    public sealed class PuzzleEngine
    {
        readonly PuzzleDefinition _definition;
        readonly Dictionary<string, ScrewDefinition> _screws;
        readonly Dictionary<string, PartDefinition> _parts;
        readonly Dictionary<string, SocketDefinition> _sockets;
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
            _sockets = definition.Sockets.ToDictionary(x => x.Id, StringComparer.Ordinal);
            Phase = PuzzlePhase.Disassembly;
            Mode = InteractionMode.Disassembly;
        }

        public PuzzlePhase Phase { get; private set; }
        public InteractionMode Mode { get; private set; }
        public int ActionCount { get; private set; }
        public int HintCount { get; private set; }
        public int Revision { get; private set; }
        public int TrayCount => _tray.Count;
        public int TrayCapacity => _definition.TrayCapacity;
        public bool CanUndo => _undo.Count > 0;

        public bool SetInteractionMode(InteractionMode mode)
        {
            if (!IsActive()) return false;
            Mode = mode;
            Phase = mode == InteractionMode.Assembly ? PuzzlePhase.Assembly : PuzzlePhase.Disassembly;
            Revision++;
            return true;
        }

        public bool CanRemoveScrew(string screwId, out string reason)
        {
            if (!IsActive()) return Reject("Puzzle is not active.", out reason);
            if (!_screws.TryGetValue(screwId, out var screw)) return Reject("Unknown screw.", out reason);
            if (_removed.Contains(screwId)) return Reject("Screw already removed.", out reason);
            if (screw.AuthoredLocked) return Reject("Screw is authored locked.", out reason);
            if (screw.RequiredRemovedScrewIds.Any(x => !_removed.Contains(x))) return Reject("Screw dependency is not satisfied.", out reason);
            if (screw.CoveringPartIds.Any(x => !_released.Contains(x))) return Reject("Screw head is covered.", out reason);
            if (_tray.Count + PartsReleasedAfterRemoving(screwId).Count > _definition.TrayCapacity) return Reject("Move would overflow the tray.", out reason);
            reason = string.Empty;
            return true;
        }

        public MoveResult TryRemoveScrew(string screwId)
        {
            if (!CanRemoveScrew(screwId, out var reason)) return MoveResult.Reject(reason);
            _undo.Push(CaptureSnapshot());
            _removed.Add(screwId);
            foreach (var partId in EligibleUnreleasedParts().OrderBy(x => x, StringComparer.Ordinal))
            { _released.Add(partId); _tray.Add(partId); }
            ActionCount++; Revision++; UpdateCompletion();
            return MoveResult.Ok();
        }

        public bool CanAssemblePart(string partId, string socketId, int rotationDegrees, out string reason)
        {
            if (!IsActive()) return Reject("Puzzle is not active.", out reason);
            if (!_parts.TryGetValue(partId, out var part)) return Reject("Unknown part.", out reason);
            if (!_tray.Contains(partId)) return Reject("Part is not in the tray.", out reason);
            if (!_sockets.TryGetValue(socketId, out var socket)) return Reject("Unknown socket.", out reason);
            if (!string.Equals(part.TargetSocketId, socketId, StringComparison.Ordinal)) return Reject("Socket is not this part's authored target.", out reason);
            if (!socket.AcceptedCompatibilityGroups.Contains(part.CompatibilityGroup, StringComparer.Ordinal)) return Reject("Compatibility group is not accepted.", out reason);
            if (socket.AllowedPartIds.Length > 0 && !socket.AllowedPartIds.Contains(partId, StringComparer.Ordinal)) return Reject("Part is not allowed in this socket.", out reason);
            if (part.AssemblyDependencyPartIds.Any(x => !_assembled.Contains(x)) || socket.AssemblyDependencyPartIds.Any(x => !_assembled.Contains(x))) return Reject("Assembly dependency is not satisfied.", out reason);
            var partRotation = part.AllowedRotations.Any(x => AngularDistance(x, rotationDegrees) <= part.RotationToleranceDegrees);
            var socketRotation = socket.AllowedRotations.Any(x => AngularDistance(x, rotationDegrees) <= socket.RotationToleranceDegrees);
            if (!partRotation || !socketRotation) return Reject("Rotation is outside tolerance.", out reason);
            reason = string.Empty;
            return true;
        }

        public MoveResult TryAssemblePart(string partId, string socketId, int rotationDegrees)
        {
            if (!CanAssemblePart(partId, socketId, rotationDegrees, out var reason)) return MoveResult.Reject(reason);
            _undo.Push(CaptureSnapshot());
            _tray.Remove(partId); _assembled.Add(partId);
            ActionCount++; Revision++; UpdateCompletion();
            return MoveResult.Ok();
        }

        public void RegisterHintUse() { HintCount++; Revision++; }

        public bool TryUndo()
        {
            if (_undo.Count == 0) return false;
            var retainedHints = HintCount;
            RestoreSnapshotUnchecked(_undo.Pop());
            HintCount = Math.Max(HintCount, retainedHints);
            return true;
        }

        public IEnumerable<string> GetRemovableScrews() => _definition.Screws.Select(x => x.Id)
            .Where(x => CanRemoveScrew(x, out _)).OrderBy(x => x, StringComparer.Ordinal);

        public IEnumerable<PartDefinition> GetAssemblableParts() => _definition.Parts
            .Where(p => _tray.Contains(p.Id) && CanAssemblePart(p.Id, p.TargetSocketId, p.AllowedRotations[0], out _))
            .OrderBy(x => x.Id, StringComparer.Ordinal);

        public PuzzleSnapshot CaptureSnapshot() => new PuzzleSnapshot
        {
            Phase = Phase, InteractionMode = Mode,
            RemovedScrewIds = _removed.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            ReleasedPartIds = _released.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            TrayPartIds = _tray.ToArray(),
            AssembledPartIds = _assembled.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            ActionCount = ActionCount, HintCount = HintCount, Revision = Revision
        };

        public bool TryRestoreSnapshot(PuzzleSnapshot snapshot, out string reason)
        {
            var errors = ValidateSnapshot(snapshot);
            if (errors.Count > 0) { reason = string.Join(" ", errors); return false; }
            RestoreSnapshotUnchecked(snapshot); _undo.Clear(); reason = string.Empty; return true;
        }

        public void RestoreSnapshot(PuzzleSnapshot snapshot)
        { if (!TryRestoreSnapshot(snapshot, out var reason)) throw new ArgumentException(reason, nameof(snapshot)); }

        public IReadOnlyList<string> ValidateSnapshot(PuzzleSnapshot snapshot)
        {
            var errors = new List<string>();
            if (snapshot == null) { errors.Add("Snapshot is null."); return errors; }
            ValidateSet(snapshot.RemovedScrewIds, _screws.Keys, "removed screw", errors);
            ValidateSet(snapshot.ReleasedPartIds, _parts.Keys, "released part", errors);
            ValidateSet(snapshot.AssembledPartIds, _parts.Keys, "assembled part", errors);
            ValidateSet(snapshot.TrayPartIds, _parts.Keys, "tray part", errors);
            var released = new HashSet<string>(snapshot.ReleasedPartIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            var assembled = new HashSet<string>(snapshot.AssembledPartIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            var tray = new HashSet<string>(snapshot.TrayPartIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            if (tray.Count > _definition.TrayCapacity) errors.Add("Snapshot tray exceeds capacity.");
            if (assembled.Any(x => !released.Contains(x))) errors.Add("Snapshot assembles an unreleased part.");
            if (tray.Any(x => !released.Contains(x))) errors.Add("Snapshot trays an unreleased part.");
            if (tray.Overlaps(assembled)) errors.Add("Snapshot part is both assembled and in tray.");
            if (released.Any(x => !tray.Contains(x) && !assembled.Contains(x))) errors.Add("Snapshot released part is neither in tray nor assembled.");
            var complete = snapshot.Phase == PuzzlePhase.Complete;
            var logicallyComplete = (snapshot.RemovedScrewIds ?? Array.Empty<string>()).Length == _screws.Count && assembled.Count == _parts.Count && released.Count == _parts.Count;
            if (complete != logicallyComplete) errors.Add("Snapshot completion state is impossible.");
            if (snapshot.ActionCount < 0 || snapshot.HintCount < 0 || snapshot.Revision < 0) errors.Add("Snapshot counters cannot be negative.");
            return errors;
        }

        bool IsActive() => Phase != PuzzlePhase.Complete && Phase != PuzzlePhase.Failed;

        void UpdateCompletion()
        {
            if (_removed.Count == _screws.Count && _released.Count == _parts.Count && _assembled.Count == _parts.Count) Phase = PuzzlePhase.Complete;
            else Phase = Mode == InteractionMode.Assembly ? PuzzlePhase.Assembly : PuzzlePhase.Disassembly;
        }

        List<string> PartsReleasedAfterRemoving(string screwId)
        {
            var simulated = new HashSet<string>(_removed, StringComparer.Ordinal) { screwId };
            return _definition.Parts.Where(p => !_released.Contains(p.Id) && p.SecuringScrewIds.All(simulated.Contains)).Select(p => p.Id).ToList();
        }

        IEnumerable<string> EligibleUnreleasedParts() => _definition.Parts.Where(p => !_released.Contains(p.Id) && p.SecuringScrewIds.All(_removed.Contains)).Select(p => p.Id);

        void RestoreSnapshotUnchecked(PuzzleSnapshot snapshot)
        {
            Replace(_removed, snapshot.RemovedScrewIds); Replace(_released, snapshot.ReleasedPartIds); Replace(_assembled, snapshot.AssembledPartIds);
            _tray.Clear(); _tray.AddRange(snapshot.TrayPartIds ?? Array.Empty<string>());
            Phase = snapshot.Phase; Mode = snapshot.InteractionMode; ActionCount = snapshot.ActionCount; HintCount = snapshot.HintCount; Revision = snapshot.Revision;
        }

        static void ValidateSet(IEnumerable<string> values, IEnumerable<string> knownValues, string label, ICollection<string> errors)
        {
            if (values == null) { errors.Add($"Snapshot {label} collection is null."); return; }
            var known = new HashSet<string>(knownValues, StringComparer.Ordinal); var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in values) { if (!known.Contains(value)) errors.Add($"Snapshot contains unknown {label} {value}."); if (!seen.Add(value)) errors.Add($"Snapshot contains duplicate {label} {value}."); }
        }

        static float AngularDistance(float a, float b) { var delta = Math.Abs((a - b) % 360f); return Math.Min(delta, 360f - delta); }
        static void Replace(HashSet<string> target, IEnumerable<string> source) { target.Clear(); foreach (var item in source ?? Array.Empty<string>()) target.Add(item); }
        static bool Reject(string message, out string reason) { reason = message; return false; }
    }
}
