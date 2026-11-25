namespace DeckRogue.Core.Run
{
    /// <summary>
    /// Seed-driven three-act night: each act shuffles its three ordinary clubs, then a fixed boss.
    /// Act I keeps the original shuffle salt. Act II and Act III use their own salts.
    /// </summary>
    public static class Act1Route
    {
        public const int ClubCount = 12;

        /// <summary>Act I ordinary clubs. Clearing these three reaches The Signal.</summary>
        public const int OrdinaryPick = 3;

        private static readonly string[] Act1Ordinary =
        {
            Act1ClubIds.NeonBasement,
            Act1ClubIds.WarehousePulse,
            Act1ClubIds.BassFactory
        };

        private static readonly string[] Act2Ordinary =
        {
            CampaignClubIds.DreamLounge,
            CampaignClubIds.RaveDistrict,
            CampaignClubIds.ChromePier
        };

        private static readonly string[] Act3Ordinary =
        {
            CampaignClubIds.GlassCathedral,
            CampaignClubIds.NightExchange,
            CampaignClubIds.RoofRelay
        };

        /// <summary>
        /// Twelve club ids. Indices 0-2 Act I ordinary (salt 0xC1A55EED), 3 The Signal,
        /// 4-6 Act II ordinary (salt 0xA27), 7 The Static Throne,
        /// 8-10 Act III ordinary (salt 0xA3F), 11 Final Festival.
        /// </summary>
        public static string[] Build(int seed)
        {
            string[] act1 = Shuffle(Act1Ordinary, seed, 0xC1A55EEDu);
            string[] act2 = Shuffle(Act2Ordinary, seed, 0xA27u);
            string[] act3 = Shuffle(Act3Ordinary, seed, 0xA3Fu);
            return new string[]
            {
                act1[0],
                act1[1],
                act1[2],
                Act1ClubIds.TheSignal,
                act2[0],
                act2[1],
                act2[2],
                CampaignClubIds.TheStaticThrone,
                act3[0],
                act3[1],
                act3[2],
                CampaignClubIds.FinalFestival
            };
        }

        private static string[] Shuffle(string[] source, int seed, uint salt)
        {
            string[] pool = new string[source.Length];
            for (int i = 0; i < source.Length; i++)
                pool[i] = source[i];

            unchecked
            {
                uint state = Mix((uint)seed ^ salt);
                for (int i = pool.Length - 1; i > 0; i--)
                {
                    state = Mix(state);
                    int j = (int)(state % (uint)(i + 1));
                    string swap = pool[i];
                    pool[i] = pool[j];
                    pool[j] = swap;
                }
            }

            return pool;
        }

        private static uint Mix(uint x)
        {
            x ^= x >> 16;
            x *= 0x7feb352du;
            x ^= x >> 15;
            x *= 0x846ca68bu;
            x ^= x >> 16;
            return x;
        }
    }
}
