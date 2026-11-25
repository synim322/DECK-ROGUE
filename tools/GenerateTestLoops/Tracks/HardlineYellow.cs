using System;

namespace GenerateTestLoops.Tracks
{
    internal static class HardlineYellow
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double bassVel = 0.5d * LoopKit.Lift(bar);
                int lead = chord.Fifth + 24;

                for (int step = 0; step < LoopKit.StepsPerBar; step++)
                {
                    int at = LoopKit.At(bar, step, false);
                    bool beat = (step & 3) == 0;
                    bool eighth = (step & 1) == 0;
                    bool offbeat = step == 2 || step == 6 || step == 10 || step == 14;

                    if (beat)
                    {
                        LoopKit.Kick(low, at, 56d, 1d);
                        if (LoopKit.Diatonic(chord.Root + 12)
                            && LoopKit.Diatonic(chord.Third + 12)
                            && LoopKit.Diatonic(chord.Fifth + 12))
                            LoopKit.ChordStab(music, at, chord, 12, 0.42d, 1);
                    }

                    if (eighth && LoopKit.Diatonic(chord.Root))
                        LoopKit.Bass(low, at, chord.Root, 0.1d, bassVel, 0);

                    if (eighth)
                        Voice(music, at, lead, 0.1d, 0.18d);

                    LoopKit.Hat(perc, at, offbeat ? 0.62d : 0.4d, false, 8400);
                }

                LoopKit.Clap(perc, LoopKit.At(bar, 4, false), 0.8d, 8500);
                LoopKit.Clap(perc, LoopKit.At(bar, 12, false), 0.8d, 8500);
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
                double saw = 0d;
                for (int h = 1; h <= 6; h++)
                    saw += Math.Sin(2d * Math.PI * freq * h * t) / h;
                double attack = t < 0.003d ? t / 0.003d : 1d;
                double remain = duration - t;
                double release = remain >= 0.012d ? 1d : (remain <= 0d ? 0d : remain / 0.012d);
                buffer[i] += saw * (2d / Math.PI) * attack * release * velocity;
            }
        }
    }
}
