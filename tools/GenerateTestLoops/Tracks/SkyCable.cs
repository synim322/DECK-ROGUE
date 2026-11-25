using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// sky_cable: even techno drive with a short metallic cable partial.
    /// </summary>
    internal static class SkyCable
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double lift = LoopKit.Lift(bar);

                for (int beat = 0; beat < 4; beat++)
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 56d, 0.96d);

                if (LoopKit.Diatonic(chord.Root))
                    LoopKit.Bass(low, LoopKit.At(bar, 0, false), chord.Root, 0.14d, 0.5d * lift, 0);
                if (LoopKit.Diatonic(chord.Fifth))
                    LoopKit.Bass(low, LoopKit.At(bar, 8, false), chord.Fifth, 0.14d, 0.5d * lift, 0);

                for (int step = 0; step < LoopKit.StepsPerBar; step++)
                {
                    bool offbeat = (step & 3) == 2;
                    double hat = offbeat ? 0.58d : 0.32d;
                    LoopKit.Hat(perc, LoopKit.At(bar, step, false), hat, false, 7200);
                }

                int midi = chord.Fifth + 24;
                if (!LoopKit.Diatonic(midi))
                    continue;

                double cable = LoopKit.Hz(midi) * 5.2d;
                for (int eighth = 0; eighth < 8; eighth++)
                {
                    int at = LoopKit.AtEighth(bar, eighth);
                    LoopKit.Fm(music, at, midi, 0.1d, 0.3d, 7d, 0.7d);
                    Voice(music, at, cable, 0.12d);
                }
            }
        }

        private static void Voice(double[] buffer, int start, double freq, double velocity)
        {
            const double duration = 0.08d;
            if (!LoopKit.Fits(start, duration))
                return;

            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                double t = n / (double)LoopKit.SampleRate;
                double env = Math.Exp(-t * 46d);
                double remain = duration - t;
                if (remain < 0.004d)
                    env *= remain / 0.004d;
                double partial = Math.Sin(2d * Math.PI * freq * t);
                buffer[start + n] += partial * env * velocity;
            }
        }
    }
}
