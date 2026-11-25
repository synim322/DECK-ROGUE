namespace DeckRogue.Core.Audio
{
    public enum MixerCommandKind
    {
        Crossfader = 0,
        BassKill = 1,
        Filter = 2,
        PercGain = 3,
        MusicGain = 4,
        LineGain = 5
    }

    /// <summary>
    /// One mixer intent. The same command is applied by native and Web backends.
    /// A repeated command id is ignored so a replay cannot double-apply.
    /// Position holds crossfader (0..1), Filter/Perc/Music amounts (0..1), or line gain (0..1).
    /// </summary>
    public readonly struct MixerCommand
    {
        private MixerCommand(long commandId, MixerCommandKind kind, DeckId deck, double value, bool bassKilled)
        {
            CommandId = commandId;
            Kind = kind;
            Deck = deck;
            Position = value;
            BassKilled = bassKilled;
        }

        public long CommandId { get; }
        public MixerCommandKind Kind { get; }
        public DeckId Deck { get; }
        public double Position { get; }
        public bool BassKilled { get; }

        public static MixerCommand Crossfader(long commandId, double position)
        {
            return new MixerCommand(commandId, MixerCommandKind.Crossfader, DeckId.A, position, false);
        }

        public static MixerCommand BassKill(long commandId, DeckId deck, bool killed)
        {
            return new MixerCommand(commandId, MixerCommandKind.BassKill, deck, 0d, killed);
        }

        public static MixerCommand Filter(long commandId, DeckId deck, double amount)
        {
            return new MixerCommand(commandId, MixerCommandKind.Filter, deck, amount, false);
        }

        public static MixerCommand PercGain(long commandId, DeckId deck, double gain)
        {
            return new MixerCommand(commandId, MixerCommandKind.PercGain, deck, gain, false);
        }

        public static MixerCommand MusicGain(long commandId, DeckId deck, double gain)
        {
            return new MixerCommand(commandId, MixerCommandKind.MusicGain, deck, gain, false);
        }

        public static MixerCommand LineGain(long commandId, DeckId deck, double gain)
        {
            return new MixerCommand(commandId, MixerCommandKind.LineGain, deck, gain, false);
        }
    }
}
