using System;
using System.Collections.Generic;
using Tacos.Logic;
using Tacos.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Tacos
{
    /// <summary>
    /// La pantalla de juego como escena: el puesto pintado (Resources/Sprites/barra_choricera) y encima lo que cambia
    /// (clientes, tortillas en el domo, carne en los cazos, platos, power-up que cae del cielo). Se toca la cosa misma.
    /// Posiciones en pixeles de la imagen de fondo (1536 x 864), medidas sobre barra_choricera_v2.
    /// </summary>
    public sealed partial class KitchenScene
    {
        const float W = 1536f, H = 864f;

        // ---- donde esta cada cosa en el fondo: depende del puesto (StallLayout) ----
        // Solo existe una escena a la vez (al cambiar de puesto se arma otra), por eso el acomodo es estatico.
        static StallLayout L = StallLayout.Taqueria;
        static Rect TrompoArea => L.TrompoArea;
        static Vector2 TrompoBadge => L.TrompoBadge;
        static Vector2[] Bowls => L.Bowls;
        static Meat[] BowlMeat => L.BowlMeat;
        static Rect RawTray => L.RawTray;
        static Vector2[] Dome2 => L.Comal2;
        static Vector2[] Dome4 => L.Comal4;
        static Vector2[] PlateSpots => L.PlateSpots;
        static float[] CustomerX => L.CustomerX;
        static float CounterTop => L.CounterTop;
        static Rect TrashArea => L.TrashArea;
        static Dictionary<Extra, Vector2> Cazuelas => L.Extras;

        public Stall Stall { get; }

        static Rect R(float cx, float cy, float w, float h) => Stage.R(cx, cy, w, h);

        readonly Action<Command> _do;
        readonly Stage _stage;
        KitchenSimulation _sim;

        readonly RectTransform _trompoHot, _trashHot, _pickupHot;
        readonly List<(Base Base, RectTransform Hot)> _stacks = new List<(Base, RectTransform)>();
        readonly BadgeView _trompoBadge;
        readonly BowlView[] _bowls;
        readonly Image _radio, _radioShadow, _ironDome, _ironGlow;
        bool _bigPots;
        readonly List<TortillaView> _tortillas = new List<TortillaView>();
        readonly RectTransform _domeLayer, _fxLayer;
        readonly Text _hotLabel, _pickupLabel;
        readonly Image _pickupImg, _pickupGlow;
        readonly PlateView[] _plates = new PlateView[3];
        readonly CustomerView[] _customers = new CustomerView[3];
        readonly Dictionary<Extra, RectTransform> _extraHot = new Dictionary<Extra, RectTransform>();
        readonly Image _hintRing;
        readonly List<Floater> _floaters = new List<Floater>();
        readonly KitchenFx _fx;
        readonly VideoPlayer _video, _trompoVideo, _burnerVideo;
        readonly RectTransform _messLayer;
        readonly List<Image> _messes = new List<Image>();
        const int MaxMesses = 45;
        /// <summary>Apagado (16 nov): las manchas encimadas se veian falsas. Queda la idea para despues.</summary>
        const bool MessEnabled = false;
        float _shake;

        public KitchenScene(RectTransform parent, Action<Command> doCommand, StallLayout layout = null)
        {
            L = layout ?? StallLayout.Taqueria;
            Stall = L.Stall;
            _do = doCommand;
            _stage = new Stage(parent, "Escena " + L.Stall, TacosArt.Get(L.Background), W, H, Stage.Fit.FitWidth, L.CropBottom);
            var root = _stage.Root;

            // Puesto vivo: el mismo dibujo animado en video que se repite (grasa, vapor, luces de la calle).
            // Encima, el trompo en su propio video, mas lento y de ida y vuelta (sus orillas son iguales al dibujo).
            // Mientras cargan, o si faltan, se queda el dibujo quieto de abajo.
            if (L.Videos)
            {
                _video = AddVideo("Video/puesto_vivo", "PuestoVivo", new Rect(0, 0, W, H), 1);
                _trompoVideo = AddVideo("Video/puesto_trompo", "TrompoVivo", new Rect(0, 0, 288, 640), 2);
                _burnerVideo = AddVideo("Video/puesto_lumbre", "LumbreViva", new Rect(128, 635.2f, 848, 132.8f), 3);
            }
            else if (L.Video != null)
                _video = AddVideo(L.Video, "PuestoVivo", new Rect(0, 0, W, H), 1); // garnachas: lumbre, vapor y luces en un solo video
            var fxBack = UiKit.Stretch(UiKit.NewRect("EfectosAtras", root)); // lumbre, trompo, burbujas (debajo de la carne)
            _messLayer = UiKit.Stretch(UiKit.NewRect("Suciedad", root));     // lo que se va tirando en la barra (debajo de los platos)

            for (int i = 0; i < 3; i++) _customers[i] = new CustomerView(this, i);

            _trompoHot = _stage.Hotspot("Trompo", TrompoArea, () => TapPot(Meat.Pastor));
            _trompoBadge = new BadgeView(this, "TrompoPorciones", TrompoBadge, 50f);

            // Lo que se esta cocinando en la grasa: tripitas, nopales y cebollitas (adorno, no se toca)
            foreach (var (name, x, y, w, h) in L.Garnish)
            {
                var sp = TacosArt.Get(name);
                if (sp == null) continue;
                // sombrita en la grasa y la pieza ya "hundida" (el dibujo trae la base mezclada con el aceite)
                bool hangs = name == "gar_papel" || name == "gar_neon"; // lo que cuelga no lleva sombra
                if (!hangs) _stage.Img("Sombra " + name, UiKit.GlowSprite, new Color(0.24f, 0.1f, 0f, 0.45f), R(x, y + h * 0.12f + 6f, w * 1.0f, h * 0.75f));
                _stage.Pic("Comal " + name, sp, R(x, y, w, h));
            }

            // El domo de fierro va antes que los cazos: si el cazo de en medio esta lleno, la carne queda encima del asa
            _ironGlow = _stage.Img("DomoFierroCalor", UiKit.GlowSprite, new Color(1f, 0.45f, 0.1f, 0.5f), R(585, 500, 330, 150));
            _ironDome = _stage.Pic("DomoFierro", TacosArt.Get("mej_domo"), R(585, 462, 262, 158));

            _bowls = new BowlView[Bowls.Length];
            for (int i = 0; i < _bowls.Length; i++) _bowls[i] = new BowlView(this, i);

            // Mejoras que se ven en la cocina (se prenden en Setup)
            _radioShadow = _stage.Img("RadioSombra", UiKit.GlowSprite, new Color(0f, 0f, 0f, 0.55f), R(1100, 354, 108, 14));
            _radio = _stage.Pic("Radio", TacosArt.Get("mej_radio"), R(1100, 310, 100, 92)); // parado en la orilla de atras, detras de la carne cruda
            _radio.rectTransform.pivot = new Vector2(0.5f, 0f);

            _domeLayer = UiKit.Stretch(UiKit.NewRect("Domo", root));
            _hotLabel = _stage.Label(L.Stall == Stall.Taqueria ? "¡Caliente!" : "¡Lista!", 22, new Color32(0xFF, 0xE6, 0x8C, 0xFF), R(L.HotLabel.x, L.HotLabel.y, 220, 34));

            // De donde se toman las bases: la pila de tortillas, o las pilas de sopes, gorditas y quesadillas
            foreach (var (b, area, art) in L.Stacks)
            {
                var bb = b;
                if (art != null) _stage.Pic("Pila " + b, TacosArt.Get(art), area);
                _stacks.Add((b, _stage.Hotspot("Pila " + b, area, () => _do(Command.AddTortilla(bb)))));
            }
            _rawHot = _stage.Hotspot("CarneCruda", RawTray, RefillFromTray);

            for (int i = 0; i < 3; i++) _plates[i] = new PlateView(this, i);

            foreach (var kv in Cazuelas)
            {
                var e = kv.Key;
                _extraHot[e] = _stage.Hotspot("Extra " + e, R(kv.Value.x, kv.Value.y, 115, 92), () => _do(Command.ToggleExtra(e)));
                // En las garnachas la crema y el queso se confundian: cada cazuelita lleva su nombre
                if (L.ExtraLabels) _stage.Label(ExtraLabel(e), 19, UiKit.Cream, R(kv.Value.x, kv.Value.y + ((L.LabelUp & e) != 0 ? -52f : 52f), 140, 24));
            }

            if (L.TrashArt) _stage.Pic("Basura", TacosArt.Get("est_basura"), TrashArea);
            _trashHot = _stage.Hotspot("TirarPlato", TrashArea, () => { if (_sim != null) _do(Command.TrashPlate(_sim.SelectedPlate)); });

            // Nopal: velo azulito sobre los clientes mientras la paciencia esta congelada

            var fxFront = UiKit.Stretch(UiKit.NewRect("EfectosEncima", root)); // vapor y chispas
            _fx = new KitchenFx(_stage, fxBack, fxFront, L);
            _fxLayer = UiKit.Stretch(UiKit.NewRect("Efectos", root));
            _hintRing = _stage.Img("Pista", UiKit.RingSprite, UiKit.Turq, R(0, 0, 10, 10));
            _hintRing.enabled = false;

            // Power-up que cae del cielo (se acomoda cada cuadro en RefreshPickup); va encima de clientes y barra
            _pickupGlow = _stage.Img("PowerUpBrillo", UiKit.GlowSprite, new Color(1f, 0.95f, 0.55f, 0.9f), R(0, 0, 10, 10));
            _pickupImg = _stage.Pic("PowerUpCae", null, R(0, 0, 10, 10));
            _pickupLabel = _stage.Label("¡Tómalo!", 22, UiKit.Limon, R(0, 0, 200, 30));
            _pickupHot = _stage.Hotspot("TomarPowerUp", R(0, 0, 10, 10), () => _do(Command.TakePickup()));
            _pickupHot.gameObject.SetActive(false);
            _pickupImg.enabled = _pickupGlow.enabled = _pickupLabel.enabled = false;
            BuildDrag();
        }

        public RectTransform Root => _stage.Root;

        /// <summary>Video en loop sobre una parte del dibujo (pixeles de la imagen). Se muestra cuando ya arranco.</summary>
        VideoPlayer AddVideo(string path, string name, Rect area, int sibling)
        {
            var clip = Resources.Load<VideoClip>(path);
            if (clip == null) return null;
            var rt = new RenderTexture((int)clip.width, (int)clip.height, 0) { name = name };
            var img = UiKit.NewRect(name, _stage.Root).gameObject.AddComponent<RawImage>();
            img.texture = rt;
            img.raycastTarget = false;
            img.enabled = false;
            _stage.Place(img.rectTransform, area);
            img.transform.SetSiblingIndex(sibling);
            var vp = img.gameObject.AddComponent<VideoPlayer>();
            vp.playOnAwake = false;
            vp.clip = clip;
            vp.isLooping = true;
            vp.audioOutputMode = VideoAudioOutputMode.None;
            vp.renderMode = VideoRenderMode.RenderTexture;
            vp.targetTexture = rt;
            vp.waitForFirstFrame = true;
            vp.started += p => img.enabled = true;
            vp.errorReceived += (p, msg) => { Debug.LogWarning($"[Tacos] Video {name}: {msg}"); img.enabled = false; };
            return vp;
        }

        // =============== cada turno ===============

        public void Setup(KitchenSimulation sim, bool radio = false, bool ironDome = false, bool bigPots = false)
        {
            _sim = sim;
            radio &= L.Upgrades; ironDome &= L.Upgrades;
            _radio.enabled = radio && _radio.sprite != null;
            _radioShadow.enabled = _radio.enabled;
            _ironDome.enabled = ironDome && _ironDome.sprite != null;
            _ironGlow.enabled = _ironDome.enabled;
            _bigPots = bigPots;
            foreach (var t in _tortillas) UnityEngine.Object.Destroy(t.Root.gameObject);
            _tortillas.Clear();
            var spots = sim.Comal.Length > 2 ? Dome4 : Dome2;
            for (int i = 0; i < sim.Comal.Length && i < spots.Length; i++)
            {
                var v = new TortillaView(this, i, spots[i]);
                _tortillas.Add(v);
                MakeTortillaDraggable(v, i);
            }
            EndDrag();
            foreach (var (b, hot) in _stacks)
                hot.gameObject.SetActive(sim.Level.NeedsTortilla && System.Array.IndexOf(sim.Level.OrderBases, b) >= 0);
            _rawHot.gameObject.SetActive(RawTray.width > 0f);
            for (int i = 0; i < _bowls.Length; i++) _bowls[i].Setup(sim.Pot(BowlMeat[i]));
            foreach (var f in _floaters) UnityEngine.Object.Destroy(f.Text.gameObject);
            _floaters.Clear();
            foreach (var m in _messes) UnityEngine.Object.Destroy(m.gameObject);
            _messes.Clear(); // cada mision empieza con la barra limpia
        }

        public void Layout()
        {
            _stage.Layout();
            if (_shake > 0f)
            {
                _shake -= Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(_shake / 0.3f) * 9f;
                _stage.Root.anchoredPosition += new Vector2(UnityEngine.Random.Range(-k, k), UnityEngine.Random.Range(-k, k));
            }
        }

        /// <summary>Sacudida corta (error o cliente que se va).</summary>
        public void Shake() => _shake = 0.3f;

        /// <summary>El radio vibra con el bajo y el domo de fierro respira calor.</summary>
        void AnimateUpgrades()
        {
            float t = Time.unscaledTime;
            if (_radio.enabled)
            {
                // quieto en la repisa; solo vibra poquito con el bajo de la cumbia
                float beat = Mathf.Pow(Mathf.Abs(Mathf.Sin(t * 3.4f)), 8f);
                _radio.rectTransform.localScale = new Vector3(1f, 1f + 0.025f * beat, 1f);
            }
            if (_ironGlow.enabled)
                _ironGlow.color = new Color(1f, 0.45f, 0.1f, 0.35f + 0.15f * Mathf.Sin(t * 2.2f));
        }

        // =============== la barra se va ensuciando ===============

        /// <summary>
        /// Deja una mancha o un pedacito en la barra donde paso la accion (solo a veces, para que se junte poco a poco).
        /// Se borra todo al empezar la siguiente mision.
        /// </summary>
        public void Mess(Command cmd)
        {
            switch (cmd.Type)
            {
                case CommandType.ToggleExtra:
                {
                    var e = (Extra)cmd.Arg;
                    if (!Cazuelas.TryGetValue(e, out var c)) return;
                    string sprite = e == Extra.Cebolla ? "sucio_cebolla" : e == Extra.Cilantro ? "sucio_cilantro" : e == Extra.Pina ? "sucio_migajas"
                                  : e == Extra.SalsaVerde ? "sucio_salsa_verde" : e == Extra.SalsaRoja ? "sucio_salsa_roja" : "sucio_limon";
                    float chance = e == Extra.Limon ? 0.25f : 0.4f;
                    // cae enfrente de la cazuela o hacia la tabla
                    var at = c + new Vector2(UnityEngine.Random.Range(-60f, 30f), c.y > 560f ? UnityEngine.Random.Range(48f, 80f) : UnityEngine.Random.Range(40f, 55f));
                    AddMess(sprite, at, chance, 44f, 64f);
                    break;
                }
                case CommandType.ServeMeat:
                    AddMess("sucio_grasa", new Vector2(UnityEngine.Random.Range(905f, 975f), UnityEngine.Random.Range(560f, 730f)), 0.4f, 40f, 62f);
                    break;
                case CommandType.AddTortilla:
                case CommandType.TakeTortilla:
                    AddMess("sucio_migajas", new Vector2(UnityEngine.Random.Range(915f, 1020f), UnityEngine.Random.Range(462f, 480f)), 0.3f, 36f, 52f);
                    break;
                case CommandType.TrashPlate:
                    AddMess("sucio_migajas", TrashArea.center + new Vector2(UnityEngine.Random.Range(-70f, -30f), UnityEngine.Random.Range(-10f, 25f)), 0.6f, 40f, 58f);
                    break;
            }
        }

        /// <summary>Taco equivocado: se embarra salsa en la barra.</summary>
        public void MessSpill() =>
            AddMess(UnityEngine.Random.value < 0.5f ? "sucio_salsa_roja" : "sucio_salsa_verde",
                    new Vector2(UnityEngine.Random.Range(920f, 1420f), UnityEngine.Random.Range(700f, 745f)), 1f, 60f, 84f);

        /// <summary>Cliente atendido: a veces deja su servilleta hecha bola.</summary>
        public void MessNapkin() =>
            AddMess("sucio_servilleta", new Vector2(UnityEngine.Random.Range(1150f, 1230f), UnityEngine.Random.Range(700f, 745f)), 0.3f, 44f, 58f);

        void AddMess(string sprite, Vector2 at, float chance, float minSize, float maxSize)
        {
            if (!MessEnabled || UnityEngine.Random.value > chance) return;
            var sp = TacosArt.Get(sprite);
            if (sp == null) return;
            if (_messes.Count >= MaxMesses)
            {
                UnityEngine.Object.Destroy(_messes[0].gameObject);
                _messes.RemoveAt(0);
            }
            float size = UnityEngine.Random.Range(minSize, maxSize);
            var img = _stage.Pic("Mancha", sp, R(at.x, at.y, size, size * 0.8f), _messLayer);
            img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
            img.color = new Color(1f, 1f, 1f, UnityEngine.Random.Range(0.75f, 0.95f));
            _messes.Add(img);
        }

        // =============== efectos ===============

        sealed class Floater { public Text Text; public float Age, Life; public Vector2 From; }

        /// <summary>Texto que sube y se desvanece, en pixeles de la imagen (por ejemplo "¡ÓRALE! +18").</summary>
        public void Popup(string text, Vector2 at, Color color, int size = 34)
        {
            var t = UiKit.Outlined(_fxLayer, text, size, color, TextAnchor.MiddleCenter, 3f);
            t.font = UiKit.Display;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            _stage.Place(t.rectTransform, R(at.x, at.y, 400, 60));
            _floaters.Add(new Floater { Text = t, Life = 1.3f, From = new Vector2(at.x, at.y) });
        }

        /// <summary>Centro de un cliente en pixeles de la imagen (para efectos).</summary>
        public Vector2 CustomerPoint(int slot) => new Vector2(CustomerX[Mathf.Clamp(slot, 0, 2)] + 60, 250);

        void UpdateFloaters(float dt)
        {
            for (int i = _floaters.Count - 1; i >= 0; i--)
            {
                var f = _floaters[i];
                f.Age += dt;
                float k = f.Age / f.Life;
                if (k >= 1f) { UnityEngine.Object.Destroy(f.Text.gameObject); _floaters.RemoveAt(i); continue; }
                _stage.Place(f.Text.rectTransform, R(f.From.x, f.From.y - 90f * k, 400, 60));
                var c = f.Text.color; c.a = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f; f.Text.color = c;
                float pop = k < 0.12f ? Mathf.Lerp(0.6f, 1.15f, k / 0.12f) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((k - 0.12f) / 0.1f));
                f.Text.rectTransform.localScale = new Vector3(pop, pop, 1f);
            }
        }

        // =============== dibujar el estado ===============

        public void Refresh(KitchenSimulation s)
        {
            UpdateFloaters(Time.unscaledDeltaTime);
            // Los videos solo pueden arrancar con la pantalla visible (Unity no reproduce en objetos apagados)
            if (_video != null && _video.isActiveAndEnabled && !_video.isPlaying) _video.Play();
            if (_trompoVideo != null && _trompoVideo.isActiveAndEnabled && !_trompoVideo.isPlaying) _trompoVideo.Play();
            if (_burnerVideo != null && _burnerVideo.isActiveAndEnabled && !_burnerVideo.isPlaying) _burnerVideo.Play();
            _fx.Update(s);
            AnimateUpgrades();
            if (s == null) return;
            var lvl = s.Level;

            for (int i = 0; i < 3; i++)
                _customers[i].Refresh(i < lvl.MaxCustomers ? s.Customers[i] : null, i < lvl.MaxCustomers && s.HasPlateFor(i), s.NopalLeft > 0f);

            var pastor = L.Trompo ? s.Pot(Meat.Pastor) : null;
            _trompoHot.gameObject.SetActive(pastor != null);
            _trompoBadge.Refresh(pastor, s);
            foreach (var b in _bowls) b.Refresh(s);

            bool anyHot = false;
            for (int i = 0; i < _tortillas.Count; i++)
            {
                var t = s.Comal[i];
                var st = t == null ? CookState.Raw : s.TortillaState(t);
                _tortillas[i].Refresh(t, st, t == null ? 0f : Mathf.Clamp01(t.Age / s.HotTime));
                if (t != null && st == CookState.Ready) anyHot = true;
            }
            _hotLabel.enabled = anyHot;

            for (int i = 0; i < 3; i++)
                _plates[i].Refresh(i < s.Plates.Length ? s.Plates[i] : null, i == s.SelectedPlate);

            RefreshPickup(s);
        }

        // ---- power-up que cae del cielo (como los soles de Plantas contra Zombies) ----
        // Cae meciendose entre los clientes, se queda flotando y brillando, y se toca para agarrarlo.
        static readonly float[] PickupLanes = { 300, 615, 905, 1200, 1390 }; // huecos entre clientes
        const float PickupFall = 2.2f;
        bool _hadPickup;
        float _pickupAge, _pickupX, _pickupY;

        void RefreshPickup(KitchenSimulation s)
        {
            bool pick = s.Pickup.HasValue;
            if (pick && !_hadPickup)
            {
                _pickupAge = 0f;
                _pickupX = PickupLanes[UnityEngine.Random.Range(0, PickupLanes.Length)] + UnityEngine.Random.Range(-30f, 30f);
                _pickupY = UnityEngine.Random.Range(250f, 295f);
            }
            _hadPickup = pick;
            _pickupHot.gameObject.SetActive(pick);
            _pickupImg.enabled = _pickupGlow.enabled = pick;
            _pickupLabel.enabled = false;
            if (!pick) return;

            _pickupAge += Time.unscaledDeltaTime;
            float t = Time.unscaledTime;
            float k = Mathf.Clamp01(_pickupAge / PickupFall);
            bool landed = k >= 1f;
            float ease = 1f - (1f - k) * (1f - k);                      // frena al llegar
            float y = Mathf.Lerp(-80f, _pickupY, ease);
            float x = _pickupX + (landed ? 0f : 26f * Mathf.Sin(_pickupAge * 3.2f) * (1f - k)); // se mece mientras baja
            if (landed) y += 5f * Mathf.Sin(t * 2.5f);                   // ya abajo, flota tantito

            _stage.Place(_pickupGlow.rectTransform, R(x, y, 190, 170));
            _stage.Place(_pickupImg.rectTransform, R(x, y, 104, 104));
            _stage.Place(_pickupHot, R(x, y, 160, 150));
            _stage.Place(_pickupLabel.rectTransform, R(x, y - 74, 200, 30));
            _pickupLabel.enabled = landed;

            _pickupImg.sprite = TacosArt.PowerUp(s.Pickup.Value);
            _pickupImg.enabled = _pickupImg.sprite != null;
            float bob = 1f + 0.07f * Mathf.Sin(t * 6f);
            _pickupImg.rectTransform.localScale = new Vector3(bob, bob, 1f);
            _pickupImg.rectTransform.localRotation = Quaternion.Euler(0f, 0f, landed ? 0f : 10f * Mathf.Sin(_pickupAge * 3.2f));
            bool fading = s.PickupLeft < 2f;
            float a = fading ? 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(t * 10f)) : 1f;
            _pickupImg.color = new Color(1f, 1f, 1f, a);
            _pickupGlow.color = new Color(1f, 0.95f, 0.55f, (0.6f + 0.3f * Mathf.Sin(t * 4f)) * a);
            _pickupLabel.color = new Color(UiKit.Limon.r, UiKit.Limon.g, UiKit.Limon.b, a);
        }

        /// <summary>Aro que late sobre lo que hay que tocar. null = sin pista.</summary>
        public void ShowHint(Hint? hint)
        {
            var target = hint.HasValue ? HintTarget(hint.Value) : null;
            _hintRing.enabled = target != null;
            if (target == null) return;
            var rt = _hintRing.rectTransform;
            rt.anchorMin = target.anchorMin;
            rt.anchorMax = target.anchorMax;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 7f);
            rt.offsetMin = new Vector2(-6f - 6f * pulse, -6f - 6f * pulse);
            rt.offsetMax = new Vector2(6f + 6f * pulse, 6f + 6f * pulse);
            _hintRing.color = new Color(UiKit.Turq.r, UiKit.Turq.g, UiKit.Turq.b, 0.65f + 0.35f * pulse);
            rt.SetAsLastSibling();
        }

        /// <summary>Lo que hay que tocar para cumplir la pista (lo usa tambien el tutorial).</summary>
        public RectTransform HintTarget(Hint h)
        {
            switch (h.Kind)
            {
                case HintKind.ServeMeat:
                case HintKind.RefillMeat:
                    if (h.Meat == Meat.Pastor) return _trompoHot;
                    for (int i = 0; i < _bowls.Length; i++) if (BowlMeat[i] == h.Meat) return _bowls[i].Hot;
                    return null;
                case HintKind.AddTortilla: return StackFor(h.Command.HasValue ? (Base)h.Command.Value.Arg : Base.None);
                case HintKind.TakeTortilla:
                case HintKind.FlipItem: return h.Slot >= 0 && h.Slot < _tortillas.Count ? _tortillas[h.Slot].Hot : null;
                case HintKind.SelectPlate: return h.Plate >= 0 && h.Plate < 3 ? _plates[h.Plate].Hot : null;
                case HintKind.TrashPlate: return _trashHot;
                case HintKind.AddExtra: return _extraHot.TryGetValue(h.Extra, out var e) ? e : null;
                case HintKind.Deliver: return h.Customer >= 0 ? _customers[h.Customer].Hot : null;
                default: return null;
            }
        }

        /// <summary>La pila de esa base (None = la primera que este prendida).</summary>
        RectTransform StackFor(Base b)
        {
            foreach (var (bb, hot) in _stacks) if ((b == Base.None || bb == b) && hot.gameObject.activeSelf) return hot;
            return _stacks.Count > 0 ? _stacks[0].Hot : null;
        }

        // =============== toques ===============

        void TapPot(Meat m)
        {
            if (_sim == null) return;
            var pot = _sim.Pot(m);
            if (pot == null) return;
            _do(pot.Portions > 0 || pot.IsRefilling ? Command.ServeMeat(m) : Command.RefillMeat(m));
        }

        /// <summary>La charola de carne cruda rellena el cazo que mas lo necesita (aunque no este vacio).</summary>
        void RefillFromTray()
        {
            if (_sim == null) return;
            MeatPot best = null;
            foreach (var p in _sim.Pots)
            {
                if (p.Meat == Meat.Pastor || p.IsRefilling || p.Portions >= _sim.Rules.PotPortions) continue;
                if (best == null || p.Portions < best.Portions) best = p;
            }
            if (best != null) _do(Command.RefillMeat(best.Meat));
            else foreach (var p in _sim.Pots) if (p.Meat != Meat.Pastor) { _do(Command.RefillMeat(p.Meat)); break; }
        }

        // =============== piezas ===============

        // ---- numero de porciones con aro de relleno ----
        sealed class BadgeView
        {
            readonly Image _bg, _ring;
            readonly Text _text;

            public BadgeView(KitchenScene k, string name, Vector2 c, float size)
            {
                _bg = k._stage.Img(name, UiKit.CircleSprite, new Color(0.08f, 0.07f, 0.12f, 0.92f), R(c.x, c.y, size, size));
                _ring = UiKit.NewRect("Aro", _bg.transform).gameObject.AddComponent<Image>();
                _ring.sprite = UiKit.RingSprite;
                _ring.raycastTarget = false;
                UiKit.Stretch(_ring.rectTransform);
                _ring.type = Image.Type.Filled;
                _ring.fillMethod = Image.FillMethod.Radial360;
                _ring.fillOrigin = (int)Image.Origin360.Top;
                _text = UiKit.FillLabel(_bg.transform, "", 24, Color.white, TextAnchor.MiddleCenter, 0f);
            }

            public void Refresh(MeatPot pot, KitchenSimulation s)
            {
                bool on = pot != null;
                _bg.enabled = _ring.enabled = _text.enabled = on;
                if (!on) return;
                if (pot.IsRefilling)
                {
                    _ring.fillAmount = 1f - pot.Refill / s.CookTime;
                    _ring.color = UiKit.Maiz;
                    _text.text = Mathf.CeilToInt(pot.Refill).ToString();
                    _text.color = UiKit.Maiz;
                    return;
                }
                float r = (float)pot.Portions / s.Rules.PotPortions;
                _ring.fillAmount = r;
                _ring.color = pot.Portions == 0 ? UiKit.Chile : r <= 0.4f ? UiKit.Maiz : UiKit.Limon;
                _text.text = pot.Portions == 0 ? "+" : pot.Portions.ToString();
                _text.color = pot.Portions == 0 ? UiKit.Chile : Color.white;
            }
        }

        // ---- un cazo en la choricera ----
        sealed class BowlView
        {
            readonly Image _heap, _cover;
            readonly Text _name;
            readonly BadgeView _badge;
            MeatPot _pot;
            public readonly RectTransform Hot;
            public readonly Meat Meat;

            readonly KitchenScene _k;

            public BowlView(KitchenScene k, int i)
            {
                _k = k;
                var c = Bowls[i];
                var meat = BowlMeat[i];
                Meat = meat;
                var hs = L.HeapSizes != null && i < L.HeapSizes.Length ? L.HeapSizes[i] : L.HeapSize;
                // La cazuela pintada ya trae comida: cuando se vacia se tapa con su fondo oscuro
                _cover = k._stage.Img("Vacio " + meat, UiKit.CircleSprite, new Color(0.22f, 0.12f, 0.07f, 0.96f), R(c.x, c.y - 2, hs.x * 0.92f, hs.y * 0.8f));
                _cover.enabled = false;
                _heap = L.FullPots != null
                    ? k._stage.Pic("Cazuela " + meat, null, R(c.x, c.y + 4, 202, 157)) // del tamaño de la cazuela pintada (la del video)
                    : k._stage.Pic("Cazo " + meat, L.HeapSizes != null ? TacosArt.Pot(meat) : TacosArt.Meat(meat), R(c.x, c.y - 4, hs.x, hs.y));
                if (L.HeapSizes != null) _heap.preserveAspect = false; // se acuesta en la boca de la olla
                _name = k._stage.Label(MeatName(meat), 20, UiKit.Cream, R(c.x, c.y + hs.y * 0.63f, 170, 26));
                Hot = k._stage.Hotspot("TocarCazo " + meat, R(c.x, c.y, Mathf.Max(150f, hs.x + 30f), 100), () => k.TapPot(meat));
                _badge = new BadgeView(k, "Porciones " + meat, new Vector2(c.x + hs.x * 0.58f, c.y - 36), 44f);
            }

            public void Setup(MeatPot pot)
            {
                _pot = pot;
                Hot.gameObject.SetActive(pot != null);
                _name.enabled = pot != null;
            }

            public void Refresh(KitchenSimulation s)
            {
                _badge.Refresh(_pot, s);
                if (L.PaintedFull)
                {
                    // Elotes: lo lleno ya esta en el dibujo; vacia se tapa la boca y al rellenarse se va destapando
                    _heap.enabled = false;
                    _name.enabled = _pot != null;
                    if (_pot == null) { _cover.enabled = true; _cover.color = new Color(0.16f, 0.13f, 0.1f, 0.92f); return; }
                    float cover = _pot.IsRefilling ? _pot.Refill / s.CookTime : _pot.Portions <= 0 ? 1f : 0f;
                    _cover.enabled = cover > 0.01f;
                    _cover.color = new Color(0.16f, 0.13f, 0.1f, 0.92f * cover);
                    return;
                }
                if (L.FullPots != null)
                {
                    // Garnachas: la cazuela entera dibujada (llena, vacia o tapada si no sale en la mision)
                    _cover.enabled = false;
                    _name.enabled = _pot != null;
                    if (_pot == null) { _heap.sprite = TacosArt.Get("cazuela_tapada"); _heap.color = Color.white; _heap.enabled = _heap.sprite != null; return; }
                    float fill = _pot.IsRefilling ? 1f - _pot.Refill / s.CookTime : 1f;
                    bool empty = _pot.Portions <= 0;
                    _heap.sprite = TacosArt.Get(empty && !_pot.IsRefilling ? "cazuela_vacia" : L.FullPots + MeatArt(Meat));
                    _heap.enabled = _heap.sprite != null;
                    _heap.color = _pot.IsRefilling ? new Color(1f, 1f, 1f, 0.35f + 0.65f * fill) : Color.white;
                    float big = _k._bigPots ? 1.06f : 1f;
                    _heap.rectTransform.localScale = new Vector3(big, big, 1f);
                    return;
                }
                if (_pot == null) { _heap.enabled = false; _cover.enabled = L.BowlCover; return; }
                float full = _pot.IsRefilling ? 1f - _pot.Refill / s.CookTime : (float)_pot.Portions / s.Rules.PotPortions;
                _heap.enabled = _heap.sprite != null && full > 0.01f;
                _cover.enabled = L.BowlCover;
                float sc = Mathf.Lerp(0.45f, 1f, Mathf.Sqrt(Mathf.Clamp01(full))) * (_k._bigPots ? 1.22f : 1f); // cazos grandes: copeteados
                _heap.rectTransform.localScale = new Vector3(sc, sc, 1f);
                _heap.color = _pot.IsRefilling ? new Color(1f, 0.6f, 0.6f, 0.85f) : Color.white;
            }
        }

        // ---- base en el domo o en el comal (tortilla, sope, gordita, quesadilla) ----
        sealed class TortillaView
        {
            readonly Image _glow, _img;
            readonly Text _flipLabel;
            public readonly RectTransform Root, Hot;
            bool _wasFlipped;
            float _flipAnim;

            public TortillaView(KitchenScene k, int slot, Vector2 c)
            {
                Root = UiKit.Stretch(UiKit.NewRect("Base " + slot, k._domeLayer));
                var bs = L.BaseSize;
                _glow = k._stage.Img("Brillo", UiKit.GlowSprite, new Color(1f, 0.85f, 0.45f, 0.85f), R(c.x, c.y, bs.x * 1.55f, bs.y * 1.7f), Root);
                _img = k._stage.Pic("Base", TacosArt.Get("ing_tortilla"), R(c.x, c.y, bs.x, bs.y), Root);
                _img.preserveAspect = L.BaseKeepsAspect;
                _flipLabel = k._stage.Label("¡Voltéala!", 20, UiKit.Maiz, R(c.x, c.y - bs.y * 0.62f, 170, 28), Root);
                _flipLabel.enabled = false;
                // Tocar: si ya tiene un lado listo se voltea; si no, se toma (o se tira si se quemo)
                Hot = k._stage.Hotspot("TocarBase " + slot, R(c.x, c.y, bs.x * 0.98f, bs.y * 1.2f), () =>
                {
                    var t = k._sim != null && slot < k._sim.Comal.Length ? k._sim.Comal[slot] : null;
                    k._do(t != null && k._sim.TortillaState(t) == CookState.Flip ? Command.FlipItem(slot) : Command.TakeTortilla(slot));
                }, Root);
            }

            public void Refresh(ComalItem t, CookState st, float heat)
            {
                bool on = t != null;
                _img.enabled = on;
                _glow.enabled = on && (st == CookState.Ready || st == CookState.Flip);
                _flipLabel.enabled = on && st == CookState.Flip;
                Hot.gameObject.SetActive(on);
                if (!on) { _wasFlipped = false; return; }

                // Al voltear se aplasta y se abre de nuevo (como cuando la volteas con la mano)
                if (t.Flipped && !_wasFlipped) _flipAnim = 0.28f;
                _wasFlipped = t.Flipped;
                if (_flipAnim > 0f) _flipAnim -= Time.unscaledDeltaTime;
                float fy = _flipAnim > 0f ? Mathf.Abs(Mathf.Cos((1f - _flipAnim / 0.28f) * Mathf.PI)) : 1f;
                _img.rectTransform.localScale = new Vector3(1f, Mathf.Max(0.08f, fy), 1f);

                if (t.Base == Base.Tortilla)
                {
                    _img.sprite = TacosArt.Get("ing_tortilla");
                    _img.color = st == CookState.Burned ? new Color(0.32f, 0.22f, 0.18f)
                               : st == CookState.Ready ? Color.white : Color.Lerp(new Color(1f, 1f, 0.85f, 0.8f), Color.white, heat);
                }
                else
                {
                    // Masa: cruda hasta que la volteas; ya volteada se ve el lado dorado
                    var sp = st == CookState.Burned ? TacosArt.Get("base_quemada") : TacosArt.Base(t.Base, t.Flipped);
                    _img.sprite = sp;
                    _img.color = st == CookState.Burned || t.Flipped || st == CookState.Flip ? Color.white
                               : Color.Lerp(new Color(1f, 1f, 0.9f), new Color(1f, 0.93f, 0.8f), heat);
                }
                _img.enabled = _img.sprite != null;
                float pulse = Mathf.Sin(Time.unscaledTime * 5f);
                if (_glow.enabled)
                    _glow.color = st == CookState.Flip ? new Color(1f, 0.6f, 0.2f, 0.65f + 0.3f * pulse) : new Color(1f, 0.85f, 0.45f, 0.6f + 0.3f * pulse);
                if (_flipLabel.enabled) _flipLabel.rectTransform.localScale = Vector3.one * (1f + 0.08f * pulse);
            }
        }

        // ---- plato en la tabla ----
        sealed class PlateView
        {
            readonly Image _ring, _plate, _tortilla, _meat, _lock;
            readonly Image[] _extras = new Image[5];
            readonly Text _lockText;
            public readonly RectTransform Hot;

            public PlateView(KitchenScene k, int i)
            {
                var c = PlateSpots[i];
                var st = k._stage;
                _ring = st.Img("Elegido " + i, UiKit.GlowSprite, new Color(1f, 0.85f, 0.55f, 0.55f), R(c.x, c.y, 230, 110)); // brillo calido suave (no aro: parecia pista)
                _plate = st.Pic("Plato " + i, L.PlateArt != null ? TacosArt.Get(L.PlateArt) : null, R(c.x, c.y + 4, 196, 84));
                _plate.preserveAspect = false;
                _plate.enabled = _plate.sprite != null;
                bool masa = L.BaseKeepsAspect;
                _tortilla = st.Pic("PlatoBase " + i, TacosArt.Get("ing_tortilla"), masa ? R(c.x, c.y - 8, L.PlateBase.x, L.PlateBase.y) : R(c.x, c.y - 2, 118, 48));
                _tortilla.preserveAspect = masa;
                _meat = st.Pic("PlatoCarne " + i, null, masa ? R(c.x, c.y - 16, L.PlateMeat.x, L.PlateMeat.y) : R(c.x, c.y - 8, 80, 42));
                for (int e = 0; e < _extras.Length; e++)
                    _extras[e] = st.Pic("PlatoExtra " + i + "." + e, null, R(c.x - 56 + e * 28, c.y + 14, 30, 30));
                _lock = st.Img("Bloqueado " + i, UiKit.CircleSprite, new Color(0.05f, 0.04f, 0.08f, 0.72f), R(c.x, c.y, 160, 64));
                _lockText = UiKit.Outlined(_lock.transform, "Mejora:\n3er plato", 16, UiKit.Muted);
                UiKit.Stretch(_lockText.rectTransform);
                Hot = st.Hotspot("TocarPlato " + i, R(c.x, c.y, 165, 70), () => k._do(Command.SelectPlate(i)));
            }

            public void Refresh(Plate p, bool selected)
            {
                bool locked = p == null;
                _lock.enabled = _lockText.enabled = locked;
                Hot.gameObject.SetActive(!locked);
                _ring.enabled = !locked && selected;
                _plate.enabled = !locked && _plate.sprite != null;
                if (!locked && p.Tortilla) _tortilla.sprite = p.Base == Base.Tortilla ? TacosArt.Get("ing_tortilla") : TacosArt.Base(p.Base, true);
                _tortilla.enabled = !locked && p.Tortilla && _tortilla.sprite != null;
                var ms = !locked && p.HasMeat ? TacosArt.Meat(p.Meat) : null;
                _meat.sprite = ms; _meat.enabled = ms != null;
                int k = 0;
                if (!locked)
                    foreach (var e in Extras.All)
                        if (Extras.Has(p.Extras, e) && k < _extras.Length) { _extras[k].sprite = TacosArt.Extra(e); _extras[k].enabled = _extras[k].sprite != null; k++; }
                for (; k < _extras.Length; k++) _extras[k].enabled = false;
            }
        }

        // ---- cliente detras de la barra con su globito ----
        sealed class CustomerView
        {
            readonly KitchenScene _k;
            readonly Image _face, _bubble, _tail, _barBg, _bar, _tagBg, _ice;
            readonly Outline _bubbleOutline;
            readonly Image[] _icons = new Image[7];
            readonly Image[] _iconBg = new Image[7]; // circulito de color: lo blanco (crema, queso) no se perdia en el globito
            readonly Text _rush, _ready, _tag;
            public readonly RectTransform Hot;
            readonly float _x;

            public CustomerView(KitchenScene k, int i)
            {
                _k = k;
                _x = CustomerX[i];
                var st = k._stage;
                _ice = st.Img("Hielo " + i, UiKit.GlowSprite, new Color(0.55f, 0.85f, 1f, 0.7f), R(_x - 50, CounterTop - 118, 300, 260));
                _ice.enabled = false;
                _face = st.Pic("Cliente " + i, null, R(_x - 50, CounterTop - 118, 250, 236));

                _bubble = st.Img("Globito " + i, UiKit.RoundedSpritePublic, new Color(1f, 0.98f, 0.94f, 0.97f), R(_x, 150, 100, 90));
                _bubble.type = Image.Type.Sliced;
                _bubbleOutline = _bubble.gameObject.AddComponent<Outline>();
                _bubbleOutline.effectDistance = new Vector2(4f, -4f);
                _tail = st.Img("Colita " + i, null, new Color(1f, 0.98f, 0.94f, 0.97f), R(_x, 200, 22, 22));
                _tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                for (int e = 0; e < _icons.Length; e++)
                {
                    _iconBg[e] = st.Img("PideFondo " + i + "." + e, UiKit.CircleSprite, new Color(0.86f, 0.62f, 0.38f, 0.55f), R(0, 0, 10, 10));
                    _icons[e] = st.Pic("Pide " + i + "." + e, null, R(0, 0, 10, 10));
                }

                _barBg = st.Img("Paciencia " + i, UiKit.RoundedSpritePublic, new Color(0.08f, 0.07f, 0.12f, 0.8f), R(_x, 220, 100, 14));
                _barBg.type = Image.Type.Sliced;
                _bar = UiKit.NewRect("Relleno", _barBg.transform).gameObject.AddComponent<Image>();
                _bar.sprite = UiKit.RoundedSpritePublic;
                _bar.type = Image.Type.Sliced;
                _bar.raycastTarget = false;
                _rush = st.Label("¡PRISA!", 24, UiKit.Chile, R(_x, 250, 200, 30));
                _ready = st.Label("¡Entrégale!", 22, UiKit.Limon, R(_x, 90, 220, 30));
                _tagBg = st.Img("Especial " + i, UiKit.RoundedSpritePublic, UiKit.Maiz, R(_x, 100, 150, 30));
                _tagBg.type = Image.Type.Sliced;
                _tag = UiKit.FillLabel(_tagBg.transform, "", 18, UiKit.Ink, TextAnchor.MiddleCenter, 2f);
                Hot = st.Hotspot("Entregar " + i, R(_x, 210, 290, 260), () => k._do(Command.Deliver(i)));
            }

            public void Refresh(Customer c, bool ready, bool frozen)
            {
                bool on = c != null;
                Hot.gameObject.SetActive(on);
                _face.enabled = on;
                if (!on) _ice.enabled = false;
                _bubble.enabled = _tail.enabled = _barBg.enabled = _bar.enabled = on;
                _rush.enabled = on && c.Rush;
                _ready.enabled = false; // ahora el globito verde que late avisa que ya esta listo
                bool special = on && c.Kind != CustomerKind.Normal;
                _tagBg.enabled = _tag.enabled = special;
                if (!on) { foreach (var ic in _icons) ic.enabled = false; foreach (var b in _iconBg) b.enabled = false; return; }

                bool boss = c.Kind == CustomerKind.Jefe;
                var face = boss ? TacosArt.Boss(L.Stall, c.PatienceRatio) : TacosArt.Customer(c.Id, c.PatienceRatio);
                _face.sprite = face;
                _face.enabled = face != null;
                // Nopal: el cliente se queda congelado (azul helado y un brillo frio detras), no toda la pantalla
                _ice.enabled = frozen;
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
                if (frozen) _ice.color = new Color(0.55f, 0.85f, 1f, 0.45f + 0.25f * pulse);
                _face.color = frozen ? new Color(0.7f, 0.88f, 1f) : Color.white;
                var st = _k._stage;
                if (face != null)
                {
                    // Pegado a la barra: cada cara mide distinto y centrada dejaba un hueco (se veia una linea)
                    float aspect = face.rect.width / face.rect.height;
                    float fw = boss ? 290f : 226f, fh = boss ? 262f : 214f; // el jefe se ve mas grande
                    if (aspect > fw / fh) fh = fw / aspect; else fw = fh * aspect;
                    st.Place(_face.rectTransform, R(_x - 50f, CounterTop - fh / 2f, fw, fh));
                    st.Place(_ice.rectTransform, R(_x - 50f, CounterTop - fh * 1.15f / 2f, fw * 1.25f, fh * 1.15f)); // sin pasar de la barra
                }

                int n = 0;
                if (c.Order.Base != Base.Tortilla && TacosArt.Base(c.Order.Base, true) != null) SetIcon(n++, TacosArt.Base(c.Order.Base, true)); // en las garnachas importa la masa
                SetIcon(n++, TacosArt.Meat(c.Order.Meat));
                foreach (var e in Extras.All) if (Extras.Has(c.Order.Extras, e) && n < _icons.Length) SetIcon(n++, TacosArt.Extra(e));
                for (int k = n; k < _icons.Length; k++) _icons[k].enabled = false;
                for (int k = 0; k < _iconBg.Length; k++) _iconBg[k].enabled = k < n && _icons[k].enabled;
                // Globito a la derecha de la cabeza: maximo 3 cosas por renglon (los pedidos largos usan 2 renglones
                // en vez de crecer de lado y taparle la cara al siguiente cliente)
                int cols = Mathf.Min(n, 3), rows = n > 3 ? 2 : 1;
                const float Step = 50f, Pad = 9f, Top = 98f;
                float bw = cols * Step + 2 * Pad, bh = rows * Step + 2 * Pad;
                float left = _x + 12f, bx = left + bw / 2f;
                st.Place(_bubble.rectTransform, new Rect(left, Top, bw, bh));
                for (int k = 0; k < n; k++)
                {
                    float ix = left + Pad + Step / 2f + (k % 3) * Step, iy = Top + Pad + Step / 2f + (k / 3) * Step;
                    st.Place(_iconBg[k].rectTransform, R(ix, iy, 48, 48));
                    st.Place(_icons[k].rectTransform, R(ix, iy, 44, 44));
                }
                st.Place(_tail.rectTransform, R(left + 6f, Top + bh - 8f, 22, 22));
                _bubbleOutline.effectColor = ready ? UiKit.Limon : frozen ? new Color(0.55f, 0.85f, 1f) : new Color(0.16f, 0.12f, 0.2f, 1f);

                // Listo para entregar: el globito se pone verde y late (sin texto encima de nadie)
                float beat = ready ? 1f + 0.05f * Mathf.Sin(Time.unscaledTime * 8f) : 1f;
                _bubble.rectTransform.localScale = new Vector3(beat, beat, 1f);
                _bubble.color = ready ? new Color(0.9f, 1f, 0.84f, 0.98f) : new Color(1f, 0.98f, 0.94f, 0.97f);

                st.Place(_barBg.rectTransform, R(bx, Top + bh + 12f, bw, 14));
                float r = c.PatienceRatio;
                var brt = _bar.rectTransform;
                brt.anchorMin = Vector2.zero; brt.anchorMax = new Vector2(r, 1f);
                brt.offsetMin = brt.offsetMax = Vector2.zero;
                _bar.color = frozen ? new Color(0.55f, 0.85f, 1f) : r > 0.5f ? UiKit.Limon : r > 0.25f ? UiKit.Maiz : UiKit.Chile;

                // Etiquetas como calcomanias en la esquina de arriba del globito
                if (special) st.Place(_tagBg.rectTransform, R(left + bw / 2f, Top - 4f, Mathf.Max(bw, 150f), 26));
                if (_rush.enabled) st.Place(_rush.rectTransform, R(left + bw / 2f, Top - 6f, 150, 28));
                if (special)
                {
                    _tag.text = c.Kind == CustomerKind.Mariachi ? $"MARIACHI · faltan {c.Remaining}"
                              : boss ? $"¡JEFE FINAL! · faltan {c.Remaining}" : PowerUps.KindName(c.Kind).ToUpper();
                    _tagBg.color = c.Kind == CustomerKind.Critico ? new Color32(0xB9, 0x9C, 0xFF, 0xFF)
                                 : c.Kind == CustomerKind.Borrachito ? new Color32(0xFF, 0xB0, 0x5A, 0xFF) : UiKit.Maiz;
                }
            }

            void SetIcon(int k, Sprite s)
            {
                if (k >= _icons.Length) return;
                _icons[k].sprite = s;
                _icons[k].enabled = s != null;
            }
        }

        static string MeatArt(Meat m) =>
            m == Meat.Chicharron ? "chicharron" : m == Meat.Tinga ? "tinga" : m == Meat.PapaChorizo ? "papa" : m == Meat.Frijoles ? "frijoles" : m.ToString().ToLower();

        static string ExtraLabel(Extra e)
        {
            switch (e)
            {
                case Extra.Crema: return "Crema";
                case Extra.Queso: return "Queso";
                case Extra.Lechuga: return "Lechuga";
                case Extra.Mayonesa: return "Mayonesa";
                case Extra.ChilePolvo: return "Chile";
                case Extra.Picante: return "Picante";
                case Extra.SalsaVerde: return "Salsa verde";
                case Extra.SalsaRoja: return "Salsa roja";
                case Extra.Cebolla: return "Cebolla";
                case Extra.Cilantro: return "Cilantro";
                case Extra.Pina: return "Piña";
                case Extra.Limon: return "Limón";
                default: return "";
            }
        }

        public static string MeatName(Meat m)
        {
            switch (m)
            {
                case Meat.Pastor: return "Pastor";
                case Meat.Asada: return "Asada";
                case Meat.Chorizo: return "Chorizo";
                case Meat.Suadero: return "Suadero";
                case Meat.Chicharron: return "Chicharrón";
                case Meat.Tinga: return "Tinga";
                case Meat.PapaChorizo: return "Papa con chorizo";
                case Meat.Frijoles: return "Frijoles";
                case Meat.Elote: return "Elotes";
                case Meat.Esquite: return "Esquites";
                case Meat.EloteAsado: return "Asados";
                default: return "";
            }
        }
    }
}
