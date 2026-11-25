using System;
using System.Collections.Generic;
using DeckRogue.Core.Audio;
using DeckRogue.Core.Music;
using DeckRogue.Core.Save;
using DeckRogue.Core.Scoring;
using DeckRogue.Core.Time;

namespace DeckRogue.Core.Run
{
    public readonly struct Act1Readout
    {
        public Act1Readout(
            int clubIndex,
            string clubId,
            bool isBoss,
            RunPhase phase,
            double hype,
            double targetHype,
            double remainingSeconds,
            SetOutcome outcome,
            string reasonId,
            int crateCount,
            string offeredTrack0,
            string offeredTrack1,
            string offeredTrack2,
            string chosenTrackId,
            bool metaHasCarrier,
            int cheerIntensity = 0,
            int crowdMotionImpulse = 0)
        {
            ClubIndex = clubIndex;
            ClubId = clubId ?? "";
            IsBoss = isBoss;
            Phase = phase;
            Hype = hype;
            TargetHype = targetHype;
            RemainingSeconds = remainingSeconds;
            Outcome = outcome;
            ReasonId = reasonId ?? "";
            CrateCount = crateCount;
            OfferedTrack0 = offeredTrack0 ?? "";
            OfferedTrack1 = offeredTrack1 ?? "";
            OfferedTrack2 = offeredTrack2 ?? "";
            ChosenTrackId = chosenTrackId ?? "";
            MetaHasCarrier = metaHasCarrier;
            CheerIntensity = cheerIntensity < 0 ? 0 : (cheerIntensity > CrowdEnergy.MaxIntensity ? CrowdEnergy.MaxIntensity : cheerIntensity);
            CrowdMotionImpulse = crowdMotionImpulse < 0
                ? 0
                : (crowdMotionImpulse > (int)Scoring.CrowdMotionImpulse.Peak
                    ? (int)Scoring.CrowdMotionImpulse.Peak
                    : crowdMotionImpulse);
        }

        public int ClubIndex { get; }
        public string ClubId { get; }
        public bool IsBoss { get; }
        public RunPhase Phase { get; }
        public double Hype { get; }
        public double TargetHype { get; }
        public double RemainingSeconds { get; }
        public SetOutcome Outcome { get; }
        public string ReasonId { get; }
        public int CrateCount { get; }
        public string OfferedTrack0 { get; }
        public string OfferedTrack1 { get; }
        public string OfferedTrack2 { get; }
        public string ChosenTrackId { get; }
        public bool MetaHasCarrier { get; }

        /// <summary>0..3 hall cheer for PixelClubView / CrowdMotion.</summary>
        public int CheerIntensity { get; }

        /// <summary>
        /// Frame impulse for hall motion: None=0, Nod=1, Bounce=2, Peak=3
        /// (<see cref="Scoring.CrowdMotionImpulse"/>).
        /// </summary>
        public int CrowdMotionImpulse { get; }
    }

    /// <summary>
    /// Three-act night: 3 ordinary clubs + 1 boss, repeated. P0 <see cref="ClubDirector"/> stays untouched.
    /// Beating The Signal or The Static Throne opens the next act. Final Festival ends the night.
    /// </summary>
    public sealed class Act1RunDirector
    {
        public const string ReasonTargetMet = "target_met";
        public const string ReasonAct1Clear = "act1_clear";
        public const string ReasonTimeout = "timeout";
        public const string ReasonHypeFloor = "hype_floor";
        public const string ReasonWarehouseRule = "warehouse_rule";
        public const string ReasonBassRule = "bass_rule";
        public const string ReasonSignalLock = "signal_lock";
        public const string ReasonSignalSurge = "signal_surge";
        public const string ReasonRoomUnanswered = "room_unanswered";
        public const string ReasonDreamStale = "dream_stale";
        public const string ReasonRaveDrop = "rave_drop";
        public const string ReasonPierBass = "pier_bass";
        public const string ReasonThroneLock = "throne_lock";
        public const string ReasonThroneSurge = "throne_surge";
        public const string ReasonCathedralEcho = "cathedral_echo";
        public const string ReasonExchangeUnpaid = "exchange_unpaid";
        public const string ReasonRelayLine = "relay_line";
        public const string ReasonFestivalLock = "festival_lock";
        public const string ReasonFestivalSurge = "festival_surge";
        public const string ReasonSignalClear = "signal_clear";
        public const string ReasonThroneClear = "throne_clear";
        public const string ReasonCampaignClear = "campaign_clear";

