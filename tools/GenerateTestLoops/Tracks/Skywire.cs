using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// skywire — thin high wire, even pulse. Same grid every bar.
    /// 120 BPM, 8 bars, natural A minor. Swing stays off.
    /// </summary>
    internal static class Skywire
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double bassVel = 0.48d * LoopKit.Lift(bar);
                int wire = chord.Root + 24;
                bool rootOk = LoopKit.Diatonic(chord.Root);
                bool wireOk = LoopKit.Diatonic(wire);

                for (int beat = 0; beat < 4; beat++)
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 54d, 0.9d);

                for (int eighth = 0; eighth < 8; eighth++)
                {
                    if (rootOk)
                        LoopKit.Bass(low, LoopKit.At(bar, eighth * 2, false), chord.Root, 0.1d, bassVel, 0);
                }

                for (int step = 0; step < LoopKit.StepsPerBar; step++)
                {
                    int at = LoopKit.At(bar, step, false);
                    LoopKit.Hat(perc, at, 0.28d, false, 7400);
                    if (wireOk)
                        LoopKit.Fm(music, at, wire, 0.05d, 0.22d, 9d, 0.3d);
                }

                if (wireOk)
                    Voice(music, LoopKit.At(bar, 0, false), LoopKit.Hz(wire), 0.08d);
            }
        }

        private static void Voice(double[] buffer, int start, double freq, double velocity)
        {
            const double duration = 1.85d;
            if (!LoopKit.Fits(start, duration))
                return;

            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double tremolo = 1d + 0.25d * Math.Sin(2d * Math.PI * 6d * t);
                double sine = Math.Sin(2d * Math.PI * freq * t);
                double attack = t < 0.008d ? t / 0.008d : 1d;
                double remain = duration - t;
                double release = 1d;
                if (remain < 0.04d)
                    release = remain <= 0d ? 0d : remain / 0.04d;
                buffer[i] += sine * tremolo * attack * release * velocity;
            }
        }
    }
}
