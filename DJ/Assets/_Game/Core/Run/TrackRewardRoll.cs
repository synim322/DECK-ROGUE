using System;
using System.Collections.Generic;
using DeckRogue.Core.Audio;

namespace DeckRogue.Core.Run
{
    /// <summary>
    /// GDD §3.2 / Doc/16 reward pools. Systems pick 1-of-N; music owns the id lists.
    /// </summary>
    public static class TrackRewardRoll
    {
        public const string PoolAct1Common = "reward_pool_act1_common";
        public const string PoolAct1Mid = "reward_pool_act1_mid";
        public const string BossCarrierId = "track_signal_carrier";

        /// <summary>Act index is clubIndex / 4. Act 0 is the original Act I pools.</summary>
        private static readonly string[][] CommonByAct =
        {
            new[] { "track_voltage_alley", "track_mutabor_pulse" },
            new[] { "track_lounge_key", "track_district_hat" },
            new[] { "track_nave_pad", "track_market_click" }
        };

        private static readonly string[][] MidByAct =
        {
            new[] { "track_foundry_drop", "track_afterglow_key", "track_mirror_shunt" },
            new[] { "track_siren_sub", "track_pier_roll", "track_chrome_wake" },
            new[] { "track_roof_hum", "track_exchange_acid", "track_relay_dub" }
        };

        private static readonly string[][] SpecialByAct =
        {
            new[]
            {
                TrackAura.AmberBloomId,
                TrackAura.VoidStaticId,
                TrackAura.GlassRainId,
                TrackAura.RedlineSirenId
            },
            new[]
            {
                TrackAura.VelvetHushId,
                TrackAura.HardlineYellowId,
                TrackAura.SaltChromeId
            },
            new[]
            {
                TrackAura.PrismChoirId,
                TrackAura.NightLedgerId,
                TrackAura.SkywireId
            }
        };

        /// <summary>Ordinary slot 0 of the act: both common ids, order salted by seed.</summary>
        public static IReadOnlyList<string> FromCommon(int seed, int act = 0)
        {
            string[] pool = Pool(CommonByAct, act);
            if (pool.Length < 2)
                return Array.Empty<string>();

            unchecked
            {
                uint mix = Mix((uint)seed ^ 0xBEE5C0DEu);
                if ((mix & 1u) == 0u)
                    return new[] { pool[0], pool[1] };
                return new[] { pool[1], pool[0] };
            }
        }

        /// <summary>
        /// Ordinary slots 1 and 2: that act's mid pool plus leftover common (unowned), up to 3.
        /// </summary>
        public static IReadOnlyList<string> FromMid(int seed, IEnumerable<string> alreadyOwned, int act = 0)
        {
            string[] mid = Pool(MidByAct, act);
            string[] common = Pool(CommonByAct, act);
            HashSet<string> owned = OwnedSet(alreadyOwned);

            var pool = new List<string>(8);
            for (int i = 0; i < mid.Length; i++)
            {
                if (!owned.Contains(mid[i]))
                    pool.Add(mid[i]);
            }

            for (int i = 0; i < common.Length; i++)
            {
                if (!owned.Contains(common[i]))
                    pool.Add(common[i]);
            }

            // Both common already taken: still offer mid (even if somehow owned) so the slot is not empty.
            if (pool.Count == 0 && mid.Length >= 2)
            {
                pool.Add(mid[0]);
                pool.Add(mid[1]);
            }

            int take = pool.Count < 3 ? pool.Count : 3;
            return ShuffleTake(pool, take, seed);
        }

        /// <summary>
        /// One unowned special from that act's special pool. Empty when the crate holds the whole pool.
        /// </summary>
        public static string FromSpecial(int seed, IEnumerable<string> alreadyOwned, int act = 0)
        {
            string[] specials = Pool(SpecialByAct, act);
            HashSet<string> owned = OwnedSet(alreadyOwned);

            var pool = new List<string>(specials.Length);
            for (int i = 0; i < specials.Length; i++)
            {
                if (!owned.Contains(specials[i]))
                    pool.Add(specials[i]);
            }

            if (pool.Count == 0)
                return "";

            IReadOnlyList<string> shuffled = ShuffleTake(pool, 1, seed);
            return shuffled.Count == 0 ? "" : shuffled[0];
        }

        /// <summary>The Signal's fixed drop (also meta_unlock of the same id).</summary>
        public static string BossCarrier()
        {
            return BossCarrierId;
        }

        /// <summary>
        /// Boss grant for a club id. Signal, Static Throne, and Final Festival.
        /// Empty when the club is not a boss drop.
        /// </summary>
        public static string BossDropId(string clubId)
        {
            if (clubId == Act1ClubIds.TheSignal)
                return BossCarrierId;
            if (clubId == CampaignClubIds.TheStaticThrone)
                return TrackAura.StaticCrownId;
            if (clubId == CampaignClubIds.FinalFestival)
                return TrackAura.FestivalAnthemId;
            return "";
        }

        private static string[] Pool(string[][] table, int act)
        {
            if (table == null || act < 0 || act >= table.Length || table[act] == null)
                return Array.Empty<string>();
            return table[act];
        }

        private static HashSet<string> OwnedSet(IEnumerable<string> alreadyOwned)
        {
            var owned = new HashSet<string>(StringComparer.Ordinal);
            if (alreadyOwned == null)
                return owned;

            foreach (string id in alreadyOwned)
            {
                if (!string.IsNullOrEmpty(id))
                    owned.Add(id);
            }

            return owned;
        }

        private static IReadOnlyList<string> ShuffleTake(List<string> pool, int take, int seed)
        {
            if (take <= 0)
                return Array.Empty<string>();
            if (take >= pool.Count)
            {
                // Stable seed-dependent order of the whole pool.
                var full = new string[pool.Count];
                for (int i = 0; i < pool.Count; i++)
                    full[i] = pool[i];
                ShuffleInPlace(full, seed);
                return full;
            }

            var work = new string[pool.Count];
            for (int i = 0; i < pool.Count; i++)
                work[i] = pool[i];
            ShuffleInPlace(work, seed);

            var result = new string[take];
            for (int i = 0; i < take; i++)
                result[i] = work[i];
            return result;
        }

        private static void ShuffleInPlace(string[] items, int seed)
        {
            unchecked
            {
                uint state = Mix((uint)seed ^ 0xA5A5F00Du);
                for (int i = items.Length - 1; i > 0; i--)
                {
                    state = Mix(state);
                    int j = (int)(state % (uint)(i + 1));
                    string tmp = items[i];
                    items[i] = items[j];
                    items[j] = tmp;
                }
            }
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
