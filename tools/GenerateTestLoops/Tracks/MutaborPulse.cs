namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// mutabor_pulse. Dense even warehouse: kick, hats, bass cell and chord stabs from bar 1.
    /// 120 BPM, 8 bars, natural A minor. Swing stays off. No G#.
    /// </summary>
    internal static class MutaborPulse
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double bassVel = 0.4d * LoopKit.Lift(bar);

                for (int step = 0; step < LoopKit.StepsPerBar; step++)
                {
                    int at = LoopKit.At(bar, step, false);
                    int slot = step & 3;
                    int midi = chord.Root;
                    if (slot == 2)
                        midi = chord.Third;
                    else if (slot == 1 || slot == 3)
                        midi = chord.Fifth;

                    if ((step & 3) == 0)
                        LoopKit.Kick(low, at, 50d, 1d);

                    if (LoopKit.Diatonic(midi))
                        LoopKit.Bass(low, at, midi, 0.06d, bassVel, 0);

                    bool offbeat = slot == 2;
                    LoopKit.Hat(perc, at, offbeat ? 0.5d : 0.34d, false, 10600);
                    Voice(perc, at, 0.1d);

                    if ((step & 1) == 0
                        && LoopKit.Diatonic(chord.Root + 12)
                        && LoopKit.Diatonic(chord.Third + 12)
                        && LoopKit.Diatonic(chord.Fifth + 12))
                        LoopKit.ChordStab(music, at, chord, 12, 0.32d, 0);
                }

                LoopKit.Clap(perc, LoopKit.At(bar, 4, false), 0.8d, 10600);
                LoopKit.Clap(perc, LoopKit.At(bar, 12, false), 0.8d, 10600);
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
                double noise = LoopKit.Hash(i + 10600);
                double tick = noise - previous;
                previous = noise;
                double attack = t < 0.0004d ? t / 0.0004d : 1d;
                double remain = duration - t;
                double release = remain < 0.0008d ? remain / 0.0008d : 1d;
                if (release < 0d)
                    release = 0d;
                buffer[i] += tick * attack * release * velocity;
            }
        }
    }
}
