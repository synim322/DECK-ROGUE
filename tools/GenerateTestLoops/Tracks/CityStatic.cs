using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// City Static. Dry edgy groove and a flat radio bed. 120 BPM, eight bars, A minor.
    /// </summary>
    internal static class CityStatic
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double bassVel = 0.58d * LoopKit.Lift(bar);

                for (int beat = 0; beat < 4; beat++)
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 58d, 0.92d);

                Bass(low, bar, 0, chord.Root, bassVel);
                Bass(low, bar, 3, chord.Root, bassVel);
                Bass(low, bar, 8, chord.Fifth, bassVel);
                Bass(low, bar, 11, chord.Third, bassVel);

                LoopKit.Rim(perc, LoopKit.At(bar, 4, false), 0.7d);
                LoopKit.Rim(perc, LoopKit.At(bar, 12, false), 0.7d);
                for (int step = 1; step < LoopKit.StepsPerBar; step += 2)
                    LoopKit.Hat(perc, LoopKit.At(bar, step, false), 0.4d, false, 6100);

                LoopKit.NoiseWash(music, LoopKit.At(bar, 0, false), 1.85d, 0.12d, 77, 1600d);
                Riff(music, bar, 0, chord.Fifth + 12, chord.Root + 24);
                Riff(music, bar, 2, chord.Root + 12, chord.Root + 24);
                Riff(music, bar, 4, chord.Third + 12, chord.Root + 24);
                Riff(music, bar, 6, chord.Fifth + 12, chord.Root + 24);
            }
        }

        private static void Bass(double[] buffer, int bar, int step, int midi, double velocity)
        {
            if (!LoopKit.Diatonic(midi))
                return;

            LoopKit.Bass(buffer, LoopKit.At(bar, step, false), midi, 0.12d, velocity, 1);
        }

        private static void Riff(double[] music, int bar, int eighth, int pluckMidi, int squareMidi)
        {
            int at = LoopKit.AtEighth(bar, eighth);
            if (LoopKit.Diatonic(pluckMidi))
                LoopKit.Pluck(music, at, pluckMidi, 0.14d, 0.4d, 0.8d);
            Voice(music, at, squareMidi, 0.16d, 0.2d);
        }

        private static void Voice(double[] buffer, int start, int midi, double duration, double velocity)
        {
            if (!LoopKit.Diatonic(midi) || !LoopKit.Fits(start, duration))
                return;

            double freq = LoopKit.Hz(midi);
            int count = (int)(duration * LoopKit.SampleRate);
            int hold = LoopKit.SampleRate / 180;
            if (hold < 1)
                hold = 1;

            double held = 0d;
            const double releaseSeconds = 0.012d;
            for (int n = 0; n < count; n++)
            {
                if (n % hold == 0)
                {
                    double cycle = freq * n / LoopKit.SampleRate;
                    cycle -= Math.Floor(cycle);
                    held = cycle < 0.5d ? 0.7d : -0.7d;
                }

                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double attack = t < 0.003d ? t / 0.003d : 1d;
                double remain = duration - t;
                double release = remain >= releaseSeconds ? 1d : (remain > 0d ? remain / releaseSeconds : 0d);
                buffer[i] += held * attack * release * velocity;
            }
        }
    }
}
