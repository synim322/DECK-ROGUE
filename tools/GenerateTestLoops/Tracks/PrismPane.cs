using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// prism_pane — glass strikes on a hard, even groove. Same grid every bar.
    /// </summary>
    internal static class PrismPane
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double lift = LoopKit.Lift(bar);
                int root = chord.Root;
                bool bassNote = LoopKit.Diatonic(root);
                bool stabNotes = LoopKit.Diatonic(root + 24)
                    && LoopKit.Diatonic(chord.Third + 24)
                    && LoopKit.Diatonic(chord.Fifth + 24);

                for (int beat = 0; beat < 4; beat++)
                {
                    int at = LoopKit.At(bar, beat * 4, false);
                    LoopKit.Kick(low, at, 56d, 0.95d);
                    if (bassNote)
                        LoopKit.Bass(low, at, root, 0.12d, 0.5d * lift, 1);
                }

                for (int step = 0; step < LoopKit.StepsPerBar; step++)
                {
                    bool offbeat = step == 2 || step == 6 || step == 10 || step == 14;
                    double hat = offbeat ? 0.62d : 0.36d;
                    LoopKit.Hat(perc, LoopKit.At(bar, step, false), hat, false, 6400);
                }

                if (!stabNotes)
                    continue;

                for (int eighth = 0; eighth < 4; eighth++)
                {
                    int at = LoopKit.At(bar, 2 + eighth * 4, false);
                    LoopKit.ChordStab(music, at, chord, 24, 0.36d, 1);
                    Voice(music, at, LoopKit.Hz(root + 24), 0.22d);
                }
            }
        }

        private static void Voice(double[] buffer, int start, double freq, double velocity)
        {
            const double duration = 0.18d;
            if (!LoopKit.Fits(start, duration))
                return;

            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double glass = Math.Sin(2d * Math.PI * freq * t)
                    + Math.Sin(2d * Math.PI * freq * 2.41d * t)
                    + Math.Sin(2d * Math.PI * freq * 3.78d * t);
                double env = Math.Exp(-t * 22d);
                double remain = duration - t;
                if (remain < 0.01d)
                    env *= remain / 0.01d;
                buffer[i] += glass * env * velocity;
            }
        }
    }
}
