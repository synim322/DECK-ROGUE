using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// afterglow_key — warm even keys, a steady pulse, walking bass, FM piano.
    /// 120 BPM, 8 bars, natural A minor. Swing stays off.
    /// </summary>
    internal static class AfterglowKey
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double lift = LoopKit.Lift(bar);

                for (int beat = 0; beat < 4; beat++)
                {
                    int at = LoopKit.At(bar, beat * 4, false);
                    LoopKit.Kick(low, at, 46d, 0.76d);

                    int bass = beat == 0 ? chord.Root : beat == 1 ? chord.Third : beat == 2 ? chord.Fifth : chord.Below;
                    if (InKey(bass))
                        LoopKit.Bass(low, at, bass, 0.2d, 0.48d * lift, 2);

                    int key = beat == 0 ? chord.Root + 12 : beat == 2 ? chord.Third + 12 : chord.Fifth + 12;
                    if (!InKey(key))
                        continue;

                    LoopKit.Fm(music, at, key, 0.3d, 0.4d, 1d, 0.85d);
                    Voice(music, at, 0.12d);
                }

                LoopKit.Rim(perc, LoopKit.At(bar, 4, false), 0.55d);
                LoopKit.Rim(perc, LoopKit.At(bar, 12, false), 0.55d);

                for (int step = 0; step < LoopKit.StepsPerBar; step += 2)
                    LoopKit.Hat(perc, LoopKit.At(bar, step, false), 0.2d, false, 10800);
            }
        }

        private static bool InKey(int midi)
        {
            int pc = midi % 12;
            if (pc < 0)
                pc += 12;
            return pc != 8 && LoopKit.Diatonic(midi);
        }

        private static void Voice(double[] buffer, int start, double velocity)
        {
            const double duration = 0.008d;
            if (!LoopKit.Fits(start, duration))
                return;

            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double noise = LoopKit.Hash(i + 10800);
                double env = Math.Exp(-t * 520d);
                buffer[i] += noise * env * velocity;
            }
        }
    }
}
