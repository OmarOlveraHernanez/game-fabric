using System.Collections.Generic;
using ChihuahuaJump.Logic;
using GameFactory.Core.Audio;
using GameFactory.Core.Controls;
using GameFactory.Core.Save;
using UnityEngine;

namespace ChihuahuaJump
{
    /// <summary>
    /// Conecta BounceSimulation con Unity: lee input, avanza la simulacion y dibuja con formas de color.
    /// HUD con OnGUI solo para el prototipo; el arte estilo codice llega en F3.
    /// </summary>
    public sealed class GameView : MonoBehaviour
    {
        // Paleta provisional inspirada en codices
        static readonly Color DogColor        = new Color(0.86f, 0.60f, 0.32f); // chihuahua
        static readonly Color OwnerColor      = new Color(0.10f, 0.55f, 0.60f); // turquesa
        static readonly Color TrampolineColor = new Color(0.70f, 0.15f, 0.15f); // rojo grana
        static readonly Color ShieldColor     = new Color(1.00f, 0.85f, 0.25f);
        static readonly Color SerpentColor    = new Color(0.20f, 0.60f, 0.30f); // jade
        static readonly Color JaguarColor     = new Color(0.90f, 0.62f, 0.15f); // ocre
        static readonly Color EagleColor      = new Color(0.30f, 0.20f, 0.12f); // cafe oscuro
        static readonly Color CacaoColor      = new Color(0.45f, 0.25f, 0.12f);
        static readonly Color PollitoColor    = new Color(1.00f, 0.90f, 0.20f);
        static readonly Color WarningColor    = new Color(0.85f, 0.15f, 0.10f);

        BounceSimulation _sim;
        Camera _cam;
        PointerFollowInput _input;
        SaveFile<GameData> _saveFile;
        GameData _data;
        GameSounds _sounds;
        bool _newRecord;
        readonly List<SpriteRenderer> _hazardViews = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _pickupViews = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _warningViews = new List<SpriteRenderer>();
        Sprite _sprite;
        SpriteRenderer _dog, _aura, _owner, _trampoline;
        GUIStyle _hud, _center, _small;
        float _fps;

        public void Init(BounceSimulation sim, Sprite sprite, Camera cam)
        {
            _sim = sim;
            _sprite = sprite;
            _cam = cam;
            _input = new PointerFollowInput(cam);
            var c = sim.Config;
            _owner = CreateBox("Owner", OwnerColor, 8);
            _owner.transform.localScale = new Vector3(0.7f, c.TrampolineY - 0.2f, 1f);
            _trampoline = CreateBox("Trampoline", TrampolineColor, 9);
            _trampoline.transform.localScale = new Vector3(c.TrampolineWidth, 0.2f, 1f);
            _aura = CreateBox("ShieldAura", ShieldColor, 10);
            _aura.transform.localScale = new Vector3(c.DogSize + 0.3f, c.DogSize + 0.3f, 1f);
            _dog = CreateBox("Dog", DogColor, 11);
            _dog.transform.localScale = new Vector3(c.DogSize, c.DogSize, 1f);

            // Guardado (Core: SaveFile) y sonidos (Core: AudioService + ToneSynth)
            _saveFile = new SaveFile<GameData>("chihuahuajump");
            _data = _saveFile.Load();
            sim.SetBestScore(_data.bestScore);
            _sounds = new GameSounds();
            var audio = AudioService.Instance;
            sim.Started += () => _newRecord = false;
            sim.Bounced += () => audio.PlaySfx(_sounds.Bounce, 0.45f, Random.Range(0.92f, 1.08f));
            sim.Collected += p => audio.PlaySfx(p.Kind == PickupKind.Cacao ? _sounds.Cacao : _sounds.Pollito, 0.7f, Random.Range(0.97f, 1.1f));
            sim.ShieldLost += () => audio.PlaySfx(_sounds.ShieldLost, 0.8f);
            sim.Died += OnDied;
        }

        void OnDied(DeathCause cause)
        {
            AudioService.Instance.PlaySfx(cause == DeathCause.Fell ? _sounds.Fell : _sounds.Hit, 0.9f);
            _newRecord = _sim.Score > _data.bestScore;
            _data.bestScore = Mathf.Max(_data.bestScore, _sim.Score);
            _data.cacaoTotal += _sim.Cacao;
            _data.runs++;
            try { _saveFile.Save(_data); }
            catch (System.Exception e) { Debug.LogWarning($"[ChihuahuaJump] No se pudo guardar: {e.Message}"); }
        }

        void Update()
        {
            if (_sim == null) return;
            float dt = Mathf.Min(Time.deltaTime, 1f / 30f); // evita saltos grandes si hay tirones
            _input.Poll();
            _sim.Tick(dt, _input.Direction, _input.Tapped, _input.TargetX);
            Sync();
            _fps = Mathf.Lerp(_fps, 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f), 0.1f);
        }

