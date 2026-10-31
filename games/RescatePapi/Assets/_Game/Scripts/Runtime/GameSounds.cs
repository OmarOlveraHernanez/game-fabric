using GameFactory.Core.Audio;
using UnityEngine;

namespace RescatePapi
{
    /// <summary>
    /// Sonidos provisionales generados por codigo (Core: ToneSynth).
    /// Se reemplazaran por percusion prehispanica cuando llegue el audio final.
    /// </summary>
    public sealed class GameSounds
    {
        public readonly AudioClip Jump = ToneSynth.CreateClip("jump", 280f, 620f, 0.12f, Wave.Square, 0.35f);
        public readonly AudioClip Cacao = ToneSynth.CreateClip("cacao", 900f, 1500f, 0.08f, Wave.Sine, 0.45f);
        public readonly AudioClip Pollito = ToneSynth.CreateClip("pollito", 600f, 1300f, 0.25f, Wave.Triangle, 0.5f);
        public readonly AudioClip ShieldLost = ToneSynth.CreateClip("shield", 520f, 180f, 0.22f, Wave.Square, 0.4f);
        public readonly AudioClip Die = ToneSynth.CreateClip("die", 420f, 70f, 0.55f, Wave.Square, 0.45f);
        public readonly AudioClip Start = ToneSynth.CreateClip("start", 440f, 880f, 0.15f, Wave.Triangle, 0.4f);
        public readonly AudioClip EagleDive = ToneSynth.CreateClip("eagle_dive", 1800f, 700f, 0.35f, Wave.Square, 0.3f);  // chillido
        public readonly AudioClip SerpentHop = ToneSynth.CreateClip("serpent_hop", 300f, 900f, 0.18f, Wave.Triangle, 0.4f); // boing
        public readonly AudioClip Click = ToneSynth.CreateClip("click", 700f, 950f, 0.05f, Wave.Sine, 0.35f);
    }
}
