using System.Collections.Generic;
using System.Globalization;
using DeckRogue.Client;
using DeckRogue.Core.Audio;
using DeckRogue.Core.Meta;
using DeckRogue.Core.Music;
using DeckRogue.Core.Run;
using DeckRogue.Core.Save;
using DeckRogue.Core.Scoring;
using DeckRogue.Core.Time;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace DeckRogue.Audio
{
    /// <summary>
    /// Four-plus Act1 records, crossfader, Bass Kill, and the Act1 night loop.
    /// Play still starts on the next bar. Mixer moves are heard immediately.
    /// Begin, continue, reward, and reset go through Act1RunDirector.
    /// Hall lines use the same snapshot tick, and only while Performing.
    /// </summary>
    public sealed class TwoDeckSession : MonoBehaviour
    {
        private const string HostName = "AudioSpike";
        private static readonly StemId[] Stems = { StemId.Low, StemId.Perc, StemId.Music };
        private RunPhase _lastPhase = (RunPhase)(-1);

        private DspTimeProvider _time;
        private MusicClock _clock;
        private DeckScheduler _scheduler;
        private DeckMixer _mixer;
        private SetJudge _judge;
        private Act1RunDirector _act1;
        private ClubRequestDirector _requests;
        private int _runSeed;
        private RunDeckLock _deckLock;
        private string _envelopeNote = "";
        /// <summary>
        /// One-shot: the first finished club of this booth visit still owes the basement door.
        /// Cleared when that win leaves for the basement, or when Again starts another night.
        /// <see cref="SaveGameDocument.HubUnlocked"/> stays open either way.
        /// </summary>
        private bool _hubBornThisSession;
        private int _seedSalt;
        private readonly ScoreChange[] _scores = new ScoreChange[8];
        private DeckRack _rack;
        private TrackLibrary _library;
        private readonly HashSet<string> _playable = new HashSet<string>();
        private readonly List<TrackDefinition> _records = new List<TrackDefinition>();
        // One chip stays free for "More" once the night outgrows the strip.
        private static int RecordPageSize => RunCrate.Cap > 1 ? RunCrate.Cap - 1 : 1;
        private int _pageStart;
        private string _recordLabels;
        private IAudioAssetLocator _locator;
        private NativeUnityAudioBackend _backend;
        private BoothHud _hud;
        private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        private readonly CueReadyTracker _cueReady = new CueReadyTracker();
        private readonly PhraseLoadQueue _phraseLoad = new PhraseLoadQueue();
        private string _barCueA;
        private string _barCueB;
        private int _barCueAfterA = int.MinValue;
        private int _barCueAfterB = int.MinValue;
        private bool _roomKnows;
        private readonly DeckPresence _presence = new DeckPresence();
        private DeckId _selected = DeckId.A;
        private DeckId _armed = DeckId.A;
        private bool _armCut;
        private string _line = "";
        private bool _scoreLine;
        private int _hallCue;
        private int _crowdImpulse;
        private int _impulseBeatsLeft;
        private int _impulsePrevBeat = -1;
        private bool _alignJudge;
        private long _commandId;
        private bool _ready;
        private string _lastScoreKind = "";
        private string _lastCleanTransitionId = "";
        private bool _cleanPrepEligible;
        private bool _wasAudibleA;
        private bool _wasAudibleB;
        private bool _paused;
        private PauseMenuPanel _pauseMenu;
        private StoryBeatPanel _clubStory;
        private int _storyClubIndex = -1;
        private FirstNightCoach _coach;
        private BoothCoachOverlay _coachUi;
        private bool _coachGateChecked;
        private bool _coachCratePicked;
        private bool _coachMixMoved;
        private bool _coachAck;
        private bool _coachSkip;

        /// <summary>Two bars at 4/4 — hall keeps dancing after a scored action.</summary>
        private const int ImpulseHoldBeats = 8;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterShell()
        {
            GameplayShell.Mount = MountHost;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterFirstScene()
        {
            MountHost();
        }

        /// <summary>Idempotent AudioSpike host spawn (ShellSceneRouter + first-scene belt).</summary>
        public static void MountHost()
        {
            // Prefer keeping the gameplay host scene named AudioSpike (HostName).
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName != HostName && sceneName != ShellScenes.AudioSpike)
                return;
            if (GameObject.Find(HostName) != null)
                return;

            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.082f, 0.086f, 0.157f, 1f);
            }

            var host = new GameObject(HostName);
            host.AddComponent<TwoDeckSession>();
        }

        private void Awake()
        {
            _time = new DspTimeProvider();
            _clock = new MusicClock(_time, TrackDefinition.MvpBpm, MusicClock.DefaultSchedulingLeadTime);
            _scheduler = new DeckScheduler(_clock);
            _mixer = new DeckMixer();
            _judge = new SetJudge(_clock);
            GameSaveService save = GameSaveService.Ensure();
            bool resume = save.ConsumePendingResume();
            RunCheckpoint checkpoint = resume && save.Document != null
                ? save.Document.Checkpoint
                : null;
            _runSeed = checkpoint != null && checkpoint.CanResume
                ? checkpoint.Seed
                : FreshSeed();
            _act1 = new Act1RunDirector(_runSeed);
            // Hydrate meta unlocks from SaveGame before the night starts (DJ-116 client half).
            if (save.Document != null)
            {
                _act1.HydrateFrom(save.Document);
                GameSettings.ApplyFrom(save.Document.Settings);
            }

            SaveGameDocument document = save.Document ?? SaveGameDocument.CreateDefault();
            bool restored = checkpoint != null && checkpoint.CanResume && _act1.TryRestore(checkpoint);
            if (restored && DeckCatalog.IsDeck(checkpoint.DeckId))
                _deckLock = RunDeckLock.FromId(checkpoint.DeckId);
            else
                _deckLock = new RunDeckLock(document);

            if (restored)
            {
                _alignJudge = true;
                string place = checkpoint.PlaceLine();
                SetMessage(string.IsNullOrEmpty(place)
                    ? "Loaded: " + _act1.CurrentClub.Id + "."
                    : "Loaded: " + place + ".");
            }
            else if (resume)
                SetMessage("Save not found — starting a new run.");

            _requests = new ClubRequestDirector(_runSeed);
            _rack = new DeckRack();
            _library = BuildPlayableLibrary();
            _locator = new UnityResourceLocator();
            _backend = new NativeUnityAudioBackend();
            _hud = new BoothHud(
                transform,
                () => Engage(DeckId.A),
                () => StopDeck(DeckId.A),
                () => Engage(DeckId.B),
                () => StopDeck(DeckId.B),
                ApplyCrossfader,
                () => ToggleBass(DeckId.A),
                () => ToggleBass(DeckId.B),
                () => Cycle(DeckId.A),
                () => Cycle(DeckId.B),
                OnRunAction,
                OnPickReward,
                LoadCrateSlot,
                amount => SetFilter(DeckId.A, amount),
                amount => SetFilter(DeckId.B, amount),
                amount => SetPerc(DeckId.A, amount),
                amount => SetPerc(DeckId.B, amount),
                amount => SetMusic(DeckId.A, amount),
                amount => SetMusic(DeckId.B, amount),
                () => ToggleLoop(DeckId.A),
                () => ToggleLoop(DeckId.B),
                amount => SetLine(DeckId.A, amount),
                amount => SetLine(DeckId.B, amount));
            _hud.SetSecondaryRunAction(RestartRun);
            _hud.SetPauseAction(TogglePauseMenu);
            _hud.ApplyDeckKit(_deckLock.Kit);
            _ready = LoadLibrary();
            ApplyCrossfader(0f);
            EnsureEventSystem();
            _lastPhase = _act1 != null ? _act1.Readout.Phase : (RunPhase)(-1);
            TryShowClubStory();
        }

        private void EnsureEventSystem()
        {
            EventSystem current = EventSystem.current;
            if (current == null)
            {
                var eventObject = new GameObject("EventSystem");
                eventObject.transform.SetParent(transform, false);
                current = eventObject.AddComponent<EventSystem>();
            }

            InputSystemUIInputModule module = current.GetComponent<InputSystemUIInputModule>();
            if (module == null)
            {
                StandaloneInputModule legacy = current.GetComponent<StandaloneInputModule>();
                if (legacy != null)
                    Destroy(legacy);
                module = current.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            module.pointerBehavior = UIPointerBehavior.SingleMouseOrPenButMultiTouchAndTrack;
        }

        private void Update()
        {
            if (_backend == null)
                return;

            PushGains();
            _backend.Tick();
            ReleaseStoppedDeck(DeckId.A);
            ReleaseStoppedDeck(DeckId.B);
            _armCut = false;
            ApplyPhraseLoads();
            ApplyBarCues();
            PollKeys();
            ReadScore();
            TrackNightEconomy();
            if (_hud != null)
            {
                BoothSnapshot shot = Capture();
                _hud.Refresh(shot);
                TickFirstNightCoach(shot);
            }
        }

        private void TickFirstNightCoach(BoothSnapshot shot)
        {
            if (!_coachGateChecked)
            {
                _coachGateChecked = true;
                if (ShouldRunFirstNightCoach())
                {
                    _coach = new FirstNightCoach();
                    _coachUi = new BoothCoachOverlay(
                        _hud.OverlayRoot,
                        () => _coachAck = true,
                        () => _coachSkip = true);
                }
            }

            if (_coach == null || _coachUi == null)
                return;

            if (_coachSkip)
            {
                _coach.Skip();
                _coachSkip = false;
                CompleteFirstNightCoach(persist: true);
                return;
            }

            bool story = _clubStory != null && _clubStory.IsOpen;
            bool inBooth = _act1 != null
                && (_act1.Readout.Phase == RunPhase.PreClub || _act1.Readout.Phase == RunPhase.Performing);
            CoachCard card;
            RectTransform anchor = null;
            bool visible = _coach.TryGetCard(out card)
                && _hud.TryGetCoachAnchor(card.Spotlight, out anchor);

            var signals = new CoachSignals();
            signals.InBooth = inBooth;
            signals.PreClub = _act1 != null && _act1.Readout.Phase == RunPhase.PreClub;
            signals.Performing = _act1 != null && _act1.Readout.Phase == RunPhase.Performing;
            signals.StoryOpen = story;
            signals.Paused = _paused;
            signals.SpotlightVisible = visible;
            signals.CratePicked = _coachCratePicked;
            signals.DeckALive = shot.A.Playing || shot.A.Queued;
            signals.DeckBLive = shot.B.Playing || shot.B.Queued;
            signals.Crossfader = _mixer != null ? (float)_mixer.Crossfader : 0f;
            signals.BassKilled = _mixer != null && (_mixer.IsBassKilled(DeckId.A) || _mixer.IsBassKilled(DeckId.B));
            signals.MixMoved = _coachMixMoved;
            signals.Acknowledged = _coachAck;
            _coachAck = false;

            _coach.Observe(signals);
            if (_coach.IsFinished)
            {
                CompleteFirstNightCoach(_coach.WasShown);
                return;
            }

            if (story || _paused || !_coach.TryGetCard(out card))
            {
                _coachUi.Hide();
                return;
            }

            if (!_hud.TryGetCoachAnchor(card.Spotlight, out anchor))
            {
                _coachUi.Hide();
                return;
            }

            _coachUi.Present(card, anchor);
            _coachUi.Tick(Time.unscaledDeltaTime);
        }

        private bool ShouldRunFirstNightCoach()
        {
            if (_act1 == null || _hud == null || _hud.OverlayRoot == null)
                return false;
            if (_act1.Readout.ClubIndex != 0 || _act1.Readout.IsBoss)
                return false;

            GameSaveService save = GameSaveService.Ensure();
            if (save.Document != null
                && save.Document.Settings != null
                && save.Document.Settings.BoothCoachCompleted)
                return false;
            return true;
        }

        private void CompleteFirstNightCoach(bool persist)
        {
            if (_coachUi != null)
            {
                _coachUi.Dismiss();
                _coachUi = null;
            }

            _coach = null;
            if (persist)
                GameSaveService.Ensure().MarkTutorialCompletedOnFirstRun();
        }

        private void OnDestroy()
        {
            if (_coachUi != null)
            {
                _coachUi.Dismiss();
                _coachUi = null;
            }

            if (_backend != null)
                _backend.Dispose();
        }

        private static TrackLibrary BuildPlayableLibrary()
        {
            return TrackLibrary.CreateAct1Playable();
        }

        private bool LoadLibrary()
        {
            RefreshClubRecords();
            TrackDefinition a = _records.Count > 0 ? _records[0] : null;
            TrackDefinition b = _records.Count > 1 ? _records[1] : a;
            bool loaded = a != null && Place(DeckId.A, a, false);
            loaded &= b != null && Place(DeckId.B, b, false);
            SetMessage(loaded ? "Tap a disc for club records. START puts it on air." : "No playable stems.");
            return loaded;
        }

        private static string[] ClubRecords(string club)
        {
            switch (club)
            {
                case Act1ClubIds.NeonBasement:
                    return new[] { "track_carbon_copy", "track_cold_cathode", "track_night_tram" };
                case Act1ClubIds.WarehousePulse:
                    return new[] { "track_warehouse_line", "track_smoke_column", "track_return_duct", "track_freight_lift" };
                case Act1ClubIds.BassFactory:
                    return new[] { "track_press_line", "track_torque_drum", "track_ratchet_works" };
                case Act1ClubIds.TheSignal:
                    return new[] { "track_phase_fault", "track_signal_breach" };
                case CampaignClubIds.DreamLounge:
                    return new[] { "track_dusk_pad", "track_velvet_room" };
                case CampaignClubIds.RaveDistrict:
                    return new[] { "track_strobe_grid", "track_yellow_siren" };
                case CampaignClubIds.ChromePier:
                    return new[] { "track_pier_salt", "track_tide_click" };
                case CampaignClubIds.TheStaticThrone:
                    return new[] { "track_throne_hum", "track_crown_loop" };
                case CampaignClubIds.GlassCathedral:
                    return new[] { "track_prism_pane", "track_choir_dust" };
                case CampaignClubIds.NightExchange:
                    return new[] { "track_ledger_tick", "track_favor_bass" };
                case CampaignClubIds.RoofRelay:
                    return new[] { "track_sky_cable", "track_wind_hat" };
                case CampaignClubIds.FinalFestival:
                    return new[] { "track_festival_grid", "track_city_choir" };
                default:
                    return System.Array.Empty<string>();
            }
        }

        private void RefreshClubRecords()
        {
            var available = new HashSet<string>(_act1.Crate.ToArray());
            available.UnionWith(ClubRecords(_act1.CurrentClub.Id));
            if (_act1.Meta.Contains(TrackRewardRoll.BossCarrierId))
                available.Add(TrackRewardRoll.BossCarrierId);
            if (_act1.Meta.Contains(TrackAura.StaticCrownId))
                available.Add(TrackAura.StaticCrownId);
            if (_act1.Meta.Contains(TrackAura.FestivalAnthemId))
                available.Add(TrackAura.FestivalAnthemId);

            _playable.Clear();
            _clips.Clear();

            for (int i = 0; i < _library.Tracks.Count; i++)
            {
                TrackDefinition track = _library.Tracks[i];
                if (!available.Contains(track.Id))
                    continue;
                bool ok = true;
                var pending = new Dictionary<string, AudioClip>(Stems.Length);
                for (int stemIndex = 0; stemIndex < Stems.Length; stemIndex++)
                {
                    StemId stem = Stems[stemIndex];
                    string path = _locator.Locate(track, stem);
                    AudioClip clip;
                    if (_clips.TryGetValue(path, out clip))
                    {
                        pending[path] = clip;
                        continue;
                    }

                    clip = Resources.Load<AudioClip>(path);
                    if (clip == null)
                    {
                        ok = false;
                        break;
                    }

                    if (System.Math.Abs(clip.length - track.LengthSeconds) > TrackLibrary.LengthToleranceSeconds)
                    {
                        ok = false;
                        break;
                    }

                    pending[path] = clip;
                }

                if (!ok)
                    continue;

                foreach (KeyValuePair<string, AudioClip> pair in pending)
                {
                    if (!_clips.ContainsKey(pair.Key))
                        _clips.Add(pair.Key, pair.Value);
                }

                _playable.Add(track.Id);
            }
            _records.Clear();
            foreach (string id in ClubRecords(_act1.CurrentClub.Id))
                if (TryPlayable(id, out TrackDefinition local))
                    _records.Add(local);
            foreach (TrackDefinition record in _library.Tracks)
                if (_playable.Contains(record.Id) && !_records.Contains(record))
                    _records.Add(record);
            _pageStart = 0;
            _recordLabels = BuildCrateSlots();
            RefreshDeckSelection(DeckId.A, "track_neon_pulse");
            RefreshDeckSelection(DeckId.B, "track_afterhours");
        }

        private void RefreshDeckSelection(DeckId deck, string starter)
        {
            TrackDefinition current = _rack.Loaded(deck);
            if (current == null || _playable.Contains(current.Id))
                return;
            TrackDefinition next = FirstPlayableInCrate(starter);
            if (next == null)
                return;
            if (_scheduler.IsActive(deck))
            {
                _rack.Replace(deck, next);
                PlayNow(deck, next);
                _scheduler.NotePlayingFrom(deck, _time.Now);
            }
            else
                Place(deck, next, false);
        }

        private TrackDefinition FirstPlayableInCrate(string preferredId)
        {
            if (_act1 != null
                && !string.IsNullOrEmpty(preferredId)
                && _act1.Crate.Contains(preferredId)
                && TryPlayable(preferredId, out TrackDefinition preferred))
                return preferred;

            if (_act1 == null)
                return null;

            string[] ids = _act1.Crate.ToArray();
            for (int i = 0; i < ids.Length; i++)
            {
                TrackDefinition track;
                if (TryPlayable(ids[i], out track))
                    return track;
            }

            return null;
        }

        private bool TryPlayable(string id, out TrackDefinition track)
        {
            track = null;
            if (string.IsNullOrEmpty(id) || !_playable.Contains(id))
                return false;
            return _library.TryGet(id, out track);
        }

        private void Engage(DeckId deck)
        {
            _selected = deck;
            _armCut = false;
            if (IsInTheMix(deck))
            {
                SetMessage(deck + " stays where the fader left it.");
                return;
            }

            _armed = deck;
            if (_scheduler.IsActive(deck))
            {
                SetMessage(deck + " is coming in on the fader.");
                return;
            }

            StartDeck(deck);
        }

        private bool IsInTheMix(DeckId deck)
        {
            if (!_scheduler.IsActive(deck) || !_scheduler.TryGetStart(deck, out double start))
                return false;
            return _time.Now >= start;
        }

        private void Cycle(DeckId deck)
        {
            if (!_ready)
                return;

            _selected = deck;
            TrackDefinition next = NextCrateTrack(deck);
            if (next != null)
                _coachCratePicked = true;
            if (next == null)
            {
                SetMessage("No playable records in the crate.");
                return;
            }

            _armCut = false;
            if (_scheduler.IsActive(deck))
            {
                QueueBarCue(deck, next.Id);
                SetMessage(deck + ": " + next.DisplayName + " on the next bar. Fader stays.");
                return;
            }

            if (!Place(deck, next, false))
                return;

            SetMessage(deck + ": " + next.DisplayName + " cued. It waits for the fader.");
        }

        private TrackDefinition NextCrateTrack(DeckId deck)
        {
            if (_act1 == null)
                return null;

            if (_records.Count == 0)
                return null;
            int index = _records.IndexOf(_rack.Loaded(deck));
            return _records[(index + 1) % _records.Count];
        }

        private void PlayNow(DeckId deck, TrackDefinition track)
        {
            _backend.PlayClipsNow(deck, Clip(track, StemId.Low), Clip(track, StemId.Perc), Clip(track, StemId.Music));
        }

        private AudioClip Clip(TrackDefinition track, StemId stem)
        {
            return _clips[_locator.Locate(track, stem)];
        }

        /// <summary>
        /// Alpha 1/2/3 and keypad 1/2/3 pick offer A/B/C. Ignored unless that card is on the reward screen.
        /// </summary>
        private void PollRewardKeys(Keyboard keyboard)
        {
            Act1Readout act1 = _act1.Readout;
            if (string.IsNullOrEmpty(act1.OfferedTrack0) || string.IsNullOrEmpty(act1.OfferedTrack1))
                return;

            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
                OnPickReward(act1.OfferedTrack0);
            else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
                OnPickReward(act1.OfferedTrack1);
            else if (!string.IsNullOrEmpty(act1.OfferedTrack2)
                && (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame))
                OnPickReward(act1.OfferedTrack2);
        }

        private void LoadCrateSlot(int slot)
        {
            if (!_ready || _act1 == null || slot < 0)
                return;

            bool paged = _records.Count > RecordPageSize + 1;
            if (paged && slot == RecordPageSize)
            {
                _pageStart += RecordPageSize;
                if (_pageStart >= _records.Count)
                    _pageStart = 0;
                _recordLabels = BuildCrateSlots();
                return;
            }
            int index = _pageStart + slot;
            if (index >= _records.Count)
                return;
            TrackDefinition track = _records[index];
            _coachCratePicked = true;

            bool performing = _act1.Readout.Phase == RunPhase.Performing;
            if (performing)
            {
                _phraseLoad.QueueLoad(_selected, track.Id);
                SetMessage(_selected + ": " + track.DisplayName + " at phrase boundary.");
                return;
            }

            Place(_selected, track, true);
        }

        private void ApplyPhraseLoads()
        {
            if (!_ready || _act1 == null || _act1.Readout.Phase != RunPhase.Performing)
                return;

            ApplyPhraseLoad(DeckId.A);
            ApplyPhraseLoad(DeckId.B);
        }

        private void QueueBarCue(DeckId deck, string trackId)
        {
            int bar = CurrentBar();
            if (deck == DeckId.A)
            {
                _barCueA = trackId;
                _barCueAfterA = bar;
            }
            else
            {
                _barCueB = trackId;
                _barCueAfterB = bar;
            }
        }

        private void ApplyBarCues()
        {
            if (!_ready || _clock == null)
                return;

            int bar = CurrentBar();
            ApplyBarCue(DeckId.A, bar);
            ApplyBarCue(DeckId.B, bar);
        }

        private void ApplyBarCue(DeckId deck, int bar)
        {
            string trackId = deck == DeckId.A ? _barCueA : _barCueB;
            int after = deck == DeckId.A ? _barCueAfterA : _barCueAfterB;
            if (string.IsNullOrEmpty(trackId) || bar <= after)
                return;

            if (deck == DeckId.A)
                _barCueA = null;
            else
                _barCueB = null;

            TrackDefinition track;
            if (!TryPlayable(trackId, out track))
                return;

            if (_scheduler.IsActive(deck))
            {
                _rack.Replace(deck, track);
                PlayNow(deck, track);
                _scheduler.NotePlayingFrom(deck, _time.Now);
                SetMessage(deck + ": " + track.DisplayName + ".");
                return;
            }

            Place(deck, track, false);
            SetMessage(deck + ": " + track.DisplayName + " cued.");
        }

        private int CurrentBar()
        {
            MusicPosition position = _clock.GetPosition();
            return position.Phrase * 8 + position.BarInPhrase;
        }

        private void ApplyPhraseLoad(DeckId deck)
        {
            string trackId;
            if (!_phraseLoad.TryApply(_clock, deck, out trackId))
                return;

            TrackDefinition track;
            if (!TryPlayable(trackId, out track))
                return;

            if (_scheduler.IsActive(deck))
            {
                _rack.Replace(deck, track);
                PlayNow(deck, track);
                _scheduler.NotePlayingFrom(deck, _time.Now);
            }
            else
            {
                Place(deck, track, true);
            }
        }

        private bool Place(DeckId deck, TrackDefinition track, bool announce)
        {
            string reason;
            if (!_rack.TryLoad(deck, track, _scheduler.IsActive(deck), out reason))
            {
                if (announce)
                    SetMessage(deck + " is still playing. Stop it first.");
                return false;
            }

            for (int stemIndex = 0; stemIndex < Stems.Length; stemIndex++)
            {
                StemId stem = Stems[stemIndex];
                string path = _locator.Locate(track, stem);
                _backend.AssignClip(deck, stem, _clips[path]);
            }

            if (announce)
                SetMessage(deck + ": " + track.DisplayName + ".");
            return true;
        }

        private void PollKeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                TogglePauseMenu();
                return;
            }

            if (_paused)
                return;

            if (keyboard.qKey.wasPressedThisFrame)
                StartDeck(DeckId.A);
            if (keyboard.pKey.wasPressedThisFrame)
                StartDeck(DeckId.B);
            if (keyboard.aKey.wasPressedThisFrame)
                ToggleBass(DeckId.A);
            if (keyboard.lKey.wasPressedThisFrame)
                ToggleBass(DeckId.B);
            // 1/2/3 pick a reward card only on the reward screen. During a set they load crate slots.
            if (_act1 != null && _act1.Readout.Phase == RunPhase.Reward)
                PollRewardKeys(keyboard);
            else
            {
                if (keyboard.digit1Key.wasPressedThisFrame)
                    LoadCrateSlot(0);
                if (keyboard.digit2Key.wasPressedThisFrame)
                    LoadCrateSlot(1);
                if (keyboard.digit3Key.wasPressedThisFrame)
                    LoadCrateSlot(2);
                if (keyboard.digit4Key.wasPressedThisFrame)
                    LoadCrateSlot(3);
                if (keyboard.digit5Key.wasPressedThisFrame)
                    LoadCrateSlot(4);
                if (keyboard.digit6Key.wasPressedThisFrame)
                    LoadCrateSlot(5);
                if (keyboard.digit7Key.wasPressedThisFrame)
                    LoadCrateSlot(6);
                if (keyboard.digit8Key.wasPressedThisFrame)
                    LoadCrateSlot(7);
            }

            float nudge = 0f;
            if (keyboard.leftArrowKey.isPressed)
                nudge -= 0.02f;
            if (keyboard.rightArrowKey.isPressed)
                nudge += 0.02f;
            if (nudge != 0f && _hud != null)
                _hud.NudgeCrossfader(nudge);

            // Optional filter nudges: F/G deck A, H/J deck B (also [ ] and ; ').
            if (_hud != null)
            {
                if (keyboard.fKey.isPressed || keyboard.leftBracketKey.isPressed)
                    _hud.NudgeFilterA(-0.02f);
                if (keyboard.gKey.isPressed || keyboard.rightBracketKey.isPressed)
                    _hud.NudgeFilterA(0.02f);
                if (keyboard.hKey.isPressed || keyboard.semicolonKey.isPressed)
                    _hud.NudgeFilterB(-0.02f);
                if (keyboard.jKey.isPressed || keyboard.quoteKey.isPressed)
                    _hud.NudgeFilterB(0.02f);
            }
        }

        private bool StartDeck(DeckId deck)
        {
            _selected = deck;
            if (!_ready)
                return false;
            if (_rack.Loaded(deck) == null)
            {
                SetMessage(deck + ": load a record first.");
                return false;
            }

            if (_scheduler.IsActive(deck))
            {
                SetMessage(deck + " is already playing.");
                return false;
            }

            for (int attempt = 0; attempt < 2; attempt++)
            {
                long commandId = ++_commandId;
                double now = _time.Now;
                ScheduleResult result = _scheduler.TryScheduleStart(deck, commandId, MusicalBoundary.Bar, now);
                if (!result.Accepted)
                {
                    SetMessage(deck + ": " + result.Reason);
                    return false;
                }

                DeckStartStatus status = _backend.TryStartDeck(deck, result.StartAudioTime);
                if (status == DeckStartStatus.Started)
                {
                    SetMessage(deck + " starts on the next bar.");
                    return true;
                }

                _scheduler.MarkStopped(deck);
                if (status != DeckStartStatus.TimeInThePast)
                {
                    SetMessage(deck + ": " + status);
                    return false;
                }
            }

            SetMessage(deck + ": could not schedule into the future.");
            return false;
        }

        private void StopDeck(DeckId deck)
        {
            if (!_scheduler.IsActive(deck))
            {
                SetMessage(deck + " is silent.");
                return;
            }

            _backend.BeginStop(deck);
            SetMessage(deck + " fading out.");
        }

        private void ToggleBass(DeckId deck)
        {
            bool killed = !_mixer.IsBassKilled(deck);
            if (!_mixer.TryApply(MixerCommand.BassKill(++_commandId, deck, killed)))
                return;
            PushGains();
            SetMessage(deck + (killed ? ": bass muted." : ": bass back in the mix."));
        }

        private BoothSnapshot Capture()
        {
            MusicPosition position = _clock.GetPosition();
            var snapshot = new BoothSnapshot();
            snapshot.BeatInBar = position.BeatInBar;
            snapshot.BarInPhrase = position.BarInPhrase;
            snapshot.PhraseNumber = position.Phrase + 1;
            snapshot.Message = _line;
            snapshot.ElapsedSeconds = _clock.Elapsed;
            snapshot.A = Readout(DeckId.A);
            snapshot.B = Readout(DeckId.B);

            Act1Readout act1 = _act1 == null ? default(Act1Readout) : _act1.Readout;
            if (_act1 != null)
            {
                snapshot.Hype = (int)System.Math.Round(act1.Hype);
                snapshot.Combo = _act1.Combo;
                snapshot.Reputation = (int)System.Math.Round(_act1.Reputation);
                snapshot.CrowdBand = act1.Hype >= 70d ? 2 : (act1.Hype >= 40d ? 1 : 0);
            }
            else
            {
                snapshot.Hype = (int)System.Math.Round(_judge.Hype);
                snapshot.Combo = _judge.Combo;
                snapshot.Reputation = (int)System.Math.Round(_judge.Reputation);
                snapshot.CrowdBand = _judge.CrowdBand;
            }

            bool cueReady = _cueReady.IsCueReady(DeckId.B) || _cueReady.IsCueReady(DeckId.A);
            int crateCount = _act1 == null ? 0 : _act1.Crate.Count;
            if (_act1 != null)
                RunChrome.ApplyAct1(ref snapshot, act1, cueReady, _lastScoreKind, crateCount);

            if (_hubBornThisSession && act1.Phase == RunPhase.Result)
                snapshot.RunAction = "В подвал";

            bool reward = act1.Phase == RunPhase.Reward;
            if (reward)
                snapshot.RunAction = "";
            snapshot.ShowReward = reward;
            snapshot.OfferA = act1.OfferedTrack0 ?? "";
            snapshot.OfferB = act1.OfferedTrack1 ?? "";
            snapshot.OfferC = act1.OfferedTrack2 ?? "";
            snapshot.OfferATitle = TrackTitle(snapshot.OfferA);
            snapshot.OfferBTitle = TrackTitle(snapshot.OfferB);
            snapshot.OfferCTitle = TrackTitle(snapshot.OfferC);

            snapshot.CrateLabel = "RECORDS";
            snapshot.CrateSlots = _recordLabels;
            snapshot.CueReadyA = _cueReady.IsCueReady(DeckId.A);
            snapshot.CueReadyB = _cueReady.IsCueReady(DeckId.B);
            snapshot.QueuedLoadLabel = PendingLoadLabel();
            snapshot.ClubRequestLabel = _requests != null && _requests.IsActive
                ? "Prep B"
                : "";

            snapshot.HeldUpgrade = "";
            snapshot.RiserLevel = 0;
            snapshot.SignFlash = false;
            int cheer = _act1 != null ? act1.CheerIntensity : _judge.CheerIntensity;
            if (_roomKnows && cheer < CrowdEnergy.MaxIntensity)
                cheer++;
            snapshot.CheerIntensity = cheer;
            AdvanceCrowdImpulse(position.Beat);
            snapshot.CrowdImpulse = _crowdImpulse;
            snapshot.MotionPhase = MotionPhaseOf(position);
            snapshot.FilterA = (float)_mixer.FilterAmount(DeckId.A);
            snapshot.FilterB = (float)_mixer.FilterAmount(DeckId.B);
            snapshot.PercA = (float)_mixer.PercTrim(DeckId.A);
            snapshot.PercB = (float)_mixer.PercTrim(DeckId.B);
            snapshot.MusicA = (float)_mixer.MusicTrim(DeckId.A);
            snapshot.MusicB = (float)_mixer.MusicTrim(DeckId.B);
            snapshot.LineA = (float)_mixer.LineGain(DeckId.A);
            snapshot.LineB = (float)_mixer.LineGain(DeckId.B);
            snapshot.LoopLengthBars = _scheduler.GetLoopLengthBars(DeckId.A);
            snapshot.Cue = _hallCue;
            snapshot.A.DimBassRim = false;
            snapshot.B.DimBassRim = false;
            snapshot.DominantTrackId = DominantAudibleTrackId();
            if (_envelopeNote.Length > 0
                && (act1.Phase == RunPhase.Result || act1.Phase == RunPhase.FinalResult))
            {
                snapshot.ClockLabel = _envelopeNote;
                snapshot.RunStatus = string.IsNullOrEmpty(snapshot.RunStatus)
                    ? _envelopeNote
                    : _envelopeNote + "  " + snapshot.RunStatus;
            }

            return snapshot;
        }

        private float MotionPhaseOf(MusicPosition position)
        {
            double beats = _clock.Elapsed / _clock.BeatDuration;
            if (double.IsNaN(beats) || double.IsInfinity(beats))
                return position.BeatInBar;
            double phase = beats % 4d;
            if (phase < 0d)
                phase += 4d;
            return (float)phase;
        }

        private void AdvanceCrowdImpulse(int absoluteBeat)
        {
            if (_impulsePrevBeat >= 0 && absoluteBeat != _impulsePrevBeat && _impulseBeatsLeft > 0)
            {
                _impulseBeatsLeft--;
                if (_impulseBeatsLeft <= 0 && _hallCue != HallCue.Fault)
                    _crowdImpulse = 0;
            }

            _impulsePrevBeat = absoluteBeat;
        }

        private void ApplyCrowdImpulse(ScoreKind kind)
        {
            int impulse = ImpulseOf(kind);
            if (impulse < 0)
                return;

            _crowdImpulse = impulse;
            _impulseBeatsLeft = ImpulseHoldBeats;
        }

        /// <summary>
        /// ScoreKind → hall motion 0..3. −1 = leave impulse unchanged.
        /// Prefers Core <see cref="CrowdMotion.FromKind"/>; Fault → 0.
        /// </summary>
        public static int ImpulseOf(ScoreKind kind)
        {
            CrowdMotionImpulse motion = CrowdMotion.FromKind(kind);
            if (motion != CrowdMotionImpulse.None)
                return (int)motion;
            // Until FromKind learns these gestures, still kick the hall.
            if (kind == ScoreKind.PhraseStart || kind == ScoreKind.BassHandoff)
                return 1;
            if (kind == ScoreKind.LineLift)
                return 2;
            if (kind == ScoreKind.BassConflict
                || kind == ScoreKind.StaleGroove
                || kind == ScoreKind.Fuss
                || kind == ScoreKind.MissedTransition
                || kind == ScoreKind.LateLoad
                || kind == ScoreKind.DeadAir
                || kind == ScoreKind.HotSwapFumble)
                return 0;
            return -1;
        }

        private string TrackTitle(string id)
        {
            if (string.IsNullOrEmpty(id))
                return "";
            TrackDefinition track;
            return _library.TryGet(id, out track) ? track.DisplayName : id;
        }

        private string BuildCrateSlots()
        {
            bool paged = _records.Count > RecordPageSize + 1;
            var parts = new string[paged ? RecordPageSize + 1 : _records.Count];
            int count = paged ? System.Math.Min(RecordPageSize, _records.Count - _pageStart) : parts.Length;
            for (int i = 0; i < count; i++)
                parts[i] = ShortName(_records[_pageStart + i].DisplayName);
            if (paged)
                parts[RecordPageSize] = "More >";
            return string.Join("|", parts);
        }

        private static string ShortName(string display)
        {
            if (string.IsNullOrEmpty(display))
                return "?";
            int space = display.IndexOf(' ');
            if (space > 0 && space <= 8)
                return display.Substring(0, space);
            if (display.Length <= 10)
                return display;
            return display.Substring(0, 10);
        }

        private string PendingLoadLabel()
        {
            string a;
            string b;
            bool hasA = _phraseLoad.TryGetPending(DeckId.A, out a);
            bool hasB = _phraseLoad.TryGetPending(DeckId.B, out b);
            if (!hasA && !hasB)
                return "";
            if (hasA && hasB)
                return "LOAD A/B → phrase boundary";
            if (hasA)
                return "LOAD A → phrase boundary";
            return "LOAD B → phrase boundary";
        }

        private DeckReadout Readout(DeckId deck)
        {
            var readout = new DeckReadout();
            readout.BassKilled = _mixer.IsBassKilled(deck);
            readout.Selected = deck == DeckId.A ? _mixer.Crossfader <= 0.5d : _mixer.Crossfader >= 0.5d;
            readout.State = "EMPTY";
            readout.Loop = "loop " + _scheduler.GetLoopLengthBars(deck);
            TrackDefinition track = _rack.Loaded(deck);
            if (track != null)
            {
                readout.TrackName = track.DisplayName;
                readout.Detail = track.Genre + " · " + track.Mood;
            }

            if (!_scheduler.IsActive(deck))
                return readout;

            double now = _time.Now;
            if (!_scheduler.TryGetStart(deck, out double start) || now < start)
            {
                readout.Queued = true;
                readout.State = "WAITING BAR";
                return readout;
            }

            readout.Playing = true;
            double length = track == null ? TrackDefinition.MvpLengthSeconds : track.LengthSeconds;
            int loop = _backend.CompletedLoops(deck, length, now) + 1;
            // Keep Loop as 1/2/4/8 mode for the LOOP toggle; count goes on State.
            readout.State = "PLAYING · " + loop.ToString(CultureInfo.InvariantCulture);
            readout.SpinDegrees = (float)((now - start) / 2d * 360d);
            return readout;
        }

        private void ReleaseStoppedDeck(DeckId deck)
        {
            if (_scheduler.IsActive(deck) && !_backend.IsRunning(deck))
                _scheduler.MarkStopped(deck);
        }

        private void ApplyCrossfader(float position)
        {
            if (!_mixer.TryApply(MixerCommand.Crossfader(++_commandId, position)))
                return;
            PushGains();
        }

        /// <summary>Client API: low-pass amount 0..1 per deck (0 open, 1 fully filtered).</summary>
        public bool SetFilter(DeckId deck, float amount)
        {
            if (!_mixer.TryApply(MixerCommand.Filter(++_commandId, deck, amount)))
                return false;
            if (amount > 0.04f)
                _coachMixMoved = true;
            PushDeck(deck);
            return true;
        }

        /// <summary>Client API: percussion stem trim 0..1 (multiplies deck gain).</summary>
        public bool SetPerc(DeckId deck, float gain)
        {
            if (!_mixer.TryApply(MixerCommand.PercGain(++_commandId, deck, gain)))
                return false;
            if (System.Math.Abs(gain - 1f) > 0.04f)
                _coachMixMoved = true;
            PushDeck(deck);
            return true;
        }

        /// <summary>Client API: music stem trim 0..1 (multiplies deck gain).</summary>
        public bool SetMusic(DeckId deck, float gain)
        {
            if (!_mixer.TryApply(MixerCommand.MusicGain(++_commandId, deck, gain)))
                return false;
            if (System.Math.Abs(gain - 1f) > 0.04f)
                _coachMixMoved = true;
            PushDeck(deck);
            return true;
        }

        /// <summary>Client API: line channel fader 0..1 (multiplies deck gain).</summary>
        public bool SetLine(DeckId deck, float gain)
        {
            if (!_mixer.TryApply(MixerCommand.LineGain(++_commandId, deck, gain)))
                return false;
            if (System.Math.Abs(gain - 1f) > 0.04f)
                _coachMixMoved = true;
            PushDeck(deck);
            return true;
        }

        /// <summary>Client API: schedule boundary 1|2|4|8 bars for the deck.</summary>
        public bool SetLoopLength(DeckId deck, LoopLengthMode mode)
        {
            return _scheduler.TrySetLoopLength(++_commandId, deck, mode);
        }

        public bool SetLoopLengthBars(DeckId deck, int bars)
        {
            return _scheduler.TrySetLoopLengthBars(++_commandId, deck, bars);
        }

        private void ToggleLoop(DeckId deck)
        {
            LoopLengthMode next = LoopLength.Next(_scheduler.GetLoopLength(deck));
            if (!SetLoopLength(deck, next))
                return;
            _coachMixMoved = true;
            int bars = LoopLength.ToBars(next);
            SetMessage(deck + ": loop " + bars.ToString(CultureInfo.InvariantCulture) + " bars.");
        }

        private void PushGains()
        {
            PushDeck(DeckId.A);
            PushDeck(DeckId.B);
        }

        private void PushDeck(DeckId deck)
        {
            _backend.SetDeckTarget(deck, (float)_mixer.DeckGain(deck));
            float low = _mixer.IsBassKilled(deck) ? 0f : 1f;
            _backend.SetStemTarget(deck, StemId.Low, low);
            _backend.SetStemTarget(deck, StemId.Perc, (float)_mixer.PercTrim(deck));
            _backend.SetStemTarget(deck, StemId.Music, (float)_mixer.MusicTrim(deck));
            _backend.SetFilterAmount(deck, (float)_mixer.FilterAmount(deck));
        }

        private void ReadScore()
        {
            MixSnapshot sample = Sample();
            if (_alignJudge)
            {
                _judge = new SetJudge(_clock);
                if (_act1 != null)
                    _judge.BeginNextSet(_act1.CurrentClub.InitialHype);
                _alignJudge = false;
                ClearScoreLine();
                ResetPrepState();
            }

            bool performing = _act1 != null && _act1.Readout.Phase == RunPhase.Performing;
            if (performing)
                TickDeckDepthBeforeAct1(sample);

            if (_act1 != null)
                _act1.Tick(sample);

            if (!performing || !_ready)
            {
                if (_act1 == null || _act1.Readout.Phase != RunPhase.Performing)
                    ClearScoreLine();
                return;
            }

            int count = _judge.Observe(sample, _scores);
            int frameImpulse = -1;
            for (int i = 0; i < count; i++)
            {
                ScoreChange change = _scores[i];
                _lastScoreKind = change.Kind.ToString();
                if (change.Kind == ScoreKind.CleanTransition)
                {
                    _lastCleanTransitionId = change.Id ?? "";
                    if (_cueReady.IsCueReady(DeckId.A) || _cueReady.IsCueReady(DeckId.B))
                        _cleanPrepEligible = true;
                }

                int impulse = ImpulseOf(change.Kind);
                if (impulse > frameImpulse)
                    frameImpulse = impulse;
                string note = Note(change);
                if (note.Length > 0)
                    SetScoreLine(note, CueOf(change.Kind));
            }

            if (frameImpulse >= 0)
            {
                _crowdImpulse = frameImpulse;
                _impulseBeatsLeft = ImpulseHoldBeats;
            }

            if (_act1.Readout.Phase != RunPhase.Performing)
                ClearScoreLine();
        }

        /// <summary>
        /// Cue / presence / club requests → ApplyDeckReactions before Act1.Tick (DJ-103 path).
        /// </summary>
        private void TickDeckDepthBeforeAct1(MixSnapshot sample)
        {
            TickCue(DeckId.A);
            TickCue(DeckId.B);

            MusicPosition position = _clock.GetPosition(sample.AudioTime);
            int phrase = position.Phrase;
            bool cueB = _cueReady.IsCueReady(DeckId.B);
            bool deadAir = _presence.Tick(sample, _clock.BeatDuration);

            if (_requests != null)
            {
                _requests.Tick(true, phrase);
                if (_requests.IsActive && !_scoreLine)
                    SetMessage("Club: prep B (CueReady).");

                DeckReactionCues cues = _requests.BuildCues(
                    phrase,
                    cueB,
                    deadAir,
                    false,
                    _cleanPrepEligible,
                    _lastCleanTransitionId);
                int applied = _act1.ApplyDeckReactions(sample, cues);
                if (applied > 0)
                {
                    if (_cleanPrepEligible)
                        _cleanPrepEligible = false;
                    if (_requests.IsActive && cueB)
                    {
                        ApplyCrowdImpulse(ScoreKind.CueReadyHonor);
                        SetScoreLine("Ready.", HallCue.Clean);
                    }
                }
            }
        }

        private void TickCue(DeckId deck)
        {
            bool loaded = _rack.Loaded(deck) != null;
            bool playing = IsAudible(deck);
            bool queued = loaded && !playing;
            _cueReady.Tick(_clock, deck, loaded, queued, playing);
        }

        private void ResetPrepState()
        {
            _cueReady.Reset();
            _phraseLoad.Reset();
            _barCueA = null;
            _barCueB = null;
            _barCueAfterA = int.MinValue;
            _barCueAfterB = int.MinValue;
            _roomKnows = false;
            _presence.Reset();
            if (_requests != null)
                _requests.Reset();
            _lastScoreKind = "";
            _lastCleanTransitionId = "";
            _cleanPrepEligible = false;
            _crowdImpulse = 0;
            _impulseBeatsLeft = 0;
            _impulsePrevBeat = -1;
            _wasAudibleA = false;
            _wasAudibleB = false;
        }

        private void OnRunAction()
        {
            if (_act1 == null)
                return;

            switch (_act1.Readout.Phase)
            {
                case RunPhase.PreClub:
                    if (_clubStory != null && _clubStory.IsOpen)
                        return;
                    _act1.BeginClub();
                    _alignJudge = true;
                    SetMessage(_act1.CurrentClub.Id + " — set started.");
                    break;
                case RunPhase.Result:
                    if (_hubBornThisSession)
                        LeaveWonClubForBasement();
                    else
                        _act1.Continue();
                    break;
                case RunPhase.Reward:
                    if (!string.IsNullOrEmpty(_act1.Readout.ChosenTrackId))
                        AdvanceClub();
                    break;
                case RunPhase.FinalResult:
                    // Primary CTA: night menu, then the day hub. Again → RestartRun stays on the booth.
                    PersistFinalResult();
                    DayHubOverlay.EnterOnNextMenu = true;
                    if (_hubBornThisSession)
                        DayHubOverlay.ArrivalLine = "Не взяли. Ночь закрыта.";
                    _hubBornThisSession = false;
                    SceneManager.LoadScene(ShellScenes.MainMenu);
                    break;
            }
        }

        private void AdvanceClub()
        {
            if (_act1.AdvanceFromReward())
            {
                RefreshClubRecords();
                Resources.UnloadUnusedAssets();
                TryShowClubStory();
            }
        }

        /// <summary>
        /// Story beat on every PreClub door — cast dialogue before the set can start.
        /// </summary>
        private void TryShowClubStory()
        {
            if (_act1 == null || _hud == null)
                return;
            if (_act1.Readout.Phase != RunPhase.PreClub)
                return;
            if (_storyClubIndex == _act1.Readout.ClubIndex && _clubStory != null && _clubStory.IsOpen)
                return;

            Transform root = _hud.OverlayRoot;
            if (root == null)
                return;

            Transform existing = root.Find("StoryBeatPanel");
            if (existing != null)
                Object.Destroy(existing.gameObject);

            Act1Readout readout = _act1.Readout;
            StoryBeat beat = CampaignStory.ForClubEntry(readout.ClubId, readout.ClubIndex, readout.IsBoss);
            if (beat.IsEmpty)
                return;

            _storyClubIndex = readout.ClubIndex;
            _clubStory = new StoryBeatPanel(
                root,
                new[] { beat },
                () => { _clubStory = null; },
                readout.IsBoss ? "BOSS STORY" : "CLUB STORY");
            SetMessage(CampaignCatalog.DisplayName(readout.ClubId) + " — story.");
        }

        private void RestartRun()
        {
            if (_act1 == null || _act1.Readout.Phase != RunPhase.FinalResult)
                return;

            PersistFinalResult();
            SettleFinishedNight();
            _hubBornThisSession = false;
            _envelopeNote = "";
            _runSeed = FreshSeed();
            _act1.Reset(_runSeed);
            RefreshClubRecords();
            Resources.UnloadUnusedAssets();
            _requests = new ClubRequestDirector(_runSeed);
            _alignJudge = true;
            ResetPrepState();
            _lastPhase = _act1.Readout.Phase;
            _storyClubIndex = -1;
            SaveOpenRun();
            SetMessage("New Act1 run.");
            TryShowClubStory();
        }

        private void TrackNightEconomy()
        {
            if (_act1 == null)
                return;

            RunPhase phase = _act1.Readout.Phase;
            bool paying = phase == RunPhase.Result || phase == RunPhase.FinalResult;
            if (!paying)
                _envelopeNote = "";

            if (_lastPhase == RunPhase.Performing && paying)
                ClaimFinishedGig();

            NoteHubBirth(phase);

            if (phase == RunPhase.FinalResult && _lastPhase != RunPhase.FinalResult)
            {
                PersistFinalResult();
                SettleFinishedNight();
            }
            else if (phase != _lastPhase)
                SaveOpenRun();

            _lastPhase = phase;
        }

        /// <summary>
        /// First finished club of a locked save opens the basement once.
        /// Later Results in this visit continue the night once <see cref="_hubBornThisSession"/> drops.
        /// </summary>
        private void NoteHubBirth(RunPhase phase)
        {
            if (_hubBornThisSession)
                return;
            if (phase != RunPhase.Result && phase != RunPhase.FinalResult)
                return;
            if (_lastPhase == phase)
                return;

            GameSaveService save = GameSaveService.Ensure();
            if (save.Document == null)
                return;
            if (!new MetaProfile(save.Document).TryUnlockHub())
                return;

            _hubBornThisSession = true;
            save.Flush(SaveFlushTrigger.MetaWalletChanged);
        }

        /// <summary>
        /// Birth win while the night is still open: park the club on the checkpoint and open the basement.
        /// Does not call <see cref="Act1RunDirector.Continue"/> and does not clear the checkpoint.
        /// Drops the one-shot so the next Result in this night continues instead of leaving again.
        /// </summary>
        private void LeaveWonClubForBasement()
        {
            if (_act1 == null)
                return;

            _hubBornThisSession = false;
            SaveOpenRun();
            DayHubOverlay.EnterOnNextMenu = true;
            DayHubOverlay.ArrivalLine = "Зал взял. Ночь на паузе.";
            SceneManager.LoadScene(ShellScenes.MainMenu);
        }

        private string CurrentRunId
        {
            get { return _runSeed.ToString(CultureInfo.InvariantCulture); }
        }

        private void ClaimFinishedGig()
        {
            if (_act1 == null)
                return;

            GameSaveService save = GameSaveService.Ensure();
            if (save.Document == null)
                return;

            Act1Readout readout = _act1.Readout;
            int coins;
            if (!new MetaProfile(save.Document).TryClaimGig(
                    CurrentRunId,
                    readout.ClubIndex,
                    readout.Outcome,
                    (int)System.Math.Floor(readout.Hype),
                    out coins))
                return;

            save.Flush(SaveFlushTrigger.MetaWalletChanged);
            _envelopeNote = "Конверт +" + coins.ToString(CultureInfo.InvariantCulture);
        }

        private void SettleFinishedNight()
        {
            GameSaveService save = GameSaveService.Ensure();
            if (save.Document == null)
                return;
            if (new MetaProfile(save.Document).SettleNight(CurrentRunId))
                save.Flush(SaveFlushTrigger.MetaWalletChanged);
        }

        private void PersistFinalResult()
        {
            GameSaveService save = GameSaveService.Ensure();
            if (save.Document == null || _act1 == null)
                return;

            bool clearedCheckpoint = save.Document.HasResumableCheckpoint;
            if (clearedCheckpoint)
                save.Document.ClearCheckpoint();

            SaveFlushTrigger[] triggers = Act1SaveSync.Apply(save.Document, _act1);
            if (triggers != null && triggers.Length > 0)
            {
                // One disk write covers meta unlock and/or best-rep raise (and cleared checkpoint).
                save.Flush(triggers[0]);
                return;
            }

            if (clearedCheckpoint)
                save.Flush(SaveFlushTrigger.RunCheckpointChanged);
        }

        private void TogglePauseMenu()
        {
            if (_act1 != null && _act1.Readout.Phase == RunPhase.FinalResult)
                return;

            if (_paused)
                ClosePauseMenu();
            else
                OpenPauseMenu();
        }

        private void OpenPauseMenu()
        {
            if (_paused || _hud == null || _hud.OverlayRoot == null)
                return;
            if (_hud.OverlayRoot.Find("PauseMenuPanel") != null)
                return;

            _paused = true;
            AudioListener.pause = true;
            _pauseMenu = new PauseMenuPanel(
                _hud.OverlayRoot,
                ClosePauseMenu,
                SaveRunFromPause,
                ReturnToMainMenuFromPause);
            if (_act1 != null)
                _pauseMenu.SetStatus(_act1.CaptureCheckpoint(WornDeckId()).PlaceLine());
        }

        private void ClosePauseMenu()
        {
            _paused = false;
            AudioListener.pause = false;
            if (_pauseMenu != null)
            {
                _pauseMenu.Destroy();
                _pauseMenu = null;
            }
        }

        private void SaveRunFromPause()
        {
            if (_act1 == null)
                return;

            RunCheckpoint checkpoint = _act1.CaptureCheckpoint(WornDeckId());
            if (!checkpoint.CanResume)
            {
                if (_pauseMenu != null)
                    _pauseMenu.SetStatus("Cannot save this screen");
                return;
            }

            GameSaveService.Ensure().SaveRunCheckpoint(checkpoint);
            if (_pauseMenu != null)
                _pauseMenu.SetStatus("Saved · " + checkpoint.PlaceLine());
        }

        private string WornDeckId()
        {
            return _deckLock != null ? _deckLock.DeckId : "";
        }

        /// <summary>
        /// Writes the open night: club index, phase, and the deck worn in this run.
        /// A finished night stays cleared.
        /// </summary>
        private void SaveOpenRun()
        {
            if (_act1 == null)
                return;

            RunCheckpoint checkpoint = _act1.CaptureCheckpoint(WornDeckId());
            if (!checkpoint.CanResume)
                return;

            GameSaveService.Ensure().SaveRunCheckpoint(checkpoint);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                SaveOpenRun();
        }

        private void OnApplicationQuit()
        {
            SaveOpenRun();
        }

        private void ReturnToMainMenuFromPause()
        {
            SaveRunFromPause();
            ClosePauseMenu();
            SceneManager.LoadScene(ShellScenes.MainMenu);
        }

        private int FreshSeed()
        {
            _seedSalt++;
            int seed = System.Environment.TickCount;
            if (seed == int.MinValue)
                seed = 1;
            seed ^= _seedSalt * 397;
            if (seed == 0)
                seed = _seedSalt;
            return seed;
        }

        /// <summary>
        /// Track id of the audible deck winning the fader. Same gain test as <see cref="Sample"/>.
        /// Empty when nothing is audible, or that deck has no record. Tie goes to B.
        /// </summary>
        private string DominantAudibleTrackId()
        {
            bool audibleA = IsAudible(DeckId.A);
            bool audibleB = IsAudible(DeckId.B);
            if (!audibleA && !audibleB)
                return "";

            TrackDefinition trackA = _rack.Loaded(DeckId.A);
            TrackDefinition trackB = _rack.Loaded(DeckId.B);
            TrackDefinition dominant;
            if (audibleB && (!audibleA || _mixer.DeckGain(DeckId.B) >= _mixer.DeckGain(DeckId.A)))
                dominant = trackB;
            else
                dominant = trackA;
            return dominant == null ? "" : dominant.Id;
        }

        private MixSnapshot Sample()
        {
            bool audibleA = IsAudible(DeckId.A);
            bool audibleB = IsAudible(DeckId.B);
            bool startA = audibleA && !_wasAudibleA;
            bool startB = audibleB && !_wasAudibleB;
            _wasAudibleA = audibleA;
            _wasAudibleB = audibleB;
            TrackDefinition trackA = _rack.Loaded(DeckId.A);
            TrackDefinition trackB = _rack.Loaded(DeckId.B);
            int loopA = _scheduler.GetLoopLength(DeckId.A) == LoopLengthMode.Half4 ? 4 : MixSnapshot.DefaultLoopBars;
            int loopB = _scheduler.GetLoopLength(DeckId.B) == LoopLengthMode.Half4 ? 4 : MixSnapshot.DefaultLoopBars;
            bool clubMatch = true;
            _roomKnows = false;
            if (_act1 != null)
            {
                string clubId = _act1.Readout.ClubId;
                if (!string.IsNullOrEmpty(clubId))
                {
                    if (!audibleA && !audibleB)
                        clubMatch = false;
                    else
                    {
                        TrackDefinition dominant;
                        if (audibleB && (!audibleA || _mixer.DeckGain(DeckId.B) >= _mixer.DeckGain(DeckId.A)))
                            dominant = trackB;
                        else
                            dominant = trackA;
                        clubMatch = RoomDemand.GenreFits(clubId, dominant == null ? "" : dominant.Genre);
                        _roomKnows = clubMatch;
                    }
                }
            }

            return new MixSnapshot(
                _time.Now,
                _paused,
                audibleA,
                audibleB,
                audibleA ? _mixer.DeckGain(DeckId.A) : 0d,
                audibleB ? _mixer.DeckGain(DeckId.B) : 0d,
                audibleA ? _mixer.StemGain(DeckId.A, StemId.Low) : 0d,
                audibleB ? _mixer.StemGain(DeckId.B, StemId.Low) : 0d,
                trackA == null ? "" : trackA.Id,
                trackB == null ? "" : trackB.Id,
                clubMatch,
                _mixer.FilterAmount(DeckId.A),
                _mixer.FilterAmount(DeckId.B),
                _mixer.PercTrim(DeckId.A),
                _mixer.PercTrim(DeckId.B),
                _mixer.MusicTrim(DeckId.A),
                _mixer.MusicTrim(DeckId.B),
                loopA,
                loopB,
                startA,
                startB,
                _mixer.LineGain(DeckId.A),
                _mixer.LineGain(DeckId.B));
        }

        private bool IsAudible(DeckId deck)
        {
            if (!_backend.IsRunning(deck))
                return false;
            if (!_scheduler.TryGetStart(deck, out double start))
                return false;
            return _time.Now + 1e-4d >= start;
        }

        private void OnPickReward(string id)
        {
            if (_act1 == null || _act1.Readout.Phase != RunPhase.Reward)
                return;
            if (!_act1.ChooseTrack(id))
                return;

            string name = TrackTitle(id);
            SetMessage("In crate: " + name + ".");
            AdvanceClub();
            _alignJudge = true;
        }

        private static int CueOf(ScoreKind kind)
        {
            if (kind == ScoreKind.CleanTransition
                || kind == ScoreKind.CleanPrepTransition
                || kind == ScoreKind.CueReadyHonor
                || kind == ScoreKind.FilterPhrase
                || kind == ScoreKind.PhraseStart
                || kind == ScoreKind.BassHandoff
                || kind == ScoreKind.LineLift
                || kind == ScoreKind.StemPunch
                || kind == ScoreKind.LoopSnap)
                return HallCue.Clean;
            if (kind == ScoreKind.Drop)
                return HallCue.Drop;
            if (kind == ScoreKind.BassConflict
                || kind == ScoreKind.StaleGroove
                || kind == ScoreKind.Fuss
                || kind == ScoreKind.MissedTransition
                || kind == ScoreKind.LateLoad
                || kind == ScoreKind.DeadAir
                || kind == ScoreKind.HotSwapFumble)
                return HallCue.Fault;
            return HallCue.None;
        }

        private static string Note(ScoreChange change)
        {
            switch (change.Kind)
            {
                case ScoreKind.CleanTransition:
                    return change.HardCut ? "Hard cut." : "Clean transition.";
                case ScoreKind.MissedTransition:
                    return "Transition missed.";
                case ScoreKind.Drop:
                    return "Drop.";
                case ScoreKind.BassConflict:
                    return "Double bass.";
                case ScoreKind.Fuss:
                    return "Fuss.";
                case ScoreKind.StaleGroove:
                    return "Floor cooling.";
                case ScoreKind.PhraseDecay:
                    return "";
                case ScoreKind.CueReadyHonor:
                    return "Ready.";
                case ScoreKind.LateLoad:
                    return "Late load.";
                case ScoreKind.DeadAir:
                    return "Dead air.";
                case ScoreKind.HotSwapFumble:
                    return "Hot-swap fumble.";
                case ScoreKind.CleanPrepTransition:
                    return "Clean prep.";
                case ScoreKind.FilterPhrase:
                    return "Filter into phrase.";
                case ScoreKind.PhraseStart:
                    return "Phrase start.";
                case ScoreKind.BassHandoff:
                    return "Bass handoff.";
                case ScoreKind.LineLift:
                    return "Line up.";
                case ScoreKind.StemPunch:
                    return "Stem punch.";
                case ScoreKind.LoopSnap:
                    return "Loop on boundary.";
                default:
                    return "";
            }
        }

        private void SetMessage(string value)
        {
            _line = value;
            _scoreLine = false;
            _hallCue = HallCue.None;
        }

        private void SetScoreLine(string value, int cue)
        {
            _line = value;
            _scoreLine = true;
            _hallCue = cue;
        }

        private void ClearScoreLine()
        {
            if (!_scoreLine)
                return;
            _line = "";
            _scoreLine = false;
            _hallCue = HallCue.None;
            if (_impulseBeatsLeft <= 0)
                _crowdImpulse = 0;
        }
    }
}
