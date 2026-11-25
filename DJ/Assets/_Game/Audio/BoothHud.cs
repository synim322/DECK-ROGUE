using System;
using DeckRogue.Core.Audio;
using DeckRogue.Core.Meta;
using DeckRogue.Core.Run;
using UnityEngine;
using UnityEngine.UI;

namespace DeckRogue.Audio
{
    public struct DeckReadout
    {
        public string TrackName;
        public string Detail;
        public string State;
        public string Loop;
        public bool Playing;
        public bool Queued;
        public bool BassKilled;
        public bool Selected;
        public float SpinDegrees;
        public bool DimBassRim;
    }

    public struct BoothSnapshot
    {
        public int BeatInBar;
        public int BarInPhrase;
        public int PhraseNumber;
        public string Message;
        public double ElapsedSeconds;
        public int Hype;
        public int Combo;
        public int Reputation;
        public int CrowdBand;
        public int TargetHype;
        public bool HasRun;
        public string Venue;
        public string SetLabel;
        public string GoalLabel;
        public string ClockLabel;
        public string RunStatus;
        public string RunAction;
        /// <summary>Optional secondary CTA (FinalResult Again). Empty hides the button.</summary>
        public string SecondaryRunAction;
        public bool ShowRunBanner;
        public int Cue;
        public bool ShowReward;
        public string OfferA;
        public string OfferB;
        public string OfferC;
        public string OfferATitle;
        public string OfferBTitle;
        public string OfferCTitle;
        public string HeldUpgrade;
        public int RiserLevel;
        public bool SignFlash;

        /// <summary>
        /// Hall cheer visual 0..3. Negative = derive from CrowdBand + Cue in BoothHud.
        /// </summary>
        public int CheerIntensity;

        /// <summary>
        /// Action impulse that moves the hall 0..3. Negative = derive from Cue / CheerIntensity.
        /// 0 idle/fault freeze · 2 clean/filter/stem · 3 drop jump.
        /// </summary>
        public int CrowdImpulse;

        /// <summary>
        /// Continuous bar phase in beats [0,4). Drives JRPG bob between discrete BeatInBar ticks.
        /// </summary>
        public float MotionPhase;

        /// <summary>Per-deck low-pass closedness 0..1 (0 open).</summary>
        public float FilterA;
        public float FilterB;

        /// <summary>Per-deck PERC stem trim 0..1.</summary>
        public float PercA;
        public float PercB;

        /// <summary>Per-deck MUSIC stem trim 0..1.</summary>
        public float MusicA;
        public float MusicB;

        /// <summary>Per-deck LINE channel fader 0..1 (default full).</summary>
        public float LineA;
        public float LineB;

        /// <summary>Active loop slice in bars (1, 2, 4, or 8). Deck A is the snapshot source.</summary>
        public int LoopLengthBars;

        public DeckReadout A;
        public DeckReadout B;

        /// <summary>
        /// Track id of the audible deck that is winning the fader. Empty when nothing is playing.
        /// </summary>
        public string DominantTrackId;

        /// <summary>Crate header, e.g. CRATE.</summary>
        public string CrateLabel;

        /// <summary>Up to <see cref="RunCrate.Cap"/> short names, pipe-separated (`Neon|Bass|…`).</summary>
        public string CrateSlots;

        public bool CueReadyA;
        public bool CueReadyB;

        /// <summary>Phrase-locked pending load, e.g. «LOAD → bar boundary».</summary>
        public string QueuedLoadLabel;

        /// <summary>Club request / promoter line when the hall asks for prep.</summary>
        public string ClubRequestLabel;

        /// <summary>
        /// Optional Act1 strip. P0 ClubDirector path leaves HasAct1 false.
        /// </summary>
        public bool HasAct1;
        public string Act1ClubId;

        /// <summary>Stable club id for the hall silhouette. Not a locale string.</summary>
        public string ClubLookId;
        public string Act1Phase;
        public string Act1Hype;
        public string Act1CueReady;
        public string Act1LastScoreKind;
        public string Act1Debug;
    }

    public static class BoothUpgrade
    {
        public static string Title(string id)
        {
            if (id == UpgradeId.AutoBass)
                return "Auto-bass";
            if (id == UpgradeId.Riser)
                return "Riser";
            if (id == UpgradeId.HardCut)
                return "Hard cut";
            return TrackTitle(id);
        }

        public static string Blurb(string id)
        {
            if (id == UpgradeId.AutoBass)
                return "quiet bass on the other deck ducks automatically";
            if (id == UpgradeId.Riser)
                return "the floor builds tension before a drop";
            if (id == UpgradeId.HardCut)
                return "sharp cuts read cleaner";
            if (TrackAura.IsSpecial(id))
                return TrackAura.CardBody(id);
            if (id == "track_voltage_alley")
                return "Acid house. The floor flashes when this deck is loudest.";
            if (id == "track_mutabor_pulse")
                return "Warehouse line. The room rolls if you keep this one up front.";
            if (id == "track_foundry_drop")
                return "Bass tech. A clean low end drops the whole floor.";
            if (id == "track_afterglow_key")
                return "Deep house. Warm hands up when it leads the mix.";
            if (id == "track_mirror_shunt")
                return "Bright electro. The lights prism when it takes the fader.";
            if (!string.IsNullOrEmpty(id) && id.StartsWith("track_"))
                return "The floor answers when this record is the loudest deck.";
            return "";
        }

        /// <summary>
        /// Act1 reward ids are tracks; fall back to a readable label from the id.
        /// </summary>
        public static string TrackTitle(string id)
        {
            if (string.IsNullOrEmpty(id) || !id.StartsWith("track_"))
                return "";
            string raw = id.Substring("track_".Length).Replace('_', ' ');
            if (raw.Length == 0)
                return id;
            char[] chars = raw.ToCharArray();
            bool cap = true;
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] == ' ')
                {
                    cap = true;
                    continue;
                }

