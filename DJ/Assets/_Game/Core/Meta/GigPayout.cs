using DeckRogue.Core.Run;

namespace DeckRogue.Core.Meta
{
    /// <summary>
    /// Coins for one finished set. Does not read or write Hype or Reputation.
    /// <paramref name="hype"/> is clamped to 0..100; that whole number is n (floor).
    /// Win: 15 + n/5 (15..35). Lose: 2 + n/10 (2..12). Anything else: 0.
    /// </summary>
    public static class GigPayout
    {
        public static int Coins(SetOutcome outcome, int hype)
        {
            int n = hype;
            if (n < 0)
                n = 0;
            else if (n > 100)
                n = 100;

            if (outcome == SetOutcome.Win)
                return 15 + (n / 5);
            if (outcome == SetOutcome.Lose)
                return 2 + (n / 10);
            return 0;
        }
    }
}
