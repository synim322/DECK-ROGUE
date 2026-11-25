using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// favor_bass. Even heavy bass hook: the same six notes every bar.
    /// 120 BPM, 8 bars, natural A minor. Offbeat hats leave the bass in front.
    /// </summary>
    internal static class FavorBass
    {
        private const double NoteDuration = 0.13d;

        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double velocity = 0.66d * LoopKit.Lift(bar);

                for (int beat = 0; beat < 4; beat++)
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 48d, 0.95d);

                Hook(low, bar, 0, chord.Root, velocity);
                Hook(low, bar, 3, chord.Root, velocity);
                Hook(low, bar, 6, chord.Third, velocity);
                Hook(low, bar, 8, chord.Fifth, velocity);
                Hook(low, bar, 11, chord.Fifth, velocity);
                Hook(low, bar, 14, chord.Below, velocity);

                for (int step = 2; step < LoopKit.StepsPerBar; step += 4)
                    LoopKit.Hat(perc, LoopKit.At(bar, step, false), 0.4d, true, 4800);

                LoopKit.ChordStab(music, LoopKit.At(bar, 4, false), chord, 12, 0.28d, 2);
            }
        }

        private static void Hook(double[] low, int bar, int step, int midi, double velocity)
        {
            if (!LoopKit.Diatonic(midi))
                return;

            int at = LoopKit.At(bar, step, false);
            LoopKit.Bass(low, at, midi, NoteDuration, velocity, 0);
            int voice = midi - 12;
            if (voice < 28)
                voice = midi;
            Voice(low, at, voice, NoteDuration, 0.3d);
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
                double sine = Math.Sin(2d * Math.PI * freq * t);
                double attack = t < 0.004d ? t / 0.004d : 1d;
                double remain = duration - t;
                double release = remain >= 0.012d ? 1d : (remain <= 0d ? 0d : remain / 0.012d);
                buffer[i] += Math.Tanh(sine) * attack * release * velocity;
            }
        }
    }
}
