using System.Collections.Generic;
using GameFactory.Core.Audio;
using GameFactory.Core.Controls;
using GameFactory.Core.Save;
using RescatePapi.Logic;
using RescatePapi.UI;
using UnityEngine;

namespace RescatePapi
{
    /// <summary>
    /// Conecta RunnerSimulation con Unity: lee input, avanza la simulacion y dibuja los sprites
    /// (o formas de color si falta el arte). Las pantallas y el HUD estan en GameMenus.
    /// </summary>
    public sealed class RunnerView : MonoBehaviour
    {
        // Paleta provisional inspirada en codices
        static readonly Color PapiColor    = new Color(0.84f, 0.63f, 0.38f); // canela (Papi)
        static readonly Color ShieldColor  = new Color(1.00f, 0.85f, 0.25f); // aura del pollito
        static readonly Color SerpentColor = new Color(0.55f, 0.30f, 0.70f); // morado (contrasta con la selva)
        static readonly Color JaguarColor  = new Color(0.90f, 0.62f, 0.15f); // ocre
        static readonly Color JaguarCharge = new Color(0.85f, 0.20f, 0.15f); // rojo grana (embistiendo)
        static readonly Color EagleColor   = new Color(0.35f, 0.22f, 0.12f); // cafe oscuro
        static readonly Color CacaoColor   = new Color(0.45f, 0.25f, 0.12f);
        static readonly Color PollitoColor = new Color(1.00f, 0.90f, 0.20f);

        RunnerSimulation _sim;
        readonly TapHoldInput _input = new TapHoldInput();
        SaveFile<GameData> _saveFile;
        GameData _data;
        GameSounds _sounds;
        bool _newRecord;
        readonly List<SpriteRenderer> _hazardViews = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _pickupViews = new List<SpriteRenderer>();
        Sprite _sprite;
        SpriteSet _art;
        System.Collections.Generic.List<ScrollingStrip> _strips;
        SpriteRenderer _player, _aura, _owner;
        Sprite[] _artAura, _artRun, _artJump, _artHit, _artOwner, _artSerpent, _artJaguar, _artEagle, _artCacao, _artPollito;
        GameMenus _menus;
        bool _pressOnUi;               // el toque empezo sobre un boton: no cuenta como salto
        RunState _lastState;
        GUIStyle _small;
        float _fps;

        public void Init(RunnerSimulation sim, Sprite sprite, SpriteSet art, System.Collections.Generic.List<ScrollingStrip> strips)
        {
            _sim = sim;
            _sprite = sprite;
            _art = art;
            _strips = strips;
            _artRun = art.Get("papi_correr");
            _artJump = art.Get("papi_salto");
            _artHit = art.Get("papi_golpe");
            _artOwner = art.Get("dueno_atrapado");
            _artSerpent = art.Get("serpiente");
            _artJaguar = art.Get("jaguar");
            _artEagle = art.Get("aguila");
            _artCacao = art.Get("cacao");
            _artPollito = art.Get("pollito");
            _artAura = art.Get("aura");
            var c = sim.Config;
            _aura = CreateBox("ShieldAura", ShieldColor, 9);
            _aura.transform.localScale = new Vector3(c.PlayerWidth + 0.3f, c.PlayerHeight + 0.3f, 1f);
            _player = CreateBox("Papi", PapiColor, 10);
            _player.transform.localScale = new Vector3(c.PlayerWidth, c.PlayerHeight, 1f);

            // Guardado (Core: SaveFile) y sonidos (Core: AudioService + ToneSynth)
            _saveFile = new SaveFile<GameData>("rescatepapi");
            _data = _saveFile.Load();
            sim.SetBestScore(_data.bestScore);
            _sounds = new GameSounds();
            var audio = AudioService.Instance;
            sim.Started += () => { _newRecord = false; _menus.SetNewRecord(false); audio.PlaySfx(_sounds.Start, 0.6f); };
            sim.Jump.Jumped += () => audio.PlaySfx(_sounds.Jump, 0.5f, Random.Range(0.95f, 1.05f));
            sim.Collected += p => audio.PlaySfx(p.Kind == PickupKind.Cacao ? _sounds.Cacao : _sounds.Pollito, 0.7f, Random.Range(0.97f, 1.1f));
            sim.ShieldLost += () => audio.PlaySfx(_sounds.ShieldLost, 0.8f);
            sim.EnemyAction += h => audio.PlaySfx(h.Kind == HazardKind.Eagle ? _sounds.EagleDive : _sounds.SerpentHop, 0.7f);
            sim.Died += OnDied;

            // Pantallas: inicio, HUD, pausa y fin de partida
            _menus = new GameObject("Menus").AddComponent<GameMenus>();
            _menus.Init(sim, _data, First(_artCacao), First(_artPollito), _sounds.Click);
            _menus.PlayPressed += () => _sim.Begin();
            _menus.RestartPressed += () => _sim.TryRestart();
            _menus.HomePressed += GoHome;
            _lastState = sim.State;
        }

        static Sprite First(Sprite[] frames) => frames != null && frames.Length > 0 ? frames[0] : null;