        /// <summary>Signal mid-set lock: Hype ≥ this at half timer (eased for early Act1).</summary>
        public const double SignalLockHype = 48d;

        /// <summary>Static Throne mid-set lock: Hype ≥ this at half timer.</summary>
        public const double ThroneLockHype = 52d;

        /// <summary>Final Festival mid-set lock: Hype ≥ this at half timer.</summary>
        public const double FestivalLockHype = 58d;

        /// <summary>
        /// First room of the night opens four points under its target, when that is warmer
        /// than the authored open. Later rooms use the authored initial.
        /// </summary>
        public static double OpeningHype(Act1ClubDefinition club, int index)
        {
            if (index > 0)
                return club.InitialHype;
            double warm = club.TargetHype - 4d;
            return warm > club.InitialHype ? warm : club.InitialHype;
        }

        private readonly MusicClock _clock;
        private readonly ScoreChange[] _applied = new ScoreChange[8];
        private readonly ScoreEvent[] _reactionEvents = new ScoreEvent[8];
        private readonly DeckReactionDetector _deckReactions = new DeckReactionDetector();
        private readonly RunCrate _crate;
        private readonly MetaUnlocks _meta;
        private readonly TrackLibrary _library;
        private int _seed;

        private string[] _route;
        private int _clubIndex;
        private Act1ClubDefinition _club;
        private double _clubDuration;
        private SetJudge _judge;
        private RunPhase _phase;
        private SetOutcome _outcome;
        private string _reason;
        private double _elapsed;
        private double _lastAudioTime;
        private bool _anchored;

        private int _cleanCount;
        private int _anyCleans;
        private int _dialectCleans;
        private int _bassConflicts;
        private int _cleanPrepCount;
        private int _cueReadyHonorCount;
        private bool _hadDrop;
        private bool _hadStale;
        private bool _signalLockChecked;
        private bool _signalLockOk;

        private string[] _offeredTracks = Array.Empty<string>();
        private string _chosenTrack = "";

        public Act1RunDirector(int seed)
        {
            _seed = seed;
            _clock = new MusicClock(new ManualTimeProvider(), MusicClock.ReferenceBpm, 0d, 0d);
            _crate = new RunCrate();
            _meta = new MetaUnlocks();
            _library = TrackLibrary.CreateAct1Playable();
            Reset(seed);
        }

        /// <summary>
        /// Boot hydration from a migrated SaveGame. Pushes track unlocks into Meta.
        /// Survives later <see cref="Reset"/> (meta is not cleared).
        /// </summary>
        public void HydrateFrom(SaveGameDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            document.ApplyToMeta(_meta);
        }

        /// <summary>
        /// Boot hydration from persisted track ids. Survives later <see cref="Reset"/>.
        /// </summary>
        public void HydrateMeta(IEnumerable<string> unlockedTrackIds)
        {
            _meta.Hydrate(unlockedTrackIds);
        }

        public double Reputation => _judge.Reputation;

        public int Combo => _judge.Combo;

        public string[] Route => _route;

        public Act1ClubDefinition CurrentClub => _club;

        public RunCrate Crate => _crate;

        public MetaUnlocks Meta => _meta;

        public Act1Readout Readout
        {
            get
            {
                double remaining = _phase == RunPhase.PreClub
                    ? _clubDuration
                    : _clubDuration - _elapsed;
                if (remaining < 0d)
                    remaining = 0d;

                string t0 = _offeredTracks.Length > 0 ? _offeredTracks[0] : "";
                string t1 = _offeredTracks.Length > 1 ? _offeredTracks[1] : "";
                string t2 = _offeredTracks.Length > 2 ? _offeredTracks[2] : "";

                return new Act1Readout(
                    _clubIndex,
                    _club.Id,
                    _club.IsBoss,
                    _phase,
                    _judge.Hype,
                    _club.TargetHype,
                    remaining,
                    _outcome,
                    _reason,
                    _crate.Count,
                    t0,
                    t1,
                    t2,
                    _chosenTrack,
                    _meta.Contains(TrackRewardRoll.BossCarrierId),
                    _judge.CheerIntensity,
                    _judge.CrowdMotionImpulseValue);
            }
        }

