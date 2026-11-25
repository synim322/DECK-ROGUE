using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// warehouse_line — пыльный rolling techno, ровный.
    /// 120 BPM, 8 bars, natural A minor. Swing stays off. Wash is 1.85 s.
    /// </summary>
    internal static class WarehouseLine
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double bassVel = 0.5d * LoopKit.Lift(bar);

                for (int beat = 0; beat < 4; beat++)
                {
                    int at = LoopKit.At(bar, beat * 4, false);
                    LoopKit.Kick(low, at, 50d, 0.95d);
                    int root = chord.Root + 12;
                    int third = chord.Third + 12;
                    int fifth = chord.Fifth + 12;
                    if (LoopKit.Diatonic(root) && LoopKit.Diatonic(third) && LoopKit.Diatonic(fifth))
                        LoopKit.ChordStab(music, at, chord, 12, 0.3d, 2);
                }

                for (int eighth = 0; eighth < 8; eighth++)
                {
                    int midi = (eighth & 1) == 0 ? chord.Root : chord.Fifth;
                    if (LoopKit.Diatonic(midi))
                        LoopKit.Bass(low, LoopKit.At(bar, eighth * 2, false), midi, 0.11d, bassVel, 0);
                }

                for (int step = 0; step < LoopKit.StepsPerBar; step++)
                {
                    int at = LoopKit.At(bar, step, false);
                    bool offbeat = step == 2 || step == 6 || step == 10 || step == 14;
                    LoopKit.Hat(perc, at, offbeat ? 0.55d : 0.32d, false, 10100);
                    if (offbeat)
                        Voice(perc, at, 0.12d);
                }

                LoopKit.Clap(perc, LoopKit.At(bar, 4, false), 0.7d, 10100);
                LoopKit.Clap(perc, LoopKit.At(bar, 12, false), 0.7d, 10100);
                LoopKit.NoiseWash(music, LoopKit.At(bar, 0, false), 1.85d, 0.1d, 10100, 900d);
            }
        }

        private static void Voice(double[] buffer, int start, double velocity)
        {
            const double duration = 0.008d;
            const double cutoff = 1800d;
            if (!LoopKit.Fits(start, duration))
                return;

            int count = (int)(duration * LoopKit.SampleRate);
            double rc = 1d / (2d * Math.PI * cutoff);
            double dt = 1d / LoopKit.SampleRate;
            double alpha = dt / (rc + dt);
            double lp = 0d;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double noise = LoopKit.Hash(i + 10100);
                lp += alpha * (noise - lp);
                double attack = t < 0.0004d ? t / 0.0004d : 1d;
                double remain = duration - t;
                double release = remain >= 0.0012d ? 1d : (remain > 0d ? remain / 0.0012d : 0d);
                buffer[i] += lp * attack * release * velocity;
            }
        }
    }
}
