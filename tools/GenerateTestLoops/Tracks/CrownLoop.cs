using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// crown_loop — a crown of fifths. The two-bar phrase repeats at bars 2, 4 and 6.
    /// </summary>
    internal static class CrownLoop
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double bassVel = 0.55d * LoopKit.Lift(bar);
                bool even = (bar & 1) == 0;

                for (int beat = 0; beat < 4; beat++)
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 46d, 1d);

                if (LoopKit.Diatonic(chord.Root))
                    LoopKit.Bass(low, LoopKit.At(bar, 0, false), chord.Root, 0.16d, bassVel, 0);
                if (LoopKit.Diatonic(chord.Fifth))
                    LoopKit.Bass(low, LoopKit.At(bar, 8, false), chord.Fifth, 0.16d, bassVel, 0);

                for (int eighth = 0; eighth < 8; eighth++)
                    LoopKit.Hat(perc, LoopKit.AtEighth(bar, eighth), 0.48d, false, 9100);

                LoopKit.Clap(perc, LoopKit.At(bar, 4, false), 0.7d, 9100);
                LoopKit.Clap(perc, LoopKit.At(bar, 12, false), 0.7d, 9100);

                if (even)
                {
                    Bell(music, bar, 0, chord.Fifth + 24);
                    Bell(music, bar, 2, chord.Root + 24);
                    Bell(music, bar, 4, chord.Third + 24);
                    Bell(music, bar, 6, chord.Fifth + 24);
                }
                else
                {
                    Bell(music, bar, 0, chord.Root + 24);
                    Bell(music, bar, 4, chord.Fifth + 24);
                }

                int fifth = chord.Fifth + 12;
                int root = chord.Root + 24;
                if (LoopKit.Diatonic(fifth) && LoopKit.Diatonic(root))
                    Voice(music, LoopKit.At(bar, 0, false), fifth, root, 0.25d);
            }
        }

        private static void Bell(double[] buffer, int bar, int eighth, int midi)
        {
            if (!LoopKit.Diatonic(midi))
                return;
            LoopKit.Bell(buffer, LoopKit.AtEighth(bar, eighth), midi, 0.4d, 3.0d);
        }

        private static void Voice(double[] buffer, int start, int fifthMidi, int rootMidi, double velocity)
        {
            const double duration = 0.4d;
            if (!LoopKit.Fits(start, duration))
                return;

            double fifthHz = LoopKit.Hz(fifthMidi);
            double rootHz = LoopKit.Hz(rootMidi);
            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                double t = n / (double)LoopKit.SampleRate;
                double fifth = Math.Sin(2d * Math.PI * fifthHz * t);
                double root = Math.Sin(2d * Math.PI * rootHz * t);
                double attack = t < 0.004d ? t / 0.004d : 1d;
                double decay = Math.Exp(-t * 2.8d);
                double remain = duration - t;
                double release = remain < 0.018d ? remain / 0.018d : 1d;
                buffer[start + n] += (fifth + root) * 0.5d * attack * decay * release * velocity;
            }
        }
    }
}
