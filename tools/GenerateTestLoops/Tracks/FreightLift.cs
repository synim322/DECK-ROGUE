using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// freight_lift. Heavy even industrial lift. The two-bar figure repeats.
    /// 120 BPM, 8 bars, natural A minor. Swing stays off.
    /// </summary>
    internal static class FreightLift
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
                    LoopKit.Kick(low, at, 44d, 1d);
                    int root = chord.Root;
                    int octave = root + 12;
                    if (LoopKit.Diatonic(root))
                        LoopKit.Bass(low, at, root, 0.14d, 0.5d * lift, 0);
                    if (LoopKit.Diatonic(octave))
                        LoopKit.Bass(low, at, octave, 0.14d, 0.28d * lift, 0);
                    Voice(perc, at, 0.16d);
                }

                for (int eighth = 0; eighth < 8; eighth++)
                    LoopKit.Hat(perc, LoopKit.AtEighth(bar, eighth), 0.42d, false, 11400);

                LoopKit.Clap(perc, LoopKit.At(bar, 4, false), 0.7d, 11400);
                LoopKit.Clap(perc, LoopKit.At(bar, 12, false), 0.7d, 11400);

                for (int place = 0; place < 4; place++)
                {
                    int midi = Figure(chord, bar, place);
                    if (!LoopKit.Diatonic(midi))
                        continue;

                    LoopKit.Square(music, LoopKit.AtEighth(bar, place * 2), midi, 0.16d, 0.32d, 0.4d);
                }
            }

            Guard(low);
            Guard(perc);
            Guard(music);
        }

        private static int Figure(LoopKit.Chord chord, int bar, int place)
        {
            if ((bar & 1) == 0)
            {
                if (place == 0)
                    return chord.Root + 12;
                if (place == 1)
                    return chord.Third + 12;
                if (place == 2)
                    return chord.Fifth + 12;

                int high = chord.Fifth + 24;
                if (LoopKit.Diatonic(high))
                    return high;
                return chord.Fifth + 12;
            }

            if (place == 0 || place == 1)
                return chord.Fifth + 12;
            if (place == 2)
                return chord.Third + 12;
            return chord.Root + 12;
        }

        private static void Voice(double[] buffer, int start, double velocity)
        {
            const double duration = 0.03d;
            const double freq = 200d;
            if (!LoopKit.Fits(start, duration))
                return;

            int count = (int)(duration * LoopKit.SampleRate);
            double previous = 0d;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double noise = LoopKit.Hash(i + 11400);
                double metal = noise - previous;
                previous = noise;
                double tone = Math.Sin(2d * Math.PI * freq * t);
                double attack = t < 0.001d ? t / 0.001d : 1d;
                double env = Math.Exp(-t * 110d);
                double remain = duration - t;
                double release = remain >= 0.004d ? 1d : (remain <= 0d ? 0d : remain / 0.004d);
                buffer[i] += Math.Tanh(tone + metal) * attack * env * release * velocity;
            }
        }

        private static void Guard(double[] buffer)
        {
            double peak = 0d;
            for (int i = 0; i < buffer.Length; i++)
            {
                double sample = buffer[i];
                if (double.IsNaN(sample) || double.IsInfinity(sample))
                    throw new InvalidOperationException("freight_lift peak");
                double abs = Math.Abs(sample);
                if (abs > peak)
                    peak = abs;
            }

            if (peak <= 0d || peak >= 12d)
                throw new InvalidOperationException("freight_lift peak");
        }
    }
}
