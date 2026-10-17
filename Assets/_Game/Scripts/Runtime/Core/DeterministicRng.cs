using System;

namespace ScrewAndCreate.Core
{
    /// <summary>PCG-XSH-RR with explicit 64-bit state and stable integer operations.</summary>
    public struct DeterministicRng
    {
        ulong _state;
        readonly ulong _increment;

        public DeterministicRng(ulong seed, ulong stream = 1442695040888963407UL)
        {
            _state = 0UL;
            _increment = (stream << 1) | 1UL;
            NextUInt();
            _state = unchecked(_state + seed);
            NextUInt();
        }

        public uint NextUInt()
        {
            var oldState = _state;
            _state = unchecked(oldState * 6364136223846793005UL + _increment);
            var xorshifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
            var rotation = (int)(oldState >> 59);
            return (xorshifted >> rotation) | (xorshifted << ((-rotation) & 31));
        }

        public int NextInt(int exclusiveMax)
        {
            if (exclusiveMax <= 0) throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
            var bound = (uint)exclusiveMax;
            var threshold = unchecked((uint)(0 - bound)) % bound;
            while (true)
            {
                var value = NextUInt();
                if (value >= threshold) return (int)(value % bound);
            }
        }

        public bool NextBool() => (NextUInt() & 1U) != 0;
    }
}
