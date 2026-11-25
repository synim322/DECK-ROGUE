using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// carbon_copy. Dry even electro: tight kick, closed hats, a short square hook
    /// copied an octave down. The same one-bar phrase every bar.
    /// 120 BPM, 8 bars, natural A minor. Swing stays off.
    /// </summary>
    internal static class CarbonCopy
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double bassVel = 0.48d * LoopKit.Lift(bar);

                for (int beat = 0; beat < 4; beat++)
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 58d, 0.92d);

                if (LoopKit.Diatonic(chord.Root))
                    LoopKit.Bass(low, LoopKit.At(bar, 0, false), chord.Root, 0.1d, bassVel, 1);
                if (LoopKit.Diatonic(chord.Fifth))
                    LoopKit.Bass(low, LoopKit.At(bar, 8, false), chord.Fifth, 0.1d, bassVel, 1);

                for (int step = 0; step < LoopKit.StepsPerBar; step++)
                    LoopKit.Hat(perc, LoopKit.At(bar, step, false), 0.4d, false, 11100);

                int[] lead = { chord.Root + 12, chord.Fifth + 12, chord.Third + 12, chord.Fifth + 12 };
                int[] eighths = { 0, 2, 4, 6 };
                for (int n = 0; n < 4; n++)
                {
                    int midi = lead[n];
                    if (!LoopKit.Diatonic(midi))
                        continue;

                    int at = LoopKit.AtEighth(bar, eighths[n]);
                    LoopKit.Square(music, at, midi, 0.14d, 0.36d, 0.3d);
                    Voice(music, at, midi, 0.16d);
                }
            }
        }

        private static void Voice(double[] buffer, int start, int midi, double velocity)
        {
            const double duration = 0.12d;
            int copy = midi - 12;
            if (!LoopKit.Diatonic(copy) || !LoopKit.Fits(start, duration))
                return;

            double freq = LoopKit.Hz(copy);
            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double saw = 0d;
                for (int h = 1; h <= 5; h++)
                    saw += Math.Sin(2d * Math.PI * freq * h * t) / h;
                double attack = t < 0.003d ? t / 0.003d : 1d;
                double remain = duration - t;
                double release = remain >= 0.012d ? 1d : (remain <= 0d ? 0d : remain / 0.012d);
                buffer[i] += saw * (2d / Math.PI) * attack * release * velocity;
            }
        }
    }
}
