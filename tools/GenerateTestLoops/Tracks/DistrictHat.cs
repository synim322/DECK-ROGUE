using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// district_hat. Open hat is the instrument; the grid stays even.
    /// 120 BPM, 8 bars, natural A minor. Swing stays off.
    /// </summary>
    internal static class DistrictHat
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double lift = LoopKit.Lift(bar);
                int root = chord.Root;
                int fifth = chord.Fifth;

                for (int beat = 0; beat < 4; beat++)
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 54d, 0.95d);

                if (LoopKit.Diatonic(root))
                    LoopKit.Bass(low, LoopKit.At(bar, 0, false), root, 0.14d, 0.5d * lift, 0);
                if (LoopKit.Diatonic(fifth))
                    LoopKit.Bass(low, LoopKit.At(bar, 8, false), fifth, 0.14d, 0.5d * lift, 0);

                for (int step = 0; step < LoopKit.StepsPerBar; step++)
                {
                    bool open = step == 2 || step == 6 || step == 10 || step == 14;
                    int at = LoopKit.At(bar, step, false);
                    LoopKit.Hat(perc, at, open ? 0.72d : 0.34d, open, 8100);
                    if (open)
                        Voice(perc, at, 0.15d);
                }

                LoopKit.Clap(perc, LoopKit.At(bar, 4, false), 0.66d, 8100);
                LoopKit.Clap(perc, LoopKit.At(bar, 12, false), 0.66d, 8100);

                int stabRoot = root + 12;
                int stabFifth = fifth + 12;
                if (LoopKit.Diatonic(stabRoot))
                {
                    LoopKit.Stab(music, LoopKit.At(bar, 0, false), stabRoot, 0.34d, 0);
                    LoopKit.Stab(music, LoopKit.At(bar, 8, false), stabRoot, 0.34d, 0);
                }

                if (LoopKit.Diatonic(stabFifth))
                {
                    LoopKit.Stab(music, LoopKit.At(bar, 0, false), stabFifth, 0.34d, 0);
                    LoopKit.Stab(music, LoopKit.At(bar, 8, false), stabFifth, 0.34d, 0);
                }
            }
        }

        private static void Voice(double[] buffer, int start, double velocity)
        {
            const double duration = 0.012d;
            const double freq = 6000d;
            if (!LoopKit.Fits(start, duration))
                return;

            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double sine = Math.Sin(2d * Math.PI * freq * t);
                double grit = 0.18d * LoopKit.Hash(i + 8117);
                double env = Math.Exp(-t * 240d);
                if (t < 0.0006d)
                    env *= t / 0.0006d;
                double remain = duration - t;
                if (remain < 0.0012d)
                    env *= remain <= 0d ? 0d : remain / 0.0012d;
                buffer[i] += (sine + grit) * env * velocity;
            }
        }
    }
}
