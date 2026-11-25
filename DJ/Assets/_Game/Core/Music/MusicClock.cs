using System;
using DeckRogue.Core.Time;

namespace DeckRogue.Core.Music
{
    /// <summary>
    /// Musical grid owned by an absolute time provider.
    /// At 120 BPM: beat = 0.5s, bar = 2s, phrase = 16s (8 bars of 4/4).
    /// A command that arrives inside <see cref="SchedulingLeadTime"/> of a boundary
    /// is moved to the following boundary and is never scheduled in the past.
    /// </summary>
    public sealed class MusicClock
    {
        public const double ReferenceBpm = 120d;
        public const int BeatsPerBar = 4;
        public const int BarsPerPhrase = 8;
        public const double DefaultSchedulingLeadTime = 0.05d;

        private const double GridEpsilon = 1e-8d;

        private readonly ITimeProvider _time;
        private double _origin;

        public MusicClock(
            ITimeProvider time,
            double bpm = ReferenceBpm,
            double schedulingLeadTime = DefaultSchedulingLeadTime,
            double origin = 0d)
        {
            if (time == null)
                throw new ArgumentNullException(nameof(time));
            if (bpm <= 0d || double.IsNaN(bpm) || double.IsInfinity(bpm))
                throw new ArgumentOutOfRangeException(nameof(bpm), "BPM must be a finite number greater than zero.");
            if (schedulingLeadTime < 0d || double.IsNaN(schedulingLeadTime) || double.IsInfinity(schedulingLeadTime))
                throw new ArgumentOutOfRangeException(nameof(schedulingLeadTime), "Lead time must be a finite number of zero or more seconds.");
            if (double.IsNaN(origin) || double.IsInfinity(origin))
                throw new ArgumentOutOfRangeException(nameof(origin));

            _time = time;
            _origin = origin;
            Bpm = bpm;
            SchedulingLeadTime = schedulingLeadTime;
            BeatDuration = 60d / bpm;
            BarDuration = BeatDuration * BeatsPerBar;
            PhraseDuration = BarDuration * BarsPerPhrase;
            HalfPhraseDuration = BarDuration * (BarsPerPhrase / 2);
        }

        public double Bpm { get; }
        public double SchedulingLeadTime { get; }
        public double BeatDuration { get; }
        public double BarDuration { get; }
        public double HalfPhraseDuration { get; }
        public double PhraseDuration { get; }
        public double Origin => _origin;
        public double Elapsed => _time.Now - _origin;

        public int GetBeat()
        {
            return GetPosition().Beat;
        }

        public int GetBar()
        {
            return GetPosition().Bar;
        }

        public int GetPhrase()
        {
            return GetPosition().Phrase;
        }

        public MusicPosition GetPosition()
        {
            return GetPosition(_time.Now);
        }

        public MusicPosition GetPosition(double audioTime)
        {
            int beat = BeatIndexAt(audioTime);
            int bar = beat / BeatsPerBar;
            int phrase = bar / BarsPerPhrase;
            return new MusicPosition(
                beat,
                bar,
                phrase,
                beat % BeatsPerBar,
                bar % BarsPerPhrase,
                audioTime);
        }

        public double ToAudioTime(int beatIndex)
        {
            if (beatIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(beatIndex));
            return _origin + beatIndex * BeatDuration;
        }

        public double GetNextBoundary(MusicalBoundary boundary)
        {
            return GetNextBoundary(boundary, _time.Now);
        }

        public double GetNextBoundary(MusicalBoundary boundary, double atAudioTime)
        {
            if (double.IsNaN(atAudioTime) || double.IsInfinity(atAudioTime))
                throw new ArgumentOutOfRangeException(nameof(atAudioTime));

            return NextFromInterval(Interval(boundary), atAudioTime);
        }

        /// <summary>
        /// Next grid point at a custom bar stride (1, 2, 4, or 8) with the same lead-time rule.
        /// </summary>
        public double GetNextBoundaryBars(int bars)
        {
            return GetNextBoundaryBars(bars, _time.Now);
        }

        public double GetNextBoundaryBars(int bars, double atAudioTime)
        {
            if (bars != 1 && bars != 2 && bars != 4 && bars != 8)
                throw new ArgumentOutOfRangeException(nameof(bars), "Bar stride must be 1, 2, 4, or 8.");
            if (double.IsNaN(atAudioTime) || double.IsInfinity(atAudioTime))
                throw new ArgumentOutOfRangeException(nameof(atAudioTime));

            return NextFromInterval(BarDuration * bars, atAudioTime);
        }

        private double NextFromInterval(double interval, double atAudioTime)
        {
            double earliest = atAudioTime + SchedulingLeadTime;
            long index = BoundaryIndexAtOrAfter(earliest, interval);
            return _origin + index * interval;
        }

        public void SetOrigin(double originAudioTime)
        {
            if (double.IsNaN(originAudioTime) || double.IsInfinity(originAudioTime))
                throw new ArgumentOutOfRangeException(nameof(originAudioTime));
            _origin = originAudioTime;
        }

        /// <summary>
        /// Keeps the musical elapsed time when the audio device clock jumps on resume.
        /// </summary>
        public void ResumeKeepingElapsed(double elapsedSeconds, double audioTimeNow)
        {
            if (elapsedSeconds < 0d || double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds))
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            if (double.IsNaN(audioTimeNow) || double.IsInfinity(audioTimeNow))
                throw new ArgumentOutOfRangeException(nameof(audioTimeNow));
            _origin = audioTimeNow - elapsedSeconds;
        }

        private int BeatIndexAt(double audioTime)
        {
            double elapsed = audioTime - _origin;
            if (elapsed <= 0d)
                return 0;

            double quotient = elapsed / BeatDuration;
            long beat = (long)Math.Floor(quotient);
            double beatStart = beat * BeatDuration;
            if (elapsed + GridEpsilon >= beatStart + BeatDuration)
                beat++;
            if (beat < 0)
                return 0;
            if (beat > int.MaxValue)
                return int.MaxValue;
            return (int)beat;
        }

        private long BoundaryIndexAtOrAfter(double earliestAudioTime, double interval)
        {
            double fromOrigin = earliestAudioTime - _origin;
            if (fromOrigin <= 0d)
                return 0;

            long index = (long)Math.Floor(fromOrigin / interval);
            double boundary = index * interval;
            if (boundary + GridEpsilon < fromOrigin)
                index++;
            if (index < 0)
                return 0;
            return index;
        }

        private double Interval(MusicalBoundary boundary)
        {
            switch (boundary)
            {
                case MusicalBoundary.Beat:
                    return BeatDuration;
                case MusicalBoundary.Bar:
                    return BarDuration;
                case MusicalBoundary.Phrase:
                    return PhraseDuration;
                case MusicalBoundary.HalfPhrase:
                    return HalfPhraseDuration;
                case MusicalBoundary.TwoBars:
                    return BarDuration * 2d;
                default:
                    throw new ArgumentOutOfRangeException(nameof(boundary));
            }
        }
    }
}
