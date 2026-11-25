using System.Globalization;
using DeckRogue.Core.Run;

namespace DeckRogue.Audio
{
    /// <summary>
    /// English run strip. Act1 is the default night loop; P0 ClubDirector Apply stays available.
    /// </summary>
    public static class RunChrome
    {
        public static void Apply(ref BoothSnapshot snapshot, ClubReadout readout)
        {
            int hype = (int)System.Math.Round(readout.Hype);
            snapshot.HasRun = true;
            snapshot.Hype = hype;
            snapshot.TargetHype = (int)System.Math.Round(readout.TargetHype);
            snapshot.CrowdBand = hype >= 70 ? 2 : (hype >= 40 ? 1 : 0);
            snapshot.Venue = Venue(readout.ClubId);
            snapshot.ClubLookId = readout.ClubId;
            snapshot.GoalLabel = Goal(readout.Rule);
            snapshot.SetLabel = SetLine(readout);
            snapshot.ClockLabel = ClockLine(readout);
            snapshot.ShowRunBanner = readout.Phase == RunPhase.Result || readout.Phase == RunPhase.FinalResult;
            snapshot.RunStatus = snapshot.ShowRunBanner ? Outcome(readout) : "";
            snapshot.RunAction = Action(readout);
            snapshot.SecondaryRunAction = readout.Phase == RunPhase.FinalResult ? "Again" : "";
        }

        /// <summary>
        /// Act1 overlay plus main chrome (venue / goal / clock / run button) from Act1Readout.
        /// </summary>
        public static void ApplyAct1(
            ref BoothSnapshot snapshot,
            Act1Readout core,
            bool cueReady,
            string lastScoreKind,
            int crateCount)
        {
            ApplyAct1(ref snapshot, Act1UiReadout.FromCore(core, cueReady, lastScoreKind, crateCount), core);
        }

        /// <summary>
        /// Act1 strip + main HasRun chrome so the booth is never empty during Act1.
        /// </summary>
        public static void ApplyAct1(ref BoothSnapshot snapshot, Act1UiReadout readout)
        {
            ApplyAct1(ref snapshot, readout, default(Act1Readout));
        }

        public static void ApplyAct1(ref BoothSnapshot snapshot, Act1UiReadout readout, Act1Readout core)
        {
            if (!readout.Present)
            {
                snapshot.HasAct1 = false;
                snapshot.Act1ClubId = "";
                snapshot.ClubLookId = "";
                snapshot.Act1Phase = "";
                snapshot.Act1Hype = "";
                snapshot.Act1CueReady = "";
                snapshot.Act1LastScoreKind = "";
                snapshot.Act1Debug = "";
                return;
            }

            snapshot.HasAct1 = true;
            snapshot.ClubLookId = readout.ClubId;
            string club = Venue(readout.ClubId);
            if (string.IsNullOrEmpty(club) || club == readout.ClubId)
                club = string.IsNullOrEmpty(readout.ClubId) ? "—" : readout.ClubId;
            if (readout.IsBoss)
                club += " · boss";
            snapshot.Act1ClubId = club;
            snapshot.Act1Phase = Act1Phase(readout.Phase);
            snapshot.Act1Hype = "Hype "
                + ((int)System.Math.Round(readout.Hype)).ToString(CultureInfo.InvariantCulture)
                + " / "
                + ((int)System.Math.Round(readout.TargetHype)).ToString(CultureInfo.InvariantCulture);
            snapshot.Act1CueReady = readout.CueReady ? "CUE READY" : "cue —";
            snapshot.Act1LastScoreKind = readout.LastScoreKind ?? "";
            snapshot.Act1Debug = "club "
                + readout.ClubIndex.ToString(CultureInfo.InvariantCulture)
                + " · crate "
                + readout.CrateCount.ToString(CultureInfo.InvariantCulture);

            // Main chrome from Act1 (not P0 ClubDirector).
            int hype = (int)System.Math.Round(readout.Hype);
            snapshot.HasRun = true;
            snapshot.Hype = hype;
            snapshot.TargetHype = (int)System.Math.Round(readout.TargetHype);
            snapshot.CrowdBand = hype >= 70 ? 2 : (hype >= 40 ? 1 : 0);
            snapshot.Venue = Venue(readout.ClubId);
            snapshot.GoalLabel = Act1BriefingGoal(readout.ClubId);
            if (readout.Phase == RunPhase.PreClub && readout.ClubIndex > 0)
                snapshot.GoalLabel += Act1HeardTail(readout.ClubId);
            snapshot.SetLabel = Act1SetLine(readout);
            snapshot.ClockLabel = Act1ClockLine(core, readout.Phase);
            snapshot.ShowRunBanner = readout.Phase == RunPhase.Result || readout.Phase == RunPhase.FinalResult;
            snapshot.RunStatus = snapshot.ShowRunBanner ? Act1Outcome(readout) : "";
            snapshot.RunAction = Act1Action(readout);
            snapshot.SecondaryRunAction = readout.Phase == RunPhase.FinalResult ? "Again" : "";
        }