        void OnDied()
        {
            AudioService.Instance.PlaySfx(_sounds.Die, 0.9f);
            _newRecord = _sim.Score > _data.bestScore;
            _menus.SetNewRecord(_newRecord);
            SaveRun();
        }

        /// <summary>Volver a la pantalla de inicio (desde pausa o desde el fin de partida).</summary>
        void GoHome()
        {
            if (_sim.State == RunState.Running) SaveRun(); // carrera abandonada: se guardan monedas y record
            _menus.SetPaused(false);
            _sim.ResetRun();
            _lastState = _sim.State;
        }

        void SaveRun()
        {
            _data.bestScore = Mathf.Max(_data.bestScore, _sim.Score);
            _sim.SetBestScore(_data.bestScore);
            _data.cacaoTotal += _sim.Cacao;
            _data.runs++;
            try { _saveFile.Save(_data); }
            catch (System.Exception e) { Debug.LogWarning($"[RescatePapi] No se pudo guardar: {e.Message}"); }
        }

        void Update()
        {
            if (_sim == null) return;
            float dt = Mathf.Min(Time.deltaTime, 1f / 30f); // evita saltos grandes si hay tirones
            _input.Poll();
            bool pressed = _input.PressedThisFrame, held = _input.Held;
            if (!held) _pressOnUi = false;
            if (pressed && UiKit.TryGetPressedPointer(out var pointer) && UiKit.IsOverUI(pointer)) _pressOnUi = true;
            if (_pressOnUi) { pressed = false; held = false; }
            if (_sim.State == RunState.Dead) pressed = false;                                 // reiniciar solo con los botones
            if (_sim.State == RunState.Running && _lastState != RunState.Running) pressed = false; // un boton empezo la carrera
            if (!_menus.Paused) _sim.Tick(dt, pressed, held);
            _lastState = _sim.State;
            Sync();
            _fps = Mathf.Lerp(_fps, 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f), 0.1f);
        }

        void Sync()
        {
            var c = _sim.Config;
            var playerPos = new Vector3(c.PlayerX, _sim.Jump.Y, 0f);
            _player.transform.position = playerPos;
            float t = Time.time;
            Sprite[] papiFrames = _sim.State == RunState.Dead ? (_artHit ?? _artJump ?? _artRun)
                                 : !_sim.Jump.IsGrounded ? (_artJump ?? _artRun)
                                 : _artRun;
            float runTime = _sim.State == RunState.Running ? _sim.Distance / c.BaseSpeed : 0f;
            ApplyArt(_player, SpriteSet.Frame(papiFrames, runTime, 10f), PapiColor, new Vector2(c.PlayerWidth, c.PlayerHeight), 1f);
            foreach (var strip in _strips) strip.SetDistance(_sim.Distance);
            // El dueno atrapado (la meta) solo se ve en la pantalla de inicio, pidiendo ayuda
            if (_owner == null && _artOwner != null) _owner = CreateBox("DuenoAtrapado", Color.white, 4);
            if (_owner != null)
            {
                _owner.enabled = _sim.State == RunState.Ready;
                _owner.sprite = SpriteSet.Frame(_artOwner, t, 3f);
                _owner.transform.position = new Vector3(5.5f, 0f, 0f);
            }
            _aura.enabled = _sim.HasShield;
            if (_artAura != null)
            {
                // Aura dorada de "super guerrero" mientras dura el escudo del tamal: flamas que parpadean y laten
                float pulse = 1f + 0.05f * Mathf.Sin(t * 9f);
                _aura.sprite = SpriteSet.Frame(_artAura, t, AuraFps);
                _aura.color = new Color(1f, 1f, 1f, 0.85f + 0.15f * Mathf.Sin(t * 13f));
                _aura.transform.localScale = new Vector3(pulse, pulse, 1f);
                _aura.transform.position = playerPos + new Vector3(0f, AuraYOffset, 0f);
            }
            else _aura.transform.position = playerPos + new Vector3(0f, -0.15f, 0f); // respaldo: cuadro de color
            // Parpadeo durante la invulnerabilidad tras perder el escudo
            _player.enabled = !_sim.IsInvulnerable || Mathf.FloorToInt(Time.time * 12f) % 2 == 0;

            var hazards = _sim.Hazards;
            Ensure(_hazardViews, hazards.Count, "Hazard", 5);
            for (int i = 0; i < _hazardViews.Count; i++)
            {
                var v = _hazardViews[i];
                bool active = i < hazards.Count;
                if (v.gameObject.activeSelf != active) v.gameObject.SetActive(active);
                if (!active) continue;
                var h = hazards[i];
                Color fallback = h.Kind switch
                {
                    HazardKind.Serpent => SerpentColor,
                    HazardKind.Jaguar => h.IsCharging ? JaguarCharge : JaguarColor,
                    _ => EagleColor
                };
                Sprite[] frames = h.Kind == HazardKind.Serpent ? _artSerpent : h.Kind == HazardKind.Jaguar ? _artJaguar : _artEagle;
                // Jaguar: el arte esta dibujado para la caja de JaguarRefHeight; escala con su altura
                float scale = h.Kind == HazardKind.Jaguar ? h.Height / JaguarRefHeight : 1f;
                float yOffset = h.Kind == HazardKind.Eagle && frames != null ? -EagleArtMargin : 0f;
                // Serpiente saltarina: antes de saltar rebota nerviosa (aviso: "a esta NO la saltes")
                bool nervous = h.WillHop && h.HopTime < 0f;
                if (nervous) yOffset += Mathf.Abs(Mathf.Sin(t * 14f + i)) * 0.12f;
                v.transform.position = new Vector3(h.X, h.Bottom + yOffset, 0f);
                float fps = h.Kind == HazardKind.Eagle ? 6f : nervous ? 12f : 5f;
                Sprite frame = h.IsDiving && frames != null && frames.Length > 1 ? frames[1]   // picada: alas pegadas
                             : SpriteSet.Frame(frames, t + i * 0.37f, fps);
                ApplyArt(v, frame, fallback, new Vector2(h.Width, h.Height), scale);
                if (frames != null && h.Kind == HazardKind.Jaguar && h.IsCharging) v.color = new Color(1f, 0.65f, 0.6f); // tinte al embestir
                if (frames != null && h.IsDiving) v.color = new Color(1f, 0.55f, 0.45f);                                 // aguila en picada: roja
                if (nervous && frames != null) v.transform.localScale = new Vector3(1f, 1f + 0.08f * Mathf.Sin(t * 28f), 1f);
            }

            var pickups = _sim.Pickups;
            Ensure(_pickupViews, pickups.Count, "Pickup", 7);
            for (int i = 0; i < _pickupViews.Count; i++)
            {
                var v = _pickupViews[i];
                bool active = i < pickups.Count;
                if (v.gameObject.activeSelf != active) v.gameObject.SetActive(active);
                if (!active) continue;
                var p = pickups[i];
                Sprite[] frames = p.Kind == PickupKind.Cacao ? _artCacao : _artPollito;
                // El tamal (escudo) flota suave arriba/abajo; solo es visual, la caja de choque no se mueve
                float bob = p.Kind == PickupKind.Pollito ? Mathf.Sin(t * TamalBobSpeed) * TamalBobHeight : 0f;
                v.transform.position = new Vector3(p.X, p.Bottom - (frames != null ? PickupArtMargin : 0f) + bob, 0f);
                ApplyArt(v, SpriteSet.Frame(frames, t, 4f), p.Kind == PickupKind.Cacao ? CacaoColor : PollitoColor, new Vector2(p.Size, p.Size), 1f);
            }
        }

