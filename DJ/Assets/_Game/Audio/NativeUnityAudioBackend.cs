using System;
using DeckRogue.Core.Audio;
using UnityEngine;

namespace DeckRogue.Audio
{
    public enum DeckStartStatus
    {
        Started = 0,
        AlreadyRunning = 1,
        TimeInThePast = 2,
        MissingClip = 3
    }

    /// <summary>
    /// Two decks, three stems each. Starts with <see cref="AudioSource.PlayScheduled"/>
    /// on <see cref="AudioSettings.dspTime"/> so the stems share one sample clock.
    /// Perc/Music trims are stem gains; Filter uses <see cref="AudioLowPassFilter"/> on native.
    /// </summary>
    public sealed class NativeUnityAudioBackend : IDisposable
    {
        public const float GainRampSeconds = 0.02f;

        // Open = essentially flat; closed = deep low-pass. Exponential Hz map.
        public const float FilterCutoffOpenHz = 22000f;
        public const float FilterCutoffClosedHz = 180f;

        // ADR-003 / DJ-008: Unity Web AudioMixer is volume-only; AudioLowPassFilter is not a
        // reliable cross-platform DSP path there. This backend implements native low-pass for
        // Windows/Android. Web must use BiquadFilterNode (or pre-rendered stems) before filter
        // UI ships — do not expose a silent knob on Web.

        private readonly GameObject _root;
        private readonly AudioSource[,] _sources = new AudioSource[2, 3];
        private readonly AudioLowPassFilter[,] _filters = new AudioLowPassFilter[2, 3];
        private readonly bool[] _running = new bool[2];
        private readonly bool[] _stopping = new bool[2];
        private readonly double[] _startDsp = new double[2];
        private readonly float[] _deckTarget = new float[2];
        private readonly float[,] _stemTarget = new float[2, 3];
        private readonly float[,] _applied = new float[2, 3];
        private readonly float[] _filterAmount = new float[2];
        private readonly float[] _stopFade = new float[2];
        private double _lastDsp;

        public NativeUnityAudioBackend()
        {
            _root = new GameObject("NativeUnityAudio");
            _lastDsp = AudioSettings.dspTime;
            for (int deck = 0; deck < 2; deck++)
            {
                _deckTarget[deck] = deck == 0 ? 1f : 0f;
                _stopFade[deck] = 1f;
                _filterAmount[deck] = 0f;
                for (int stem = 0; stem < 3; stem++)
                {
                    _stemTarget[deck, stem] = 1f;
                    var child = new GameObject(((DeckId)deck) + "_" + ((StemId)stem));
                    child.transform.SetParent(_root.transform, false);
                    AudioSource source = child.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.loop = true;
                    source.spatialBlend = 0f;
                    source.dopplerLevel = 0f;
                    source.priority = 64;
                    source.volume = 0f;
                    _sources[deck, stem] = source;

                    AudioLowPassFilter filter = child.AddComponent<AudioLowPassFilter>();
                    filter.cutoffFrequency = FilterCutoffOpenHz;
                    filter.lowpassResonanceQ = 1f;
                    _filters[deck, stem] = filter;
                }
            }
        }

        public bool IsRunning(DeckId deck)
        {
            return _running[(int)deck];
        }

        public double StartDspTime(DeckId deck)
        {
            return _startDsp[(int)deck];
        }

        public int CompletedLoops(DeckId deck, double clipLength, double dspNow)
        {
            int index = (int)deck;
            if (!_running[index] || clipLength <= 0d || dspNow < _startDsp[index])
                return 0;
            return (int)Math.Floor((dspNow - _startDsp[index]) / clipLength);
        }

        public void AssignClip(DeckId deck, StemId stem, AudioClip clip)
        {
            AudioSource source = _sources[(int)deck, (int)stem];
            source.clip = clip;
            source.loop = true;
        }

