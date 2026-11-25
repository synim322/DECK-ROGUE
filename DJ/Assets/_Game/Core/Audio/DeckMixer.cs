using System;
using System.Collections.Generic;

namespace DeckRogue.Core.Audio
{
    /// <summary>
    /// Equal-power crossfader, per-deck line fader, Bass Kill, filter intent, and PERC/MUSIC trims.
    /// Deck gain is crossfade × line. Stem gain is deck gain × stem trim × LOW mute. Filter does not scale gain.
    /// </summary>
    public sealed class DeckMixer
    {
        private readonly HashSet<long> _usedCommandIds = new HashSet<long>();
        private readonly bool[] _bassKilled = new bool[2];
        private readonly double[] _filter = new double[2];
        private readonly double[] _percTrim = new double[2];
        private readonly double[] _musicTrim = new double[2];
        private readonly double[] _line = new double[2];
        private double _position;

        public DeckMixer()
        {
            for (int deck = 0; deck < 2; deck++)
            {
                _filter[deck] = 0d;
                _percTrim[deck] = 1d;
                _musicTrim[deck] = 1d;
                _line[deck] = 1d;
            }
        }

        public double Crossfader
        {
            get { return _position; }
        }

        public bool IsBassKilled(DeckId deck)
        {
            return _bassKilled[Index(deck)];
        }

        /// <summary>Low-pass closedness 0..1 (0 open, 1 fully filtered).</summary>
        public double FilterAmount(DeckId deck)
        {
            return _filter[Index(deck)];
        }

        /// <summary>Alias for <see cref="FilterAmount"/>.</summary>
        public double Filter(DeckId deck)
        {
            return FilterAmount(deck);
        }

        public double PercTrim(DeckId deck)
        {
            return _percTrim[Index(deck)];
        }

        public double MusicTrim(DeckId deck)
        {
            return _musicTrim[Index(deck)];
        }

        /// <summary>Channel fader 0..1. A line of 1 leaves the equal-power crossfade unchanged.</summary>
        public double LineGain(DeckId deck)
        {
            return _line[Index(deck)];
        }

        /// <summary>Direct filter set (no command id). Prefer MixerCommand for replay safety.</summary>
        public bool SetFilter(DeckId deck, double closedness)
        {
            if (double.IsNaN(closedness) || double.IsInfinity(closedness))
                return false;
            _filter[Index(deck)] = Clamp01(closedness);
            return true;
        }

        /// <summary>Direct PERC/MUSIC trim. LOW is rejected — use Bass Kill.</summary>
        public bool SetStemGain(DeckId deck, StemId stem, double gain)
        {
            if (double.IsNaN(gain) || double.IsInfinity(gain))
                return false;
            int i = Index(deck);
            if (stem == StemId.Perc)
            {
                _percTrim[i] = Clamp01(gain);
                return true;
            }

            if (stem == StemId.Music)
            {
                _musicTrim[i] = Clamp01(gain);
                return true;
            }

            return false;
        }

        public bool TryApply(MixerCommand command)
        {
            if (command.CommandId <= 0 || _usedCommandIds.Contains(command.CommandId))
                return false;

            if (command.Kind == MixerCommandKind.Crossfader)
            {
                if (double.IsNaN(command.Position) || double.IsInfinity(command.Position))
                    return false;
                _position = Clamp01(command.Position);
            }
            else if (command.Kind == MixerCommandKind.BassKill)
            {
                _bassKilled[Index(command.Deck)] = command.BassKilled;
            }
            else if (command.Kind == MixerCommandKind.Filter)
            {
                if (double.IsNaN(command.Position) || double.IsInfinity(command.Position))
                    return false;
                _filter[Index(command.Deck)] = Clamp01(command.Position);
            }
            else if (command.Kind == MixerCommandKind.PercGain)
            {
                if (double.IsNaN(command.Position) || double.IsInfinity(command.Position))
                    return false;
                _percTrim[Index(command.Deck)] = Clamp01(command.Position);
            }
            else if (command.Kind == MixerCommandKind.MusicGain)
            {
                if (double.IsNaN(command.Position) || double.IsInfinity(command.Position))
                    return false;
                _musicTrim[Index(command.Deck)] = Clamp01(command.Position);
            }
            else if (command.Kind == MixerCommandKind.LineGain)
            {
                if (double.IsNaN(command.Position) || double.IsInfinity(command.Position))
                    return false;
                _line[Index(command.Deck)] = Clamp01(command.Position);
            }
            else
            {
                return false;
            }

            _usedCommandIds.Add(command.CommandId);
            return true;
        }

        public double DeckGain(DeckId deck)
        {
            int index = Index(deck);
            EqualPowerCrossfade.Evaluate(_position, out double gainA, out double gainB);
            double crossfade = deck == DeckId.A ? gainA : gainB;
            return crossfade * _line[index];
        }

        public double StemGain(DeckId deck, StemId stem)
        {
            double gain = DeckGain(deck);
            if (stem == StemId.Low && IsBassKilled(deck))
                return 0d;
            if (stem == StemId.Perc)
                return gain * _percTrim[Index(deck)];
            if (stem == StemId.Music)
                return gain * _musicTrim[Index(deck)];
            return gain;
        }

        private static double Clamp01(double value)
        {
            if (value <= 0d)
                return 0d;
            if (value >= 1d)
                return 1d;
            return value;
        }

        private static int Index(DeckId deck)
        {
            if (deck != DeckId.A && deck != DeckId.B)
                throw new ArgumentOutOfRangeException(nameof(deck));
            return (int)deck;
        }
    }
}
