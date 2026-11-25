using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// mirror_shunt. Prismatic even electro: kick every beat, bright hats,
    /// a pluck hook on the eighths, and a shorter fifth-above mirror 15 cents sharp.
    /// 120 BPM, 8 bars, natural A minor. Swing stays off. No G#.
    /// </summary>
    internal static class MirrorShunt
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double bassVel = 0.48d * LoopKit.Lift(bar);

                for (int beat = 0; beat < 4; beat++)
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 56d, 0.92d);

                if (InKey(chord.Root))
                    LoopKit.Bass(low, LoopKit.At(bar, 0, false), chord.Root, 0.12d, bassVel, 0);
                if (InKey(chord.Fifth))
                    LoopKit.Bass(low, LoopKit.At(bar, 10, false), chord.Fifth, 0.12d, bassVel, 0);

                for (int step = 0; step < LoopKit.StepsPerBar; step += 2)
                    LoopKit.Hat(perc, LoopKit.At(bar, step, false), 0.48d, false, 11800);

                LoopKit.Hat(perc, LoopKit.At(bar, 14, false), 0.4d, true, 11800);

                Hook(music, bar, 0, chord.Fifth + 12);
                Hook(music, bar, 2, chord.Root + 24);
                Hook(music, bar, 4, chord.Third + 12);
                Hook(music, bar, 6, chord.Fifth + 12);
            }

            Guard(low);
            Guard(perc);
            Guard(music);
        }

        private static void Hook(double[] music, int bar, int eighth, int midi)
        {
            if (!InKey(midi))
                return;

            int at = LoopKit.AtEighth(bar, eighth);
            LoopKit.Pluck(music, at, midi, 0.14d, 0.36d, 0.8d);
            int mirror = midi + 7;
            if (!InKey(mirror))
                mirror = midi;
            Voice(music, at, mirror, 0.1d, 0.14d);
        }

        private static void Voice(double[] buffer, int start, int midi, double duration, double velocity)
        {
            if (!InKey(midi) || !LoopKit.Fits(start, duration))
                return;

            double freq = LoopKit.Hz(midi);
            double sharp = freq * Math.Pow(2d, 15d / 1200d);
            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double pair = Math.Sin(2d * Math.PI * freq * t) + Math.Sin(2d * Math.PI * sharp * t);
                double attack = t < 0.003d ? t / 0.003d : 1d;
                double remain = duration - t;
                double release = remain >= 0.012d ? 1d : (remain <= 0d ? 0d : remain / 0.012d);
                buffer[i] += pair * 0.5d * attack * release * velocity;
            }
        }

        private static bool InKey(int midi)
        {
            int pc = midi % 12;
            if (pc < 0)
                pc += 12;
            return pc != 8 && LoopKit.Diatonic(midi);
        }

        private static void Guard(double[] buffer)
        {
            double peak = 0d;
            for (int i = 0; i < buffer.Length; i++)
            {
                double sample = buffer[i];
                if (double.IsNaN(sample) || double.IsInfinity(sample))
                    throw new InvalidOperationException("mirror_shunt peak");
                double abs = Math.Abs(sample);
                if (abs > peak)
                    peak = abs;
            }

            if (peak <= 0d || peak >= 12d)
                throw new InvalidOperationException("mirror_shunt peak");
        }
    }
}
