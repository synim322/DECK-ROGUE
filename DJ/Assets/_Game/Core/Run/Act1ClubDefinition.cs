using System;

namespace DeckRogue.Core.Run
{
    /// <summary>
    /// Pass rules from GDD §3. CleanPrep / CueReadyHonor are not ScoreKinds yet —
    /// director tracks them via Notify* until DJ-103/104 land.
    /// </summary>
    public enum Act1PassRule
    {
        None = 0,
        /// <summary>Warehouse: ≥1 CleanPrepTransition OR no StaleGroove.</summary>
        CleanPrepOrNoStale = 1,
        /// <summary>Bass Factory: ≤1 BassConflict AND ≥1 clean transition.</summary>
        BassCleanLimit = 2,
        /// <summary>Signal: mid-timer lock + surge (Drop + CueReadyHonor) stub.</summary>
        SignalPhases = 3,
        /// <summary>Dream Lounge: fail if the set went stale.</summary>
        SoftHold = 4,
        /// <summary>Rave District: a drop and at least one clean.</summary>
        NeedDrop = 5,
        /// <summary>Chrome Pier: zero bass clashes and at least one clean.</summary>
        SaltLimit = 6,
        /// <summary>Static Throne: mid-timer lock + drop + honored cue.</summary>
        ThroneLock = 7,
        /// <summary>Glass Cathedral: two dialect cleans. HeardEnough still applies.</summary>
        EchoTwice = 8,
        /// <summary>Night Exchange: a drop and at most one bass clash.</summary>
        PaidDrop = 9,
        /// <summary>Roof Relay: an honored cue and no stale groove.</summary>
        ClearLine = 10,
        /// <summary>Final Festival: mid-timer lock + drop + one dialect clean.</summary>
        Finale = 11
    }

    /// <summary>
    /// Per-club night table row: thresholds and special pass rule. Ids only — no locale strings.
    /// </summary>
    public readonly struct Act1ClubDefinition
    {
        private static readonly Act1ClubDefinition[] Clubs =
        {
            new Act1ClubDefinition(Act1ClubIds.NeonBasement, false, 42d, 50d, 64, Act1PassRule.None),
            new Act1ClubDefinition(Act1ClubIds.WarehousePulse, false, 40d, 50d, 64, Act1PassRule.CleanPrepOrNoStale),
            new Act1ClubDefinition(Act1ClubIds.BassFactory, false, 40d, 50d, 64, Act1PassRule.BassCleanLimit),
            new Act1ClubDefinition(Act1ClubIds.TheSignal, true, 45d, 65d, 72, Act1PassRule.SignalPhases),
            new Act1ClubDefinition(CampaignClubIds.DreamLounge, false, 40d, 58d, 72, Act1PassRule.SoftHold),
            new Act1ClubDefinition(CampaignClubIds.RaveDistrict, false, 38d, 60d, 72, Act1PassRule.NeedDrop),
            new Act1ClubDefinition(CampaignClubIds.ChromePier, false, 38d, 62d, 72, Act1PassRule.SaltLimit),
            new Act1ClubDefinition(CampaignClubIds.TheStaticThrone, true, 42d, 70d, 96, Act1PassRule.ThroneLock),
            new Act1ClubDefinition(CampaignClubIds.GlassCathedral, false, 36d, 64d, 80, Act1PassRule.EchoTwice),
            new Act1ClubDefinition(CampaignClubIds.NightExchange, false, 36d, 66d, 80, Act1PassRule.PaidDrop),
            new Act1ClubDefinition(CampaignClubIds.RoofRelay, false, 36d, 68d, 80, Act1PassRule.ClearLine),
            new Act1ClubDefinition(CampaignClubIds.FinalFestival, true, 40d, 75d, 96, Act1PassRule.Finale)
        };

        public Act1ClubDefinition(
            string id,
            bool isBoss,
            double initialHype,
            double targetHype,
            int durationBars,
            Act1PassRule passRule)
        {
            Id = id ?? "";
            IsBoss = isBoss;
            InitialHype = initialHype;
            TargetHype = targetHype;
            DurationBars = durationBars;
            PassRule = passRule;
        }

        public string Id { get; }
        public bool IsBoss { get; }
        public double InitialHype { get; }
        public double TargetHype { get; }
        public int DurationBars { get; }
        public Act1PassRule PassRule { get; }

        public static Act1ClubDefinition NeonBasement => Clubs[0];

        public static Act1ClubDefinition WarehousePulse => Clubs[1];

        public static Act1ClubDefinition BassFactory => Clubs[2];

        public static Act1ClubDefinition TheSignal => Clubs[3];

        public static Act1ClubDefinition DreamLounge => Clubs[4];

        public static Act1ClubDefinition RaveDistrict => Clubs[5];

        public static Act1ClubDefinition ChromePier => Clubs[6];

        public static Act1ClubDefinition TheStaticThrone => Clubs[7];

        public static Act1ClubDefinition GlassCathedral => Clubs[8];

        public static Act1ClubDefinition NightExchange => Clubs[9];

        public static Act1ClubDefinition RoofRelay => Clubs[10];

        public static Act1ClubDefinition FinalFestival => Clubs[11];

        public static bool TryGet(string clubId, out Act1ClubDefinition definition)
        {
            if (clubId != null)
            {
                for (int i = 0; i < Clubs.Length; i++)
                {
                    if (Clubs[i].Id == clubId)
                    {
                        definition = Clubs[i];
                        return true;
                    }
                }
            }

            definition = default(Act1ClubDefinition);
            return false;
        }

        public static Act1ClubDefinition Require(string clubId)
        {
            Act1ClubDefinition definition;
            if (!TryGet(clubId, out definition))
                throw new ArgumentException("Unknown Act1 club id: " + clubId, nameof(clubId));
            return definition;
        }
    }
}
