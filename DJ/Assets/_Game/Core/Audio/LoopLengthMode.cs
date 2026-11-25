using System;
using DeckRogue.Core.Music;

namespace DeckRogue.Core.Audio
{
    /// <summary>
    /// Loop / schedule slice length on the fixed 120 BPM 8-bar phrase grid.
    /// Phrase8 = 8 bars / 16 s. Half4 = 4 bars / 8 s. Bars2 = 2 bars / 4 s. Bars1 = 1 bar / 2 s.
    /// </summary>
    public enum LoopLengthMode
    {
        Phrase8 = 0,
        Half4 = 1,
        Bars2 = 2,
        Bars1 = 3
    }

    public static class LoopLength
    {
        public static MusicalBoundary ToBoundary(LoopLengthMode mode)
        {
            switch (mode)
            {
                case LoopLengthMode.Bars1:
                    return MusicalBoundary.Bar;
                case LoopLengthMode.Bars2:
                    return MusicalBoundary.TwoBars;
                case LoopLengthMode.Half4:
                    return MusicalBoundary.HalfPhrase;
                default:
                    return MusicalBoundary.Phrase;
            }
        }

        public static int ToBars(LoopLengthMode mode)
        {
            switch (mode)
            {
                case LoopLengthMode.Bars1:
                    return 1;
                case LoopLengthMode.Bars2:
                    return 2;
                case LoopLengthMode.Half4:
                    return 4;
                default:
                    return 8;
            }
        }

        public static bool IsValidBars(int bars)
        {
            return bars == 1 || bars == 2 || bars == 4 || bars == 8;
        }

        public static LoopLengthMode FromBars(int bars)
        {
            switch (bars)
            {
                case 1:
                    return LoopLengthMode.Bars1;
                case 2:
                    return LoopLengthMode.Bars2;
                case 4:
                    return LoopLengthMode.Half4;
                case 8:
                    return LoopLengthMode.Phrase8;
                default:
                    throw new ArgumentOutOfRangeException(nameof(bars), bars, "Loop length bars must be 1, 2, 4, or 8.");
            }
        }

        public static bool TryFromBars(int bars, out LoopLengthMode mode)
        {
            if (!IsValidBars(bars))
            {
                mode = LoopLengthMode.Phrase8;
                return false;
            }

            mode = FromBars(bars);
            return true;
        }

        /// <summary>Cycle 1 → 2 → 4 → 8 → 1.</summary>
        public static LoopLengthMode Next(LoopLengthMode mode)
        {
            switch (mode)
            {
                case LoopLengthMode.Bars1:
                    return LoopLengthMode.Bars2;
                case LoopLengthMode.Bars2:
                    return LoopLengthMode.Half4;
                case LoopLengthMode.Half4:
                    return LoopLengthMode.Phrase8;
                default:
                    return LoopLengthMode.Bars1;
            }
        }

        public static bool IsValid(LoopLengthMode mode)
        {
            return mode == LoopLengthMode.Phrase8
                || mode == LoopLengthMode.Half4
                || mode == LoopLengthMode.Bars2
                || mode == LoopLengthMode.Bars1;
        }
    }
}