        /// <summary>Optional: collect mixer expression events without applying (tests / probes).</summary>
        public int CollectMixerExpressions(MixSnapshot snapshot, ScoreEvent[] results, int offset)
        {
            return _judge.CollectMixerExpressions(snapshot, results, offset);
        }

        public void BeginClub()
        {
            if (_phase != RunPhase.PreClub)
                return;

            _phase = RunPhase.Performing;
            _outcome = SetOutcome.InProgress;
            _reason = "";
            _elapsed = 0d;
            _anchored = false;
            ClearClubCounters();
        }

        public void Tick(MixSnapshot snapshot)
        {
            if (_phase != RunPhase.Performing)
                return;
            if (double.IsNaN(snapshot.AudioTime) || double.IsInfinity(snapshot.AudioTime))
                return;

            if (!_anchored)
            {
                _anchored = true;
                _lastAudioTime = snapshot.AudioTime;
                if (snapshot.Paused)
                {
                    _judge.Observe(snapshot, _applied);
                }
                else
                    Score(snapshot);
                CheckPhaseLock();
                CloseIfNeeded();
                return;
            }

            if (snapshot.AudioTime < _lastAudioTime)
                return;

            if (snapshot.Paused)
            {
                _judge.Observe(snapshot, _applied);
                _lastAudioTime = snapshot.AudioTime;
                return;
            }

            _elapsed += snapshot.AudioTime - _lastAudioTime;
            _lastAudioTime = snapshot.AudioTime;
            Score(snapshot);
            CheckPhaseLock();
            CloseIfNeeded();
        }

        /// <summary>
        /// Ordinary win: Result → Reward and rolls this act's slot.
        /// Signal / Static Throne win: opens the next club. The boss track is already granted.
        /// </summary>
        public void Continue()
        {
            if (_phase != RunPhase.Result || _outcome != SetOutcome.Win)
                return;

            if (ContinuesAfterBoss(_club.Id))
            {
                if (_clubIndex >= _route.Length - 1)
                    return;
                OpenClub(_clubIndex + 1, keepReputation: true);
                return;
            }

            if (_club.IsBoss)
                return;

            _phase = RunPhase.Reward;
            _chosenTrack = "";
            RollTrackOffer();
        }

        /// <summary>
        /// Pick one offered track into the run crate. Returns false if id is not in the offer.
        /// </summary>
        public bool ChooseTrack(string trackId)
        {
            if (_phase != RunPhase.Reward || _outcome != SetOutcome.Win)
                return false;
            if (string.IsNullOrEmpty(trackId))
                return false;

            bool offered = false;
            for (int i = 0; i < _offeredTracks.Length; i++)
            {
                if (_offeredTracks[i] == trackId)
                {
                    offered = true;
                    break;
                }
            }

            if (!offered)
                return false;

            if (!_crate.Contains(trackId) && !_crate.TryAdd(trackId))
                return false;

            _chosenTrack = trackId;
            return true;
        }

        /// <summary>
        /// Leaves Reward and opens the next club.
        /// </summary>
        public bool AdvanceFromReward()
        {
            if (_phase != RunPhase.Reward || _outcome != SetOutcome.Win)
                return false;
            if (_clubIndex >= _route.Length - 1)
                return false;

            OpenClub(_clubIndex + 1, keepReputation: true);
            return true;
        }

        /// <summary>
        /// Manual mark for warehouse pass when reactions are not driven this frame.
        /// Prefer <see cref="ApplyDeckReactions"/> with CleanPrepEligible cues.
        /// </summary>
        public void NotifyCleanPrepTransition()
        {
            if (_phase != RunPhase.Performing)
                return;
            _cleanPrepCount++;
        }

        /// <summary>
        /// Manual mark for Signal surge when reactions are not driven this frame.
        /// Prefer <see cref="ApplyDeckReactions"/> with honored request cues.
        /// </summary>
        public void NotifyCueReadyHonor()
        {
            if (_phase != RunPhase.Performing)
                return;
            _cueReadyHonorCount++;
        }