                if (cap && chars[i] >= 'a' && chars[i] <= 'z')
                {
                    chars[i] = (char)(chars[i] - 'a' + 'A');
                    cap = false;
                }
                else
                    cap = false;
            }

            return new string(chars);
        }
    }

    /// <summary>
    /// What the hall line already counted. The view does not score.
    /// </summary>
    public static class HallCue
    {
        public const int None = 0;
        public const int Clean = 1;
        public const int Drop = 2;
        public const int Fault = 3;
    }

    /// <summary>
    /// Club on top, one status line, one reply, booth underneath.
    /// Each caption owns a rectangle and clips with an ellipsis.
    /// </summary>
    public sealed class BoothHud
    {
        // Doc/19 JRPG booth chrome
        private static readonly Color Night = Hex(0x1A1830);
        private static readonly Color Panel = Hex(0x2A2740);
        private static readonly Color DeckA = Hex(0x6EB6FF);
        private static readonly Color DeckB = Hex(0xFFB07A);
        private static readonly Color Mint = Hex(0x5ED3B1);
        private static readonly Color Warning = Hex(0xF2CF6E);
        private static readonly Color Ink = Hex(0xF7F4EA);
        private static readonly Color Muted = new Color(0.62f, 0.64f, 0.72f, 1f);
        private static readonly Color Face = Hex(0x0E0C18);
        private static readonly Color Inset = new Color(0.07f, 0.07f, 0.11f, 1f);
        private static readonly Color LampOff = new Color(0.16f, 0.18f, 0.26f, 1f);
        private static readonly Color LampDown = new Color(0.22f, 0.42f, 0.38f, 1f);
        private const int CrateColumns = 8;
        private const int CrateCap = RunCrate.Cap;

        private readonly PixelClubView _club;
        private readonly Text _venue;
        private readonly Text _setLabel;
        private readonly Text _goal;
        private readonly Text _clock;
        private readonly Text _position;
        private readonly Text _score;
        private readonly Text _message;
        private readonly GameObject _runButton;
        private readonly Text _runButtonLabel;
        private readonly GameObject _secondaryRunButton;
        private readonly Text _secondaryRunButtonLabel;
        private readonly RectTransform _secondaryRunButtonRect;
        private Action _secondaryRunAction;
        private Action _pauseAction;
        private readonly Action _runAction;
        private readonly GameObject _pauseButton;
        private readonly RectTransform _pauseButtonRect;
        private readonly Canvas _canvas;
        private readonly Text _trackA;
        private readonly Text _trackB;
        private readonly Text _detailA;
        private readonly Text _detailB;
        private readonly Text _stateA;
        private readonly Text _stateB;
        private readonly Text _loopA;
        private readonly Text _loopB;
        private readonly Slider _crossfader;
        private readonly Text _bassA;
        private readonly Text _bassB;
        private readonly Image _bassAImage;
        private readonly Image _bassBImage;
        private readonly Image[] _beats = new Image[4];
        private readonly Image[] _phrase = new Image[8];
        private readonly RectTransform _beatColumn;
        private readonly RectTransform _discA;
        private readonly RectTransform _discB;
        private readonly Image _discAMark;
        private readonly Image _discBMark;
        private readonly RectTransform _switchA;
        private readonly RectTransform _switchB;
        private readonly Image _rimA;
        private readonly Image _rimB;
        private readonly GameObject[] _rewardCards = new GameObject[3];
        private readonly Text[] _rewardTitles = new Text[3];
        private readonly Text[] _rewardBodies = new Text[3];
        private readonly Text[] _rewardKeys = new Text[3];
        private readonly string[] _rewardIds = new string[3];
        private GameObject _rewardScrim;
        private bool _rewardShown;
        private readonly GameObject _upgradeChip;
        private readonly Text _upgradeLabel;
        private readonly RectTransform _safe;
        private readonly RectTransform _stage;
        private readonly RectTransform _chromeBar;
        private readonly RectTransform _console;
        private readonly RectTransform _deckARoot;
        private readonly RectTransform _deckBRoot;
        private readonly RectTransform _runButtonRect;
        private readonly RectTransform _crossfaderRect;
        private readonly RectTransform _crateRowRect;
        private readonly RectTransform _startARect;
        private readonly RectTransform _startBRect;
        private readonly RectTransform _bassBRect;
        private readonly RectTransform _filterARect;
        private readonly RectTransform _feedbackRail;
        private readonly CanvasScaler _scaler;
        private readonly GameObject _act1Strip;
        private bool _compactBooth;
        private int _layoutKey = int.MinValue;
        private readonly Text _act1Club;
        private readonly Text _act1Phase;
        private readonly Text _act1Hype;
        private readonly Text _act1Cue;
        private readonly Text _act1Score;
        private readonly Text _act1Debug;
        private readonly GameObject _crateRow;
        private readonly Text _crateHeader;
        private readonly Image[] _crateChipImages = new Image[CrateCap];
        private readonly Text[] _crateChipLabels = new Text[CrateCap];
        private readonly GameObject _cueReadyBadgeA;
        private readonly GameObject _cueReadyBadgeB;
        private readonly Text _queuedLoad;
        private readonly Text _clubRequest;
        private readonly Slider _filterA;
        private readonly Slider _filterB;
        private readonly Slider _percA;
        private readonly Slider _percB;
        private readonly Slider _lineA;
        private readonly Slider _lineB;
        private readonly Slider _musicA;
        private readonly Slider _musicB;
        private readonly Text _loopLabelA;
        private readonly Text _loopLabelB;
        private readonly Image _loopImageA;
        private readonly Image _loopImageB;
        private Action<int> _onCratePick;
        private bool _syncingMix;

        public BoothHud(
            Transform parent,
            Action startA,
            Action stopA,
            Action startB,
            Action stopB,
            Action<float> crossfade,
            Action toggleBassA,
            Action toggleBassB,
            Action cycleA,
            Action cycleB,
            Action runAction,
            Action<string> pickReward)
            : this(parent, startA, stopA, startB, stopB, crossfade, toggleBassA, toggleBassB,
                cycleA, cycleB, runAction, pickReward, null)
        {
        }

        public BoothHud(
            Transform parent,
            Action startA,
            Action stopA,
            Action startB,
            Action stopB,
            Action<float> crossfade,
            Action toggleBassA,
            Action toggleBassB,
            Action cycleA,
            Action cycleB,
            Action runAction,
            Action<string> pickReward,
            Action<int> onCratePick)
            : this(parent, startA, stopA, startB, stopB, crossfade, toggleBassA, toggleBassB,
                cycleA, cycleB, runAction, pickReward, onCratePick,
                null, null, null, null, null, null, null, null, null, null)
        {
        }

        public BoothHud(
            Transform parent,
            Action startA,
            Action stopA,
            Action startB,
            Action stopB,
            Action<float> crossfade,
            Action toggleBassA,
            Action toggleBassB,
            Action cycleA,
            Action cycleB,
            Action runAction,
            Action<string> pickReward,
            Action<int> onCratePick,
            Action<float> filterA,
            Action<float> filterB,
            Action<float> percA,
            Action<float> percB,
            Action<float> musicA,
            Action<float> musicB,
            Action toggleLoopA,
            Action toggleLoopB,
            Action<float> lineA,
            Action<float> lineB)
        {
            _runAction = runAction;
            _onCratePick = onCratePick;
            Sprite solid = SolidSprite();
            Sprite panel = PanelSprite(Panel, new Color(0.22f, 0.25f, 0.36f, 1f));
            Sprite disc = DiscSprite();
            Font font = Font();

            var canvasObject = new GameObject("BoothCanvas");
            canvasObject.transform.SetParent(parent, false);
            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 20;
            _scaler = canvasObject.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1920f, 1080f);
            _scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            Image backdrop = ImageOf(canvasObject.transform, "Backdrop", solid, Night, false);
            Stretch(backdrop.rectTransform);

            var safeObject = new GameObject("Safe");
            safeObject.transform.SetParent(canvasObject.transform, false);
            _safe = safeObject.AddComponent<RectTransform>();
            Stretch(_safe);
            Transform ui = safeObject.transform;

            var stage = new GameObject("Stage");
            stage.transform.SetParent(ui, false);
            _stage = stage.AddComponent<RectTransform>();
            var rawObject = new GameObject("Club");
            rawObject.transform.SetParent(stage.transform, false);
            RectTransform rawRect = rawObject.AddComponent<RectTransform>();
            Stretch(rawRect);
            var raw = rawObject.AddComponent<RawImage>();
            var fitter = rawObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 480f / 168f;
            _club = new PixelClubView(raw);

            // One ChromeBar (AD lock) — no dual Status+Reply full-width strips.
            Image chrome = ImageOf(ui, "ChromeBar", panel, Color.white, false);
            chrome.type = Image.Type.Sliced;
            _chromeBar = chrome.rectTransform;
            _venue = Label(chrome.transform, "Venue", font, "Neon Basement", 22, Ink, TextAnchor.MiddleLeft, new Vector2(0.00f, 0f), new Vector2(0.16f, 1f));
            _setLabel = Label(chrome.transform, "Set", font, "set 1/2", 20, Ink, TextAnchor.MiddleLeft, new Vector2(0.16f, 0f), new Vector2(0.28f, 1f));
            _goal = Label(chrome.transform, "Goal", font, "Push Hype to the target", 22, Ink, TextAnchor.MiddleCenter, new Vector2(0.28f, 0f), new Vector2(0.56f, 1f));
            _clock = Label(chrome.transform, "Clock", font, "—", 26, Ink, TextAnchor.MiddleRight, new Vector2(0.56f, 0f), new Vector2(0.76f, 1f));
            _score = Label(chrome.transform, "Hype", font, "", 30, Ink, TextAnchor.MiddleRight, new Vector2(0.76f, 0f), new Vector2(1f, 1f));
            StampOutline(_setLabel);
            StampOutline(_clock);
            StampOutline(_score);
            _upgradeChip = ImageOf(chrome.transform, "UpgradeChip", panel, Color.white, false).gameObject;
            var chipImage = _upgradeChip.GetComponent<Image>();
            chipImage.type = Image.Type.Sliced;
            Place(chipImage.rectTransform, 0.68f, 0.10f, 0.82f, 0.90f);
            _upgradeLabel = Label(_upgradeChip.transform, "Upgrade", font, "", 14, Ink, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            _upgradeChip.SetActive(false);
            // Kept for API paint paths; cue merges into Goal. Never a second full-width strip.
            _clubRequest = Label(chrome.transform, "ClubRequest", font, "", 14, Warning, TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(0.01f, 0.01f));
            _clubRequest.gameObject.SetActive(false);

            // Horizontal crate strip under chrome / above decks.
            _crateRow = BuildCrateRow(ui, font, solid, panel, out _crateHeader);
            _crateRowRect = _crateRow.GetComponent<RectTransform>();

            Image console = ImageOf(ui, "Console", panel, Color.white, false);
            console.type = Image.Type.Sliced;
            _console = console.rectTransform;

            _beatColumn = new GameObject("BeatColumn").AddComponent<RectTransform>();
            _beatColumn.SetParent(ui, false);
            _position = BuildBeatGrid(_beatColumn, font, solid);
            // Feedback rail in center column (≤1 line) — replaces ReplyBar.
            _message = Label(ui, "FeedbackRail", font, "", 16, Ink, TextAnchor.MiddleCenter, new Vector2(0.40f, 0.125f), new Vector2(0.60f, 0.155f));
            _feedbackRail = _message.rectTransform;
            _queuedLoad = Label(ui, "QueuedLoad", font, "", 14, Warning, TextAnchor.MiddleCenter, new Vector2(0.40f, 0.110f), new Vector2(0.60f, 0.135f));
            _queuedLoad.gameObject.SetActive(false);

            BuildDeck(ui, font, solid, disc, DeckA, "A", false, new Vector2(0.03f, 0.026f), new Vector2(0.365f, 0.304f), startA, stopA, toggleBassA, cycleA, out _trackA, out _detailA, out _stateA, out _loopA, out _bassA, out _bassAImage, out _discA, out _discAMark, out _switchA, out _rimA, out _cueReadyBadgeA);
            BuildDeck(ui, font, solid, disc, DeckB, "B", true, new Vector2(0.635f, 0.026f), new Vector2(0.97f, 0.304f), startB, stopB, toggleBassB, cycleB, out _trackB, out _detailB, out _stateB, out _loopB, out _bassB, out _bassBImage, out _discB, out _discBMark, out _switchB, out _rimB, out _cueReadyBadgeB);
            _deckARoot = (RectTransform)ui.Find("DeckA");
            _deckBRoot = (RectTransform)ui.Find("DeckB");
            Transform padA = ui.Find("DeckA/Pad");
            Transform padB = ui.Find("DeckB/Pad");
            BuildDeckMix(padA, font, solid, DeckA, filterA, percA, musicA, lineA, toggleLoopA,
                out _filterA, out _percA, out _musicA, out _lineA, out _loopLabelA, out _loopImageA);
            BuildDeckMix(padB, font, solid, DeckB, filterB, percB, musicB, lineB, toggleLoopB,
                out _filterB, out _percB, out _musicB, out _lineB, out _loopLabelB, out _loopImageB);
            _startARect = padA != null ? padA.Find("StartTransport") as RectTransform : null;
            _startBRect = padB != null ? padB.Find("StartTransport") as RectTransform : null;
            _bassBRect = _bassBImage != null ? _bassBImage.rectTransform : null;
            _filterARect = _filterA != null ? _filterA.transform as RectTransform : null;
            Image scrim = ImageOf(ui, "RewardScrim", solid, new Color(0.04f, 0.03f, 0.07f, 0.78f), true);
            Stretch(scrim.rectTransform);
            _rewardScrim = scrim.gameObject;
            _rewardScrim.SetActive(false);
            BuildRewardCard(ui, font, panel, DeckA, 0, new Vector2(0.04f, 0.46f), new Vector2(0.34f, 0.82f), pickReward);
            BuildRewardCard(ui, font, panel, DeckB, 1, new Vector2(0.35f, 0.46f), new Vector2(0.65f, 0.82f), pickReward);
            BuildRewardCard(ui, font, panel, Mint, 2, new Vector2(0.66f, 0.46f), new Vector2(0.96f, 0.82f), pickReward);
            _crossfader = BuildCrossfader(ui, font, solid, crossfade);
            _crossfaderRect = _crossfader.GetComponent<RectTransform>();

            // Raised above thicker crossfader for vertical breathing room.
            Image run = ButtonOf(ui, font, solid, "Start set", DeckA, Night, 26, new Vector2(0.40f, 0.200f), new Vector2(0.60f, 0.295f), () =>
            {
                if (_runAction != null)
                    _runAction();
            });
            run.gameObject.name = "RunAction";
            _runButton = run.gameObject;
            _runButtonRect = run.rectTransform;
            _runButtonLabel = run.GetComponentInChildren<Text>();

            Image secondary = ButtonOf(ui, font, solid, "Again", Night, Ink, 22, new Vector2(0.40f, 0.105f), new Vector2(0.60f, 0.185f), () =>
            {
                if (_secondaryRunAction != null)
                    _secondaryRunAction();
            });
            secondary.gameObject.name = "SecondaryRunAction";
            _secondaryRunButton = secondary.gameObject;
            _secondaryRunButtonRect = secondary.rectTransform;
            _secondaryRunButtonLabel = secondary.GetComponentInChildren<Text>();
            _secondaryRunButton.SetActive(false);

            // Large touch target for phones; placed again in ApplyBoothLayout.
            Image pause = ButtonOf(
                ui,
                font,
                solid,
                "PAUSE",
                new Color(0.10f, 0.36f, 0.35f, 1f),
                Ink,
                22,
                new Vector2(0.015f, 0.90f),
                new Vector2(0.12f, 0.985f),
                () =>
                {
                    if (_pauseAction != null)
                        _pauseAction();
                });
            pause.gameObject.name = "PauseMenu";
            _pauseButton = pause.gameObject;
            _pauseButtonRect = pause.rectTransform;
            _pauseButton.transform.SetAsLastSibling();

            // Top-right of the club stage — clear of the crossfader and deck pads.
            BuildAct1Strip(ui, font, panel, out _act1Strip, out _act1Club, out _act1Phase, out _act1Hype, out _act1Cue, out _act1Score, out _act1Debug);
            _act1Strip.SetActive(false);

            if (_loopA != null)
                _loopA.gameObject.SetActive(false);
            if (_loopB != null)
                _loopB.gameObject.SetActive(false);

            ApplyBoothLayout(force: true);
            Canvas.ForceUpdateCanvases();
        }

        public void SetCratePick(Action<int> onCratePick)
        {
            _onCratePick = onCratePick;
        }

        /// <summary>FinalResult secondary CTA (restart run). Null hides via empty SecondaryRunAction.</summary>
        public void SetSecondaryRunAction(Action secondaryRunAction)
        {
            _secondaryRunAction = secondaryRunAction;
        }

        /// <summary>Top-right pause / menu button.</summary>
        public void SetPauseAction(Action pauseAction)
        {
            _pauseAction = pauseAction;
        }

        /// <summary>
        /// One kit for this booth. Hides filter or stem columns, captions included.
        /// LOOP stays as built. Play, crossfader, and bass stay up.
        /// A hidden slider takes no clicks and does not push the mixer.
        /// </summary>
        public void ApplyDeckKit(DeckKit kit)
        {
            ShowMix(_filterA, kit.Filter);
            ShowMix(_filterB, kit.Filter);
            ShowMix(_percA, kit.Stems);
            ShowMix(_percB, kit.Stems);
            ShowMix(_musicA, kit.Stems);
            ShowMix(_musicB, kit.Stems);
            ShowMix(_lineA, kit.Stems);
            ShowMix(_lineB, kit.Stems);
        }

        private static void ShowMix(Slider slider, bool visible)
        {
            if (slider == null)
                return;
            slider.interactable = visible;
            if (slider.gameObject.activeSelf != visible)
                slider.gameObject.SetActive(visible);
        }

        /// <summary>Root for pause/settings overlays above the booth.</summary>
        public Transform OverlayRoot => _canvas != null ? _canvas.transform : null;

        /// <summary>
        /// Live rectangle for the first-night spotlight. Inactive or empty controls report false
        /// so the lesson can skip a control that is not on screen.
        /// </summary>
        public bool TryGetCoachAnchor(CoachSpotlight spotlight, out RectTransform anchor)
        {
            switch (spotlight)
            {
                case CoachSpotlight.StartSet:
                    anchor = _runButtonRect;
                    break;
                case CoachSpotlight.Crate:
                    anchor = _crateRowRect;
                    break;
                case CoachSpotlight.DeckAStart:
                    anchor = _startARect;
                    break;
                case CoachSpotlight.Crossfader:
                    anchor = _crossfaderRect;
                    break;
                case CoachSpotlight.DeckBStart:
                    anchor = _startBRect;
                    break;
                case CoachSpotlight.Bass:
                    anchor = _bassBRect;
                    break;
                case CoachSpotlight.Filter:
                    anchor = _filterARect;
                    break;
                case CoachSpotlight.Hype:
                    anchor = _chromeBar;
                    break;
                case CoachSpotlight.Pause:
                    anchor = _pauseButtonRect;
                    break;
                default:
                    anchor = null;
                    break;
            }

            return IsCoachAnchorLive(anchor);
        }

        private static bool IsCoachAnchorLive(RectTransform rect)
        {
            return rect != null && rect.gameObject.activeInHierarchy;
        }

        /// <summary>
        /// Optional Act1 uGUI texts for <see cref="Act1RunPresenter.BindLabels"/>.
        /// Present but hidden until a snapshot with HasAct1 arrives.
        /// </summary>
        public void GetAct1Labels(
            out Text clubId,
            out Text phase,
            out Text hype,
            out Text cueReady,
            out Text lastScore,
            out Text debug)
        {
            clubId = _act1Club;
            phase = _act1Phase;
            hype = _act1Hype;
            cueReady = _act1Cue;
            lastScore = _act1Score;
            debug = _act1Debug;
        }

        public void Refresh(BoothSnapshot snapshot)
        {
            FitSafeArea();
            bool showOutcome = snapshot.ShowRunBanner && !string.IsNullOrEmpty(snapshot.RunStatus);
            bool showAction = !string.IsNullOrEmpty(snapshot.RunAction);
            bool showSecondary = !string.IsNullOrEmpty(snapshot.SecondaryRunAction);
            int cue = showOutcome ? HallCue.None : snapshot.Cue;
            int riser = showOutcome ? 0 : snapshot.RiserLevel;
            int cheerIntensity = snapshot.CheerIntensity;
            if (cheerIntensity < 0 || cheerIntensity > 3)
                cheerIntensity = MapCheerIntensity(snapshot.CrowdBand, cue, riser);
            int fromCue = MapCrowdImpulse(cue, cheerIntensity);
            int crowdImpulse = snapshot.CrowdImpulse;
            if (showOutcome)
                crowdImpulse = 0;
            else if (crowdImpulse < 0 || crowdImpulse > 3)
                crowdImpulse = fromCue;
            else if (cue == HallCue.Fault)
                crowdImpulse = 0;
            else if (fromCue > crowdImpulse)
                // Session lag / unset default 0 — successful Cue still moves the hall.
                crowdImpulse = fromCue;
            float motionPhase = snapshot.MotionPhase;
            if (motionPhase < 0f || float.IsNaN(motionPhase))
                motionPhase = snapshot.BeatInBar;
            string clubId = !string.IsNullOrEmpty(snapshot.ClubLookId)
                ? snapshot.ClubLookId
                : (snapshot.HasAct1 && !string.IsNullOrEmpty(snapshot.Act1ClubId)
                    ? snapshot.Act1ClubId
                    : snapshot.Venue);
            _club.SetActiveTrack(snapshot.DominantTrackId ?? "");
            _club.SetPerformance(
                snapshot.BeatInBar,
                snapshot.CrowdBand,
                cue,
                riser,
                snapshot.SignFlash && !showOutcome,
                clubId,
                cheerIntensity,
                crowdImpulse,
                motionPhase);

            _runButton.SetActive(showAction);
            if (_secondaryRunButton != null)
                _secondaryRunButton.SetActive(showSecondary);
            bool largeBeat = !showAction;
            LayoutBeat(largeBeat, snapshot.BeatInBar, snapshot.BarInPhrase);
            Canvas.ForceUpdateCanvases();

            Assign(_venue, string.IsNullOrEmpty(snapshot.Venue) ? "Neon Basement" : snapshot.Venue);
            Assign(_setLabel, snapshot.SetLabel ?? "");
            Assign(_clock, snapshot.ClockLabel ?? "");
            string dominantId = snapshot.DominantTrackId ?? "";
            string aura = "";
            if (TrackAura.IsSpecial(dominantId))
                aura = "  " + TrackAura.ShortTag(dominantId);
            if (snapshot.HasRun)
                Assign(_score, "HYPE " + snapshot.Hype + "/" + snapshot.TargetHype + aura);
            else
                Assign(_score, "HYPE " + snapshot.Hype + "  ×" + snapshot.Combo + aura);
            _score.color = snapshot.Hype >= 60 ? Mint : (snapshot.Hype < 30 ? Warning : Ink);

            bool showHeld = !snapshot.ShowReward && !string.IsNullOrEmpty(snapshot.HeldUpgrade);
            _upgradeChip.SetActive(showHeld);
            if (showHeld)
                Assign(_upgradeLabel, BoothUpgrade.Title(snapshot.HeldUpgrade));
            SetSpan(_score.rectTransform, showHeld ? 0.84f : 0.76f, 1f);

            // Club request / cue takes Goal slot (P0); no second full-width reply strip.
            bool hasRequest = !string.IsNullOrEmpty(snapshot.ClubRequestLabel);
            if (hasRequest)
            {
                Assign(_goal, snapshot.ClubRequestLabel);
                _goal.color = Warning;
            }
            else
            {
                Assign(_goal, snapshot.GoalLabel ?? "");
                _goal.color = Ink;
            }

            string feedback = showOutcome ? snapshot.RunStatus : (snapshot.Message ?? "");
            bool showFeedback = !string.IsNullOrEmpty(feedback);
            _message.gameObject.SetActive(showFeedback);
            if (showFeedback)
            {
                Assign(_message, feedback);
                _message.color = showOutcome ? Ink : ReplyColor(cue);
            }
            ShowReward(snapshot);
            PaintCrate(snapshot);
            PaintCueBadges(snapshot);
            bool hasQueued = !string.IsNullOrEmpty(snapshot.QueuedLoadLabel);
            _queuedLoad.gameObject.SetActive(hasQueued && !showAction);
            if (hasQueued)
                Assign(_queuedLoad, snapshot.QueuedLoadLabel);
            if (showAction && _runButtonLabel != null)
                Assign(_runButtonLabel, snapshot.RunAction);
            if (showSecondary && _secondaryRunButtonLabel != null)
                Assign(_secondaryRunButtonLabel, snapshot.SecondaryRunAction);
            int beatNumber = snapshot.BeatInBar + 1;
            Assign(_position, largeBeat
                ? (_compactBooth
                    ? beatNumber + "/4 · " + snapshot.PhraseNumber
                    : "beat " + beatNumber + "/4 · phrase " + snapshot.PhraseNumber)
                : beatNumber + "/4");

            PaintGrid(snapshot.BeatInBar, snapshot.BarInPhrase);
            ApplyDeck(_trackA, _detailA, _stateA, _loopA, _bassA, _bassAImage, _discA, _discAMark, _switchA, _rimA, DeckA, snapshot.A);
            ApplyDeck(_trackB, _detailB, _stateB, _loopB, _bassB, _bassBImage, _discB, _discBMark, _switchB, _rimB, DeckB, snapshot.B);
            PaintMix(snapshot);
            PaintAct1(snapshot);
        }

        private void PaintMix(BoothSnapshot snapshot)
        {
            _syncingMix = true;
            SetSlider(_filterA, snapshot.FilterA);
            SetSlider(_filterB, snapshot.FilterB);
            SetSlider(_percA, snapshot.PercA);
            SetSlider(_percB, snapshot.PercB);
            SetSlider(_musicA, snapshot.MusicA);
            SetSlider(_musicB, snapshot.MusicB);
            SetSlider(_lineA, snapshot.LineA);
            SetSlider(_lineB, snapshot.LineB);
            _syncingMix = false;

            PaintLoopButton(_loopLabelA, _loopImageA, snapshot.A.Loop);
            PaintLoopButton(_loopLabelB, _loopImageB, snapshot.B.Loop);
        }

        private static void SetSlider(Slider slider, float value)
        {
            if (slider == null)
                return;
            float v = Mathf.Clamp01(value);
            if (Mathf.Abs(slider.value - v) > 0.0005f)
                slider.value = v;
        }

        private static void PaintLoopButton(Text label, Image face, string loopReadout)
        {
            if (label == null || face == null)
                return;
            int bars = ParseLoopBars(loopReadout);
            Assign(label, "LOOP " + bars);
            bool shortLoop = bars <= 4;
            face.color = shortLoop ? Warning : Face;
            label.color = shortLoop ? Night : Ink;
        }

        private static int ParseLoopBars(string loopReadout)
        {
            if (string.IsNullOrEmpty(loopReadout))
                return 8;
            if (loopReadout.IndexOf("loop 1", StringComparison.Ordinal) >= 0)
                return 1;
            if (loopReadout.IndexOf("loop 2", StringComparison.Ordinal) >= 0)
                return 2;
            if (loopReadout.IndexOf("loop 4", StringComparison.Ordinal) >= 0)
                return 4;
            return 8;
        }

        private void PaintAct1(BoothSnapshot snapshot)
        {
            bool on = snapshot.HasAct1;
            if (_act1Strip.activeSelf != on)
                _act1Strip.SetActive(on);
            if (!on)
                return;

            Assign(_act1Club, snapshot.Act1ClubId ?? "");
            Assign(_act1Phase, snapshot.Act1Phase ?? "");
            Assign(_act1Hype, snapshot.Act1Hype ?? "");
            Assign(_act1Cue, snapshot.Act1CueReady ?? "");
            Assign(_act1Score, snapshot.Act1LastScoreKind ?? "");
            // Always show a useful line when Act1 is on (crate count / last score fallback).
            string useful = snapshot.Act1Debug;
            if (string.IsNullOrEmpty(useful))
                useful = string.IsNullOrEmpty(snapshot.Act1LastScoreKind)
                    ? "cue the next record"
                    : snapshot.Act1LastScoreKind;
            Assign(_act1Debug, useful);
        }

        private void PaintCueBadges(BoothSnapshot snapshot)
        {
            if (_cueReadyBadgeA.activeSelf != snapshot.CueReadyA)
                _cueReadyBadgeA.SetActive(snapshot.CueReadyA);
            if (_cueReadyBadgeB.activeSelf != snapshot.CueReadyB)
                _cueReadyBadgeB.SetActive(snapshot.CueReadyB);
        }

        private void PaintCrate(BoothSnapshot snapshot)
        {
            string slots = snapshot.CrateSlots ?? "";
            string[] parts = string.IsNullOrEmpty(slots) ? Array.Empty<string>() : slots.Split('|');
            int count = Math.Min(CrateCap, parts.Length);
            bool show = count > 0;
            if (_crateRow.activeSelf != show)
                _crateRow.SetActive(show);
            if (!show)
                return;

            Assign(_crateHeader, string.IsNullOrEmpty(snapshot.CrateLabel) ? "CRATE" : snapshot.CrateLabel);
            string shortA = ShortTrack(snapshot.A.TrackName);
            string shortB = ShortTrack(snapshot.B.TrackName);
            for (int i = 0; i < CrateCap; i++)
            {
                bool on = i < count && !string.IsNullOrEmpty(parts[i]);
                _crateChipImages[i].gameObject.SetActive(on);
                if (!on)
                    continue;

                string name = parts[i].Trim();
                Assign(_crateChipLabels[i], name);
                bool onA = !string.IsNullOrEmpty(shortA) && NamesMatch(name, shortA, snapshot.A.TrackName);
                bool onB = !string.IsNullOrEmpty(shortB) && NamesMatch(name, shortB, snapshot.B.TrackName);
                if (onA && onB)
                    _crateChipImages[i].color = Mint;
                else if (onA)
                    _crateChipImages[i].color = DeckA;
                else if (onB)
                    _crateChipImages[i].color = DeckB;
                else
                    _crateChipImages[i].color = Inset;
                _crateChipLabels[i].color = onA || onB ? Face : Ink;
            }
        }

        private void ShowReward(BoothSnapshot snapshot)
        {
            bool show = snapshot.ShowReward
                && !string.IsNullOrEmpty(snapshot.OfferA)
                && !string.IsNullOrEmpty(snapshot.OfferB);
            _rewardIds[0] = snapshot.OfferA ?? "";
            _rewardIds[1] = snapshot.OfferB ?? "";
            _rewardIds[2] = snapshot.OfferC ?? "";
            bool three = show && !string.IsNullOrEmpty(snapshot.OfferC);

            string[] titles =
            {
                snapshot.OfferATitle,
                snapshot.OfferBTitle,
                snapshot.OfferCTitle
            };
            for (int i = 0; i < 3; i++)
            {
                bool cardOn = show && (i < 2 || three);
                _rewardCards[i].SetActive(cardOn);
                if (!cardOn)
                    continue;
                string id = _rewardIds[i];
                string title = titles[i];
                if (string.IsNullOrEmpty(title))
                    title = BoothUpgrade.Title(id);
                if (string.IsNullOrEmpty(title))
                    title = id;
                AssignWrapped(_rewardTitles[i], title);
                string body = BoothUpgrade.Blurb(id);
                AssignWrapped(_rewardBodies[i], string.IsNullOrEmpty(body) ? "into the record crate" : body);
                if (_rewardKeys[i] != null)
                    _rewardKeys[i].gameObject.SetActive(!_compactBooth);
            }

            if (show)
                LayoutRewardCards(three);

            if (show != _rewardShown)
            {
                _rewardShown = show;
                if (_rewardScrim != null)
                    _rewardScrim.SetActive(show);
                SetRewardBlocking(show);
                if (show)
                    RaiseRewardOverlay();
            }
        }

        private void LayoutRewardCards(bool three)
        {
            if (_compactBooth)
            {
                // Full-width stack. Two cards ≥28% of height, three ≥22%.
                const float x0 = 0.04f;
                const float x1 = 0.96f;
                int count = three ? 3 : 2;
                float height = three ? 0.24f : 0.32f;
                const float gap = 0.02f;
                const float top = 0.90f;
                for (int i = 0; i < count; i++)
                {
                    float y1 = top - i * (height + gap);
                    float y0 = y1 - height;
                    Place(_rewardCards[i].GetComponent<RectTransform>(), x0, y0, x1, y1);
                }
            }
            else if (three)
            {
                Place(_rewardCards[0].GetComponent<RectTransform>(), 0.04f, 0.28f, 0.34f, 0.78f);
                Place(_rewardCards[1].GetComponent<RectTransform>(), 0.35f, 0.28f, 0.65f, 0.78f);
                Place(_rewardCards[2].GetComponent<RectTransform>(), 0.66f, 0.28f, 0.96f, 0.78f);
            }
            else
            {
                Place(_rewardCards[0].GetComponent<RectTransform>(), 0.06f, 0.28f, 0.47f, 0.78f);
                Place(_rewardCards[1].GetComponent<RectTransform>(), 0.53f, 0.28f, 0.94f, 0.78f);
            }

            int titleSize = _compactBooth ? 20 : 28;
            int bodySize = _compactBooth ? 16 : 18;
            for (int i = 0; i < 3; i++)
            {
                if (_rewardTitles[i] != null)
                {
                    _rewardTitles[i].fontSize = titleSize;
                    _rewardTitles[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                    _rewardTitles[i].verticalOverflow = VerticalWrapMode.Truncate;
                    AnchorText(_rewardTitles[i].rectTransform, 0f, _compactBooth ? 0.58f : 0.62f, _compactBooth ? 1f : 0.76f, 0.94f);
                }

                if (_rewardBodies[i] != null)
                {
                    _rewardBodies[i].fontSize = bodySize;
                    _rewardBodies[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                    _rewardBodies[i].verticalOverflow = VerticalWrapMode.Truncate;
                    AnchorText(_rewardBodies[i].rectTransform, 0f, 0.06f, 1f, _compactBooth ? 0.56f : 0.58f);
                }

                if (_rewardKeys[i] != null)
                    _rewardKeys[i].fontSize = 22;

                Transform pad = _rewardCards[i].transform.Find("Pad");
                var padRect = pad as RectTransform;
                if (padRect != null)
                {
                    float inset = _compactBooth ? 10f : 16f;
                    padRect.offsetMin = new Vector2(inset, inset);
                    padRect.offsetMax = new Vector2(-inset, -inset);
                }
            }
        }

        /// <summary>
        /// Reward cards sit above the booth. Crate, decks, and faders stop taking the hit.
        /// </summary>
        private void SetRewardBlocking(bool blocked)
        {
            bool live = !blocked;
            SetRaycastGroup(_deckARoot, live);
            SetRaycastGroup(_deckBRoot, live);
            SetRaycastGroup(_crateRowRect, live);
            SetRaycastGroup(_console, live);
            SetRaycastGroup(_crossfaderRect, live);
            SetRaycastGroup(_beatColumn, live);
            SetRaycastGroup(_runButtonRect, live);
            SetRaycastGroup(_secondaryRunButtonRect, live);
        }

        private static void SetRaycastGroup(RectTransform rect, bool blocks)
        {
            if (rect == null)
                return;
            CanvasGroup group = rect.GetComponent<CanvasGroup>();
            if (group == null)
                group = rect.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = blocks;
            group.interactable = blocks;
        }

        private void RaiseRewardOverlay()
        {
            if (_rewardScrim != null)
                _rewardScrim.transform.SetAsLastSibling();
            for (int i = 0; i < 3; i++)
            {
                if (_rewardCards[i] != null && _rewardCards[i].activeSelf)
                    _rewardCards[i].transform.SetAsLastSibling();
            }

            if (_pauseButton != null)
                _pauseButton.transform.SetAsLastSibling();
        }

        private static bool NamesMatch(string chip, string shortName, string fullName)
        {
            if (string.Equals(chip, shortName, StringComparison.OrdinalIgnoreCase))
                return true;
            if (!string.IsNullOrEmpty(fullName)
                && string.Equals(chip, fullName, StringComparison.OrdinalIgnoreCase))
                return true;
            return !string.IsNullOrEmpty(fullName)
                && fullName.IndexOf(chip, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string ShortTrack(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
                return "";
            int space = displayName.IndexOf(' ');
            if (space > 0 && space <= 10)
                return displayName.Substring(0, space);
            return displayName.Length <= 10 ? displayName : displayName.Substring(0, 10);
        }

        private static Color ReplyColor(int cue)
        {
            if (cue == HallCue.Clean || cue == HallCue.Drop)
                return Mint;
            if (cue == HallCue.Fault)
                return Warning;
            return Ink;
        }

        /// <summary>
        /// CrowdBand 0..2 → cheer 0..2; Clean lifts to ≥2; Drop peaks at 3; Fault zeros.
        /// </summary>
        private static int MapCheerIntensity(int crowdBand, int cue, int riserLevel)
        {
            if (cue == HallCue.Fault)
                return 0;

            int intensity = crowdBand < 0 ? 0 : (crowdBand > 2 ? 2 : crowdBand);
            if (riserLevel >= 2)
                intensity = intensity < 2 ? 2 : intensity;
            else if (riserLevel >= 1 && intensity < 1)
                intensity = 1;

            if (cue == HallCue.Drop)
                return 3;
            if (cue == HallCue.Clean && intensity < 2)
                intensity = 2;
            return intensity;
        }

        /// <summary>
        /// Cue → hall motion when session left CrowdImpulse unset.
        /// Fault 0 · Clean 2 · Drop 3 · else CheerIntensity.
        /// </summary>
        public static int MapCrowdImpulse(int cue, int cheerIntensity)
        {
            if (cue == HallCue.Fault)
                return 0;
            if (cue == HallCue.Drop)
                return 3;
            if (cue == HallCue.Clean)
                return 2;
            if (cheerIntensity < 0)
                return 0;
            return cheerIntensity > 3 ? 3 : cheerIntensity;
        }

        private Text BuildBeatGrid(RectTransform column, Font font, Sprite solid)
        {
            for (int i = 0; i < 4; i++)
            {
                Image lamp = ImageOf(column, "Beat" + i, solid, LampOff, false);
                _beats[i] = lamp;
            }

            for (int i = 0; i < 8; i++)
            {
                Image tick = ImageOf(column, "Phrase" + i, solid, LampOff, false);
                _phrase[i] = tick;
            }

            Text caption = Label(column, "PhraseCaption", font, "", 30, Ink, TextAnchor.MiddleCenter, new Vector2(0.02f, 0f), new Vector2(0.98f, 0.34f));
            StampOutline(caption);
            return caption;
        }

        private void LayoutBeat(bool large, int beatInBar, int barInPhrase)
        {
            int beat = Mathf.Clamp(beatInBar, 0, 3);
            int bar = Mathf.Clamp(barInPhrase, 0, 7);
            if (_compactBooth)
            {
                if (large)
                    Place(_beatColumn, 0.37f, 0.200f, 0.63f, 0.560f);
                else
                    Place(_beatColumn, 0.37f, 0.200f, 0.63f, 0.320f);
            }
            else if (large)
                Place(_beatColumn, 0.39f, 0.170f, 0.61f, 0.455f);
            else
                Place(_beatColumn, 0.39f, 0.170f, 0.61f, 0.280f);

            if (large)
            {
                for (int i = 0; i < 4; i++)
                {
                    bool on = i == beat;
                    bool down = i == 0;
                    float x0 = 0.06f + i * 0.23f;
                    float width = down ? 0.18f : 0.16f;
                    float y0 = on ? 0.54f : 0.60f;
                    float y1 = down ? 0.98f : (on ? 0.92f : 0.84f);
                    Place(_beats[i].rectTransform, x0, y0, x0 + width, y1);
                }

                for (int i = 0; i < 8; i++)
                {
                    bool on = i == bar;
                    float x0 = 0.06f + i * 0.115f;
                    float y0 = on ? 0.36f : 0.40f;
                    float y1 = on ? 0.50f : 0.46f;
                    Place(_phrase[i].rectTransform, x0, y0, x0 + (on ? 0.09f : 0.07f), y1);
                }

                Place(_position.rectTransform, 0.02f, 0.00f, 0.98f, 0.34f);
                _position.fontSize = _compactBooth ? 44 : 30;
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    bool on = i == beat;
                    bool down = i == 0;
                    float x0 = 0.02f + i * 0.11f;
                    float width = down ? 0.10f : 0.08f;
                    float y0 = on ? 0.12f : 0.30f;
                    float y1 = on ? 0.92f : 0.70f;
                    Place(_beats[i].rectTransform, x0, y0, x0 + width, y1);
                }

                for (int i = 0; i < 8; i++)
                {
                    bool on = i == bar;
                    float x0 = 0.48f + i * 0.04f;
                    float y0 = on ? 0.18f : 0.32f;
                    float y1 = on ? 0.82f : 0.68f;
                    Place(_phrase[i].rectTransform, x0, y0, x0 + 0.032f, y1);
                }

                Place(_position.rectTransform, 0.82f, 0.00f, 1f, 1f);
                _position.fontSize = _compactBooth ? 36 : 24;
            }
        }

        private void PaintGrid(int beatInBar, int barInPhrase)
        {
            int beat = Mathf.Clamp(beatInBar, 0, 3);
            int bar = Mathf.Clamp(barInPhrase, 0, 7);
            for (int i = 0; i < 4; i++)
            {
                if (i == beat)
                    _beats[i].color = i == 0 ? Mint : Ink;
                else
                    _beats[i].color = i == 0 ? LampDown : LampOff;
            }

            for (int i = 0; i < 8; i++)
                _phrase[i].color = i == bar ? Warning : LampOff;
        }

        public void SetCrossfader(float position)
        {
            _crossfader.value = Mathf.Clamp01(position);
        }

        public void NudgeCrossfader(float delta)
        {
            SetCrossfader(_crossfader.value + delta);
        }

        public void NudgeFilterA(float delta)
        {
            if (!MixLive(_filterA))
                return;
            _filterA.value = Mathf.Clamp01(_filterA.value + delta);
        }

        public void NudgeFilterB(float delta)
        {
            if (!MixLive(_filterB))
                return;
            _filterB.value = Mathf.Clamp01(_filterB.value + delta);
        }

        private static bool MixLive(Slider slider)
        {
            return slider != null && slider.isActiveAndEnabled && slider.interactable;
        }

        private void FitSafeArea()
        {
            Rect safe = Screen.safeArea;
            float width = Screen.width;
            float height = Screen.height;
            if (width < 1f || height < 1f)
                return;

            var min = new Vector2(safe.xMin / width, safe.yMin / height);
            var max = new Vector2(safe.xMax / width, safe.yMax / height);
            if (_safe.anchorMin != min || _safe.anchorMax != max)
            {
                _safe.anchorMin = min;
                _safe.anchorMax = max;
                _safe.offsetMin = Vector2.zero;
                _safe.offsetMax = Vector2.zero;
            }

            ApplyBoothLayout(force: false);
        }

        /// <summary>
        /// Density token switch for one adaptive skeleton (PERF_BOOTH_LAYOUT_REDESIGN).
        /// Compact = phone landscape / ultra-wide short height — not a second prefab.
        /// </summary>
        private static bool IsCompactBooth()
        {
            float h = Screen.height;
            if (h < 1f)
                return false;
            float aspect = Screen.width / h;
            return h <= 540f || (aspect >= 1.9f && h <= 1080f);
        }

        private void ApplyBoothLayout(bool force)
        {
            bool compact = IsCompactBooth();
            int key = (Screen.width & 0x7FFF) | ((Screen.height & 0x7FFF) << 15) | (compact ? 1 << 30 : 0);
            if (!force && key == _layoutKey)
                return;
            _layoutKey = key;
            _compactBooth = compact;

            if (_scaler != null)
                _scaler.matchWidthOrHeight = compact ? 0.85f : 0.5f;

            if (compact)
            {
                if (CrateCap > CrateColumns)
                {
                    // Two chip rows grow up into the old chrome band. Crossfader stays at the bottom.
                    Place(_stage, 0.02f, 0.86f, 0.98f, 1f);
                    Place(_chromeBar, 0.02f, 0.784f, 0.98f, 0.856f);
                    Place(_crateRowRect, 0.02f, 0.600f, 0.98f, 0.776f);
                    Place(_console, 0.015f, 0.012f, 0.985f, 0.590f);
                    Place(_deckARoot, 0.02f, 0.018f, 0.355f, 0.590f);
                    Place(_deckBRoot, 0.645f, 0.018f, 0.98f, 0.590f);
                }
                else
                {
                    // Stage shrinks a little so the progress strip can hold a larger clock and Hype.
                    Place(_stage, 0.02f, 0.80f, 0.98f, 1f);
                    Place(_chromeBar, 0.02f, 0.724f, 0.98f, 0.796f);
                    Place(_crateRowRect, 0.02f, 0.668f, 0.98f, 0.716f);
                    Place(_console, 0.015f, 0.012f, 0.985f, 0.660f);
                    Place(_deckARoot, 0.02f, 0.018f, 0.355f, 0.660f);
                    Place(_deckBRoot, 0.645f, 0.018f, 0.98f, 0.660f);
                }

                // Short XF travel (~20% W panel); thumb stays fat for touch.
                Place(_crossfaderRect, 0.40f, 0.028f, 0.60f, 0.130f);
                Place(_runButtonRect, 0.37f, 0.470f, 0.63f, 0.585f);
                if (_secondaryRunButtonRect != null)
                    Place(_secondaryRunButtonRect, 0.37f, 0.360f, 0.63f, 0.450f);
                Place(_feedbackRail, 0.37f, 0.160f, 0.63f, 0.195f);
                Place(_queuedLoad.rectTransform, 0.37f, 0.155f, 0.63f, 0.175f);
                // ≥48px-ish pause hit on phones — top-left over the stage.
                if (_pauseButtonRect != null)
                    Place(_pauseButtonRect, 0.02f, 0.905f, 0.22f, 0.985f);
                SetChromeFonts(28, 32, 18, 36, 24);
            }
            else
            {
                if (CrateCap > CrateColumns)
                {
                    // Two chip rows. Strip stays above the crossfader.
                    Place(_stage, 0.03f, 0.66f, 0.97f, 1f);
                    Place(_chromeBar, 0.03f, 0.584f, 0.97f, 0.656f);
                    Place(_crateRowRect, 0.03f, 0.476f, 0.97f, 0.576f);
                    Place(_console, 0.02f, 0.014f, 0.98f, 0.468f);
                    Place(_deckARoot, 0.03f, 0.024f, 0.355f, 0.468f);
                    Place(_deckBRoot, 0.645f, 0.024f, 0.97f, 0.468f);
                }
                else
                {
                    // Stage ~38%; chrome tall enough for the clock and Hype; console still ≥48%.
                    Place(_stage, 0.03f, 0.62f, 0.97f, 1f);
                    Place(_chromeBar, 0.03f, 0.544f, 0.97f, 0.616f);
                    Place(_crateRowRect, 0.03f, 0.488f, 0.97f, 0.536f);
                    Place(_console, 0.02f, 0.014f, 0.98f, 0.480f);
                    Place(_deckARoot, 0.03f, 0.024f, 0.355f, 0.480f);
                    Place(_deckBRoot, 0.645f, 0.024f, 0.97f, 0.480f);
                }

                // Short XF travel (~16% W panel).
                Place(_crossfaderRect, 0.42f, 0.028f, 0.58f, 0.110f);
                Place(_runButtonRect, 0.39f, 0.340f, 0.61f, 0.460f);
                if (_secondaryRunButtonRect != null)
                    Place(_secondaryRunButtonRect, 0.39f, 0.230f, 0.61f, 0.320f);
                Place(_feedbackRail, 0.39f, 0.130f, 0.61f, 0.160f);
                Place(_queuedLoad.rectTransform, 0.39f, 0.125f, 0.61f, 0.145f);
                if (_pauseButtonRect != null)
                    Place(_pauseButtonRect, 0.015f, 0.92f, 0.12f, 0.985f);
                SetChromeFonts(22, 26, 18, 30, 26);
            }

            if (_pauseButton != null)
                _pauseButton.transform.SetAsLastSibling();
            if (_rewardShown)
                RaiseRewardOverlay();
        }

        private void SetChromeFonts(int status, int clock, int message, int score, int run)
        {
            if (_venue != null) _venue.fontSize = status;
            if (_setLabel != null) _setLabel.fontSize = Mathf.Max(20, status - 2);
            if (_goal != null) _goal.fontSize = status;
            if (_clock != null) _clock.fontSize = clock;
            if (_message != null) _message.fontSize = message;
            if (_score != null) _score.fontSize = score;
            if (_runButtonLabel != null) _runButtonLabel.fontSize = run;
            if (_secondaryRunButtonLabel != null) _secondaryRunButtonLabel.fontSize = Mathf.Max(16, run - 4);
            if (_crateHeader != null) _crateHeader.fontSize = status <= 14 ? 12 : 14;
            for (int i = 0; i < CrateCap; i++)
            {
                if (_crateChipLabels[i] != null)
                    _crateChipLabels[i].fontSize = status <= 14 ? 12 : 14;
            }
        }

        private static void ApplyDeck(Text track, Text detail, Text state, Text loop, Text bass, Image bassImage, RectTransform disc, Image mark, RectTransform switcher, Image rim, Color accent, DeckReadout readout)
        {
            string name = string.IsNullOrEmpty(readout.TrackName) ? "—" : readout.TrackName;
            FitSwitcher(switcher, track, name);
            track.color = readout.Selected ? accent : Ink;
            Assign(detail, string.IsNullOrEmpty(readout.Detail) ? "LOW · PERC · MUSIC" : readout.Detail);
            Assign(state, readout.Playing && readout.Selected ? "ON AIR" : readout.State);
            state.color = readout.Playing ? accent : (readout.Queued ? Warning : Muted);
            Assign(loop, readout.Loop);
            Assign(bass, readout.BassKilled ? "OFF" : "BASS");
            bass.color = readout.BassKilled ? Night : Ink;
            bassImage.color = readout.BassKilled ? Warning : Face;
            FitDisc(disc);
            PlaceRim(rim, disc, readout.DimBassRim);
            disc.localRotation = Quaternion.Euler(0f, 0f, readout.Playing || readout.Queued ? -readout.SpinDegrees : 0f);
            mark.color = readout.Playing ? accent : Muted;
        }

        private static void FitSwitcher(RectTransform button, Text label, string value)
        {
            float textWidth = Measure(label.font, label.fontSize, label.fontStyle, value);
            float width = Mathf.Clamp(textWidth + 28f, 112f, 240f);
            // Keep height under the DECK title row (title uses the top ~7% of the pad).
            button.sizeDelta = new Vector2(width, 34f);
            AssignBudget(label, value, width - 24f);
        }

        private static void PlaceRim(Image rim, RectTransform disc, bool dim)
        {
            float side = disc.sizeDelta.x;
            if (side < 8f)
                side = 96f;
            rim.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rim.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rim.rectTransform.pivot = new Vector2(0.5f, 0f);
            rim.rectTransform.sizeDelta = new Vector2(Mathf.Max(28f, side * 0.62f), 8f);
            rim.rectTransform.anchoredPosition = new Vector2(0f, -side * 0.5f + 5f);
            rim.color = Night;
            rim.enabled = dim;
        }

        private static void BuildDeck(
            Transform parent,
            Font font,
            Sprite solid,
            Sprite discSprite,
            Color accent,
            string letter,
            bool discOnLeft,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Action start,
            Action stop,
            Action bass,
            Action cycle,
            out Text track,
            out Text detail,
            out Text state,
            out Text loop,
            out Text bassLabel,
            out Image bassImage,
            out RectTransform disc,
            out Image discMark,
            out RectTransform switcher,
            out Image rim,
            out GameObject cueReady)
        {
            Image card = ImageOf(parent, "Deck" + letter, PanelSprite(Face, accent), Color.white, false);
            card.type = Image.Type.Sliced;
            card.rectTransform.anchorMin = anchorMin;
            card.rectTransform.anchorMax = anchorMax;
            card.rectTransform.offsetMin = Vector2.zero;
            card.rectTransform.offsetMax = Vector2.zero;
            card.raycastTarget = false;

            var padObject = new GameObject("Pad");
            padObject.transform.SetParent(card.transform, false);
            var pad = padObject.AddComponent<RectTransform>();
            Stretch(pad);
            pad.offsetMin = new Vector2(16f, 16f);
            pad.offsetMax = new Vector2(-16f, -16f);

            float textLeft = discOnLeft ? 0.38f : 0f;
            float textRight = discOnLeft ? 1f : 0.62f;

            // Title row first — track chip sits below it (was overlapping DECK A/B).
            Text deckTitle = Label(
                pad,
                "DeckTitle",
                font,
                "DECK " + letter,
                18,
                accent,
                TextAnchor.MiddleLeft,
                new Vector2(textLeft, 0.93f),
                new Vector2(textLeft + 0.50f, 1f));

            var badgeRoot = new GameObject("CueBadge");
            badgeRoot.transform.SetParent(pad, false);
            var badgeRect = badgeRoot.AddComponent<RectTransform>();
            Place(badgeRect, textRight - 0.36f, 0.93f, textRight, 1f);
            Image cueBg = ImageOf(badgeRoot.transform, "CueReadyBg", solid, accent, false);
            Stretch(cueBg.rectTransform);
            Label(badgeRoot.transform, "CueReadyLabel", font, "READY", 12, Face, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            badgeRoot.SetActive(false);
            cueReady = badgeRoot;

            Image trackHit = ImageOf(pad, "TrackSwitch", solid, Inset, true);
            switcher = trackHit.rectTransform;
            switcher.anchorMin = switcher.anchorMax = new Vector2(textLeft, 1f);
            switcher.pivot = new Vector2(0f, 1f);
            switcher.sizeDelta = new Vector2(160f, 34f);
            // Clear the title band (~0.93–1.0) so "DECK A" is never under the chip.
            switcher.anchoredPosition = new Vector2(0f, -34f);
            var cycleButton = trackHit.gameObject.AddComponent<Button>();
            cycleButton.targetGraphic = trackHit;
            cycleButton.transition = Selectable.Transition.None;
            cycleButton.navigation = new Navigation { mode = Navigation.Mode.None };
            cycleButton.onClick.AddListener(() => cycle());
            track = Label(trackHit.transform, "Track", font, "—", 16, Ink, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            deckTitle.transform.SetAsLastSibling();

            // Compact meta above mix well (FILTER / vertical stems / LOOP).
            detail = Label(pad, "Detail", font, "LOW · PERC · MUSIC", 13, Muted, TextAnchor.MiddleLeft, new Vector2(textLeft, 0.70f), new Vector2(textRight, 0.78f));
            state = Label(pad, "State", font, "EMPTY", 16, Muted, TextAnchor.MiddleLeft, new Vector2(textLeft, 0.62f), new Vector2(textRight, 0.70f));
            loop = Label(pad, "Loop", font, "loop —", 12, Muted, TextAnchor.MiddleLeft, new Vector2(textLeft, 0.58f), new Vector2(textRight, 0.62f));

            var slotObject = new GameObject("DiscSlot");
            slotObject.transform.SetParent(pad, false);
            var slot = slotObject.AddComponent<RectTransform>();
            if (discOnLeft)
                Place(slot, 0f, 0.70f, 0.34f, 1f);
            else
                Place(slot, 0.66f, 0.70f, 1f, 1f);

            var discObject = new GameObject("Disc");
            discObject.transform.SetParent(slot, false);
            disc = discObject.AddComponent<RectTransform>();
            disc.anchorMin = disc.anchorMax = new Vector2(0.5f, 0.5f);
            disc.pivot = new Vector2(0.5f, 0.5f);
            disc.sizeDelta = new Vector2(96f, 96f);
            disc.anchoredPosition = Vector2.zero;
            var discImage = discObject.AddComponent<Image>();
            discImage.sprite = discSprite;
            discImage.color = accent;
            discImage.preserveAspect = true;
            discImage.raycastTarget = true;
            var discButton = discObject.AddComponent<Button>();
            discButton.targetGraphic = discImage;
            discButton.transition = Selectable.Transition.None;
            discButton.navigation = new Navigation { mode = Navigation.Mode.None };
            discButton.onClick.AddListener(() => cycle());

            var markObject = new GameObject("Mark");
            markObject.transform.SetParent(disc, false);
            var markRect = markObject.AddComponent<RectTransform>();
            Place(markRect, 0.42f, 0.58f, 0.58f, 0.96f);
            discMark = markObject.AddComponent<Image>();
            discMark.sprite = solid;
            discMark.color = Ink;
            discMark.raycastTarget = false;

            rim = ImageOf(slot, "BassRim", solid, Night, false);
            rim.enabled = false;

            // Transport row ≥48px @1080 (fraction of taller console pad).
            Image startImage = ButtonOf(pad, font, solid, "START", accent, Night, 17, new Vector2(0f, 0f), new Vector2(0.31f, 0.14f), start);
            startImage.gameObject.name = "StartTransport";
            Image stopImage = ButtonOf(pad, font, solid, "STOP", Face, Ink, 17, new Vector2(0.345f, 0f), new Vector2(0.655f, 0.14f), stop);
            stopImage.gameObject.name = "StopTransport";
            bassImage = ButtonOf(pad, font, solid, "BASS", Face, Ink, 17, new Vector2(0.69f, 0f), new Vector2(1f, 0.14f), bass);
            bassImage.gameObject.name = "BassTransport";
            bassLabel = bassImage.GetComponentInChildren<Text>();
        }

        /// <summary>
        /// Mix well: short travel (not long drag rails). Fat thumbs for touch; thin tracks.
        /// FILTER ~38% pad; PERC/MUSIC/LINE vertical ~72px travel; LOOP compact.
        /// </summary>
        private void BuildDeckMix(
            Transform pad,
            Font font,
            Sprite solid,
            Color accent,
            Action<float> onFilter,
            Action<float> onPerc,
            Action<float> onMusic,
            Action<float> onLine,
            Action onLoop,
            out Slider filter,
            out Slider perc,
            out Slider music,
            out Slider line,
            out Text loopLabel,
            out Image loopImage)
        {
            if (pad == null)
            {
                filter = null;
                perc = null;
                music = null;
                line = null;
                loopLabel = null;
                loopImage = null;
                return;
            }

            filter = BuildFilterSlider(pad, "Filter", font, solid, accent, onFilter);
            // Narrow stem columns — short vertical travel, not wide slabs.
            perc = BuildStemSlider(pad, "Perc", font, solid, accent, "PERC",
                new Vector2(0.04f, 0.18f), new Vector2(0.18f, 0.50f), 1f, onPerc);
            music = BuildStemSlider(pad, "Music", font, solid, Ink, "MUSIC",
                new Vector2(0.20f, 0.18f), new Vector2(0.34f, 0.50f), 1f, onMusic);
            line = BuildStemSlider(pad, "Line", font, solid, accent, "LINE",
                new Vector2(0.36f, 0.18f), new Vector2(0.50f, 0.50f), 1f, onLine);

            loopImage = ButtonOf(pad, font, solid, "LOOP 8", Face, Ink, 15,
                new Vector2(0.54f, 0.18f), new Vector2(0.96f, 0.50f), () =>
                {
                    if (onLoop != null)
                        onLoop();
                });
            loopLabel = loopImage.GetComponentInChildren<Text>();
        }

        private Slider BuildFilterSlider(
            Transform parent,
            string name,
            Font font,
            Sprite solid,
            Color accent,
            Action<float> changed)
        {
            // Compact cap on horizontal rail (not a 48×48 slab). Full-row Hit keeps touch.
            const float thumbW = 12f;
            const float thumbH = 22f;
            const float trackHeight = 10f;
            const float trackLeft = 0.30f;
            const float trackRight = 0.68f;

            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var rect = root.AddComponent<RectTransform>();
            Place(rect, 0.08f, 0.54f, 0.92f, 0.68f);

            Image hit = ImageOf(root.transform, "Hit", solid, Inset, true);
            Stretch(hit.rectTransform);
            Label(root.transform, "Caption", font, "FILTER", 12, Ink, TextAnchor.MiddleLeft,
                new Vector2(0.02f, 0f), new Vector2(0.26f, 1f));

            Image track = ImageOf(root.transform, "Track", solid, new Color(0.18f, 0.20f, 0.28f, 1f), false);
            track.rectTransform.anchorMin = new Vector2(trackLeft, 0.5f);
            track.rectTransform.anchorMax = new Vector2(trackRight, 0.5f);
            track.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            track.rectTransform.sizeDelta = new Vector2(0f, trackHeight);
            track.rectTransform.anchoredPosition = Vector2.zero;

            var area = new GameObject("HandleArea");
            area.transform.SetParent(root.transform, false);
            var areaRect = area.AddComponent<RectTransform>();
            Place(areaRect, trackLeft, 0.20f, trackRight, 0.80f);

            Image handle = ImageOf(area.transform, "Handle", solid, accent, true);
            handle.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            handle.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            handle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            handle.rectTransform.sizeDelta = new Vector2(thumbW, thumbH);

            var fill = ImageOf(area.transform, "Fill", solid, new Color(accent.r, accent.g, accent.b, 0.35f), false);
            fill.rectTransform.anchorMin = new Vector2(0f, 0.38f);
            fill.rectTransform.anchorMax = new Vector2(0f, 0.62f);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;

            return WireMixSlider(root, fill.rectTransform, handle, Slider.Direction.LeftToRight, 0f, changed);
        }

        private Slider BuildStemSlider(
            Transform parent,
            string name,
            Font font,
            Sprite solid,
            Color accent,
            string caption,
            Vector2 anchorMin,
            Vector2 anchorMax,
            float initial,
            Action<float> changed)
        {
            // Compact horizontal cap on thin vertical rail (not a wide slab).
            const float thumbW = 22f;
            const float thumbH = 10f;
            const float trackWidth = 8f;
            const float travel = 72f;

            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var rect = root.AddComponent<RectTransform>();
            Place(rect, anchorMin.x, anchorMin.y, anchorMax.x, anchorMax.y);

            Image hit = ImageOf(root.transform, "Hit", solid, Inset, true);
            Stretch(hit.rectTransform);
            Label(root.transform, "Caption", font, caption, 11, Ink, TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0.82f), new Vector2(0.95f, 1f));

            var area = new GameObject("HandleArea");
            area.transform.SetParent(root.transform, false);
            var areaRect = area.AddComponent<RectTransform>();
            areaRect.anchorMin = new Vector2(0.5f, 0.38f);
            areaRect.anchorMax = new Vector2(0.5f, 0.38f);
            areaRect.pivot = new Vector2(0.5f, 0.5f);
            areaRect.sizeDelta = new Vector2(48f, travel);

            Image track = ImageOf(area.transform, "Track", solid, new Color(0.18f, 0.20f, 0.28f, 1f), false);
            track.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            track.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            track.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            track.rectTransform.sizeDelta = new Vector2(trackWidth, 0f);
            track.rectTransform.anchoredPosition = Vector2.zero;

            Image handle = ImageOf(area.transform, "Handle", solid, accent, true);
            handle.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            handle.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            handle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            handle.rectTransform.sizeDelta = new Vector2(thumbW, thumbH);

            var fill = ImageOf(area.transform, "Fill", solid, new Color(accent.r, accent.g, accent.b, 0.35f), false);
            fill.rectTransform.anchorMin = new Vector2(0.42f, 0f);
            fill.rectTransform.anchorMax = new Vector2(0.58f, 0f);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;

            return WireMixSlider(root, fill.rectTransform, handle, Slider.Direction.BottomToTop, initial, changed);
        }

        private Slider WireMixSlider(
            GameObject root,
            RectTransform fill,
            Image handle,
            Slider.Direction direction,
            float initial,
            Action<float> changed)
        {
            var slider = root.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = direction;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = initial;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            slider.onValueChanged.AddListener(value =>
            {
                if (_syncingMix || !slider.isActiveAndEnabled || !slider.interactable)
                    return;
                if (changed != null)
                    changed(value);
            });
            return slider;
        }

        private void BuildRewardCard(Transform parent, Font font, Sprite panel, Color accent, int index, Vector2 anchorMin, Vector2 anchorMax, Action<string> pick)
        {
            Image card = ImageOf(parent, "Reward" + index, panel, Color.white, true);
            card.type = Image.Type.Sliced;
            card.sprite = PanelSprite(new Color(0.07f, 0.08f, 0.13f, 1f), accent);
            Place(card.rectTransform, anchorMin.x, anchorMin.y, anchorMax.x, anchorMax.y);
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            int captured = index;
            button.onClick.AddListener(() =>
            {
                if (pick != null)
                    pick(_rewardIds[captured]);
            });
            var pad = new GameObject("Pad").AddComponent<RectTransform>();
            pad.SetParent(card.transform, false);
            Stretch(pad);
            pad.offsetMin = new Vector2(16f, 16f);
            pad.offsetMax = new Vector2(-16f, -16f);
            _rewardTitles[index] = Label(pad, "Title", font, "", 28, Ink, TextAnchor.MiddleLeft, new Vector2(0f, 0.52f), new Vector2(1f, 0.92f));
            _rewardBodies[index] = Label(pad, "Body", font, "", 18, Muted, TextAnchor.UpperLeft, new Vector2(0f, 0.08f), new Vector2(1f, 0.50f));
            _rewardKeys[index] = Label(pad, "Key", font, (index + 1).ToString(), 22, Warning, TextAnchor.MiddleCenter, new Vector2(0.78f, 0.70f), new Vector2(1f, 0.98f));
            _rewardKeys[index].gameObject.SetActive(false);
            _rewardCards[index] = card.gameObject;
            _rewardCards[index].SetActive(false);
        }

        private GameObject BuildCrateRow(Transform parent, Font font, Sprite solid, Sprite panel, out Text header)
        {
            Image root = ImageOf(parent, "CrateRow", panel, Color.white, false);
            root.type = Image.Type.Sliced;
            root.color = new Color(Panel.r, Panel.g, Panel.b, 0.96f);
            // Height set by ApplyBoothLayout. Two rows of eight once the crate cap passes 8.
            Place(root.rectTransform, 0.03f, 0.508f, 0.97f, 0.550f);

            header = Label(root.transform, "CrateHeader", font, "CRATE", 14, Ink, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(0.08f, 1f));

            for (int i = 0; i < CrateCap; i++)
            {
                Image chip = ImageOf(root.transform, "CrateChip" + i, solid, Inset, true);
                LayoutCrateChip(chip.rectTransform, i);
                var button = chip.gameObject.AddComponent<Button>();
                button.targetGraphic = chip;
                button.transition = Selectable.Transition.None;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                int slot = i;
                button.onClick.AddListener(() =>
                {
                    if (_onCratePick != null)
                        _onCratePick(slot);
                });
                Text label = Label(chip.transform, "Name", font, "", 14, Ink, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
                _crateChipImages[i] = chip;
                _crateChipLabels[i] = label;
                chip.gameObject.SetActive(false);
            }

            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        private static void BuildAct1Strip(
            Transform parent,
            Font font,
            Sprite panel,
            out GameObject strip,
            out Text club,
            out Text phase,
            out Text hype,
            out Text cue,
            out Text score,
            out Text debug)
        {
            Image root = ImageOf(parent, "Act1Strip", panel, Color.white, false);
            root.type = Image.Type.Sliced;
            root.sprite = PanelSprite(Face, Mint);
            root.color = Color.white;
            // Larger readable strip — stage top-right; clear of crossfader.
            Place(root.rectTransform, 0.62f, 0.82f, 0.96f, 0.988f);
            strip = root.gameObject;

            var pad = new GameObject("Pad").AddComponent<RectTransform>();
            pad.SetParent(root.transform, false);
            Stretch(pad);
            pad.offsetMin = new Vector2(10f, 8f);
            pad.offsetMax = new Vector2(-10f, -8f);

            club = Label(pad, "Act1Club", font, "", 20, Ink, TextAnchor.MiddleLeft, new Vector2(0f, 0.70f), new Vector2(1f, 1f));
            phase = Label(pad, "Act1Phase", font, "", 18, Mint, TextAnchor.MiddleLeft, new Vector2(0f, 0.48f), new Vector2(0.50f, 0.72f));
            hype = Label(pad, "Act1Hype", font, "", 18, Ink, TextAnchor.MiddleRight, new Vector2(0.48f, 0.48f), new Vector2(1f, 0.72f));
            cue = Label(pad, "Act1Cue", font, "", 17, Warning, TextAnchor.MiddleLeft, new Vector2(0f, 0.24f), new Vector2(0.55f, 0.50f));
            score = Label(pad, "Act1Score", font, "", 16, Muted, TextAnchor.MiddleRight, new Vector2(0.50f, 0.24f), new Vector2(1f, 0.50f));
            debug = Label(pad, "Act1Hint", font, "", 16, Ink, TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(1f, 0.26f));
        }

        private static Color Hex(int rgb)
        {
            float r = ((rgb >> 16) & 0xFF) / 255f;
            float g = ((rgb >> 8) & 0xFF) / 255f;
            float b = (rgb & 0xFF) / 255f;
            return new Color(r, g, b, 1f);
        }

        private static Slider BuildCrossfader(Transform parent, Font font, Sprite solid, Action<float> crossfade)
        {
            const float trackHeight = 10f;
            // Compact XF cap (hit = full panel Hit image).
            const float thumbW = 12f;
            const float thumbH = 24f;
            const float trackLeft = 0.28f;
            const float trackRight = 0.72f;

            var root = new GameObject("Crossfader");
            root.transform.SetParent(parent, false);
            var rect = root.AddComponent<RectTransform>();
            Place(rect, 0.42f, 0.028f, 0.58f, 0.110f);
            root.AddComponent<RectMask2D>();

            Image hit = ImageOf(root.transform, "Hit", solid, new Color(0.06f, 0.07f, 0.11f, 1f), true);
            Stretch(hit.rectTransform);
            Label(root.transform, "SideA", font, "A", 18, DeckA, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(0.22f, 1f));
            Label(root.transform, "SideB", font, "B", 18, DeckB, TextAnchor.MiddleCenter, new Vector2(0.78f, 0f), new Vector2(1f, 1f));

            Image track = ImageOf(root.transform, "Track", solid, new Color(0.20f, 0.22f, 0.30f, 1f), false);
            track.rectTransform.anchorMin = new Vector2(trackLeft, 0.5f);
            track.rectTransform.anchorMax = new Vector2(trackRight, 0.5f);
            track.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            track.rectTransform.sizeDelta = new Vector2(0f, trackHeight);
            track.rectTransform.anchoredPosition = Vector2.zero;

            var area = new GameObject("HandleArea");
            area.transform.SetParent(root.transform, false);
            var areaRect = area.AddComponent<RectTransform>();
            Place(areaRect, trackLeft, 0.10f, trackRight, 0.90f);

            Image handle = ImageOf(area.transform, "Handle", solid, Ink, true);
            handle.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            handle.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            handle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            handle.rectTransform.sizeDelta = new Vector2(thumbW, thumbH);

            var slider = root.AddComponent<Slider>();
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            slider.onValueChanged.AddListener(value => crossfade(value));
            return slider;
        }

        private static Image ButtonOf(Transform parent, Font font, Sprite sprite, string caption, Color face, Color textColor, int fontSize, Vector2 anchorMin, Vector2 anchorMax, Action clicked)
        {
            Image image = ImageOf(parent, caption, sprite, face, true);
            Place(image.rectTransform, anchorMin.x, anchorMin.y, anchorMax.x, anchorMax.y);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => clicked());
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.04f;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Label(image.transform, "Caption", font, caption, fontSize, textColor, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            return image;
        }

        private static void StampOutline(Text text)
        {
            if (text == null)
                return;
            Outline outline = text.GetComponent<Outline>();
            if (outline == null)
                outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.04f, 0.03f, 0.07f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;
        }

        private static Text Label(Transform parent, string name, Font font, string value, int size, Color color, TextAnchor anchor, Vector2 anchorMin, Vector2 anchorMax)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var rect = root.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(6f, 2f);
            rect.offsetMax = new Vector2(-6f, -2f);
            var text = root.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = false;
            text.supportRichText = false;
            text.raycastTarget = false;
            text.text = value;
            root.AddComponent<RectMask2D>();
            return text;
        }

        private static void Assign(Text label, string value)
        {
            AssignBudget(label, value, 0f);
        }

        /// <summary>
        /// Reward copy wraps inside the card. Single-line fitting would erase a long aura line.
        /// </summary>
        private static void AssignWrapped(Text label, string value)
        {
            if (label == null)
                return;
            value = value ?? "";
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            if (label.text != value)
                label.text = value;
        }

        private static void AnchorText(RectTransform rect, float x0, float y0, float x1, float y1)
        {
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = new Vector2(6f, 2f);
            rect.offsetMax = new Vector2(-6f, -2f);
        }

        /// <summary>
        /// Eight chips per row. A 16-cap crate is two rows, not one row of tiny chips.
        /// </summary>
        private static void LayoutCrateChip(RectTransform chip, int index)
        {
            int columns = CrateColumns;
            int rows = (CrateCap + columns - 1) / columns;
            if (rows < 1)
                rows = 1;
            if (rows <= 1)
            {
                const float chipLeft = 0.09f;
                const float chipSpan = 0.91f;
                const float gap = 0.016f;
                float width = (chipSpan - gap * (columns - 1)) / columns;
                float x0 = chipLeft + index * (width + gap);
                Place(chip, x0, 0.06f, x0 + width, 0.94f);
                return;
            }

            int col = index % columns;
            int row = index / columns;
            const float gapX = 0.008f;
            const float gapY = 0.05f;
            const float left = 0.09f;
            const float right = 0.995f;
            const float bottom = 0.05f;
            const float top = 0.95f;
            float chipWidth = (right - left - gapX * (columns - 1)) / columns;
            float chipHeight = (top - bottom - gapY * (rows - 1)) / rows;
            float x = left + col * (chipWidth + gapX);
            float y1 = top - row * (chipHeight + gapY);
            Place(chip, x, y1 - chipHeight, x + chipWidth, y1);
        }

        private static void AssignBudget(Text label, string value, float budget)
        {
            if (label == null)
                return;
            value = value ?? "";
            string shown = Fit(label, value, budget);
            if (label.text != shown)
                label.text = shown;
        }

        private static string Fit(Text label, string value, float budget)
        {
            if (value.Length == 0 || label.font == null)
                return value;

            float width = budget > 1f ? budget : label.rectTransform.rect.width;
            if (width < 8f)
                return value;

            float limit = width - 4f;
            if (limit < 8f)
                limit = width;
            int size = label.fontSize;
            FontStyle style = label.fontStyle;
            if (Measure(label.font, size, style, value) <= limit)
                return value;

            const string ellipsis = "...";
            if (Measure(label.font, size, style, ellipsis) > limit)
                return "";

            int low = 0;
            int high = value.Length;
            string best = ellipsis;
            while (low <= high)
            {
                int mid = (low + high) >> 1;
                string candidate = value.Substring(0, mid).TrimEnd() + ellipsis;
                if (Measure(label.font, size, style, candidate) <= limit)
                {
                    best = candidate;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return best;
        }

        private static float Measure(Font font, int size, FontStyle style, string value)
        {
            if (value.Length == 0)
                return 0f;
            font.RequestCharactersInTexture(value, size, style);
            float width = 0f;
            for (int i = 0; i < value.Length; i++)
            {
                CharacterInfo info;
                if (font.GetCharacterInfo(value[i], out info, size, style))
                    width += info.advance;
                else
                    width += size;
            }

            return width;
        }

        private static void FitDisc(RectTransform disc)
        {
            var slot = disc.parent as RectTransform;
            if (slot == null)
                return;
            float side = Mathf.Min(slot.rect.width, slot.rect.height);
            if (side < 8f)
                return;
            if (Mathf.Abs(disc.sizeDelta.x - side) > 0.5f)
                disc.sizeDelta = new Vector2(side, side);
        }

        private static void SetSpan(RectTransform rect, float x0, float x1)
        {
            rect.anchorMin = new Vector2(x0, rect.anchorMin.y);
            rect.anchorMax = new Vector2(x1, rect.anchorMax.y);
        }

        private static void Place(RectTransform rect, float x0, float y0, float x1, float y1)
        {
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Image ImageOf(Transform parent, string name, Sprite sprite, Color color, bool raycast)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.AddComponent<RectTransform>();
            var image = root.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Font Font()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }

        private static Sprite SolidSprite()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }

        private static Sprite PanelSprite(Color fill, Color border)
        {
            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool edge = x < 2 || y < 2 || x >= size - 2 || y >= size - 2;
                    texture.SetPixel(x, y, edge ? border : fill);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect, new Vector4(4f, 4f, 4f, 4f));
        }

        private static Sprite DiscSprite()
        {
            const int size = 49;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;
            var clear = new Color32(0, 0, 0, 0);
            var hole = new Color32(12, 14, 22, 255);
            var groove = new Color32(168, 168, 168, 255);
            var vinyl = new Color32(255, 255, 255, 255);
            int center = 24;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = x - center;
                    int dy = y - center;
                    int distance = dx * dx + dy * dy;
                    if (distance > 24 * 24)
                        texture.SetPixel(x, y, clear);
                    else if (distance <= 4 * 4)
                        texture.SetPixel(x, y, hole);
                    else if (distance <= 9 * 9)
                        texture.SetPixel(x, y, vinyl);
                    else if (distance >= 22 * 22)
                        texture.SetPixel(x, y, vinyl);
                    else
                    {
                        int ring = (int)Mathf.Sqrt(distance);
                        texture.SetPixel(x, y, ((ring / 2) & 1) == 0 ? vinyl : groove);
                    }
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
