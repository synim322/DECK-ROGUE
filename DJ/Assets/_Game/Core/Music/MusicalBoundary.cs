namespace DeckRogue.Core.Music
{
    public enum MusicalBoundary
    {
        Beat = 0,
        Bar = 1,
        Phrase = 2,
        /// <summary>Four bars — half of the fixed 8-bar phrase (8 s at 120 BPM).</summary>
        HalfPhrase = 3,
        /// <summary>Two bars (4 s at 120 BPM).</summary>
        TwoBars = 4
    }
}
