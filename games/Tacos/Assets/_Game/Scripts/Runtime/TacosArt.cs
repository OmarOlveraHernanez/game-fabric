using System.Collections.Generic;
using GameFactory.Core.Audio;
using Tacos.Logic;
using UnityEngine;

namespace Tacos
{
    /// <summary>
    /// Arte de Resources/Sprites (generado en art-source/TaqueriaNeon con procesar.py).
    /// Si falta un archivo devuelve null y la interfaz sigue con cuadros de colores.
    /// </summary>
    public static class TacosArt
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string name)
        {
            if (Cache.TryGetValue(name, out var s)) return s;
            s = Resources.Load<Sprite>("Sprites/" + name);
            if (s == null)
            {
                var tex = Resources.Load<Texture2D>("Sprites/" + name);
                if (tex != null) s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            Cache[name] = s;
            return s;
        }

        public static Sprite Meat(Meat m)
        {
            switch (m)
            {
                case Logic.Meat.Pastor: return Get("ing_pastor");
                case Logic.Meat.Asada: return Get("ing_asada");
                case Logic.Meat.Chorizo: return Get("ing_chorizo");
                case Logic.Meat.Suadero: return Get("ing_suadero");
                case Logic.Meat.Chicharron: return Get("ing_chicharron");
                case Logic.Meat.Tinga: return Get("ing_tinga");
                case Logic.Meat.PapaChorizo: return Get("ing_papa");
                case Logic.Meat.Frijoles: return Get("ing_frijoles");
                default: return null;
            }
        }

        public static Sprite Extra(Extra e)
        {
            switch (e)
            {
                case Logic.Extra.Cebolla: return Get("ing_cebolla");
                case Logic.Extra.Cilantro: return Get("ing_cilantro");
                case Logic.Extra.Pina: return Get("ing_pina");
                case Logic.Extra.SalsaVerde: return Get("ing_salsa_verde");
                case Logic.Extra.SalsaRoja: return Get("ing_salsa_roja");
                case Logic.Extra.Limon: return Get("ing_limon");
                case Logic.Extra.Crema: return Get("ing_crema");
                case Logic.Extra.Queso: return Get("ing_queso");
                case Logic.Extra.Lechuga: return Get("ing_lechuga");
                default: return null;
            }
        }

        /// <summary>Sope, gordita o quesadilla: cruda o ya dorada (la tortilla es ing_tortilla).</summary>
        public static Sprite Base(Base b, bool golden)
        {
            string n = b == Logic.Base.Sope ? "sope" : b == Logic.Base.Gordita ? "gordita" : b == Logic.Base.Quesadilla ? "quesadilla" : null;
            if (n == null) return Get("ing_tortilla");
            return Get("base_" + n + (golden ? "_lista" : ""));
        }

        /// <summary>El puesto que se esta jugando (cambia el dibujo de los power-ups).</summary>
        public static Stall Stall = Stall.Taqueria;

        public static Sprite PowerUp(PowerUp p)
        {
            if (Stall == Stall.Garnachas)
                switch (p)
                {
                    case Logic.PowerUp.Cebollitas: return Get("pw_papas");
                    case Logic.PowerUp.Nopal: return Get("pw_nopalitos");
                    case Logic.PowerUp.Salsa: return Get("pw_molcajete");
                    case Logic.PowerUp.Trompo: return Get("pw_comal");
                }
            switch (p)
            {
                case Logic.PowerUp.Cebollitas: return Get("pw_cebollitas");
                case Logic.PowerUp.Nopal: return Get("pw_nopal");
                case Logic.PowerUp.Salsa: return Get("pw_salsa");
                case Logic.PowerUp.Trompo: return Get("pw_trompo");
                default: return null;
            }
        }

        /// <summary>La moneda de Taquería Neón (dorada con un taco y aro de neón).</summary>
        public static Sprite Coin => Get("ui_moneda");

        public enum Pose { Quieto, Cocinando, Festeja }

        public static Sprite Taquero(string id, Pose pose) =>
            Get(id + (pose == Pose.Cocinando ? "_cocinando" : pose == Pose.Festeja ? "_festeja" : "_quieto"));

        /// <summary>Jefe final del puesto: Don Chema (taqueria) o Dona Lupe (garnachas).</summary>
        public static Sprite Boss(Stall s, float patienceRatio)
        {
            string mood = patienceRatio > 0.6f ? "contento" : patienceRatio > 0.3f ? "espera" : "enojado";
            return Get($"jefe_{(s == Logic.Stall.Garnachas ? "garnachas" : "taqueria")}_{mood}");
        }

        /// <summary>Cara del cliente segun su paciencia: contento, esperando o enojado.</summary>
        public static Sprite Customer(int id, float patienceRatio)
        {
            int who = 1 + (id * 7) % 11; // 11 personajes (7 a 11 llegaron con las garnachas)
            string mood = patienceRatio > 0.6f ? "contento" : patienceRatio > 0.3f ? "espera" : "enojado";
            return Get($"cliente{who}_{mood}");
        }
    }

    /// <summary>
    /// Musica y efectos de Resources/Audio (compuestos con art-source/TaqueriaNeon/audio/synth_tacos.py).
    /// Si falta un archivo se usa un tono provisional del Core.
    /// </summary>
    public sealed class TacosSounds
    {
        public readonly AudioClip Plancha = Load("sfx_plancha", 200f, 120f, 0.3f, Wave.Noise);
        public readonly AudioClip Tortilla = Load("sfx_tortilla", 300f, 200f, 0.08f, Wave.Noise);
        public readonly AudioClip ExtraPop = Load("sfx_extra", 900f, 1300f, 0.06f, Wave.Sine);
        public readonly AudioClip Listo = Load("sfx_listo", 1320f, 1320f, 0.25f, Wave.Sine);
        public readonly AudioClip Cobro = Load("sfx_cobro", 1568f, 2093f, 0.35f, Wave.Triangle);
        public readonly AudioClip Error = Load("sfx_error", 330f, 247f, 0.35f, Wave.Square);
        public readonly AudioClip Cliente = Load("sfx_cliente", 988f, 784f, 0.3f, Wave.Sine);
        public readonly AudioClip Quemado = Load("sfx_quemado", 300f, 110f, 0.4f, Wave.Noise);
        public readonly AudioClip Click = Load("sfx_click", 1100f, 1100f, 0.04f, Wave.Sine);
        public readonly AudioClip Fin = Load("sfx_fin", 440f, 880f, 0.5f, Wave.Triangle);
        public readonly AudioClip MusicMenu = Resources.Load<AudioClip>("Audio/musica_menu");
        public readonly AudioClip MusicGame = Resources.Load<AudioClip>("Audio/musica_partida");

        static AudioClip Load(string name, float f0, float f1, float d, Wave w)
        {
            var clip = Resources.Load<AudioClip>("Audio/" + name);
            return clip != null ? clip : ToneSynth.CreateClip(name, f0, f1, d, w, 0.4f);
        }
    }
}