        public static string Venue(string clubId)
        {
            if (string.IsNullOrEmpty(clubId) || clubId == ClubDirector.NeonBasementId)
                return CampaignCatalog.DisplayName(Act1ClubIds.NeonBasement);
            string name = CampaignCatalog.DisplayName(clubId);
            return name;
        }

        /// <summary>
        /// GDD §3 briefing goals. Act1ClubDefinition has no BriefingGoal field yet.
        /// </summary>
        public static string Act1BriefingGoal(string clubId)
        {
            if (clubId == Act1ClubIds.WarehousePulse)
                return "Long lines — keep B ready";
            if (clubId == Act1ClubIds.BassFactory)
                return "Heavy low end, keep it clean";
            if (clubId == Act1ClubIds.TheSignal)
                return "Hold the signal, then break through";
            if (clubId == Act1ClubIds.NeonBasement || clubId == ClubDirector.NeonBasementId)
                return "Warm floor, clean transitions";
            return "Push Hype to the target";
        }

        /// <summary>
        /// Pre-club line for every night after the first. Tells the player what the room wants to hear.
        /// </summary>
        private static string Act1HeardTail(string clubId)
        {
            if (clubId == Act1ClubIds.NeonBasement || clubId == ClubDirector.NeonBasementId)
                return ". Play a house record clean, or land two clean mixes.";
            if (clubId == Act1ClubIds.WarehousePulse)
                return ". Play a warehouse line clean, or land two clean mixes.";
            if (clubId == Act1ClubIds.BassFactory)
                return ". Play a bass-tech record clean, or land two clean mixes.";
            if (clubId == Act1ClubIds.TheSignal)
                return ". Play a night record clean, or land two clean mixes.";
            return "";
        }

        private static string Act1Phase(RunPhase phase)
        {
            switch (phase)
            {
                case RunPhase.PreClub:
                    return "PreClub";
                case RunPhase.Performing:
                    return "Performing";
                case RunPhase.Result:
                    return "Result";
                case RunPhase.Reward:
                    return "Reward";
                case RunPhase.FinalResult:
                    return "FinalResult";
                default:
                    return phase.ToString();
            }
        }

        private static string Act1SetLine(Act1UiReadout readout)
        {
            int number = readout.ClubIndex + 1;
            string text = "club " + number.ToString(CultureInfo.InvariantCulture) + "/"
                + Act1Route.ClubCount.ToString(CultureInfo.InvariantCulture);
            if (readout.IsBoss)
                text += " · boss";
            if (readout.Phase == RunPhase.PreClub)
                return text + " · briefing";
            if (readout.Phase == RunPhase.Performing)
                return text + " · live";
            if (readout.Phase == RunPhase.Reward)
                return text + " · reward";
            return text;
        }

        private static string Act1ClockLine(Act1Readout core, RunPhase phase)
        {
            double remaining = core.RemainingSeconds;
            if (phase == RunPhase.PreClub)
                return "set length " + Clock(remaining);
            if (phase == RunPhase.Performing)
                return "left " + Clock(remaining);
            return "";
        }

        private static string Act1Action(Act1UiReadout readout)
        {
            if (readout.Phase == RunPhase.PreClub)
                return "Start set";
            if (readout.Phase == RunPhase.Result)
            {
                if (readout.IsBoss && readout.ClubIndex < Act1Route.ClubCount - 1)
                    return "Next act";
                return "To reward";
            }
            if (readout.Phase == RunPhase.Reward)
                return "";
            if (readout.Phase == RunPhase.FinalResult)
                return "Main menu";
            return "";
        }

        private static string Act1Outcome(Act1UiReadout readout)
        {
            string lead;
            if (readout.Phase == RunPhase.FinalResult)
                lead = readout.Outcome == SetOutcome.Win ? "Night cleared" : "Defeat";
            else if (readout.Outcome == SetOutcome.Win)
                lead = "Club cleared";
            else if (readout.Outcome == SetOutcome.Lose)
                lead = "Defeat";
            else
                lead = "Set over";

            string reason = Act1Reason(readout.ReasonId);
            if (reason.Length == 0)
                return lead + ".";
            return lead + ". " + char.ToUpperInvariant(reason[0]) + reason.Substring(1) + ".";
        }

