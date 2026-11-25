using System.Globalization;
using DeckRogue.Core.Run;
using UnityEngine;
using UnityEngine.UI;

namespace DeckRogue.Audio
{
    /// <summary>
    /// UI-facing mirror of Core.Run.Act1Readout plus deck-depth extras.
    /// Lead owns Act1Readout in Core; map into this until / when the Core type lands.
    /// </summary>
    public struct Act1UiReadout
    {
        public bool Present;
        public int ClubIndex;
        public string ClubId;
        public bool IsBoss;
        public RunPhase Phase;
        public double Hype;
        public double TargetHype;
        public SetOutcome Outcome;
        public string ReasonId;
        public bool CueReady;
        public string LastScoreKind;
        public int CrateCount;

        /// <summary>
        /// Maps Core.Run.Act1Readout plus deck-depth extras the Core readout does not carry.
        /// </summary>
        public static Act1UiReadout FromCore(
            Act1Readout core,
            bool cueReady,
            string lastScoreKind,
            int crateCount)
        {
            return new Act1UiReadout
            {
                Present = true,
                ClubIndex = core.ClubIndex,
                ClubId = core.ClubId ?? "",
                IsBoss = core.IsBoss,
                Phase = core.Phase,
                Hype = core.Hype,
                TargetHype = core.TargetHype,
                Outcome = core.Outcome,
                ReasonId = core.ReasonId ?? "",
                CueReady = cueReady,
                LastScoreKind = lastScoreKind ?? "",
                CrateCount = crateCount
            };
        }
    }

    /// <summary>
    /// Optional Act1 strip over the P0 booth. Keep the component disabled on the
    /// scene until an Act1 host feeds Bind each frame. Does not touch ClubDirector.
    /// </summary>
    public sealed class Act1RunPresenter : MonoBehaviour
    {
        [SerializeField] private bool _driveLabels = true;
        [SerializeField] private bool _showDebug = true;
        [SerializeField] private Text _clubIdLabel;
        [SerializeField] private Text _phaseLabel;
        [SerializeField] private Text _hypeLabel;
        [SerializeField] private Text _cueReadyLabel;
        [SerializeField] private Text _lastScoreLabel;
        [SerializeField] private Text _debugLabel;

        private Act1UiReadout _last;
        private bool _hasLast;

        public bool DriveLabels
        {
            get { return _driveLabels; }
            set { _driveLabels = value; }
        }

        public bool ShowDebug
        {
            get { return _showDebug; }
            set { _showDebug = value; }
        }

        public Act1UiReadout LastReadout
        {
            get { return _last; }
        }

        public void BindLabels(
            Text clubId,
            Text phase,
            Text hype,
            Text cueReady,
            Text lastScore,
            Text debug)
        {
            _clubIdLabel = clubId;
            _phaseLabel = phase;
            _hypeLabel = hype;
            _cueReadyLabel = cueReady;
            _lastScoreLabel = lastScore;
            _debugLabel = debug;
        }

        /// <summary>
        /// Wires BoothHud Act1 strip texts and leaves the component disabled.
        /// Prefer feeding the strip via <see cref="RunChrome.ApplyAct1"/> + BoothHud.Refresh
        /// while TwoDeckSession/ClubDirector stays on the P0 path; enable this only for
        /// a dedicated Act1 host or SerializeField labels.
        /// </summary>
        public static Act1RunPresenter AttachDisabled(GameObject host, BoothHud hud)
        {
            if (host == null || hud == null)
                return null;

            Act1RunPresenter presenter = host.GetComponent<Act1RunPresenter>();
            if (presenter == null)
                presenter = host.AddComponent<Act1RunPresenter>();

            Text club;
            Text phase;
            Text hype;
            Text cue;
            Text score;
            Text debug;
            hud.GetAct1Labels(out club, out phase, out hype, out cue, out score, out debug);
            presenter.BindLabels(club, phase, hype, cue, score, debug);
            presenter.enabled = false;
            return presenter;
        }

