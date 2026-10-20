using System.Collections.Generic;
using GameFactory.Core.Audio;
using GameFactory.Core.Controls;
using GameFactory.Core.Save;
using RescatePapi.Logic;
using UnityEngine;

namespace RescatePapi
{
    /// <summary>
    /// Conecta RunnerSimulation con Unity: lee input, avanza la simulacion y dibuja con formas de color.
    /// HUD con OnGUI solo para el prototipo; el arte estilo codice llega despues.
    /// </summary>
    public sealed class RunnerView : MonoBehaviour
    {
        // Paleta provisional inspirada en codices
        static readonly Color OwnerColor   = new Color(0.10f, 0.55f, 0.60f); // turquesa
        static readonly Color ShieldColor  = new Color(1.00f, 0.85f, 0.25f); // aura del pollito
        static readonly Color SerpentColor = new Color(0.20f, 0.60f, 0.30f); // jade
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
        SpriteRenderer _player, _aura;
        GUIStyle _hud, _center, _small;
        float _fps;

        public void Init(RunnerSimulation sim, Sprite sprite)
        {
            _sim = sim;
            _sprite = sprite;
            var c = sim.Config;
            _aura = CreateBox("ShieldAura", ShieldColor, 9);
            _aura.transform.localScale = new Vector3(c.PlayerWidth + 0.3f, c.PlayerHeight + 0.3f, 1f);
            _player = CreateBox("Owner", OwnerColor, 10);
            _player.transform.localScale = new Vector3(c.PlayerWidth, c.PlayerHeight, 1f);

            // Guardado (Core: SaveFile) y sonidos (Core: AudioService + ToneSynth)
            _saveFile = new SaveFile<GameData>("rescatepapi");
            _data = _saveFile.Load();
            sim.SetBestScore(_data.bestScore);
            _sounds = new GameSounds();
            var audio = AudioService.Instance;
            sim.Started += () => { _newRecord = false; audio.PlaySfx(_sounds.Start, 0.6f); };
            sim.Jump.Jumped += () => audio.PlaySfx(_sounds.Jump, 0.5f, Random.Range(0.95f, 1.05f));
            sim.Collected += p => audio.PlaySfx(p.Kind == PickupKind.Cacao ? _sounds.Cacao : _sounds.Pollito, 0.7f, Random.Range(0.97f, 1.1f));
            sim.ShieldLost += () => audio.PlaySfx(_sounds.ShieldLost, 0.8f);
            sim.Died += OnDied;
        }

        void OnDied()
        {
            AudioService.Instance.PlaySfx(_sounds.Die, 0.9f);
            _newRecord = _sim.Score > _data.bestScore;
            _data.bestScore = Mathf.Max(_data.bestScore, _sim.Score);
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
            _sim.Tick(dt, _input.PressedThisFrame, _input.Held);
            Sync();
            _fps = Mathf.Lerp(_fps, 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f), 0.1f);
        }

        void Sync()
        {
            var c = _sim.Config;
            var playerPos = new Vector3(c.PlayerX, _sim.Jump.Y, 0f);
            _player.transform.position = playerPos;
            _aura.transform.position = playerPos + new Vector3(0f, -0.15f, 0f);
            _aura.enabled = _sim.HasShield;
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
                v.transform.position = new Vector3(h.X, h.Bottom, 0f);
                v.transform.localScale = new Vector3(h.Width, h.Height, 1f);
                v.color = h.Kind switch
                {
                    HazardKind.Serpent => SerpentColor,
                    HazardKind.Jaguar => h.IsCharging ? JaguarCharge : JaguarColor,
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
                v.transform.localScale = new Vector3(p.Size, p.Size, 1f);
                v.color = p.Kind == PickupKind.Cacao ? CacaoColor : PollitoColor;
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

            float pad = Screen.height * 0.03f;
            float lineH = Screen.height * 0.08f;
            GUI.Label(new Rect(pad, pad, Screen.width * 0.5f, lineH), $"Puntos: {_sim.Score}", _hud);
            GUI.Label(new Rect(pad, pad + lineH, Screen.width * 0.6f, lineH),
                $"Cacao: {_sim.Cacao}  (total {_data.cacaoTotal + (_sim.State == RunState.Running ? _sim.Cacao : 0)}){(_sim.HasShield ? "   + Pollito" : "")}", _hud);
            var right = new GUIStyle(_hud) { alignment = TextAnchor.UpperRight };
            GUI.Label(new Rect(Screen.width * 0.5f - pad, pad, Screen.width * 0.5f, lineH), $"Record: {_sim.BestScore}", right);

            string msg = _sim.State switch
            {
                RunState.Ready => "Rescata a Papi\n\nToca = salto corto  |  Mantener = salto alto\nAguila: no saltes",
                RunState.Dead => (_newRecord ? "¡Nuevo récord!\n" : "Game Over\n")
                    + $"{_sim.Score} puntos  -  {_sim.Cacao} cacao"
                    + (_sim.CanRestart ? "\n\nToca para reintentar" : ""),
                _ => null
            };
            if (msg != null) GUI.Label(new Rect(0, 0, Screen.width, Screen.height * 0.8f), msg, _center);

            if (Debug.isDebugBuild)
                GUI.Label(new Rect(pad, Screen.height - pad * 2.5f, Screen.width, pad * 2), $"{_fps:0} fps  vel {_sim.Speed:0.0}", _small);
        }

        void BuildStyles()
        {
            var dark = new Color(0.12f, 0.1f, 0.08f);
            _hud = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(18, Screen.height / 18), fontStyle = FontStyle.Bold };
            _hud.normal.textColor = dark;
            _center = new GUIStyle(_hud) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
            _small = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(12, Screen.height / 40) };
            _small.normal.textColor = dark;
        }
    }
}
