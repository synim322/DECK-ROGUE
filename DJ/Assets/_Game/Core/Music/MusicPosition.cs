namespace DeckRogue.Core.Music
{
    public readonly struct MusicPosition
    {
        public MusicPosition(int beat, int bar, int phrase, int beatInBar, int barInPhrase, double audioTime)
        {
            Beat = beat;
            Bar = bar;
            Phrase = phrase;
            BeatInBar = beatInBar;
            BarInPhrase = barInPhrase;
            AudioTime = audioTime;
        }

        public int Beat { get; }
        public int Bar { get; }
        public int Phrase { get; }
        public int BeatInBar { get; }
        public int BarInPhrase { get; }
        public double AudioTime { get; }
    }
}