        /// <summary>
        /// Runs <see cref="DeckReactionDetector"/> and applies accepted events through
        /// <see cref="SetJudge.ApplyEvent"/>. CueReadyHonor / CleanPrepTransition also
        /// bump club pass counters. Call before <see cref="Tick"/> so a last-frame honor
        /// still counts toward close.
        /// </summary>
        /// <returns>How many events were newly applied this call.</returns>
        public int ApplyDeckReactions(MixSnapshot snapshot, DeckReactionCues cues)
        {
            if (_phase != RunPhase.Performing)
                return 0;
            if (double.IsNaN(snapshot.AudioTime) || double.IsInfinity(snapshot.AudioTime))
                return 0;

            int found = _deckReactions.Collect(snapshot, cues, _reactionEvents, 0);
            int applied = 0;
            for (int i = 0; i < found; i++)
            {
                ScoreEvent scoreEvent = _reactionEvents[i];
                if (!_judge.ApplyEvent(scoreEvent, out _))
                    continue;

                applied++;
                ScoreKind kind = scoreEvent.Kind;
                if (kind == ScoreKind.CueReadyHonor)
                    _cueReadyHonorCount++;
                else if (kind == ScoreKind.CleanPrepTransition)
                    _cleanPrepCount++;
            }

            CloseIfNeeded();
            return applied;
        }

        public void Reset(int seed)
        {
            _seed = seed;
            _route = Act1Route.Build(seed);
            _judge = new SetJudge(_clock);
            _crate.ResetToMvp();
            // Meta unlocks persist across run Reset (GDD: meta does not roll back on lose).
            OpenClub(0, keepReputation: false);
        }

        /// <summary>
        /// Snapshot for pause-menu Save and for quitting an open night.
        /// <paramref name="deckId"/> is the deck worn in this run. FinalResult yields an inactive checkpoint.
        /// </summary>
        public RunCheckpoint CaptureCheckpoint(string deckId = "")
        {
            if (_phase == RunPhase.FinalResult)
                return RunCheckpoint.CreateEmpty();

            var checkpoint = RunCheckpoint.CreateEmpty();
            checkpoint.Active = true;
            checkpoint.Seed = _seed;
            checkpoint.ClubIndex = _clubIndex;
            checkpoint.Phase = _phase;
            checkpoint.Outcome = _outcome;
            checkpoint.ReasonId = _reason ?? "";
            checkpoint.ElapsedSeconds = _elapsed;
            checkpoint.Hype = _judge.Hype;
            checkpoint.Combo = _judge.Combo;
            checkpoint.Reputation = _judge.Reputation;
            checkpoint.DeckId = deckId ?? "";
            checkpoint.ChosenTrackId = _chosenTrack ?? "";
            checkpoint.CleanCount = _cleanCount;
            checkpoint.BassConflicts = _bassConflicts;
            checkpoint.CleanPrepCount = _cleanPrepCount;
            checkpoint.CueReadyHonorCount = _cueReadyHonorCount;
            checkpoint.HadDrop = _hadDrop;
            checkpoint.HadStale = _hadStale;
            checkpoint.SignalLockChecked = _signalLockChecked;
            checkpoint.SignalLockOk = _signalLockOk;

            string[] crate = _crate.ToArray();
            for (int i = 0; i < crate.Length; i++)
                checkpoint.CrateTrackIds.Add(crate[i]);

            for (int i = 0; i < _offeredTracks.Length; i++)
            {
                if (!string.IsNullOrEmpty(_offeredTracks[i]))
                    checkpoint.OfferedTrackIds.Add(_offeredTracks[i]);
            }

            return checkpoint;
        }

