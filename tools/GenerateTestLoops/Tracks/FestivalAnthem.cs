using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// festival_anthem. Even anthem, dense from the first bar.
    /// 120 BPM, 8 bars, natural A minor. Swing stays off.
    /// </summary>
    internal static class FestivalAnthem
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
                    LoopKit.Kick(low, at, 46d, 1d);
                    if (LoopKit.Diatonic(chord.Root))
                    {
                        LoopKit.Bass(low, at, chord.Root, 0.12d, 0.58d * lift, 0);
                        LoopKit.Bass(low, at, chord.Root + 12, 0.12d, 0.28d, 0);
                    }

                    LoopKit.ChordStab(music, at, chord, 12, 0.36d, 0);
                }

                for (int step = 0; step < LoopKit.StepsPerBar; step++)
                {
                    bool offbeat = (step & 3) == 2;
                    double hat = offbeat ? 0.64d : 0.36d;
                    LoopKit.Hat(perc, LoopKit.At(bar, step, false), hat, false, 10001);
                }

                LoopKit.Clap(perc, LoopKit.At(bar, 4, false), 0.88d, 10001);
                LoopKit.Clap(perc, LoopKit.At(bar, 12, false), 0.88d, 10001);

                for (int place = 0; place < 4; place++)
                {
                    int midi = Lead(chord, bar, place);
                    if (!LoopKit.Diatonic(midi))
                        continue;

                    int at = LoopKit.AtEighth(bar, place * 2);
                    LoopKit.Square(music, at, midi, 0.18d, 0.4d * lift, 0.45d);
                    Voice(music, at, midi - 12, 0.16d, 0.16d);
                }
            }
        }

        private static int Lead(LoopKit.Chord chord, int bar, int place)
        {
            int midi;
            if ((bar & 1) == 0)
            {
                if (place == 1)
                    midi = chord.Root + 24;
                else if (place == 2)
                    midi = chord.Third + 12;
                else
                    midi = chord.Fifth + 12;
            }
            else if (place == 1)
                midi = chord.Third + 12;
            else if (place == 2)
                midi = chord.Root + 12;
            else
                midi = chord.Fifth + 12;

            if (!LoopKit.Diatonic(midi))
                midi = chord.Fifth + 12;
            return midi;
        }

        private static void Voice(double[] buffer, int start, int midi, double duration, double velocity)
        {
            if (!LoopKit.Diatonic(midi) || !LoopKit.Fits(start, duration))
                return;

            double freq = LoopKit.Hz(midi);
            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double saw = 0d;
                for (int h = 1; h <= 5; h++)
                    saw += Math.Sin(2d * Math.PI * freq * h * t) / h;
                double attack = t < 0.004d ? t / 0.004d : 1d;
                double remain = duration - t;
                double release = remain >= 0.012d ? 1d : (remain <= 0d ? 0d : remain / 0.012d);
                buffer[i] += saw * attack * release * velocity;
            }
        }
    }
}
