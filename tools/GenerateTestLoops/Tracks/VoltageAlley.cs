using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// voltage_alley — bright even acid opener, same 16th cell every bar.
    /// 120 BPM, 8 bars, natural A minor. Swing stays off.
    /// </summary>
    internal static class VoltageAlley
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double acidVel = 0.5d * LoopKit.Lift(bar);
                int answer = chord.Fifth + 12;

                for (int beat = 0; beat < 4; beat++)
                {
                    int at = LoopKit.At(bar, beat * 4, false);
                    LoopKit.Kick(low, at, 52d, 1d);
                    if (LoopKit.Diatonic(answer))
                    {
                        LoopKit.Square(music, at, answer, 0.1d, 0.28d, 0.55d);
                        Voice(music, at, answer + 12, 0.08d, 0.16d);
                    }
                }

                for (int step = 0; step < LoopKit.StepsPerBar; step++)
                {
                    int at = LoopKit.At(bar, step, false);
                    int tone = Cell(chord, step);
                    double accent = (step & 3) == 0 ? 1d : 0.62d;
                    if (LoopKit.Diatonic(tone))
                        LoopKit.Acid(low, at, tone, 0.07d, acidVel, accent);

                    bool offbeat = (step & 3) == 2;
                    LoopKit.Hat(perc, at, offbeat ? 0.58d : 0.32d, false, 10500);
                }

                LoopKit.Clap(perc, LoopKit.At(bar, 4, false), 0.72d, 10500);
                LoopKit.Clap(perc, LoopKit.At(bar, 12, false), 0.72d, 10500);
            }
        }

        private static int Cell(LoopKit.Chord chord, int step)
        {
            switch (step & 3)
            {
                case 2:
                    return chord.Fifth;
                case 3:
                    return chord.Third;
                default:
                    return chord.Root;
            }
        }

        private static void Voice(double[] buffer, int start, int midi, double duration, double velocity)
        {
            if (!LoopKit.Diatonic(midi) || !LoopKit.Fits(start, duration))
                return;

            double freq = LoopKit.Hz(midi);
            int count = (int)(duration * LoopKit.SampleRate);
            double lowpass = 0d;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double saw = 0d;
                for (int h = 1; h <= 6; h++)
                    saw += Math.Sin(2d * Math.PI * freq * h * t) / h;

                double cutoff = 6800d + 2600d * Math.Exp(-t * 20d);
                double dt = 1d / LoopKit.SampleRate;
                double rc = 1d / (2d * Math.PI * cutoff);
                double alpha = dt / (rc + dt);
                lowpass += alpha * (saw - lowpass);
                double air = Math.Sin(2d * Math.PI * freq * 6d * t) * Math.Exp(-t * 40d);
                double bright = lowpass * 0.7d + air * 0.55d;
                double attack = t < 0.002d ? t / 0.002d : 1d;
                double remain = duration - t;
                double release = remain >= 0.008d ? 1d : (remain <= 0d ? 0d : remain / 0.008d);
                buffer[i] += Math.Tanh(bright) * attack * release * velocity;
            }
        }
    }
}