        /// <summary>
        /// Restore a pause-menu checkpoint. Meta unlocks stay; caller should <see cref="HydrateFrom"/> first.
        /// </summary>
        public bool TryRestore(RunCheckpoint checkpoint)
        {
            if (checkpoint == null || !checkpoint.CanResume)
                return false;
            if (checkpoint.ClubIndex < 0 || checkpoint.ClubIndex >= Act1Route.ClubCount)
                return false;

            _seed = checkpoint.Seed;
            _route = Act1Route.Build(_seed);
            if (checkpoint.ClubIndex >= _route.Length)
                return false;

            _crate.ReplaceFrom(checkpoint.CrateTrackIds);
            _judge = new SetJudge(_clock);
            OpenClub(checkpoint.ClubIndex, keepReputation: false);
            _judge.RestoreSet(checkpoint.Hype, checkpoint.Combo, checkpoint.Reputation);

            _phase = checkpoint.Phase;
            _outcome = checkpoint.Outcome;
            _reason = checkpoint.ReasonId ?? "";
            _elapsed = checkpoint.ElapsedSeconds < 0d ? 0d : checkpoint.ElapsedSeconds;
            if (_elapsed > _clubDuration)
                _elapsed = _clubDuration;
            _anchored = false;
            _chosenTrack = checkpoint.ChosenTrackId ?? "";
            _cleanCount = checkpoint.CleanCount;
            _bassConflicts = checkpoint.BassConflicts;
            _cleanPrepCount = checkpoint.CleanPrepCount;
            _cueReadyHonorCount = checkpoint.CueReadyHonorCount;
            _hadDrop = checkpoint.HadDrop;
            _hadStale = checkpoint.HadStale;
            _signalLockChecked = checkpoint.SignalLockChecked;
            _signalLockOk = checkpoint.SignalLockOk;

            if (checkpoint.OfferedTrackIds != null && checkpoint.OfferedTrackIds.Count > 0)
            {
                _offeredTracks = new string[checkpoint.OfferedTrackIds.Count];
                for (int i = 0; i < checkpoint.OfferedTrackIds.Count; i++)
                    _offeredTracks[i] = checkpoint.OfferedTrackIds[i] ?? "";
            }
            else if (_phase == RunPhase.Reward)
                RollTrackOffer();
            else
                _offeredTracks = Array.Empty<string>();

            return true;
        }

        private void OpenClub(int index, bool keepReputation)
        {
            _clubIndex = index;
            _club = Act1ClubDefinition.Require(_route[index]);
            _clubDuration = _clock.BarDuration * _club.DurationBars;
            _phase = RunPhase.PreClub;
            _outcome = SetOutcome.InProgress;
            _reason = "";
            _elapsed = 0d;
            _anchored = false;
            _offeredTracks = Array.Empty<string>();
            _chosenTrack = "";
            ClearClubCounters();

            if (keepReputation)
                _judge.BeginNextSet(OpeningHype(_club, index));
            else
            {
                _judge = new SetJudge(_clock);
                double open = OpeningHype(_club, index);
                if (Math.Abs(open - HypeSystem.StartingHype) > 1e-9d)
                    _judge.BeginNextSet(open);
            }

            _judge.SoftNight = index == 0;
        }

        private void RollTrackOffer()
        {
            int act = _clubIndex / 4;
            int slot = _clubIndex % 4;
            int salted = unchecked(_seed ^ (_clubIndex * 397) ^ 0x51CED);
            string[] owned = _crate.ToArray();
            IReadOnlyList<string> ordinary = slot == 0
                ? TrackRewardRoll.FromCommon(salted, act)
                : TrackRewardRoll.FromMid(salted, owned, act);
            string special = TrackRewardRoll.FromSpecial(salted, owned, act);
            _offeredTracks = MergeSpecial(ordinary, special);
        }

        /// <summary>
        /// Keeps the three readout slots. A special id is an extra card: appended when the
        /// ordinary offer is short, otherwise it replaces the last ordinary id.
        /// </summary>
        private static string[] MergeSpecial(IReadOnlyList<string> ordinary, string special)
        {
            if (string.IsNullOrEmpty(special))
                return ToArray(ordinary);

            int count = ordinary == null ? 0 : ordinary.Count;
            if (count >= 3)
            {
                var replaced = new string[3];
                replaced[0] = ordinary[0];
                replaced[1] = ordinary[1];
                replaced[2] = special;
                return replaced;
            }

            var appended = new string[count + 1];
            for (int i = 0; i < count; i++)
                appended[i] = ordinary[i];
            appended[count] = special;
            return appended;
        }

