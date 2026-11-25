using System;
using System.Collections.Generic;
using System.Globalization;
using DeckRogue.Core.Run;

namespace DeckRogue.Core.Save
{
    /// <summary>
    /// Mid-run Act1 snapshot for pause-menu Save / MainMenu Continue.
    /// Audio deck state is not captured — resume returns to the club with crate/score.
    /// </summary>
    public sealed class RunCheckpoint
    {
        public bool Active { get; set; }

        public int Seed { get; set; }

        public int ClubIndex { get; set; }

        public RunPhase Phase { get; set; }

        public SetOutcome Outcome { get; set; }

        public string ReasonId { get; set; }

        public double ElapsedSeconds { get; set; }

        public double Hype { get; set; }

        public int Combo { get; set; }

        public double Reputation { get; set; }

        public List<string> CrateTrackIds { get; set; }

        public List<string> OfferedTrackIds { get; set; }

        public string ChosenTrackId { get; set; }

        public int CleanCount { get; set; }

        public int BassConflicts { get; set; }

        public int CleanPrepCount { get; set; }

        public int CueReadyHonorCount { get; set; }

        public bool HadDrop { get; set; }

        public bool HadStale { get; set; }

        public bool SignalLockChecked { get; set; }

        public bool SignalLockOk { get; set; }

        /// <summary>
        /// Deck worn when this night was saved. Empty on older checkpoints:
        /// resume then uses the hub's equipped deck.
        /// </summary>
        public string DeckId { get; set; }

        public static RunCheckpoint CreateEmpty()
        {
            return new RunCheckpoint
            {
                Active = false,
                Seed = 0,
                ClubIndex = 0,
                Phase = RunPhase.PreClub,
                Outcome = SetOutcome.InProgress,
                ReasonId = "",
                ElapsedSeconds = 0d,
                Hype = 0d,
                Combo = 1,
                Reputation = 0d,
                CrateTrackIds = new List<string>(),
                OfferedTrackIds = new List<string>(),
                ChosenTrackId = "",
                CleanCount = 0,
                BassConflicts = 0,
                CleanPrepCount = 0,
                CueReadyHonorCount = 0,
                HadDrop = false,
                HadStale = false,
                SignalLockChecked = false,
                SignalLockOk = false,
                DeckId = ""
            };
        }

        public RunCheckpoint Clone()
        {
            var copy = CreateEmpty();
            copy.Active = Active;
            copy.Seed = Seed;
            copy.ClubIndex = ClubIndex;
            copy.Phase = Phase;
            copy.Outcome = Outcome;
            copy.ReasonId = ReasonId ?? "";
            copy.ElapsedSeconds = ElapsedSeconds;
            copy.Hype = Hype;
            copy.Combo = Combo;
            copy.Reputation = Reputation;
            copy.ChosenTrackId = ChosenTrackId ?? "";
            copy.CleanCount = CleanCount;
            copy.BassConflicts = BassConflicts;
            copy.CleanPrepCount = CleanPrepCount;
            copy.CueReadyHonorCount = CueReadyHonorCount;
            copy.HadDrop = HadDrop;
            copy.HadStale = HadStale;
            copy.SignalLockChecked = SignalLockChecked;
            copy.SignalLockOk = SignalLockOk;
            copy.DeckId = DeckId ?? "";
            CopyIds(CrateTrackIds, copy.CrateTrackIds);
            CopyIds(OfferedTrackIds, copy.OfferedTrackIds);
            return copy;
        }

        /// <summary>True when a resumeable night is stored (not finished FinalResult).</summary>
        public bool CanResume
        {
            get
            {
                return Active
                    && Phase != RunPhase.FinalResult
                    && ClubIndex >= 0
                    && ClubIndex < Act1Route.ClubCount;
            }
        }

        /// <summary>
        /// Where this night stopped: 1-based club among <see cref="Act1Route.ClubCount"/>
        /// and the venue name for that seed. Empty when the night cannot resume.
        /// </summary>
        public string PlaceLine()
        {
            if (!CanResume)
                return "";

            string[] route = Act1Route.Build(Seed);
            string clubId = ClubIndex < route.Length ? route[ClubIndex] : "";
            int level = ClubIndex + 1;
            return "club "
                + level.ToString(CultureInfo.InvariantCulture)
                + "/"
                + Act1Route.ClubCount.ToString(CultureInfo.InvariantCulture)
                + " · "
                + CampaignCatalog.DisplayName(clubId);
        }

        private static void CopyIds(IEnumerable<string> source, List<string> target)
        {
            target.Clear();
            if (source == null)
                return;
            foreach (string id in source)
            {
                if (!string.IsNullOrEmpty(id))
                    target.Add(id);
            }
        }
    }
}
