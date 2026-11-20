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
            public float Age, Life, Size0, Size1, Alpha, Sway;
            public Color Tint = Color.white;
        }

        readonly Stage _stage;
        readonly RectTransform _front;
        readonly List<Particle> _live = new List<Particle>();
        readonly Stack<Image> _pool = new Stack<Image>();
        readonly Sprite _steam;
        readonly float[] _steamT = new float[8]; // 0..6 cazos, 7 el domo
        // Ambiente por codigo (puestos sin video): vapor que sube siempre de las ollas llenas y brasas que titilan
        readonly (Vector2 At, float Width, int Bowl)[] _ambient;
        readonly float[] _ambientT;
        readonly Rect _embers;
        readonly Image _emberGlow;
        float _sparkT;

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
            _ambient = layout.AmbientSteam ?? new (Vector2, float, int)[0];
            _ambientT = new float[_ambient.Length];
            _embers = layout.Embers;
            if (_embers.width > 0f)
            {
                // brillo de las brasas: va DETRAS de lo demas (capa de atras), respira y titila
                _emberGlow = stage.Img("Brasas", UiKit.GlowSprite, new Color(1f, 0.45f, 0.1f, 0f), _embers, backLayer);
                _emberGlow.raycastTarget = false;
            }
        }

        void Spawn(Vector2 pos, Vector2 vel, float life, float size0, float size1, float alpha, float sway = 0f, Sprite sprite = null, Color? tint = null)
        {
            var sp = sprite ?? _steam;
            if (sp == null) return;
            var img = _pool.Count > 0 ? _pool.Pop() : UiKit.NewRect("Vapor", _front).gameObject.AddComponent<Image>();
            img.sprite = sp;
            img.raycastTarget = false;
            img.preserveAspect = true;
            img.enabled = true;
            img.color = new Color(1f, 1f, 1f, 0f);
            _live.Add(new Particle { Img = img, Pos = pos, Vel = vel, Life = life, Size0 = size0, Size1 = size1, Alpha = alpha, Sway = sway, Tint = tint ?? Color.white });
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
            UpdateAmbient(s, dt);
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var p = _live[i];
                p.Age += dt;
                float k = p.Age / p.Life;
                if (k >= 1f) { p.Img.enabled = false; _pool.Push(p.Img); _live.RemoveAt(i); continue; }
                p.Pos += p.Vel * dt + new Vector2(p.Sway * Mathf.Sin(p.Age * 2.1f + p.Size0) * dt, 0f);
                float size = Mathf.Lerp(p.Size0, p.Size1, k);
                _stage.Place(p.Img.rectTransform, Stage.R(p.Pos.x, p.Pos.y, size, size));
                float a = k < 0.25f ? k / 0.25f : 1f - (k - 0.25f) / 0.75f;
                p.Img.color = new Color(p.Tint.r, p.Tint.g, p.Tint.b, p.Alpha * a);
            }
        }

        /// <summary>Vapor suave y continuo de las ollas que tienen algo, y brasas que respiran con chispitas.</summary>
        void UpdateAmbient(KitchenSimulation s, float dt)
        {
            for (int i = 0; i < _ambient.Length; i++)
            {
                var (at, width, bowl) = _ambient[i];
                if (s != null && bowl >= 0 && bowl < BowlMeat.Length)
                {
                    var pot = s.Pot(BowlMeat[bowl]);
                    if (pot != null && pot.Portions <= 0 && !pot.IsRefilling) continue; // olla vacia: no humea
                }
                _ambientT[i] -= dt;
                if (_ambientT[i] > 0f) continue;
                _ambientT[i] = Random.Range(0.32f, 0.55f);
                Spawn(at + new Vector2(Random.Range(-width / 2f, width / 2f), 0f), new Vector2(Random.Range(-4f, 4f), -Random.Range(22f, 34f)),
                      Random.Range(2.2f, 3.0f), width * 0.35f, width * 0.85f, 0.16f, 10f);
            }
            if (_emberGlow == null) return;
            float t = Time.unscaledTime;
            // respira lento y titila un poco, como carbon vivo
            float flick = 0.55f + 0.18f * Mathf.Sin(t * 1.7f) + 0.1f * Mathf.Sin(t * 7.3f) + 0.06f * Mathf.Sin(t * 13.1f);
            _emberGlow.color = new Color(1f, 0.42f, 0.08f, 0.32f * flick);
            _sparkT -= dt;
            if (_sparkT <= 0f)
            {
                _sparkT = Random.Range(0.25f, 0.6f);
                var at = new Vector2(Random.Range(_embers.xMin + 20f, _embers.xMax - 20f), _embers.center.y);
                Spawn(at, new Vector2(Random.Range(-12f, 12f), -Random.Range(40f, 70f)), Random.Range(0.8f, 1.3f), 9f, 3f, 0.9f, 25f,
                      UiKit.GlowSprite, new Color(1f, 0.65f, 0.2f));
            }
        }
    }
}