        private static string[] ToArray(IReadOnlyList<string> list)
        {
            if (list == null || list.Count == 0)
                return Array.Empty<string>();
            var result = new string[list.Count];
            for (int i = 0; i < list.Count; i++)
                result[i] = list[i];
            return result;
        }

        private void ClearClubCounters()
        {
            _cleanCount = 0;
            _anyCleans = 0;
            _dialectCleans = 0;
            _bassConflicts = 0;
            _cleanPrepCount = 0;
            _cueReadyHonorCount = 0;
            _hadDrop = false;
            _hadStale = false;
            _signalLockChecked = false;
            _signalLockOk = false;
            _deckReactions.Reset();
        }

        private bool CleanIsDialect(MixSnapshot snapshot)
        {
            string trackId = DominantTrackId(snapshot);
            if (trackId.Length == 0)
                return false;

            TrackDefinition track;
            if (!_library.TryGet(trackId, out track))
                return false;

            return RoomDemand.GenreFits(_club.Id, track.Genre);
        }

        /// <summary>
        /// Playing deck with the higher gain. Equal gains take B when it is playing, otherwise A.
        /// </summary>
        private static string DominantTrackId(MixSnapshot snapshot)
        {
            bool a = snapshot.DeckAPlaying;
            bool b = snapshot.DeckBPlaying;
            if (a && b)
            {
                if (snapshot.GainB > snapshot.GainA)
                    return snapshot.TrackB ?? "";
                if (snapshot.GainA > snapshot.GainB)
                    return snapshot.TrackA ?? "";
                return snapshot.TrackB ?? "";
            }

            if (b)
                return snapshot.TrackB ?? "";
            if (a)
                return snapshot.TrackA ?? "";
            return "";
        }

        private void Score(MixSnapshot snapshot)
        {
            int count = _judge.Observe(snapshot, _applied);
            for (int i = 0; i < count; i++)
            {
                ScoreKind kind = _applied[i].Kind;
                if (kind == ScoreKind.CleanTransition)
                {
                    _cleanCount++;
                    _anyCleans++;
                    if (CleanIsDialect(snapshot))
                        _dialectCleans++;
                }
                else if (kind == ScoreKind.BassConflict)
                    _bassConflicts++;
                else if (kind == ScoreKind.Drop)
                    _hadDrop = true;
                else if (kind == ScoreKind.StaleGroove)
                    _hadStale = true;
                else if (kind == ScoreKind.CleanPrepTransition)
                    _cleanPrepCount++;
                else if (kind == ScoreKind.CueReadyHonor)
                    _cueReadyHonorCount++;
            }
        }

        private void CheckPhaseLock()
        {
            double threshold;
            if (!TryLockThreshold(out threshold) || _signalLockChecked)
                return;
            if (_elapsed + 1e-9 < _clubDuration * 0.5d)
                return;

            _signalLockChecked = true;
            _signalLockOk = _judge.Hype + 1e-9d >= threshold;
        }

        private bool TryLockThreshold(out double threshold)
        {
            if (_club.PassRule == Act1PassRule.SignalPhases)
            {
                threshold = SignalLockHype;
                return true;
            }

            if (_club.PassRule == Act1PassRule.ThroneLock)
            {
                threshold = ThroneLockHype;
                return true;
            }

            if (_club.PassRule == Act1PassRule.Finale)
            {
                threshold = FestivalLockHype;
                return true;
            }

            threshold = 0d;
            return false;
        }

