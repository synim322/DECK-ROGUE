using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// market_click. Wooden clicks on a fixed syncopation, even bounce.
    /// 120 BPM, 8 bars, natural A minor. No swing.
    /// </summary>
    internal static class MarketClick
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double lift = LoopKit.Lift(bar);
                double bass = 0.52d * lift;

                for (int beat = 0; beat < 4; beat++)
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 50d, 0.82d);

                Bass(low, bar, 0, chord.Root, bass);
                Bass(low, bar, 6, chord.Third, bass);
                Bass(low, bar, 8, chord.Fifth, bass);
                Bass(low, bar, 14, chord.Root, bass);

                Click(perc, bar, 0);
                Click(perc, bar, 3);
                Click(perc, bar, 6);
                Click(perc, bar, 8);
                Click(perc, bar, 11);
                Click(perc, bar, 14);

                for (int step = 0; step < LoopKit.StepsPerBar; step += 2)
                    LoopKit.Hat(perc, LoopKit.At(bar, step, false), 0.22d, false, 3600);

                Pluck(music, bar, 0, chord.Root + 24);
                Pluck(music, bar, 2, chord.Fifth + 12);
                Pluck(music, bar, 4, chord.Third + 12);
                Pluck(music, bar, 6, chord.Fifth + 12);
            }
        }

        private static void Bass(double[] buffer, int bar, int step, int midi, double velocity)
        {
            if (!LoopKit.Diatonic(midi))
                return;
            LoopKit.Bass(buffer, LoopKit.At(bar, step, false), midi, 0.12d, velocity, 1);
        }

        private static void Click(double[] buffer, int bar, int step)
        {
            int at = LoopKit.At(bar, step, false);
            LoopKit.Rim(buffer, at, 0.55d);
            Voice(buffer, at, 0.18d);
        }

        private static void Pluck(double[] buffer, int bar, int eighth, int midi)
        {
            if (!LoopKit.Diatonic(midi))
                return;
            LoopKit.Pluck(buffer, LoopKit.AtEighth(bar, eighth), midi, 0.12d, 0.34d, 0.7d);
        }

        private static void Voice(double[] buffer, int start, double velocity)
        {
            const double duration = 0.01d;
            if (!LoopKit.Fits(start, duration))
                return;

            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double sine = Math.Sin(2d * Math.PI * 800d * t);
                double noise = LoopKit.Hash(i + 801);
                double attack = t < 0.0004d ? t / 0.0004d : 1d;
                double decay = Math.Exp(-t * 400d);
                double remain = duration - t;
                double release = remain >= 0.001d ? 1d : (remain <= 0d ? 0d : remain / 0.001d);
                buffer[i] += (sine + noise * 0.18d) * attack * decay * release * velocity;
            }
        }
    }
}
