namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// strobe_grid. Even hardgroove: kick, hats and the hook never leave the grid.
    /// 120 BPM, 8 bars, natural A minor. Swing stays off.
    /// </summary>
    internal static class StrobeGrid
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double bassVel = 0.48d * LoopKit.Lift(bar);

                for (int beat = 0; beat < 4; beat++)
                {
                    int downbeat = LoopKit.At(bar, beat * 4, false);
                    LoopKit.Kick(low, downbeat, 54d, 1d);
                    LoopKit.ChordStab(music, downbeat, chord, 12, 0.38d, 0);
                }

                for (int eighth = 0; eighth < 8; eighth++)
                {
                    int step = eighth * 2;
                    int midi = (eighth & 1) == 0 ? chord.Root : chord.Fifth;
                    LoopKit.Bass(low, LoopKit.At(bar, step, false), midi, 0.09d, bassVel, 0);
                }

                for (int step = 0; step < LoopKit.StepsPerBar; step++)
                {
                    double hatVel = 0.4d;
                    if ((step & 1) == 1)
                        hatVel = 0.34d;
                    else if (step == 2 || step == 6 || step == 10 || step == 14)
                        hatVel = 0.7d;
                    LoopKit.Hat(perc, LoopKit.At(bar, step, false), hatVel, false, 7700);
                }

                LoopKit.Clap(perc, LoopKit.At(bar, 4, false), 0.8d, 7700);
                LoopKit.Clap(perc, LoopKit.At(bar, 12, false), 0.8d, 7700);

                Voice(music, LoopKit.At(bar, 2, false), 0.2d);
                Voice(music, LoopKit.At(bar, 6, false), 0.2d);
                Voice(music, LoopKit.At(bar, 10, false), 0.2d);
                Voice(music, LoopKit.At(bar, 14, false), 0.2d);
            }
        }

        private static void Voice(double[] buffer, int start, double velocity)
        {
            const double duration = 0.004d;
            if (!LoopKit.Fits(start, duration))
                return;

            int count = (int)(duration * LoopKit.SampleRate);
            double previous = 0d;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double noise = LoopKit.Hash(i + 7700);
                double bright = noise - previous;
                previous = noise;
                double attack = t < 0.0004d ? t / 0.0004d : 1d;
                double remain = duration - t;
                double release = remain < 0.0008d ? remain / 0.0008d : 1d;
                if (release < 0d)
                    release = 0d;
                buffer[i] += bright * attack * release * velocity;
            }
        }
    }
}
