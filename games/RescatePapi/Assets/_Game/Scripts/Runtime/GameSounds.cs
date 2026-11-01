using GameFactory.Core.Audio;
using UnityEngine;

namespace RescatePapi
{
    /// <summary>
    /// Musica y efectos. Se cargan de Resources/Audio (compuestos por codigo con
    /// art-source/RescatePapi/audio/synth.py: huehuetl, teponaztli, sonajas, flauta de barro y caracol).
    /// Si falta un archivo se usa un sonido provisional generado al vuelo (Core: ToneSynth).
    /// </summary>
    public sealed class GameSounds
    {
        public readonly AudioClip Jump = Load("sfx_salto", () => ToneSynth.CreateClip("jump", 280f, 620f, 0.12f, Wave.Square, 0.35f));
        public readonly AudioClip Cacao = Load("sfx_moneda", () => ToneSynth.CreateClip("cacao", 900f, 1500f, 0.08f, Wave.Sine, 0.45f));
        public readonly AudioClip Pollito = Load("sfx_tamal", () => ToneSynth.CreateClip("pollito", 600f, 1300f, 0.25f, Wave.Triangle, 0.5f));
        public readonly AudioClip ShieldLost = Load("sfx_escudo_roto", () => ToneSynth.CreateClip("shield", 520f, 180f, 0.22f, Wave.Square, 0.4f));
        public readonly AudioClip Die = Load("sfx_golpe", () => ToneSynth.CreateClip("die", 420f, 70f, 0.55f, Wave.Square, 0.45f));
        public readonly AudioClip Start = Load("sfx_inicio", () => ToneSynth.CreateClip("start", 440f, 880f, 0.15f, Wave.Triangle, 0.4f));
        public readonly AudioClip EagleDive = Load("sfx_aguila", () => ToneSynth.CreateClip("eagle_dive", 1800f, 700f, 0.35f, Wave.Square, 0.3f));
        public readonly AudioClip SerpentHop = Load("sfx_serpiente", () => ToneSynth.CreateClip("serpent_hop", 300f, 900f, 0.18f, Wave.Triangle, 0.4f));
        public readonly AudioClip Click = Load("sfx_click", () => ToneSynth.CreateClip("click", 700f, 950f, 0.05f, Wave.Sine, 0.35f));

        /// <summary>Musica (se repite sin corte). Null si falta: el juego sigue sin musica.</summary>
        public readonly AudioClip MusicMenu = Resources.Load<AudioClip>("Audio/musica_menu");
        public readonly AudioClip MusicRun = Resources.Load<AudioClip>("Audio/musica_carrera");

        static AudioClip Load(string name, System.Func<AudioClip> fallback)
        {
            var clip = Resources.Load<AudioClip>("Audio/" + name);
            return clip != null ? clip : fallback();
        }
    }
}
