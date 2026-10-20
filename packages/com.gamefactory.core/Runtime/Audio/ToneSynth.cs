using System;

namespace GameFactory.Core.Audio
{
    public enum Wave { Sine, Square, Triangle, Noise }

    /// <summary>
    /// Genera sonidos simples por codigo (barridos de tono con envolvente) para prototipos,
    /// sin archivos de audio. La parte matematica es pura C# (testeable); CreateClip esta en ToneSynth.Unity.cs.
    /// </summary>
    public static partial class ToneSynth
    {
        public const int SampleRate = 44100;

        /// <summary>
        /// Muestras mono en [-volume, volume]. El tono va de startHz a endHz; ataque corto y caida lineal hasta 0
        /// (sin "clicks" al inicio ni al final).
        /// </summary>
        public static float[] Generate(float startHz, float endHz, float duration, Wave wave = Wave.Sine,
                                       float volume = 0.5f, int sampleRate = SampleRate, int seed = 1)
        {
            if (duration <= 0f) throw new ArgumentOutOfRangeException(nameof(duration));
            volume = Math.Max(0f, Math.Min(1f, volume));
            int n = Math.Max(2, (int)(duration * sampleRate));
            int attack = Math.Max(1, Math.Min(n / 4, (int)(0.005f * sampleRate)));
            var data = new float[n];
            var rng = new Random(seed);
            double phase = 0.0;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                double hz = startHz + (endHz - startHz) * t;
                phase += 2.0 * Math.PI * hz / sampleRate;
                double s = Math.Sin(phase);
                switch (wave)
                {
                    case Wave.Square: s = (s >= 0 ? 1.0 : -1.0) * 0.6; break;      // mas suave que una cuadrada pura
                    case Wave.Triangle: s = 2.0 / Math.PI * Math.Asin(s); break;
                    case Wave.Noise: s = rng.NextDouble() * 2.0 - 1.0; break;
                }
                float env = i < attack ? i / (float)attack : 1f - (i - attack) / (float)(n - 1 - attack);
                data[i] = (float)(s * env * volume);
            }
            data[n - 1] = 0f;
            return data;
        }
    }
}
