using System;
using DeckRogue.Core.Music;

namespace DeckRogue.Core.Scoring
{
    /// <summary>
    /// DJ-010/011/012. Reads the mix on the audio clock and applies each event once.
    /// v0.1 numbers: Hype starts at 35, clean transition +8, drop +12, bass conflict -6,
    /// phrase decay -2, stale groove -4 after the groove has already held.
    /// </summary>
    public sealed class SetJudge
    {
        private readonly MusicClock _clock;
        private readonly HypeSystem _hype = new HypeSystem();
        private readonly TransitionEvaluator _transitions = new TransitionEvaluator();
        private readonly BassConflictDetector _bass = new BassConflictDetector();
        private readonly DropDetector _drops = new DropDetector();
        private readonly MixerExpressionDetector _expressions = new MixerExpressionDetector();
        private readonly DeckActionDetector _deckActions = new DeckActionDetector();
        private readonly ScoreEvent[] _found = new ScoreEvent[24];

        private double _lastTime = double.NegativeInfinity;
        private bool _havePhrase;
        private int _phrase;
        private string _groove = "";
        private int _groovePhrases;
        private int _riserPhrase = -1;
        private bool _haveBar;
        private int _bar;
        private CrowdMotionImpulse _crowdMotion;

        public SetJudge(MusicClock clock)
        {
            if (clock == null)
                throw new ArgumentNullException(nameof(clock));
            _clock = clock;
        }

        public double Hype => _hype.Hype;
        public int Combo => _hype.Combo;
        public double Reputation => _hype.Reputation;
        public bool IsLost => _hype.IsLost;
        public int CrowdBand => _hype.CrowdBand;
        public int CheerIntensity => _hype.CheerIntensity;
        public bool HoldingRise => _drops.HoldingRise;

        /// <summary>
        /// Max <see cref="CrowdMotionImpulse"/> from quality kinds applied on the last Observe call.
        /// </summary>
        public CrowdMotionImpulse CrowdMotionImpulse => _crowdMotion;

        /// <summary>Int form for BoothSnapshot / client (None=0 .. Peak=3).</summary>
        public int CrowdMotionImpulseValue => (int)_crowdMotion;

        /// <summary>
        /// First club of a night: fuss, decay, stale, dead air and hot-swap do not sink Hype.
        /// Bass conflict still does. The crowd still hears the event.
        /// </summary>
        public bool SoftNight { get; set; }

        public int Observe(MixSnapshot snapshot, ScoreChange[] results)
        {
            return Observe(snapshot, results, 0d, 0d);
        }

