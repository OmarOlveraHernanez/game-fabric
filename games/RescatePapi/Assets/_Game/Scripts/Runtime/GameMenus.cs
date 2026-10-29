using System;
using GameFactory.Core.Audio;
using RescatePapi.Logic;
using RescatePapi.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RescatePapi
{
    /// <summary>
    /// Pantallas del juego (uGUI armado por codigo): inicio, HUD, pausa y fin de partida.
    /// Solo muestra el estado de la simulacion y avisa con eventos; RunnerView decide que hacer.
    /// </summary>
    public sealed class GameMenus : MonoBehaviour
    {
        public event Action PlayPressed, RestartPressed, HomePressed;
        public bool Paused { get; private set; }

        RunnerSimulation _sim;
        GameData _data;
        AudioClip _click;
        GameObject _title, _hud, _pause, _over, _pauseButton, _shield;
        RectTransform _playButton, _pauseCard, _overCard;
        Text _titleStats, _score, _coins, _record, _overTitle, _overStats;
        readonly Text[] _soundLabels = new Text[2];
        bool _newRecord;
        int _shownScore = -1, _shownCoins = -1, _shownBest = -1, _shownTotal = -1;
        float _cardTime;

        public void Init(RunnerSimulation sim, GameData data, Sprite coinIcon, Sprite shieldIcon, AudioClip click)
        {
            _sim = sim;
            _data = data;
            _click = click;
            var canvas = UiKit.CreateCanvas("MenusCanvas", 100);
            canvas.transform.SetParent(transform, false);
            var safe = UiKit.Stretch(UiKit.NewRect("ZonaSegura", canvas.transform));
            safe.gameObject.AddComponent<SafeArea>();
            BuildTitle(safe);
            BuildHud(safe, coinIcon, shieldIcon);
            BuildPause(canvas.transform);
            BuildGameOver(canvas.transform);
            RefreshSoundLabels();
            Update();
        }

        public void SetNewRecord(bool value) => _newRecord = value;

        public void SetPaused(bool paused)
        {
            if (paused && (_sim == null || _sim.State != RunState.Running)) return;
            Paused = paused;
        }

        // ---------- construccion ----------

        void BuildTitle(Transform root)
        {
            var rt = UiKit.Stretch(UiKit.NewRect("Inicio", root));
            _title = rt.gameObject;

            var title = UiKit.Label(rt, "¡RESCATE PAPI!", 150, UiKit.Gold);
            UiKit.Place((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1750f, 190f));
            var sub = UiKit.Label(rt, "¡Papi, salva a tu dueño! Está atrapado en la pirámide", 46, UiKit.Amate);
            UiKit.Place((RectTransform)sub.transform, new Vector2(0.5f, 1f), new Vector2(0f, -235f), new Vector2(1650f, 80f));

            var play = UiKit.MakeButton(rt, "¡JUGAR!", UiKit.Gold, UiKit.Ink, 90, () => { Click(); PlayPressed?.Invoke(); });
            _playButton = (RectTransform)play.transform;
            UiKit.Place(_playButton, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(560f, 170f));

            _titleStats = UiKit.Label(rt, "", 44, Color.white);
            UiKit.Place((RectTransform)_titleStats.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(1400f, 70f));

            var hint = UiKit.Label(rt, "Toca: salto corto   ·   Mantén: salto alto   ·   Al águila no la saltes", 36, Color.white);
            UiKit.Place((RectTransform)hint.transform, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1800f, 70f));

            var sound = UiKit.MakeButton(rt, "", UiKit.Turq, Color.white, 40, ToggleSound);
            UiKit.Place((RectTransform)sound.transform, new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(340f, 100f));
            _soundLabels[0] = UiKit.ButtonLabel(sound);
        }

        void BuildHud(Transform root, Sprite coinIcon, Sprite shieldIcon)
        {
            var rt = UiKit.Stretch(UiKit.NewRect("HUD", root));
            _hud = rt.gameObject;

            _score = UiKit.Label(rt, "0", 72, Color.white, TextAnchor.UpperLeft);
            UiKit.Place((RectTransform)_score.transform, new Vector2(0f, 1f), new Vector2(40f, -25f), new Vector2(700f, 100f));

            var coin = UiKit.Icon(rt, coinIcon);
            if (coinIcon == null) coin.color = UiKit.Gold;
            UiKit.Place(coin.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -130f), new Vector2(72f, 72f));
            _coins = UiKit.Label(rt, "x 0", 56, Color.white, TextAnchor.MiddleLeft);
            UiKit.Place((RectTransform)_coins.transform, new Vector2(0f, 1f), new Vector2(128f, -130f), new Vector2(500f, 72f));

            var shield = UiKit.NewRect("Escudo", rt);
            _shield = shield.gameObject;
            UiKit.Place(shield, new Vector2(0f, 1f), new Vector2(40f, -220f), new Vector2(420f, 90f));
            var tamal = UiKit.Icon(shield, shieldIcon);
            if (shieldIcon == null) tamal.color = UiKit.Gold;
            UiKit.Place(tamal.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(70f, 90f));
            var shieldText = UiKit.Label(shield, "¡Escudo!", 42, UiKit.Gold, TextAnchor.MiddleLeft);
            UiKit.Place((RectTransform)shieldText.transform, new Vector2(0f, 0.5f), new Vector2(88f, 0f), new Vector2(320f, 80f));

            _record = UiKit.Label(rt, "", 40, Color.white);
            UiKit.Place((RectTransform)_record.transform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(700f, 60f));

            var pause = UiKit.MakeButton(rt, "II", UiKit.Turq, Color.white, 60, () => { Click(); SetPaused(true); });
            _pauseButton = pause.gameObject;
            UiKit.Place((RectTransform)pause.transform, new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(130f, 130f));
        }

        void BuildPause(Transform canvas)
        {
            var rt = UiKit.Stretch(UiKit.NewRect("Pausa", canvas));
            _pause = rt.gameObject;
            UiKit.Stretch(UiKit.Panel(rt, "Oscuro", new Color(0f, 0f, 0f, 0.55f), false, true).rectTransform);
            _pauseCard = Card(rt, new Vector2(760f, 640f));

            var title = UiKit.Label(_pauseCard, "Pausa", 100, UiKit.Ink, TextAnchor.MiddleCenter, false);
            UiKit.Place((RectTransform)title.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 225f), new Vector2(700f, 130f));
            var resume = UiKit.MakeButton(_pauseCard, "Continuar", UiKit.Gold, UiKit.Ink, 64, () => { Click(); SetPaused(false); });
            UiKit.Place((RectTransform)resume.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(540f, 140f));
            var sound = UiKit.MakeButton(_pauseCard, "", UiKit.Turq, Color.white, 44, ToggleSound);
            UiKit.Place((RectTransform)sound.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -90f), new Vector2(540f, 110f));
            _soundLabels[1] = UiKit.ButtonLabel(sound);
            var home = UiKit.MakeButton(_pauseCard, "Inicio", UiKit.Grana, Color.white, 48, () => { Click(); HomePressed?.Invoke(); });
            UiKit.Place((RectTransform)home.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -225f), new Vector2(540f, 110f));
        }

        void BuildGameOver(Transform canvas)
        {
            var rt = UiKit.Stretch(UiKit.NewRect("FinPartida", canvas));
            _over = rt.gameObject;
            UiKit.Stretch(UiKit.Panel(rt, "Oscuro", new Color(0f, 0f, 0f, 0.45f), false, true).rectTransform);
            _overCard = Card(rt, new Vector2(900f, 740f));

            _overTitle = UiKit.Label(_overCard, "", 96, UiKit.Grana);
            UiKit.Place((RectTransform)_overTitle.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 265f), new Vector2(860f, 130f));
            _overStats = UiKit.Label(_overCard, "", 54, UiKit.Ink, TextAnchor.MiddleCenter, false);
            UiKit.Place((RectTransform)_overStats.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 75f), new Vector2(800f, 240f));
            var again = UiKit.MakeButton(_overCard, "Otra vez", UiKit.Gold, UiKit.Ink, 70, () => { Click(); RestartPressed?.Invoke(); });
            UiKit.Place((RectTransform)again.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(580f, 150f));
            var home = UiKit.MakeButton(_overCard, "Inicio", UiKit.Turq, Color.white, 50, () => { Click(); HomePressed?.Invoke(); });
            UiKit.Place((RectTransform)home.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -290f), new Vector2(580f, 110f));
        }

        static RectTransform Card(Transform parent, Vector2 size)
        {
            var card = UiKit.Panel(parent, "Tarjeta", UiKit.Amate, true, true);
            var outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = UiKit.Ink;
            outline.effectDistance = new Vector2(6f, -6f);
            return UiKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, size);
        }

        // ---------- cada frame ----------

        void Update()
        {
            if (_sim == null) return;
            var state = _sim.State;
            if (Paused && state != RunState.Running) Paused = false;
            bool pauseVisible = Paused;
            bool overVisible = state == RunState.Dead && _sim.CanRestart;

            Show(_title, state == RunState.Ready);
            Show(_hud, state != RunState.Ready);
            Show(_pauseButton, state == RunState.Running && !Paused);
            if (Show(_pause, pauseVisible) && pauseVisible) _cardTime = 0f;
            if (Show(_over, overVisible) && overVisible) { _cardTime = 0f; FillGameOver(); }

            RefreshTexts(state);
            Animate();
            HandleKeys(state, overVisible);
        }

        void RefreshTexts(RunState state)
        {
            if (state == RunState.Ready)
            {
                if (_shownBest != _data.bestScore || _shownTotal != _data.cacaoTotal)
                {
                    _shownBest = _data.bestScore;
                    _shownTotal = _data.cacaoTotal;
                    _titleStats.text = $"Récord: {_shownBest}      Monedas: {_shownTotal}";
                    _shownScore = -1; // forzar el HUD al empezar
                }
                return;
            }
            if (_shownScore != _sim.Score)
            {
                _shownScore = _sim.Score;
                _score.text = _shownScore.ToString();
                _record.text = $"Récord {Mathf.Max(_sim.BestScore, _shownScore)}";
            }
            if (_shownCoins != _sim.Cacao)
            {
                _shownCoins = _sim.Cacao;
                _coins.text = $"x {_shownCoins}";
            }
            Show(_shield, _sim.HasShield);
        }

        void FillGameOver()
        {
            _overTitle.text = _newRecord ? "¡Nuevo récord!" : "¡Ay, Papi!";
            _overTitle.color = _newRecord ? UiKit.Gold : UiKit.Grana;
            _overStats.text = $"Puntos: {_sim.Score}\nMonedas: +{_sim.Cacao}\nRécord: {_data.bestScore}";
        }

        void Animate()
        {
            float t = Time.unscaledTime;
            if (_title.activeSelf)
            {
                float s = 1f + 0.04f * Mathf.Sin(t * 4f);
                _playButton.localScale = new Vector3(s, s, 1f);
            }
            _cardTime += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(_cardTime / 0.15f);
            float pop = Mathf.Lerp(0.85f, 1f, 1f - (1f - k) * (1f - k));
            if (_pause.activeSelf) _pauseCard.localScale = new Vector3(pop, pop, 1f);
            if (_over.activeSelf) _overCard.localScale = new Vector3(pop, pop, 1f);
        }

        void HandleKeys(RunState state, bool overVisible)
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            // Escape en PC = boton "atras" en Android
            if (kb.escapeKey.wasPressedThisFrame && state == RunState.Running) SetPaused(!Paused);
            if (overVisible && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) RestartPressed?.Invoke();
        }

        void ToggleSound()
        {
            var audio = AudioService.Instance;
            audio.Muted = !audio.Muted;
            RefreshSoundLabels();
            Click();
        }

        void RefreshSoundLabels()
        {
            string text = AudioService.Instance.Muted ? "Sonido: No" : "Sonido: Sí";
            foreach (var label in _soundLabels) if (label != null) label.text = text;
        }

        void Click()
        {
            if (_click != null) AudioService.Instance.PlaySfx(_click, 0.5f);
        }

        /// <summary>Activa/desactiva y dice si cambio.</summary>
        static bool Show(GameObject go, bool visible)
        {
            if (go.activeSelf == visible) return false;
            go.SetActive(visible);
            return true;
        }
    }
}