        public void Bind(Act1Readout core, bool cueReady, string lastScoreKind, int crateCount)
        {
            Bind(Act1UiReadout.FromCore(core, cueReady, lastScoreKind, crateCount));
        }

        public void Bind(Act1UiReadout readout)
        {
            _last = readout;
            _hasLast = readout.Present;
            if (!_driveLabels || !isActiveAndEnabled)
                return;
            Paint(readout);
        }

        public void Clear()
        {
            _last = default(Act1UiReadout);
            _hasLast = false;
            if (!_driveLabels)
                return;
            Assign(_clubIdLabel, "");
            Assign(_phaseLabel, "");
            Assign(_hypeLabel, "");
            Assign(_cueReadyLabel, "");
            Assign(_lastScoreLabel, "");
            Assign(_debugLabel, "");
            SetStrip(false);
            SetActive(_clubIdLabel, false);
            SetActive(_phaseLabel, false);
            SetActive(_hypeLabel, false);
            SetActive(_cueReadyLabel, false);
            SetActive(_lastScoreLabel, false);
            SetActive(_debugLabel, false);
        }

        private void OnEnable()
        {
            if (_hasLast && _driveLabels)
                Paint(_last);
        }

        private void OnDisable()
        {
            if (!_driveLabels)
                return;
            SetStrip(false);
            SetActive(_clubIdLabel, false);
            SetActive(_phaseLabel, false);
            SetActive(_hypeLabel, false);
            SetActive(_cueReadyLabel, false);
            SetActive(_lastScoreLabel, false);
            SetActive(_debugLabel, false);
        }

        private void Paint(Act1UiReadout readout)
        {
            bool on = readout.Present;
            SetStrip(on);
            SetActive(_clubIdLabel, on);
            SetActive(_phaseLabel, on);
            SetActive(_hypeLabel, on);
            SetActive(_cueReadyLabel, on);
            SetActive(_lastScoreLabel, on);
            SetActive(_debugLabel, on && _showDebug);

            if (!on)
                return;

            string club = string.IsNullOrEmpty(readout.ClubId) ? "—" : readout.ClubId;
            if (readout.IsBoss)
                club += " · boss";
            Assign(_clubIdLabel, club);
            Assign(_phaseLabel, PhaseLabel(readout.Phase));
            Assign(_hypeLabel, "Hype " + Round(readout.Hype) + " / " + Round(readout.TargetHype));
            Assign(_cueReadyLabel, readout.CueReady ? "CUE READY" : "cue —");
            Assign(_lastScoreLabel, string.IsNullOrEmpty(readout.LastScoreKind) ? "" : readout.LastScoreKind);
            if (_showDebug)
            {
                Assign(_debugLabel,
                    "club " + (readout.ClubIndex + 1).ToString(CultureInfo.InvariantCulture)
                    + " · crate " + readout.CrateCount.ToString(CultureInfo.InvariantCulture) + "/8");
            }
        }

        private static string PhaseLabel(RunPhase phase)
        {
            switch (phase)
            {
                case RunPhase.PreClub:
                    return "Pre-club";
                case RunPhase.Performing:
                    return "Set live";
                case RunPhase.Result:
                    return "Set result";
                case RunPhase.Reward:
                    return "Reward";
                case RunPhase.FinalResult:
                    return "Act finale";
                default:
                    return phase.ToString();
            }
        }

        private static string Round(double value)
        {
            return ((int)System.Math.Round(value)).ToString(CultureInfo.InvariantCulture);
        }

        private static void Assign(Text label, string value)
        {
            if (label == null)
                return;
            value = value ?? "";
            if (label.text != value)
                label.text = value;
        }

        private void SetStrip(bool active)
        {
            if (_clubIdLabel == null)
                return;
            Transform parent = _clubIdLabel.transform.parent;
            if (parent == null || parent.name != "Act1Strip")
                return;
            GameObject strip = parent.gameObject;
            if (strip.activeSelf != active)
                strip.SetActive(active);
        }

        private static void SetActive(Text label, bool active)
        {
            if (label == null)
                return;
            GameObject go = label.gameObject;
            if (go.activeSelf != active)
                go.SetActive(active);
        }
    }
}
