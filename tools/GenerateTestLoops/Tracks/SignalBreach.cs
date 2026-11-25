using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// signal_breach — critical even boss peak, full from the first bar.
    /// 120 BPM, 8 bars, natural A minor. Swing stays off. Wash is 1.85 s.
    /// </summary>
    internal static class SignalBreach
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double bassVel = 0.55d * LoopKit.Lift(bar);
                int beep = chord.Fifth + 24;

                for (int step = 0; step < LoopKit.StepsPerBar; step++)
                {
                    int at = LoopKit.At(bar, step, false);
                    bool beat = (step & 3) == 0;
                    bool offbeat = step == 2 || step == 6 || step == 10 || step == 14;

                    if (beat)
                    {
                        LoopKit.Kick(low, at, 40d, 1d);
                        if (LoopKit.Diatonic(chord.Root))
                            LoopKit.Bass(low, at, chord.Root, 0.1d, bassVel, 0);
                        if (LoopKit.Diatonic(chord.Root + 12)
                            && LoopKit.Diatonic(chord.Third + 12)
                            && LoopKit.Diatonic(chord.Fifth + 12))
                            LoopKit.ChordStab(music, at, chord, 12, 0.4d, 1);
                    }

                    if (offbeat)
                        Voice(music, at, beep, 0.16d);

                    LoopKit.Hat(perc, at, offbeat ? 0.7d : 0.4d, false, 11000);
                }

                LoopKit.Clap(perc, LoopKit.At(bar, 4, false), 0.88d, 11000);
                LoopKit.Clap(perc, LoopKit.At(bar, 12, false), 0.88d, 11000);
                LoopKit.NoiseWash(music, LoopKit.At(bar, 0, false), 1.85d, 0.12d, 11000, 2200d);
            }
        }

        private static void Voice(double[] buffer, int start, int midi, double velocity)
        {
            const double duration = 0.04d;
            if (!LoopKit.Diatonic(midi) || !LoopKit.Fits(start, duration))
                return;

            double freq = LoopKit.Hz(midi);
            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double wave = Math.Sin(2d * Math.PI * freq * t) + Math.Sin(2d * Math.PI * freq * 3d * t) / 3d;
                double attack = t < 0.002d ? t / 0.002d : 1d;
                double remain = duration - t;
                double release = remain >= 0.006d ? 1d : (remain <= 0d ? 0d : remain / 0.006d);
                buffer[i] += wave * attack * release * velocity;
            }
        }
    }
}
