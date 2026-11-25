using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// night_ledger — тёмная ровная книга.
    /// </summary>
    internal static class NightLedger
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
                    LoopKit.Kick(low, at, 44d, 0.86d);
                    int bass = beat == 1 ? chord.Below : beat == 2 ? chord.Fifth : chord.Root;
                    if (LoopKit.Diatonic(bass))
                        LoopKit.Bass(low, at, bass, 0.16d, 0.5d * lift, 2);
                }

                for (int eighth = 0; eighth < 8; eighth++)
                    LoopKit.Hat(perc, LoopKit.At(bar, eighth * 2, false), 0.24d, false, 1800);

                for (int step = 0; step < LoopKit.StepsPerBar; step += 4)
                {
                    int at = LoopKit.At(bar, step, false);
                    LoopKit.Rim(perc, at, 0.4d);
                    Voice(perc, at, 0.12d);
                }

                int root = chord.Root + 12;
                int fifth = chord.Fifth + 12;
                if (LoopKit.Diatonic(root))
                    LoopKit.Square(music, LoopKit.AtEighth(bar, 0), root, 0.2d, 0.34d, 0.2d);
                if (LoopKit.Diatonic(fifth))
                    LoopKit.Square(music, LoopKit.AtEighth(bar, 3), fifth, 0.2d, 0.34d, 0.2d);
            }
        }

        private static void Voice(double[] buffer, int start, double velocity)
        {
            const double duration = 0.025d;
            const double freq = 400d;
            if (!LoopKit.Fits(start, duration))
                return;

            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                double t = n / (double)LoopKit.SampleRate;
                double attack = t < 0.0015d ? t / 0.0015d : 1d;
                double remain = duration - t;
                double release = remain >= 0.004d ? 1d : remain / 0.004d;
                double tick = Math.Sin(2d * Math.PI * freq * t);
                buffer[start + n] += tick * attack * release * velocity;
            }
        }
    }
}
