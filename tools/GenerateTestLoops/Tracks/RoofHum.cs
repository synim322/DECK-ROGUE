using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// roof_hum. Night-roof drone and a muted even kick.
    /// 120 BPM, 8 bars, natural A minor. Drone is 1.85 s.
    /// </summary>
    internal static class RoofHum
    {
        private const double DroneDuration = 1.85d;

        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double lift = LoopKit.Lift(bar);
                int barStart = LoopKit.At(bar, 0, false);

                for (int beat = 0; beat < 4; beat++)
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 40d, 0.7d);

                if (LoopKit.Diatonic(chord.Root))
                {
                    double sub = 0.4d * lift;
                    LoopKit.Sub(low, barStart, chord.Root, 0.45d, sub);
                    LoopKit.Sub(low, LoopKit.At(bar, 8, false), chord.Root, 0.45d, sub);
                    Voice(low, barStart, chord.Root, chord.Fifth, DroneDuration, 0.18d);
                }

                for (int step = 0; step < LoopKit.StepsPerBar; step++)
                    LoopKit.Shaker(perc, LoopKit.At(bar, step, false), 0.2d, 1900);

                int bell = chord.Fifth + 24;
                if (LoopKit.Diatonic(bell))
                    LoopKit.Bell(music, LoopKit.AtEighth(bar, 4), bell, 0.32d, 2.5d);
            }
        }

        private static void Voice(double[] buffer, int start, int root, int fifth, double duration, double velocity)
        {
            if (!LoopKit.Diatonic(root) || !LoopKit.Diatonic(fifth) || !LoopKit.Fits(start, duration))
                return;

            double freqRoot = LoopKit.Hz(root);
            double freqFifth = LoopKit.Hz(fifth);
            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double attack = t < 0.06d ? t / 0.06d : 1d;
                double a = Math.Sin(2d * Math.PI * freqRoot * t);
                double b = Math.Sin(2d * Math.PI * freqFifth * t);
                double beat = 0.78d + 0.22d * Math.Sin(2d * Math.PI * 0.2d * t);
                double remain = duration - t;
                double release = remain >= 0.08d ? 1d : (remain <= 0d ? 0d : remain / 0.08d);
                buffer[i] += (a + b) * 0.5d * beat * attack * release * velocity;
            }
        }
    }
}
