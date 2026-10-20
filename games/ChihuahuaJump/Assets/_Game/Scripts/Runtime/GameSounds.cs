using GameFactory.Core.Audio;
using UnityEngine;

namespace ChihuahuaJump
{
    /// <summary>Sonidos provisionales generados por codigo (Core: ToneSynth).</summary>
    public sealed class GameSounds
    {
        public readonly AudioClip Bounce = ToneSynth.CreateClip("bounce", 180f, 420f, 0.12f, Wave.Sine, 0.5f);
        public readonly AudioClip Cacao = ToneSynth.CreateClip("cacao", 900f, 1500f, 0.08f, Wave.Sine, 0.45f);
        public readonly AudioClip Pollito = ToneSynth.CreateClip("pollito", 600f, 1300f, 0.25f, Wave.Triangle, 0.5f);
        public readonly AudioClip ShieldLost = ToneSynth.CreateClip("shield", 520f, 180f, 0.22f, Wave.Square, 0.4f);
        public readonly AudioClip Hit = ToneSynth.CreateClip("hit", 300f, 90f, 0.35f, Wave.Noise, 0.45f);
        public readonly AudioClip Fell = ToneSynth.CreateClip("fell", 600f, 60f, 0.7f, Wave.Triangle, 0.5f);
    }
}