        /// <summary>
        /// dropHypeBonus rides the first accepted drop of each phrase.
        /// hardCutHypeBonus rides a clean transition that is already a hard cut.
        /// Same event id still pays once; a smooth transition takes no hard-cut bonus.
        /// </summary>
        public int Observe(MixSnapshot snapshot, ScoreChange[] results, double dropHypeBonus, double hardCutHypeBonus)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));
            if (double.IsNaN(snapshot.AudioTime) || double.IsInfinity(snapshot.AudioTime))
                return 0;
            if (snapshot.AudioTime < _lastTime)
                return 0;

            _crowdMotion = CrowdMotionImpulse.None;
            MusicPosition position = _clock.GetPosition(snapshot.AudioTime);
            if (snapshot.Paused)
            {
                _transitions.Reset();
                _bass.ResetSustain();
                _drops.ResetPrepare();
                _deckActions.Reset();
                _hype.Paused = true;
                _hype.HoldDefeatClock(snapshot.AudioTime);
                SyncPhrase(snapshot, position);
                SyncCrowdBar(position);
                _lastTime = snapshot.AudioTime;
                return 0;
            }

            _hype.Paused = false;
            SyncCrowdBar(position);
            int found = 0;
            found = CollectPhrase(snapshot, position, found);
            if (_transitions.TryRead(snapshot, position, _clock.BeatDuration, _clock.BarDuration, out ScoreEvent transition))
                found = Push(found, transition);
            if (_drops.TryRead(snapshot, position, _clock.BarDuration, out ScoreEvent drop))
                found = Push(found, drop);
            if (_bass.TryRead(snapshot, position, _clock.BeatDuration, out ScoreEvent conflict))
                found = Push(found, conflict);
            found += _expressions.Collect(
                snapshot,
                position,
                _clock.PhraseDuration,
                _found,
                found);
            found += _deckActions.Collect(
                snapshot,
                position,
                _clock.BarDuration,
                _found,
                found);
            found = SuppressBassHandoffOnDrop(found);
            found = SuppressPhraseStartOnLineLift(found);

            int written = 0;
            for (int i = 0; i < found && written < results.Length; i++)
            {
                ScoreEvent scoreEvent = _found[i];
                bool riser = false;
                int riserPhrase = -1;
                if (dropHypeBonus > 0d
                    && scoreEvent.Kind == ScoreKind.Drop
                    && FirstDropOfPhrase(scoreEvent.Id, out riserPhrase))
                {
                    scoreEvent = scoreEvent.WithBonus(dropHypeBonus, 0d);
                    riser = true;
                }
                else                 if (hardCutHypeBonus > 0d
                    && scoreEvent.Kind == ScoreKind.CleanTransition
                    && scoreEvent.HardCut)
                {
                    scoreEvent = scoreEvent.WithBonus(hardCutHypeBonus, 0d);
                }

                if (SoftNight && scoreEvent.HypeDelta < 0d && scoreEvent.Kind != ScoreKind.BassConflict)
                    scoreEvent = scoreEvent.WithBonus(-scoreEvent.HypeDelta, 0d);

                scoreEvent = TrackAuraModifier.Apply(scoreEvent, snapshot);

                if (!_hype.TryApply(scoreEvent, out ScoreChange change))
                    continue;
                if (riser)
                    _riserPhrase = riserPhrase;
                _crowdMotion = CrowdMotion.Max(_crowdMotion, CrowdMotion.FromKind(change.Kind));
                results[written++] = change;
            }

            _hype.AdvanceDefeat(snapshot.AudioTime, _clock.PhraseDuration);
            _lastTime = snapshot.AudioTime;
            return written;
        }

        /// <summary>
        /// Applies an external ScoreEvent (deck reactions) once by id.
        /// Does not advance mix detectors or the defeat clock — call <see cref="Observe"/> for those.
        /// Updates <see cref="CrowdMotionImpulse"/> when the event is quality motion.
        /// </summary>
        public bool ApplyEvent(ScoreEvent scoreEvent, out ScoreChange change)
        {
            if (!_hype.TryApply(scoreEvent, out change))
                return false;
            _crowdMotion = CrowdMotion.Max(_crowdMotion, CrowdMotion.FromKind(change.Kind));
            return true;
        }

        /// <summary>
        /// Collects mixer expression ScoreEvents without applying them (Act1 optional path).
        /// </summary>
        public int CollectMixerExpressions(MixSnapshot snapshot, ScoreEvent[] results, int offset)
        {
            if (results == null || offset < 0)
                return 0;
            if (double.IsNaN(snapshot.AudioTime) || double.IsInfinity(snapshot.AudioTime))
                return 0;
            MusicPosition position = _clock.GetPosition(snapshot.AudioTime);
            return _expressions.Collect(snapshot, position, _clock.PhraseDuration, results, offset);
        }

        /// <summary>
        /// Opens another set on the same run. Reputation stays. Hype, Combo and detectors start clean.
        /// </summary>
        public void BeginNextSet()
        {
            BeginNextSet(HypeSystem.StartingHype);
        }

        /// <summary>
        /// Opens the next club/set at a specific initial Hype (Act1 GDD table).
        /// </summary>
        public void BeginNextSet(double initialHype)
        {
            _hype.BeginNextSet(initialHype);
            ResetDetectorsForSet();
        }

        /// <summary>
        /// Pause-menu resume: restore Hype / Combo / Reputation and clear mix detectors.
        /// </summary>
        public void RestoreSet(double hype, int combo, double reputation)
        {
            _hype.Restore(hype, combo, reputation);
            ResetDetectorsForSet();
        }

        private void ResetDetectorsForSet()
        {
            _transitions.ResetForNewSet();
            _bass.Reset();
            _drops.ResetAll();
            _expressions.Reset();
            _deckActions.Reset();
            _lastTime = double.NegativeInfinity;
            _havePhrase = false;
            _phrase = 0;
            _groove = "";
            _groovePhrases = 0;
            _riserPhrase = -1;
            _haveBar = false;
            _bar = 0;
            _crowdMotion = CrowdMotionImpulse.None;
        }

        private void SyncCrowdBar(MusicPosition position)
        {
            if (!_haveBar)
            {
                _haveBar = true;
                _bar = position.Bar;
                return;
            }

            if (position.Bar <= _bar)
                return;

            for (int bar = _bar + 1; bar <= position.Bar; bar++)
                _hype.AdvanceCrowdBar();
            _bar = position.Bar;
        }

        private bool FirstDropOfPhrase(string id, out int phrase)
        {
            phrase = DropPhrase(id);
            return phrase >= 0 && phrase != _riserPhrase;
        }

        private static int DropPhrase(string id)
        {
            const string prefix = "drop-";
            if (id == null || id.Length <= prefix.Length)
                return -1;
            int bar;
            if (!int.TryParse(id.Substring(prefix.Length), out bar) || bar < 0)
                return -1;
            return bar / 8;
        }

        private int CollectPhrase(MixSnapshot snapshot, MusicPosition position, int found)
        {
            if (!_havePhrase)
            {
                _havePhrase = true;
                _phrase = position.Phrase;
                _groove = Groove(snapshot);
                _groovePhrases = _groove.Length == 0 ? 0 : 1;
                return found;
            }

            if (position.Phrase == _phrase)
                return found;

            for (int phrase = _phrase + 1; phrase <= position.Phrase; phrase++)
            {
                found = Push(found, ScoreEvent.Decay(phrase));
                string groove = Groove(snapshot);
                if (groove.Length == 0)
                {
                    _groove = "";
                    _groovePhrases = 0;
                }
                else if (groove == _groove)
                {
                    _groovePhrases++;
                }
                else
                {
                    _groove = groove;
                    _groovePhrases = 1;
                }

                if (groove.Length > 0 && _groovePhrases >= 6 && ((_groovePhrases - 6) % 2) == 0)
                    found = Push(found, ScoreEvent.Stale(phrase));
            }

            _phrase = position.Phrase;
            return found;
        }

        private void SyncPhrase(MixSnapshot snapshot, MusicPosition position)
        {
            _havePhrase = true;
            _phrase = position.Phrase;
            _groove = Groove(snapshot);
            _groovePhrases = _groove.Length == 0 ? 0 : 1;
        }

        /// <summary>
        /// A drop already in this batch eats the bass handoff. Shift the buffer and shrink the count
        /// before TryApply so the handoff id is never spent.
        /// </summary>
        private int SuppressBassHandoffOnDrop(int found)
        {
            bool drop = false;
            for (int i = 0; i < found; i++)
            {
                if (_found[i].Kind == ScoreKind.Drop)
                {
                    drop = true;
                    break;
                }
            }

            if (!drop)
                return found;

            int write = 0;
            for (int i = 0; i < found; i++)
            {
                if (_found[i].Kind == ScoreKind.BassHandoff)
                    continue;
                _found[write++] = _found[i];
            }

            return write;
        }

        /// <summary>
        /// One deck entering the mix on one sample is one gesture.
        /// LineLift of that deck eats PhraseStart before the id is spent.
        /// </summary>
        private int SuppressPhraseStartOnLineLift(int found)
        {
            bool liftA = false;
            bool liftB = false;
            for (int i = 0; i < found; i++)
            {
                if (_found[i].Kind != ScoreKind.LineLift)
                    continue;
                string id = _found[i].Id ?? "";
                if (id.EndsWith("-0"))
                    liftA = true;
                else if (id.EndsWith("-1"))
                    liftB = true;
            }

            if (!liftA && !liftB)
                return found;

            int write = 0;
            for (int i = 0; i < found; i++)
            {
                if (_found[i].Kind == ScoreKind.PhraseStart)
                {
                    string id = _found[i].Id ?? "";
                    if ((liftA && id.EndsWith("-0")) || (liftB && id.EndsWith("-1")))
                        continue;
                }

                _found[write++] = _found[i];
            }

            return write;
        }

        private int Push(int count, ScoreEvent scoreEvent)
        {
            if (count >= _found.Length)
                return count;
            _found[count] = scoreEvent;
            return count + 1;
        }

        private static string Groove(MixSnapshot snapshot)
        {
            if (snapshot.DeckAPlaying && snapshot.GainA >= TransitionEvaluator.DominantGain && snapshot.GainB <= TransitionEvaluator.QuietGain)
                return "A:" + snapshot.TrackA;
            if (snapshot.DeckBPlaying && snapshot.GainB >= TransitionEvaluator.DominantGain && snapshot.GainA <= TransitionEvaluator.QuietGain)
                return "B:" + snapshot.TrackB;
            return "";
        }
    }
}
