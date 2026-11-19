using System.Collections.Generic;
using Tacos.Logic;
using Tacos.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Tacos
{
    /// <summary>
    /// Efectos que dependen del juego (el ambiente vivo lo da el video del puesto: lumbre, grasa, trompo).
    /// Solo vapor extra mientras un cazo se rellena y cuando hay tortillas en el domo.
    /// Posiciones en pixeles de barra_choricera_v2 (1536 x 864).
    /// </summary>
    public sealed class KitchenFx
    {
        sealed class Particle
        {
            public Image Img;
            public Vector2 Pos, Vel;
            public float Age, Life, Size0, Size1, Alpha;
        }

        readonly Stage _stage;
        readonly RectTransform _front;
        readonly List<Particle> _live = new List<Particle>();
        readonly Stack<Image> _pool = new Stack<Image>();
        readonly Sprite _steam;
        readonly float[] _steamT = new float[8]; // 0..6 cazos, 7 el domo

        // Donde sale vapor: el domo y los cazos de cada puesto (StallLayout)
        readonly Vector2 DomeCenter;
        readonly Vector2[] BowlSpots;
        readonly Meat[] BowlMeat;

        public KitchenFx(Stage stage, RectTransform backLayer, RectTransform frontLayer, StallLayout layout)
        {
            _stage = stage;
            DomeCenter = layout.FxDome;
            BowlSpots = new Vector2[layout.Bowls.Length];
            for (int i = 0; i < BowlSpots.Length; i++) BowlSpots[i] = layout.Bowls[i] - new Vector2(0f, 8f);
            BowlMeat = layout.BowlMeat;
            _front = frontLayer;
            _steam = TacosArt.Get("fx_vapor");
        }

        void Spawn(Vector2 pos, Vector2 vel, float life, float size0, float size1, float alpha)
        {
            if (_steam == null) return;
            var img = _pool.Count > 0 ? _pool.Pop() : UiKit.NewRect("Vapor", _front).gameObject.AddComponent<Image>();
            img.sprite = _steam;
            img.raycastTarget = false;
            img.preserveAspect = true;
            img.enabled = true;
            img.color = new Color(1f, 1f, 1f, 0f);
            _live.Add(new Particle { Img = img, Pos = pos, Vel = vel, Life = life, Size0 = size0, Size1 = size1, Alpha = alpha });
        }

        public void Update(KitchenSimulation s)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (s != null)
            {
                for (int i = 0; i < BowlMeat.Length && i < 7; i++)
                {
                    var pot = s.Pot(BowlMeat[i]);
                    if (pot == null || !pot.IsRefilling) continue;
                    _steamT[i] -= dt;
                    if (_steamT[i] > 0f) continue;
                    _steamT[i] = Random.Range(0.18f, 0.3f);
                    Spawn(BowlSpots[i] + new Vector2(Random.Range(-25f, 25f), 0f), new Vector2(Random.Range(-6f, 6f), -Random.Range(30f, 45f)),
                          Random.Range(1.4f, 1.9f), 34f, 90f, 0.3f);
                }
                bool hot = false;
                foreach (var tor in s.Comal) if (tor != null) hot = true;
                _steamT[7] -= dt;
                if (hot && _steamT[7] <= 0f)
                {
                    _steamT[7] = Random.Range(0.35f, 0.6f);
                    Spawn(DomeCenter + new Vector2(Random.Range(-60f, 60f), -10f), new Vector2(0f, -38f), 1.6f, 30f, 80f, 0.18f);
                }
            }
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var p = _live[i];
                p.Age += dt;
                float k = p.Age / p.Life;
                if (k >= 1f) { p.Img.enabled = false; _pool.Push(p.Img); _live.RemoveAt(i); continue; }
                p.Pos += p.Vel * dt;
                float size = Mathf.Lerp(p.Size0, p.Size1, k);
                _stage.Place(p.Img.rectTransform, Stage.R(p.Pos.x, p.Pos.y, size, size));
                float a = k < 0.25f ? k / 0.25f : 1f - (k - 0.25f) / 0.75f;
                p.Img.color = new Color(1f, 1f, 1f, p.Alpha * a);
            }
        }
    }
}