        public void PlayClipsNow(DeckId deck, AudioClip low, AudioClip percussion, AudioClip music)
        {
            int index = (int)deck;
            var clips = new[] { low, percussion, music };
            for (int stem = 0; stem < 3; stem++)
            {
                AudioSource source = _sources[index, stem];
                source.Stop();
                source.clip = clips[stem];
                source.loop = true;
                source.time = 0f;
                source.Play();
            }

            _running[index] = true;
            _stopping[index] = false;
            _stopFade[index] = 1f;
            _startDsp[index] = AudioSettings.dspTime;
        }

        public void SetDeckTarget(DeckId deck, float gain)
        {
            _deckTarget[(int)deck] = Mathf.Clamp01(gain);
        }

        public void SetStemTarget(DeckId deck, StemId stem, float gain)
        {
            _stemTarget[(int)deck, (int)stem] = Mathf.Clamp01(gain);
        }

        /// <summary>
        /// 0 = open (no audible low-pass), 1 = fully filtered. Applied to all stems on the deck.
        /// Alias: SetLowPass. Web must use Biquad before shipping filter UI there.
        /// </summary>
        public void SetFilterAmount(DeckId deck, float amount)
        {
            int index = (int)deck;
            _filterAmount[index] = Mathf.Clamp01(amount);
            float cutoff = CutoffHz(_filterAmount[index]);
            for (int stem = 0; stem < 3; stem++)
                _filters[index, stem].cutoffFrequency = cutoff;
        }

        public void SetLowPass(DeckId deck, float closedness)
        {
            SetFilterAmount(deck, closedness);
        }

        public DeckStartStatus TryStartDeck(DeckId deck, double dspTime)
        {
            int index = (int)deck;
            if (_running[index])
                return DeckStartStatus.AlreadyRunning;

            for (int stem = 0; stem < 3; stem++)
            {
                if (_sources[index, stem].clip == null)
                    return DeckStartStatus.MissingClip;
            }

            double now = AudioSettings.dspTime;
            if (dspTime < now + 0.01d)
                return DeckStartStatus.TimeInThePast;

            for (int stem = 0; stem < 3; stem++)
            {
                AudioSource source = _sources[index, stem];
                source.time = 0f;
                source.PlayScheduled(dspTime);
            }

            _running[index] = true;
            _stopping[index] = false;
            _stopFade[index] = 1f;
            _startDsp[index] = dspTime;
            return DeckStartStatus.Started;
        }

        public void BeginStop(DeckId deck)
        {
            int index = (int)deck;
            if (!_running[index])
                return;
            _stopping[index] = true;
        }

        public void Tick()
        {
            double now = AudioSettings.dspTime;
            float dt = (float)Math.Max(0d, now - _lastDsp);
            _lastDsp = now;
            float step = GainRampSeconds <= 0f ? 1f : Mathf.Clamp01(dt / GainRampSeconds);

            for (int deck = 0; deck < 2; deck++)
            {
                if (_stopping[deck])
                    _stopFade[deck] = Mathf.MoveTowards(_stopFade[deck], 0f, step);

                for (int stem = 0; stem < 3; stem++)
                {
                    float target = _deckTarget[deck] * _stemTarget[deck, stem] * _stopFade[deck];
                    _applied[deck, stem] = Mathf.MoveTowards(_applied[deck, stem], target, step);
                    _sources[deck, stem].volume = _applied[deck, stem];
                }

                if (_stopping[deck] && _stopFade[deck] <= 0.001f)
                    FinishStop(deck);
            }
        }

        public void Dispose()
        {
            if (_root != null)
                UnityEngine.Object.Destroy(_root);
        }

        public static float CutoffHz(float amount01)
        {
            float t = Mathf.Clamp01(amount01);
            // Exponential: amount 0 → open, amount 1 → closed.
            return FilterCutoffClosedHz * Mathf.Pow(FilterCutoffOpenHz / FilterCutoffClosedHz, 1f - t);
        }

        private void FinishStop(int deck)
        {
            for (int stem = 0; stem < 3; stem++)
            {
                AudioSource source = _sources[deck, stem];
                source.Stop();
                source.volume = 0f;
                _applied[deck, stem] = 0f;
            }

            _running[deck] = false;
            _stopping[deck] = false;
            _stopFade[deck] = 1f;
        }
    }
}
