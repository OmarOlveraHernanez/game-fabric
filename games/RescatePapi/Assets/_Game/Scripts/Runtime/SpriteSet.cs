using System.Collections.Generic;
using UnityEngine;

namespace RescatePapi
{
    /// <summary>
    /// Carga todos los sprites de Resources/Sprites y los agrupa por nombre:
    /// "dueno_correr_0", "dueno_correr_1"... forman la animacion "dueno_correr"; "cacao" es un solo cuadro.
    /// Si un sprite no existe, Get devuelve null y la vista usa la forma de color (respaldo).
    /// </summary>
    public sealed class SpriteSet
    {
        readonly Dictionary<string, Sprite[]> _frames = new Dictionary<string, Sprite[]>();

        public SpriteSet(string resourcesFolder = "Sprites")
        {
            var groups = new Dictionary<string, List<KeyValuePair<int, Sprite>>>();
            foreach (var s in Resources.LoadAll<Sprite>(resourcesFolder))
            {
                Parse(s.name, out string key, out int index);
                if (!groups.TryGetValue(key, out var list)) groups[key] = list = new List<KeyValuePair<int, Sprite>>();
                list.Add(new KeyValuePair<int, Sprite>(index, s));
            }
            foreach (var g in groups)
            {
                g.Value.Sort((a, b) => a.Key.CompareTo(b.Key));
                var arr = new Sprite[g.Value.Count];
                for (int i = 0; i < arr.Length; i++) arr[i] = g.Value[i].Value;
                _frames[g.Key] = arr;
            }
            if (_frames.Count > 0) Debug.Log($"[RescatePapi] Sprites cargados: {string.Join(", ", _frames.Keys)}");
        }

        public Sprite[] Get(string key) => _frames.TryGetValue(key, out var f) ? f : null;

        /// <summary>Cuadro de una animacion segun el tiempo (fps cuadros por segundo).</summary>
        public static Sprite Frame(Sprite[] frames, float time, float fps)
        {
            if (frames == null || frames.Length == 0) return null;
            int i = Mathf.FloorToInt(time * fps) % frames.Length;
            return frames[i < 0 ? 0 : i];
        }

        static void Parse(string name, out string key, out int index)
        {
            int u = name.LastIndexOf('_');
            if (u > 0 && int.TryParse(name.Substring(u + 1), out index)) { key = name.Substring(0, u); return; }
            key = name;
            index = 0;
        }
    }
}
