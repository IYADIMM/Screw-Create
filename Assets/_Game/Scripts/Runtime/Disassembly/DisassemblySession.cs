using System;
using System.Collections.Generic;

namespace ScrewAndCreate.Disassembly
{
    public sealed class DisassemblySession
    {
        readonly DisassemblyDefinition _definition;
        readonly Stack<DisassemblySnapshot> _undo = new Stack<DisassemblySnapshot>();
        DisassemblySnapshot _current;

        public DisassemblySession(DisassemblyDefinition definition)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            var errors = DisassemblyValidator.ValidateDefinition(definition);
            if (errors.Count > 0) throw new ArgumentException(string.Join(" ", errors), nameof(definition));
            _current = DisassemblyValidator.CreateInitialSnapshot(definition);
        }

        public DisassemblySnapshot Snapshot => _current.Clone();
        public bool CanUndo => _undo.Count > 0;
        public int UndoCount => _undo.Count;

        public IReadOnlyList<string> GetAccessibleScrewIds() =>
            DisassemblyRules.GetAccessibleScrewIds(_definition, _current);

        public IReadOnlyList<string> GetLegalDestinationIds(string screwId) =>
            DisassemblyRules.GetLegalDestinationIds(_definition, _current, screwId);

        public bool HasAnyLegalMove() => DisassemblyRules.HasAnyLegalMove(_definition, _current);

        public DisassemblyTransitionResult TryTransferScrew(string screwId, string destinationHoleId)
        {
            var result = DisassemblyRules.TryTransferScrew(_definition, _current, screwId, destinationHoleId);
            if (!result.Success) return result;
            _undo.Push(_current.Clone());
            _current = result.Snapshot.Clone();
            return DisassemblyTransitionResult.Accept(_current, result.ReleasedComponentIds);
        }

        public bool TryUndo()
        {
            if (_undo.Count == 0) return false;
            _current = _undo.Pop();
            return true;
        }
    }
}
