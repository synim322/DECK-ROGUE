using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// foundry_drop — brutal even anvil grid from the first bar.
    /// 120 BPM, 8 bars, natural A minor. Swing stays off.
    /// </summary>
    internal static class FoundryDrop
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double lift = LoopKit.Lift(bar);
                int root = chord.Root;
                int octave = root + 12;
                int third = chord.Third + 12;
                int fifth = chord.Fifth + 12;

                for (int beat = 0; beat < 4; beat++)
                {
                    int at = LoopKit.At(bar, beat * 4, false);
                    LoopKit.Kick(low, at, 40d, 1d);
                    if (LoopKit.Diatonic(root))
                        LoopKit.Bass(low, at, root, 0.12d, 0.58d * lift, 0);
                    if (LoopKit.Diatonic(octave))
                        LoopKit.Bass(low, at, octave, 0.12d, 0.3d, 0);
                    if (LoopKit.Diatonic(octave) && LoopKit.Diatonic(third) && LoopKit.Diatonic(fifth))
                        LoopKit.ChordStab(music, at, chord, 12, 0.4d, 2);
                    Voice(perc, at, 0.2d);
                }

                for (int step = 0; step < LoopKit.StepsPerBar; step++)
                {
                    int at = LoopKit.At(bar, step, false);
                    bool offbeat = step == 2 || step == 6 || step == 10 || step == 14;
                    LoopKit.Hat(perc, at, offbeat ? 0.66d : 0.36d, false, 10700);
                }

                LoopKit.Clap(perc, LoopKit.At(bar, 4, false), 0.85d, 10700);
                LoopKit.Clap(perc, LoopKit.At(bar, 12, false), 0.85d, 10700);
                LoopKit.Tom(perc, LoopKit.At(bar, 14, false), 90d, 0.4d);
            }
        }

        private static void Voice(double[] buffer, int start, double velocity)
        {
            const double duration = 0.06d;
            const double freq = 180d;
            if (!LoopKit.Fits(start, duration))
                return;

            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double sine = Math.Sin(2d * Math.PI * freq * t);
                double noise = LoopKit.Hash(i + 10700);
                double attack = t < 0.0015d ? t / 0.0015d : 1d;
                double remain = duration - t;
                double release = remain >= 0.008d ? 1d : (remain > 0d ? remain / 0.008d : 0d);
                double env = Math.Exp(-t * 42d) * attack * release;
                buffer[i] += (sine * 0.72d + noise * 0.28d) * env * velocity;
            }
        }
    }
}
