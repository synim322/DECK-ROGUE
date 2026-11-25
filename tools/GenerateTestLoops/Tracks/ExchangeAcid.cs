using System;

namespace GenerateTestLoops.Tracks
{
    internal static class ExchangeAcid
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double lift = LoopKit.Lift(bar);
                double acidVel = 0.5d * lift;

                for (int beat = 0; beat < 4; beat++)
                {
                    int at = LoopKit.At(bar, beat * 4, false);
                    LoopKit.Kick(low, at, 50d, 1d);
                    int fifth = chord.Fifth + 12;
                    if (LoopKit.Diatonic(fifth))
                        LoopKit.Square(music, at, fifth, 0.08d, 0.22d, 0.5d);
                }

                for (int step = 0; step < LoopKit.StepsPerBar; step++)
                {
                    int at = LoopKit.At(bar, step, false);
                    int tone = Cell(chord, step);
                    double accent = step % 4 == 0 ? 1d : 0.62d;
                    if (LoopKit.Diatonic(tone))
                    {
                        LoopKit.Acid(low, at, tone, 0.07d, acidVel, accent);
                        Voice(low, at, tone + 12, 0.05d, 0.16d);
                    }

                    double hat = step % 4 == 2 ? 0.6d : 0.34d;
                    LoopKit.Hat(perc, at, hat, false, 8000);
                }

                LoopKit.Clap(perc, LoopKit.At(bar, 4, false), 0.7d, 8400);
                LoopKit.Clap(perc, LoopKit.At(bar, 12, false), 0.7d, 8400);
            }
        }

        private static int Cell(LoopKit.Chord chord, int step)
        {
            switch (step & 7)
            {
                case 2:
                case 5:
                    return chord.Third;
                case 3:
                case 4:
                case 7:
                    return chord.Fifth;
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

                double cutoff = 5400d + 2200d * Math.Exp(-t * 26d);
                double dt = 1d / LoopKit.SampleRate;
                double rc = 1d / (2d * Math.PI * cutoff);
                double alpha = dt / (rc + dt);
                lowpass += alpha * (saw - lowpass);
                double fifth = Math.Sin(2d * Math.PI * freq * 5d * t) * Math.Exp(-t * 42d);
                double bright = lowpass * 0.72d + fifth * 0.85d;
                double attack = t < 0.002d ? t / 0.002d : 1d;
                double remain = duration - t;
                double release = remain >= 0.008d ? 1d : remain <= 0d ? 0d : remain / 0.008d;
                buffer[i] += Math.Tanh(bright) * attack * release * velocity;
            }
        }
    }
}