        private void CloseIfNeeded()
        {
            if (_phase != RunPhase.Performing)
                return;

            if (_judge.IsLost)
            {
                Finish(SetOutcome.Lose, ReasonHypeFloor, RunPhase.FinalResult);
                return;
            }

            if (_elapsed + 1e-9 < _clubDuration)
                return;

            if (_judge.Hype + 1e-9d < _club.TargetHype)
            {
                Finish(SetOutcome.Lose, ReasonTimeout, RunPhase.FinalResult);
                return;
            }

            string specialFail;
            if (!PassesSpecial(out specialFail))
            {
                Finish(SetOutcome.Lose, specialFail, RunPhase.FinalResult);
                return;
            }

            if (!RoomDemand.HeardEnough(_clubIndex, _dialectCleans, _anyCleans))
            {
                Finish(SetOutcome.Lose, ReasonRoomUnanswered, RunPhase.FinalResult);
                return;
            }

            if (_club.Id == CampaignClubIds.FinalFestival)
            {
                GrantBossDrop();
                Finish(SetOutcome.Win, ReasonCampaignClear, RunPhase.FinalResult);
            }
            else if (_club.Id == Act1ClubIds.TheSignal)
            {
                GrantBossDrop();
                Finish(SetOutcome.Win, ReasonSignalClear, RunPhase.Result);
            }
            else if (_club.Id == CampaignClubIds.TheStaticThrone)
            {
                GrantBossDrop();
                Finish(SetOutcome.Win, ReasonThroneClear, RunPhase.Result);
            }
            else
                Finish(SetOutcome.Win, ReasonTargetMet, RunPhase.Result);
        }

        private void GrantBossDrop()
        {
            string drop = TrackRewardRoll.BossDropId(_club.Id);
            if (string.IsNullOrEmpty(drop))
                return;
            _crate.TryAdd(drop);
            _meta.Unlock(drop);
            _chosenTrack = drop;
        }

        private static bool ContinuesAfterBoss(string clubId)
        {
            return clubId == Act1ClubIds.TheSignal
                || clubId == CampaignClubIds.TheStaticThrone;
        }

        private bool PassesSpecial(out string failReason)
        {
            failReason = "";
            switch (_club.PassRule)
            {
                case Act1PassRule.None:
                    return true;

                case Act1PassRule.CleanPrepOrNoStale:
                    if (_cleanPrepCount >= 1 || !_hadStale)
                        return true;
                    failReason = ReasonWarehouseRule;
                    return false;

                case Act1PassRule.BassCleanLimit:
                    if (_bassConflicts <= 1 && _cleanCount >= 1)
                        return true;
                    failReason = ReasonBassRule;
                    return false;

                case Act1PassRule.SignalPhases:
                    if (!_signalLockOk)
                    {
                        failReason = ReasonSignalLock;
                        return false;
                    }

                    if (!_hadDrop || _cueReadyHonorCount < 1)
                    {
                        failReason = ReasonSignalSurge;
                        return false;
                    }

                    return true;

                case Act1PassRule.SoftHold:
                    if (!_hadStale)
                        return true;
                    failReason = ReasonDreamStale;
                    return false;

                case Act1PassRule.NeedDrop:
                    if (_hadDrop && _cleanCount >= 1)
                        return true;
                    failReason = ReasonRaveDrop;
                    return false;

                case Act1PassRule.SaltLimit:
                    if (_bassConflicts == 0 && _cleanCount >= 1)
                        return true;
                    failReason = ReasonPierBass;
                    return false;

                case Act1PassRule.ThroneLock:
                    if (!_signalLockOk)
                    {
                        failReason = ReasonThroneLock;
                        return false;
                    }

                    if (!_hadDrop || _cueReadyHonorCount < 1)
                    {
                        failReason = ReasonThroneSurge;
                        return false;
                    }

                    return true;

                case Act1PassRule.EchoTwice:
                    if (_dialectCleans >= 2)
                        return true;
                    failReason = ReasonCathedralEcho;
                    return false;

                case Act1PassRule.PaidDrop:
                    if (_hadDrop && _bassConflicts <= 1)
                        return true;
                    failReason = ReasonExchangeUnpaid;
                    return false;

                case Act1PassRule.ClearLine:
                    if (_cueReadyHonorCount >= 1 && !_hadStale)
                        return true;
                    failReason = ReasonRelayLine;
                    return false;

                case Act1PassRule.Finale:
                    if (!_signalLockOk)
                    {
                        failReason = ReasonFestivalLock;
                        return false;
                    }

                    if (!_hadDrop || _dialectCleans < 1)
                    {
                        failReason = ReasonFestivalSurge;
                        return false;
                    }

                    return true;

                default:
                    return true;
            }
        }

        private void Finish(SetOutcome outcome, string reason, RunPhase phase)
        {
            _outcome = outcome;
            _reason = reason;
            _phase = phase;
        }
    }
}
