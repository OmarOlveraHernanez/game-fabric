using UnityEngine;

namespace GameFactory.Core.Audio
{
    public static partial class ToneSynth
    {
        /// <summary>Crea un AudioClip mono con el sonido generado.</summary>
        public static AudioClip CreateClip(string name, float startHz, float endHz, float duration,
                                           Wave wave = Wave.Sine, float volume = 0.5f)
        {
            float[] data = Generate(startHz, endHz, duration, wave, volume);
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
