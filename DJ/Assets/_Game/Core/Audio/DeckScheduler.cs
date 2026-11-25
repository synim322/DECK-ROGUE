using System;
using System.Collections.Generic;
using DeckRogue.Core.Music;

namespace DeckRogue.Core.Audio
{
    public readonly struct ScheduleResult
    {
        public ScheduleResult(bool accepted, double startAudioTime, string reason)
        {
            Accepted = accepted;
            StartAudioTime = startAudioTime;
            Reason = reason;
        }

        public bool Accepted { get; }
        public double StartAudioTime { get; }
        public string Reason { get; }
    }

    /// <summary>
    /// Quantizes a deck start onto the music grid and refuses a second start
    /// for a deck that is already scheduled or playing, including a repeated command id.
    /// Per-deck preferred loop length (1|2|4|8 bars) selects the ScheduleStart boundary.
    /// </summary>
    public sealed class DeckScheduler
    {
        private readonly MusicClock _clock;
        private readonly HashSet<long> _usedCommandIds = new HashSet<long>();
        private readonly Dictionary<DeckId, double> _activeStarts = new Dictionary<DeckId, double>();
        private readonly LoopLengthMode[] _loopLength =
        {
            LoopLengthMode.Phrase8,
            LoopLengthMode.Phrase8
        };

        public DeckScheduler(MusicClock clock)
        {
            if (clock == null)
                throw new ArgumentNullException(nameof(clock));
            _clock = clock;
        }

        public bool IsActive(DeckId deck)
        {
            return _activeStarts.ContainsKey(deck);
        }

        public bool TryGetStart(DeckId deck, out double startAudioTime)
        {
            return _activeStarts.TryGetValue(deck, out startAudioTime);
        }

        public LoopLengthMode GetLoopLength(DeckId deck)
        {
            return _loopLength[Index(deck)];
        }

        /// <summary>Preferred quantize length in bars (1, 2, 4, or 8).</summary>
        public int GetLoopLengthBars(DeckId deck)
        {
            return LoopLength.ToBars(GetLoopLength(deck));
        }

        public MusicalBoundary LoopBoundary(DeckId deck)
        {
            return LoopLength.ToBoundary(GetLoopLength(deck));
        }

        /// <summary>
        /// Sets loop/schedule length for the deck. Same command id cannot re-apply.
        /// </summary>
        public bool TrySetLoopLength(long commandId, DeckId deck, LoopLengthMode mode)
        {
            if (commandId <= 0 || _usedCommandIds.Contains(commandId))
                return false;
            if (!LoopLength.IsValid(mode))
                return false;

            _loopLength[Index(deck)] = mode;
            _usedCommandIds.Add(commandId);
            return true;
        }

        /// <summary>Sets preferred quantize bars (1|2|4|8). Same command id cannot re-apply.</summary>
        public bool TrySetLoopLengthBars(long commandId, DeckId deck, int bars)
        {
            if (!LoopLength.TryFromBars(bars, out LoopLengthMode mode))
                return false;
            return TrySetLoopLength(commandId, deck, mode);
        }

        public ScheduleResult TryScheduleStart(DeckId deck, long commandId, MusicalBoundary boundary, double requestedAt)
        {
            if (_usedCommandIds.Contains(commandId))
                return new ScheduleResult(false, 0d, "command-already-used");
            if (_activeStarts.ContainsKey(deck))
                return new ScheduleResult(false, _activeStarts[deck], "deck-already-started");

            double start = _clock.GetNextBoundary(boundary, requestedAt);
            if (start + 1e-6d < requestedAt)
                return new ScheduleResult(false, start, "boundary-in-the-past");

            _usedCommandIds.Add(commandId);
            _activeStarts[deck] = start;
            return new ScheduleResult(true, start, "scheduled");
        }

        /// <summary>
        /// Schedules onto the deck's preferred loop-length bar stride (1|2|4|8).
        /// </summary>
        public ScheduleResult TryScheduleLoopStart(DeckId deck, long commandId, double requestedAt)
        {
            if (_usedCommandIds.Contains(commandId))
                return new ScheduleResult(false, 0d, "command-already-used");
            if (_activeStarts.ContainsKey(deck))
                return new ScheduleResult(false, _activeStarts[deck], "deck-already-started");

            int bars = GetLoopLengthBars(deck);
            double start = _clock.GetNextBoundaryBars(bars, requestedAt);
            if (start + 1e-6d < requestedAt)
                return new ScheduleResult(false, start, "boundary-in-the-past");

            _usedCommandIds.Add(commandId);
            _activeStarts[deck] = start;
            return new ScheduleResult(true, start, "scheduled");
        }

        public void MarkStopped(DeckId deck)
        {
            _activeStarts.Remove(deck);
        }

        public void NotePlayingFrom(DeckId deck, double startAudioTime)
        {
            _activeStarts[deck] = startAudioTime;
        }

        private static int Index(DeckId deck)
        {
            if (deck != DeckId.A && deck != DeckId.B)
                throw new ArgumentOutOfRangeException(nameof(deck));
            return (int)deck;
        }
    }
}