        public static string Act1Reason(string reasonId)
        {
            if (reasonId == Act1RunDirector.ReasonTargetMet)
                return "crowd target hit";
            if (reasonId == Act1RunDirector.ReasonAct1Clear)
                return "Signal broken — Act1 clear";
            if (reasonId == Act1RunDirector.ReasonTimeout)
                return "time ran out before Hype hit the target";
            if (reasonId == Act1RunDirector.ReasonHypeFloor)
                return "the floor went cold — Hype hit zero";
            if (reasonId == Act1RunDirector.ReasonWarehouseRule)
                return "Warehouse: need a B prep or no stale groove";
            if (reasonId == Act1RunDirector.ReasonBassRule)
                return "Bass Factory: dirty low end or no clean transition";
            if (reasonId == Act1RunDirector.ReasonSignalLock)
                return "signal lock: Hype too low at mid-set";
            if (reasonId == Act1RunDirector.ReasonSignalSurge)
                return "signal surge: need a Drop and CueReady honor";
            if (reasonId == Act1RunDirector.ReasonSignalClear)
                return "Signal broken — the night continues uptown";
            if (reasonId == Act1RunDirector.ReasonThroneClear)
                return "Throne taken — the city can hear you";
            if (reasonId == Act1RunDirector.ReasonCampaignClear)
                return "the city has its nights back";
            if (reasonId == Act1RunDirector.ReasonDreamStale)
                return "dream lounge: the groove went stale";
            if (reasonId == Act1RunDirector.ReasonRaveDrop)
                return "rave district: need a drop and a clean";
            if (reasonId == Act1RunDirector.ReasonPierBass)
                return "chrome pier: zero bass clashes";
            if (reasonId == Act1RunDirector.ReasonThroneLock)
                return "throne: hype too low at mid-set";
            if (reasonId == Act1RunDirector.ReasonThroneSurge)
                return "throne: need a drop and a cue";
            if (reasonId == Act1RunDirector.ReasonCathedralEcho)
                return "cathedral: need two taste cleans";
            if (reasonId == Act1RunDirector.ReasonExchangeUnpaid)
                return "exchange: drop without a dirty bass";
            if (reasonId == Act1RunDirector.ReasonRelayLine)
                return "relay: cue without going stale";
            if (reasonId == Act1RunDirector.ReasonFestivalLock)
                return "festival: hype too low at mid-set";
            if (reasonId == Act1RunDirector.ReasonFestivalSurge)
                return "festival: need a drop plus taste";
            return Reason(reasonId);
        }

        public static string Goal(ClubRuleId rule)
        {
            switch (rule)
            {
                case ClubRuleId.MaintainEnergy:
                    return "hold the energy";
                case ClubRuleId.CleanDrop:
                    return "land at least one clean drop";
                default:
                    return "Push Hype to the target";
            }
        }

        public static string Reason(string reasonId)
        {
            if (reasonId == ClubDirector.ReasonTargetMet)
                return "crowd target hit";
            if (reasonId == ClubDirector.ReasonTimeout)
                return "time ran out before Hype hit the target";
            if (reasonId == ClubDirector.ReasonHypeFloor)
                return "the floor went cold — Hype stayed at zero for a phrase";
            if (reasonId == ClubDirector.ReasonCleanDropMissing)
                return "no clean drop landed";
            if (reasonId == ClubDirector.ReasonEnergyLost)
                return "set energy collapsed";
            return "";
        }

        private static string SetLine(ClubReadout readout)
        {
            int number = readout.SetIndex;
            int count = readout.SetCount > 0 ? readout.SetCount : ClubDirector.SetCount;
            if (number < 1)
                number = 1;
            string text = "set " + number.ToString(CultureInfo.InvariantCulture)
                + "/" + count.ToString(CultureInfo.InvariantCulture);
            if (readout.Phase == RunPhase.PreClub)
                return text + " · not started";
            if (readout.Phase == RunPhase.Performing)
                return text + " · live";
            return text;
        }

        private static string ClockLine(ClubReadout readout)
        {
            if (readout.Phase == RunPhase.PreClub)
                return "set length " + Clock(readout.RemainingSeconds);
            if (readout.Phase == RunPhase.Performing)
                return "left " + Clock(readout.RemainingSeconds);
            return "";
        }

        private static string Action(ClubReadout readout)
        {
            if (readout.Phase == RunPhase.PreClub)
                return "Start set";
            if (readout.Phase == RunPhase.Result)
                return "Next set";
            if (readout.Phase == RunPhase.FinalResult)
                return "Main menu";
            return "";
        }

        private static string Outcome(ClubReadout readout)
        {
            string lead;
            if (readout.Phase == RunPhase.FinalResult)
                lead = readout.Outcome == SetOutcome.Win ? "Run victory" : "Defeat";
            else if (readout.Outcome == SetOutcome.Win)
                lead = "Set victory";
            else if (readout.Outcome == SetOutcome.Lose)
                lead = "Defeat";
            else
                lead = "Set over";

            string reason = Reason(readout.ReasonId);
            if (reason.Length == 0)
                return lead + ".";
            return lead + ". " + char.ToUpperInvariant(reason[0]) + reason.Substring(1) + ".";
        }

        private static string Clock(double seconds)
        {
            if (double.IsNaN(seconds) || seconds < 0d)
                seconds = 0d;
            int total = (int)System.Math.Round(seconds);
            int minutes = total / 60;
            int secs = total % 60;
            return minutes.ToString(CultureInfo.InvariantCulture)
                + ":" + secs.ToString("00", CultureInfo.InvariantCulture);
        }
    }
}