        // Relacion entre el lienzo del arte y la caja de colision (ver ARTE.md)
        const float JaguarRefHeight = 1.4f;   // el lienzo del jaguar (1.5 u) corresponde a una caja de 1.4 u de alto
        const float EagleArtMargin = 0.075f;  // margen inferior del lienzo del aguila
        const float PickupArtMargin = 0.025f;
        const float AuraFps = 14f;            // cuadros por segundo del aura
        const float AuraYOffset = -0.03f;     // el aura nace un poco debajo de los pies
        const float TamalBobHeight = 0.08f;   // unidades que sube/baja el tamal al flotar
        const float TamalBobSpeed = 4f;       // velocidad de la flotacion (radianes por segundo)

        /// <summary>Usa el sprite si existe (tamano natural * escala); si no, la forma de color del tamano de la caja.</summary>
        void ApplyArt(SpriteRenderer sr, Sprite art, Color fallbackColor, Vector2 fallbackSize, float scale)
        {
            if (art != null)
            {
                sr.sprite = art;
                sr.color = Color.white;
                sr.transform.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                sr.sprite = _sprite;
                sr.color = fallbackColor;
                sr.transform.localScale = new Vector3(fallbackSize.x, fallbackSize.y, 1f);
            }
        }

        void Ensure(List<SpriteRenderer> pool, int count, string name, int order)
        {
            while (pool.Count < count) pool.Add(CreateBox(name, Color.white, order));
        }

        SpriteRenderer CreateBox(string name, Color color, int order)
        {
            var sr = new GameObject(name).AddComponent<SpriteRenderer>();
            sr.sprite = _sprite;
            sr.color = color;
            sr.sortingOrder = order;
            sr.transform.SetParent(transform, false);
            return sr;
        }

        // Pausa automatica si la app pasa a segundo plano (llamada, notificacion, boton de inicio)
        void OnApplicationPause(bool paused)
        {
            if (paused && _menus != null) _menus.SetPaused(true);
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused && _menus != null) _menus.SetPaused(true);
        }

        void OnGUI()
        {
            if (_sim == null || !Debug.isDebugBuild) return;
            if (_small == null)
            {
                _small = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(12, Screen.height / 40) };
                _small.normal.textColor = new Color(0.12f, 0.1f, 0.08f);
            }
            float pad = Screen.height * 0.03f;
            GUI.Label(new Rect(pad, Screen.height - pad * 2.5f, Screen.width, pad * 2), $"{_fps:0} fps  vel {_sim.Speed:0.0}", _small);
        }
    }
}