        void Sync()
        {
            var c = _sim.Config;
            float tx = _sim.Trampoline.X;
            _owner.transform.position = new Vector3(tx, 0f, 0f);
            _trampoline.transform.position = new Vector3(tx, c.TrampolineY - 0.2f, 0f);

            var d = _sim.Dog;
            _dog.transform.position = new Vector3(d.X, d.Bottom, 0f);
            _aura.transform.position = new Vector3(d.X, d.Bottom - 0.15f, 0f);
            _aura.enabled = _sim.HasShield;
            _dog.enabled = !_sim.IsInvulnerable || Mathf.FloorToInt(Time.time * 12f) % 2 == 0;

            float visibleTop = _cam.transform.position.y + _cam.orthographicSize;
            var hazards = _sim.Hazards;
            Ensure(_hazardViews, hazards.Count, "Hazard", 6);
            Ensure(_warningViews, hazards.Count, "Warning", 20);
            for (int i = 0; i < _hazardViews.Count; i++)
            {
                var v = _hazardViews[i];
                var w = _warningViews[i];
                bool active = i < hazards.Count;
                if (!active) { v.gameObject.SetActive(false); w.gameObject.SetActive(false); continue; }
                var h = hazards[i];
                bool offscreen = h.Bottom > visibleTop;
                v.gameObject.SetActive(!offscreen);
                w.gameObject.SetActive(offscreen);
                if (offscreen)
                {
                    // Aviso parpadeante en el borde superior, en la columna por donde entrara
                    w.color = WarningColor;
                    w.enabled = Mathf.FloorToInt(Time.time * 6f) % 2 == 0;
                    w.transform.position = new Vector3(Mathf.Clamp(h.X, -c.HalfWidth + 0.3f, c.HalfWidth - 0.3f), visibleTop - 0.8f, 0f);
                    w.transform.localScale = new Vector3(0.35f, 0.6f, 1f);
                    continue;
                }
                v.transform.position = new Vector3(h.X, h.Bottom, 0f);
                v.transform.localScale = new Vector3(h.W, h.H, 1f);
                v.color = h.Kind switch
                {
                    HazardKind.Serpent => SerpentColor,
                    HazardKind.Jaguar => JaguarColor,
                    _ => EagleColor
                };
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
                v.transform.position = new Vector3(p.X, p.Bottom, 0f);
                v.transform.localScale = new Vector3(p.W, p.H, 1f);
                v.color = p.Kind == PickupKind.Cacao ? CacaoColor : PollitoColor;
                // Parpadea en su ultimo segundo y medio antes de desaparecer
                v.enabled = p.Life > 1.5f || Mathf.FloorToInt(Time.time * 8f) % 2 == 0;
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

        void OnGUI()
        {
            if (_sim == null) return;
            if (_hud == null) BuildStyles();

            float pad = Screen.width * 0.04f;
            float lineH = Screen.height * 0.045f;
            GUI.Label(new Rect(pad, pad, Screen.width, lineH), $"Rebotes: {_sim.Score}", _hud);
            GUI.Label(new Rect(pad, pad + lineH, Screen.width, lineH),
                $"Cacao: {_sim.Cacao}  (total {_data.cacaoTotal + (_sim.State == GameState.Playing ? _sim.Cacao : 0)}){(_sim.HasShield ? "   + Pollito" : "")}", _hud);
            var right = new GUIStyle(_hud) { alignment = TextAnchor.UpperRight };
            GUI.Label(new Rect(0, pad, Screen.width - pad, lineH), $"Record: {_sim.BestScore}", right);

            string msg = null;
            if (_sim.State == GameState.Ready)
                msg = "Salva a tu chihuahua\n\nDesliza el dedo: el trampolín\nva a donde apuntas\n\nCentro = sube recto\nOrillas = lo desvía\n\nToca para empezar";
            else if (_sim.State == GameState.Dead)
            {
                string cause = _sim.LastDeath == DeathCause.Fell ? "¡Se cayó!" : "¡Lo atrapó un enemigo!";
                msg = (_newRecord ? "¡Nuevo récord!\n" : "") + $"{cause}\n\n{_sim.Score} rebotes  -  {_sim.Cacao} cacao" + (_sim.CanRestart ? "\n\nToca para reintentar" : "");
            }
            if (msg != null) GUI.Label(new Rect(pad, 0, Screen.width - 2 * pad, Screen.height * 0.75f), msg, _center);

            if (Debug.isDebugBuild)
                GUI.Label(new Rect(pad, Screen.height - lineH * 1.2f, Screen.width, lineH), $"{_fps:0} fps  nivel {_sim.Level:0.00}", _small);
        }

        void BuildStyles()
        {
            var dark = new Color(0.12f, 0.1f, 0.08f);
            _hud = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(18, Screen.width / 18), fontStyle = FontStyle.Bold };
            _hud.normal.textColor = dark;
            _center = new GUIStyle(_hud) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
            _small = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(12, Screen.width / 36) };
            _small.normal.textColor = dark;
        }
    }
}
