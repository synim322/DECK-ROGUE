using System;

namespace GenerateTestLoops.Tracks
{
    // chrome_wake — chrome bell, even bright grid, same four-note motif every bar.
    internal static class ChromeWake
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double bassVel = 0.48d * LoopKit.Lift(bar);

                for (int beat = 0; beat < 4; beat++)
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 60d, 0.92d);

                LoopKit.Bass(low, LoopKit.At(bar, 0, false), Tone(chord.Root), 0.12d, bassVel, 1);
                LoopKit.Bass(low, LoopKit.At(bar, 10, false), Tone(chord.Fifth), 0.12d, bassVel, 1);

                for (int step = 0; step < LoopKit.StepsPerBar; step += 2)
                    LoopKit.Hat(perc, LoopKit.At(bar, step, false), 0.5d, false, 6500);

                LoopKit.Hat(perc, LoopKit.At(bar, 14, false), 0.45d, true, 6500);

                Place(music, bar, 0, chord.Fifth + 24);
                Place(music, bar, 2, chord.Root + 24);
                Place(music, bar, 4, chord.Third + 24);
                Place(music, bar, 6, chord.Fifth + 24);
            }

            Guard(low);
            Guard(perc);
            Guard(music);
        }

        private static void Place(double[] music, int bar, int eighth, int midi)
        {
            int note = Tone(midi);
            int at = LoopKit.AtEighth(bar, eighth);
            LoopKit.Bell(music, at, note, 0.4d, 4.2d);
            Voice(music, at, note, 0.16d);
        }

        private static void Voice(double[] buffer, int start, int midi, double velocity)
        {
            const double duration = 0.12d;
            const double index = 0.4d;
            if (!LoopKit.Fits(start, duration))
                return;

            double carrier = LoopKit.Hz(midi);
            double modulator = carrier * 11d;
            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double mod = Math.Sin(2d * Math.PI * modulator * t) * index * Math.Exp(-t * 24d);
                double wave = Math.Sin(2d * Math.PI * carrier * t + mod);
                double attack = t < 0.002d ? t / 0.002d : 1d;
                double remain = duration - t;
                double release = remain >= 0.012d ? 1d : (remain <= 0d ? 0d : remain / 0.012d);
                buffer[i] += wave * attack * release * velocity;
            }
        }

        private static int Tone(int midi)
        {
            if (!LoopKit.Diatonic(midi))
                throw new InvalidOperationException("chrome_wake left A minor");
            return midi;
        }

        private static void Guard(double[] buffer)
        {
            double peak = 0d;
            for (int i = 0; i < buffer.Length; i++)
            {
                double sample = buffer[i];
                if (double.IsNaN(sample) || double.IsInfinity(sample))
                    throw new InvalidOperationException("chrome_wake peak");
                double abs = Math.Abs(sample);
                if (abs > peak)
                    peak = abs;
            }

            if (peak <= 0d || peak >= 12d)
                throw new InvalidOperationException("chrome_wake peak");
        }
    }
}
