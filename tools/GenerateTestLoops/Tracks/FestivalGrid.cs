using System;

namespace GenerateTestLoops.Tracks
{
    internal static class FestivalGrid
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double lift = LoopKit.Lift(bar);
                int lead = chord.Fifth + 12;

                for (int step = 0; step < LoopKit.StepsPerBar; step++)
                {
                    int at = LoopKit.At(bar, step, false);
                    bool beat = (step & 3) == 0;
                    bool offbeat = step == 2 || step == 6 || step == 10 || step == 14;

                    if (beat)
                    {
                        LoopKit.Kick(low, at, 48d, 1d);
                        LoopKit.Bass(low, at, chord.Root, 0.1d, 0.55d * lift, 0);
                        LoopKit.Bass(low, at, chord.Root + 12, 0.1d, 0.35d, 0);
                        LoopKit.ChordStab(music, at, chord, 12, 0.4d, 0);
                    }

                    LoopKit.Hat(perc, at, offbeat ? 0.66d : 0.38d, false, 9900);
                    if (offbeat)
                        Voice(music, at, lead, 0.1d, 0.22d);
                }

                LoopKit.Clap(perc, LoopKit.At(bar, 4, false), 0.85d, 9917);
                LoopKit.Clap(perc, LoopKit.At(bar, 12, false), 0.85d, 9917);
            }
        }

        private static void Voice(double[] buffer, int start, int midi, double duration, double velocity)
        {
            if (!LoopKit.Diatonic(midi) || !LoopKit.Fits(start, duration))
                return;

            double freq = LoopKit.Hz(midi);
            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double wave = 0d;
                for (int h = 1; h <= 5; h++)
                    wave += Math.Sin(2d * Math.PI * freq * h * t) / h;
                double attack = t < 0.003d ? t / 0.003d : 1d;
                double remain = duration - t;
                double release = remain >= 0.012d ? 1d : (remain <= 0d ? 0d : remain / 0.012d);
                buffer[i] += wave * (2d / Math.PI) * attack * release * velocity;
            }
        }
    }
}
