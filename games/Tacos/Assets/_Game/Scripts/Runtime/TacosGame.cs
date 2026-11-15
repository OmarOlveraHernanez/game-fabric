using System;
using System.Collections.Generic;
using GameFactory.Core.Save;
using Tacos.Logic;
using Tacos.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Tacos
{
    /// <summary>
    /// Toda la interfaz del prototipo T1 (cuadros de colores, sin arte final): menu de turnos y taqueros,
    /// HUD, pista, clientes, la barra del puesto y la pantalla final. Solo dibuja la simulacion y
    /// le manda comandos; las reglas viven en Tacos.Logic.
    /// Medidas: pixeles de referencia con 1080 de alto; lo horizontal va en fracciones del ancho
    /// porque el ancho cambia mucho entre tablet (4:3) y telefono (20:9).
    /// </summary>
    public sealed class TacosGame : MonoBehaviour
    {
        static readonly string[] Nombres = { "Lupita", "Don Beto", "Karla", "Memo", "Sofi", "Rosa", "Toño", "Mari", "Pepe", "Ale", "Nacho", "Fer" };
        static readonly Color[] Avatares =
            { new Color32(0xFF, 0x4F, 0xA3, 0xFF), new Color32(0x2F, 0xC6, 0xB8, 0xFF), new Color32(0xFF, 0xC5, 0x3D, 0xFF),
              new Color32(0x8F, 0xDC, 0x4F, 0xFF), new Color32(0x9A, 0x7B, 0xFF, 0xFF), new Color32(0xFF, 0x8A, 0x3D, 0xFF) };

        static readonly Color PastorColor = new Color32(0xFF, 0x8A, 0x3D, 0xFF);
        static readonly Color AsadaColor = new Color32(0xC9, 0x65, 0x3A, 0xFF);
        static readonly Color TortillaColor = new Color32(0xE9, 0xC9, 0x8A, 0xFF);

        SaveFile<TacosData> _saveFile;
        TacosData _data;
        int _turno;
        TaqueroDef _taq = TaqueroDef.Guero;
        KitchenSimulation _sim;

        RectTransform _root, _menu, _game, _end, _counter;
        Text _hudTurno, _hudTiempo, _hudMonedas, _hudTaquero, _hint, _toast;
        GameObject _hintBar, _toastPanel;
        Image[] _hudStars;
        float _toastTime;

        // menu
        readonly List<Button> _turnoButtons = new List<Button>();
        readonly List<Text> _turnoTexts = new List<Text>();
        readonly List<Image[]> _turnoStars = new List<Image[]>();
        readonly List<Button> _taqButtons = new List<Button>();

        // clientes
        readonly Button[] _cards = new Button[3];
        readonly Text[] _cardName = new Text[3], _cardOrder = new Text[3];
        readonly Image[] _cardAvatar = new Image[3], _cardBar = new Image[3];
        readonly RectTransform[] _cardBarRt = new RectTransform[3];

        // barra (se reconstruye en cada turno)
        Button _tortillaButton, _trashButton;
        readonly List<Button> _comalSlots = new List<Button>();
        readonly List<Button> _grillSlots = new List<Button>();
        readonly List<Button> _plateButtons = new List<Button>();
        readonly Dictionary<Meat, Button> _meatButtons = new Dictionary<Meat, Button>();
        readonly Dictionary<Extra, Button> _extraButtons = new Dictionary<Extra, Button>();
        Component _pulsing;

        // fin
        Text _endTitle, _endStats, _endInfo;
        Image[] _endStars;
        Button _nextButton;

        void Awake()
        {
            _saveFile = new SaveFile<TacosData>("tacos");
            _data = _saveFile.Load();
            if (_data.bestStars == null || _data.bestStars.Length != LevelConfig.Turnos.Length)
                _data.bestStars = new int[LevelConfig.Turnos.Length];
            _taq = Array.Find(TaqueroDef.Starters, t => t.Id == _data.taquero) ?? TaqueroDef.Guero;

            var canvas = UiKit.CreateCanvas("TacosCanvas", 10);
            canvas.transform.SetParent(transform, false);
            UiKit.Stretch((RectTransform)UiKit.Panel(canvas.transform, "Fondo", UiKit.Night, false, false).transform);
            _root = UiKit.Stretch(UiKit.NewRect("ZonaSegura", canvas.transform));
            _root.gameObject.AddComponent<SafeArea>();

            BuildGame();
            BuildMenu();
            BuildEnd();
            ShowMenu();
        }

        // =============== utilidades de layout ===============

        /// <summary>Caja de x0 a x1 (fracciones del ancho del padre), "top" px desde arriba y "h" px de alto.</summary>
        static RectTransform Box(RectTransform rt, float x0, float x1, float top, float h, float gap = 0f)
        {
            rt.anchorMin = new Vector2(x0, 1f);
            rt.anchorMax = new Vector2(x1, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -top);
            rt.sizeDelta = new Vector2(-gap, h);
            return rt;
        }

        static RectTransform BoxOf(Component c, float x0, float x1, float top, float h, float gap = 0f) =>
            Box((RectTransform)c.transform, x0, x1, top, h, gap);

        // =============== construccion ===============

        void BuildGame()
        {
            _game = UiKit.Stretch(UiKit.NewRect("Juego", _root));

            // HUD
            var hud = Box(UiKit.NewRect("HUD", _game), 0f, 1f, 14f, 70f, 40f);
            _hudTurno = UiKit.Label(hud, "", 34, UiKit.Maiz, TextAnchor.MiddleLeft);
            BoxOf(_hudTurno, 0f, 0.14f, 0f, 70f);
            _hudTiempo = UiKit.Label(hud, "", 38, UiKit.Cream, TextAnchor.MiddleLeft);
            BoxOf(_hudTiempo, 0.14f, 0.3f, 0f, 70f);
            _hudMonedas = UiKit.Label(hud, "", 38, UiKit.Cream, TextAnchor.MiddleLeft);
            BoxOf(_hudMonedas, 0.3f, 0.48f, 0f, 70f);
            _hudStars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                _hudStars[i] = UiKit.Star(hud, "Estrella");
                BoxOf(_hudStars[i], 0.48f + i * 0.03f, 0.51f + i * 0.03f, 10f, 50f);
            }
            _hudTaquero = UiKit.Label(hud, "", 26, UiKit.Muted, TextAnchor.MiddleRight);
            BoxOf(_hudTaquero, 0.58f, 0.92f, 0f, 70f);
            var menuBtn = UiKit.MakeButton(hud, "Menu", "Menú", UiKit.Panel2, UiKit.Cream, 28, ShowMenu);
            BoxOf(menuBtn, 0.93f, 1f, 4f, 62f);

            // Pista (solo turno 1)
            var hintImg = UiKit.Panel(_game, "Pista", new Color(UiKit.Turq.r, UiKit.Turq.g, UiKit.Turq.b, 0.18f), true, false);
            BoxOf(hintImg, 0f, 1f, 96f, 64f, 40f);
            _hintBar = hintImg.gameObject;
            _hint = UiKit.FillLabel(hintImg.transform, "", 32, UiKit.Cream, TextAnchor.MiddleLeft, 18f);

            // Clientes
            for (int i = 0; i < 3; i++)
            {
                int slot = i;
                var card = UiKit.MakeButton(_game, "Cliente " + i, null, UiKit.Panel1, UiKit.Cream, 30,
                    () => Do(Command.Deliver(slot)));
                BoxOf(card, i / 3f, (i + 1) / 3f, 172f, 300f, 40f);
                _cards[i] = card;
                _cardAvatar[i] = UiKit.Panel(card.transform, "Avatar", Color.white, true, false);
                UiKit.TopLeft((RectTransform)_cardAvatar[i].transform, 20f, 20f, 52f, 52f);
                _cardName[i] = UiKit.Label(card.transform, "", 32, UiKit.Cream, TextAnchor.MiddleLeft);
                Box((RectTransform)_cardName[i].transform, 0f, 1f, 20f, 52f, 0f).offsetMin += new Vector2(86f, 0f);
                _cardOrder[i] = UiKit.Label(card.transform, "", 34, UiKit.Cream, TextAnchor.UpperLeft);
                Box((RectTransform)_cardOrder[i].transform, 0f, 1f, 90f, 150f, 40f);
                var barBg = UiKit.Panel(card.transform, "Paciencia", UiKit.Panel2, true, false);
                BoxOf(barBg, 0f, 1f, 254f, 24f, 40f);
                _cardBar[i] = UiKit.Panel(barBg.transform, "Relleno", UiKit.Limon, true, false);
                _cardBarRt[i] = UiKit.Stretch((RectTransform)_cardBar[i].transform);
            }

            // Barra del puesto
            var counterBg = UiKit.Panel(_game, "Barra", UiKit.Panel1, true, false);
            BoxOf(counterBg, 0f, 1f, 492f, 572f, 40f);
            _counter = (RectTransform)counterBg.transform;

            // Aviso flotante
            var toast = UiKit.Panel(_game, "Aviso", UiKit.Ink, true, false);
            BoxOf(toast, 0.25f, 0.75f, 400f, 76f);
            _toastPanel = toast.gameObject;
            _toast = UiKit.FillLabel(toast.transform, "", 32, UiKit.Cream, TextAnchor.MiddleCenter, 10f);
            _toastPanel.SetActive(false);
        }

        void BuildCounter(LevelConfig level)
        {
            for (int i = _counter.childCount - 1; i >= 0; i--) Destroy(_counter.GetChild(i).gameObject);
            _comalSlots.Clear(); _grillSlots.Clear(); _plateButtons.Clear(); _meatButtons.Clear(); _extraButtons.Clear();
            _tortillaButton = null;

            float[] cols = level.NeedsTortilla ? new[] { 0f, 0.19f, 0.43f, 0.7f, 1f } : new[] { 0f, 0.3f, 0.62f, 1f };
            int c = 0;
            const float pad = 28f;

            if (level.NeedsTortilla)
            {
                var col = Column("Comal", "TORTILLAS AL COMAL", cols[c], cols[c + 1]); c++;
                _tortillaButton = UiKit.MakeButton(col, "Tortilla", "+ Tortilla", TortillaColor, UiKit.Ink, 34,
                    () => Do(Command.AddTortilla()));
                BoxOf(_tortillaButton, 0f, 1f, 52f, 96f, pad);
                int slots = _sim.Comal.Length;
                for (int i = 0; i < slots; i++)
                {
                    int s = i;
                    var b = UiKit.MakeButton(col, "Comal " + i, "", UiKit.Panel2, UiKit.Cream, 26, () => Do(Command.TakeTortilla(s)));
                    float x = (i % 2) * 0.5f;
                    BoxOf(b, x, x + 0.5f, 164f + (i / 2) * 124f, 110f, pad);
                    _comalSlots.Add(b);
                }
            }

            {
                var col = Column("Plancha", "PLANCHA", cols[c], cols[c + 1]); c++;
                _meatButtons[Meat.Pastor] = UiKit.MakeButton(col, "Pastor", "Pastor", PastorColor, UiKit.Ink, 36,
                    () => Do(Command.StartMeat(Meat.Pastor)));
                BoxOf(_meatButtons[Meat.Pastor], 0f, 0.5f, 52f, 104f, pad);
                _meatButtons[Meat.Asada] = UiKit.MakeButton(col, "Asada", "Asada", AsadaColor, UiKit.Ink, 36,
                    () => Do(Command.StartMeat(Meat.Asada)));
                BoxOf(_meatButtons[Meat.Asada], 0.5f, 1f, 52f, 104f, pad);
                for (int i = 0; i < _sim.Grill.Length; i++)
                {
                    int s = i;
                    var b = UiKit.MakeButton(col, "Plancha " + i, "", UiKit.Panel2, UiKit.Cream, 28, () => Do(Command.TakeMeat(s)));
                    BoxOf(b, i * 0.5f, i * 0.5f + 0.5f, 176f, 200f, pad);
                    _grillSlots.Add(b);
                }
            }

            {
                var col = Column("Platos", "PLATOS", cols[c], cols[c + 1]); c++;
                for (int i = 0; i < _sim.Plates.Length; i++)
                {
                    int p = i;
                    var b = UiKit.MakeButton(col, "Plato " + i, "", UiKit.Plato, UiKit.Ink, 28, () => Do(Command.SelectPlate(p)));
                    BoxOf(b, i * 0.5f, i * 0.5f + 0.5f, 52f, 330f, pad);
                    var t = b.GetComponentInChildren<Text>();
                    t.alignment = TextAnchor.UpperCenter;
                    _plateButtons.Add(b);
                }
                _trashButton = UiKit.MakeButton(col, "Tirar", "Tirar plato elegido", UiKit.Panel2, UiKit.Chile, 28,
                    () => Do(Command.TrashPlate(_sim.SelectedPlate)));
                BoxOf(_trashButton, 0f, 1f, 400f, 80f, pad);
            }

            {
                var col = Column("Extras", "EXTRAS AL PLATO ELEGIDO", cols[c], cols[c + 1]);
                var all = Extras.All;
                for (int i = 0; i < all.Length; i++)
                {
                    var e = all[i];
                    var b = UiKit.MakeButton(col, "Extra " + e, ExtraName(e), ExtraColor(e), UiKit.Ink, 32,
                        () => Do(Command.ToggleExtra(e)));
                    float x = (i % 3) / 3f;
                    BoxOf(b, x, x + 1f / 3f, 52f + (i / 3) * 140f, 124f, pad * 0.8f);
                    _extraButtons[e] = b;
                }
            }
        }

        RectTransform Column(string name, string title, float x0, float x1)
        {
            var col = Box(UiKit.NewRect(name, _counter), x0, x1, 16f, 540f, 16f);
            var t = UiKit.Label(col, title, 22, UiKit.Muted, TextAnchor.MiddleLeft);
            Box((RectTransform)t.transform, 0f, 1f, 0f, 40f, 28f);
            return col;
        }

        void BuildMenu()
        {
            _menu = UiKit.Stretch(UiKit.NewRect("Menu", _root));
            UiKit.Stretch((RectTransform)UiKit.Panel(_menu, "Velo", UiKit.Night, false, true).transform);
            var title = UiKit.Label(_menu, "¡A TAQUEAR!", 96, UiKit.Maiz);
            BoxOf(title, 0f, 1f, 40f, 120f);
            var sub = UiKit.Label(_menu, "Prototipo T1 · cuadros de colores, sin arte final. Empieza por el turno 1: te guía paso a paso.", 30, UiKit.Muted);
            BoxOf(sub, 0.08f, 0.92f, 160f, 60f);

            var lt = UiKit.Label(_menu, "TURNO", 26, UiKit.Muted, TextAnchor.MiddleLeft);
            BoxOf(lt, 0.06f, 0.94f, 236f, 40f);
            for (int i = 0; i < LevelConfig.Turnos.Length; i++)
            {
                int t = i;
                var lvl = LevelConfig.Turnos[i];
                var b = UiKit.MakeButton(_menu, "Turno " + i, null, UiKit.Panel1, UiKit.Cream, 30, () => { _turno = t; RefreshMenu(); });
                BoxOf(b, 0.06f + i * 0.293f, 0.06f + (i + 1) * 0.293f, 280f, 200f, 24f);
                var name = UiKit.Label(b.transform, lvl.Name, 40, UiKit.Cream, TextAnchor.UpperLeft);
                Box((RectTransform)name.transform, 0f, 1f, 18f, 50f, 40f);
                var desc = UiKit.Label(b.transform, lvl.Description, 26, UiKit.Muted, TextAnchor.UpperLeft);
                Box((RectTransform)desc.transform, 0f, 1f, 72f, 70f, 40f);
                _turnoTexts.Add(desc);
                var stars = new Image[3];
                for (int s = 0; s < 3; s++)
                {
                    stars[s] = UiKit.Star(b.transform, "Estrella");
                    UiKit.TopLeft((RectTransform)stars[s].transform, 20f + s * 48f, 146f, 40f, 40f);
                }
                _turnoStars.Add(stars);
                _turnoButtons.Add(b);
            }

            var lq = UiKit.Label(_menu, "TAQUERO", 26, UiKit.Muted, TextAnchor.MiddleLeft);
            BoxOf(lq, 0.06f, 0.94f, 500f, 40f);
            for (int i = 0; i < TaqueroDef.Starters.Length; i++)
            {
                var taq = TaqueroDef.Starters[i];
                var b = UiKit.MakeButton(_menu, "Taquero " + taq.Id, null, UiKit.Panel1, UiKit.Cream, 30,
                    () => { _taq = taq; RefreshMenu(); });
                BoxOf(b, 0.06f + i * 0.293f, 0.06f + (i + 1) * 0.293f, 544f, 170f, 24f);
                var name = UiKit.Label(b.transform, taq.Name, 40, UiKit.Cream, TextAnchor.UpperLeft);
                Box((RectTransform)name.transform, 0f, 1f, 18f, 50f, 40f);
                var perk = UiKit.Label(b.transform, taq.Perk, 26, UiKit.Muted, TextAnchor.UpperLeft);
                Box((RectTransform)perk.transform, 0f, 1f, 74f, 80f, 40f);
                _taqButtons.Add(b);
            }

            var go = UiKit.MakeButton(_menu, "Empezar", "Empezar turno", UiKit.Chile, Color.white, 46, StartTurn);
            BoxOf(go, 0.33f, 0.67f, 760f, 120f);
        }

        void BuildEnd()
        {
            _end = UiKit.Stretch(UiKit.NewRect("Fin", _root));
            UiKit.Stretch((RectTransform)UiKit.Panel(_end, "Velo", new Color(0.08f, 0.07f, 0.12f, 0.94f), false, true).transform);
            _endTitle = UiKit.Label(_end, "", 90, UiKit.Maiz);
            BoxOf(_endTitle, 0f, 1f, 110f, 120f);
            _endStars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                _endStars[i] = UiKit.Star(_end, "Estrella");
                BoxOf(_endStars[i], 0.38f + i * 0.08f, 0.46f + i * 0.08f, 250f, 120f);
            }
            _endStats = UiKit.Label(_end, "", 44, UiKit.Cream);
            BoxOf(_endStats, 0.1f, 0.9f, 400f, 70f);
            _endInfo = UiKit.Label(_end, "", 30, UiKit.Muted);
            BoxOf(_endInfo, 0.1f, 0.9f, 480f, 110f);
            _nextButton = UiKit.MakeButton(_end, "Siguiente", "Siguiente turno", UiKit.Chile, Color.white, 40, () =>
            {
                _turno = Mathf.Min(_turno + 1, LevelConfig.Turnos.Length - 1);
                StartTurn();
            });
            BoxOf(_nextButton, 0.16f, 0.44f, 650f, 110f, 24f);
            var again = UiKit.MakeButton(_end, "Repetir", "Repetir", UiKit.Panel2, UiKit.Cream, 40, StartTurn);
            BoxOf(again, 0.44f, 0.64f, 650f, 110f, 24f);
            var menu = UiKit.MakeButton(_end, "MenuFin", "Menú", UiKit.Panel2, UiKit.Cream, 40, ShowMenu);
            BoxOf(menu, 0.64f, 0.84f, 650f, 110f, 24f);
        }

        // =============== flujo ===============

        bool Unlocked(int turno) => turno == 0 || _data.bestStars[turno - 1] > 0;

        void ShowMenu()
        {
            _sim = null;
            _menu.gameObject.SetActive(true);
            _game.gameObject.SetActive(false);
            _end.gameObject.SetActive(false);
            if (!Unlocked(_turno)) _turno = 0;
            RefreshMenu();
        }

        void RefreshMenu()
        {
            for (int i = 0; i < _turnoButtons.Count; i++)
            {
                bool open = Unlocked(i);
                _turnoButtons[i].interactable = open;
                _turnoTexts[i].text = open ? LevelConfig.Turnos[i].Description : "Gana 1 estrella en el turno anterior.";
                UiKit.Highlight(_turnoButtons[i], UiKit.Turq, i == _turno);
                for (int s = 0; s < 3; s++) _turnoStars[i][s].color = s < _data.bestStars[i] ? UiKit.Maiz : UiKit.Line;
            }
            for (int i = 0; i < _taqButtons.Count; i++)
                UiKit.Highlight(_taqButtons[i], UiKit.Turq, TaqueroDef.Starters[i] == _taq);
        }

        void StartTurn()
        {
            if (!Unlocked(_turno)) return;
            _data.taquero = _taq.Id;
            _sim = new KitchenSimulation(LevelConfig.Turnos[_turno], _taq, Environment.TickCount);
            _sim.OrderServed += (slot, pay, tip) => Toast($"+{pay} monedas" + (tip >= 4 ? " · ¡propinota!" : ""), UiKit.Limon);
            _sim.OrderWrong += slot => Toast("¡Eso no pedí!", UiKit.Chile);
            _sim.CustomerLeft += slot => Toast("Un cliente se cansó de esperar", UiKit.Chile);
            _sim.Finished += EndTurn;
            BuildCounter(_sim.Level);
            _menu.gameObject.SetActive(false);
            _end.gameObject.SetActive(false);
            _game.gameObject.SetActive(true);
            _hintBar.SetActive(_sim.Level.Hints);
            _toastTime = 0f;
            _toastPanel.SetActive(false);
            Refresh();
        }

        void EndTurn()
        {
            int stars = _sim.Stars;
            if (stars > _data.bestStars[_turno]) _data.bestStars[_turno] = stars;
            _saveFile.Save(_data);
            _endTitle.text = stars == 3 ? "¡Taquero legendario!" : stars > 0 ? "¡Buen turno!" : "Se acabó el turno";
            for (int i = 0; i < 3; i++) _endStars[i].color = i < stars ? UiKit.Maiz : UiKit.Line;
            _endStats.text = $"Servidos {_sim.Served}   ·   Fallados {_sim.Failed}   ·   Monedas {_sim.Coins}";
            var st = _sim.Level.Stars;
            bool last = _turno >= LevelConfig.Turnos.Length - 1;
            _endInfo.text = $"Estrellas a las {st[0]}, {st[1]} y {st[2]} monedas.\n" +
                (last ? "Es el último turno del prototipo." : stars > 0 ? "Ya puedes pasar al siguiente turno." : "Necesitas 1 estrella para abrir el siguiente turno.");
            _nextButton.gameObject.SetActive(!last && stars > 0);
            _end.gameObject.SetActive(true);
        }

        void Do(Command cmd)
        {
            if (_sim == null || _sim.IsFinished) return;
            var r = _sim.Apply(cmd);
            string msg = Message(r);
            if (msg != null) Toast(msg, UiKit.Chile);
            Refresh();
        }

        void Toast(string text, Color color)
        {
            _toast.text = text;
            _toast.color = color;
            _toastPanel.SetActive(true);
            _toastTime = 1.4f;
        }

        static string Message(CommandResult r)
        {
            switch (r)
            {
                case CommandResult.ComalFull: return "El comal está lleno";
                case CommandResult.EmptySlot: return null;
                case CommandResult.TortillaCold: return "Todavía está fría";
                case CommandResult.TortillaBurned: return "Tortilla quemada a la basura";
                case CommandResult.NoFreePlate: return "No hay plato libre";
                case CommandResult.GrillFull: return "La plancha está llena";
                case CommandResult.MeatRaw: return "Espera a que se ponga verde";
                case CommandResult.MeatBurned: return "Carne quemada a la basura";
                case CommandResult.NeedTortillaFirst: return "Primero pasa una tortilla caliente al plato";
                case CommandResult.PlatesFull: return "Los platos ya tienen carne: entrega o tira uno";
                case CommandResult.NeedMeatFirst: return "Primero pon la carne en el plato";
                case CommandResult.OneSalsaOnly: return "Una sola salsa por taco";
                case CommandResult.NoPlateReady: return "Ningún plato está listo para este cliente";
                default: return null;
            }
        }

        // =============== cada cuadro ===============

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame && _game.gameObject.activeSelf) ShowMenu();
            if (_sim == null) return;
            if (!_sim.IsFinished) _sim.Tick(Time.deltaTime);
            if (_toastTime > 0f)
            {
                _toastTime -= Time.deltaTime;
                if (_toastTime <= 0f) _toastPanel.SetActive(false);
            }
            Refresh();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && _sim != null && !_sim.IsFinished) ShowMenu(); // el turno no corre en segundo plano
        }

        void Refresh()
        {
            if (_sim == null) return;
            var s = _sim;
            var lvl = s.Level;
            _hudTurno.text = lvl.Name;
            int left = Mathf.CeilToInt(s.TimeLeft);
            _hudTiempo.text = $"Tiempo {left / 60}:{left % 60:00}";
            _hudMonedas.text = $"Monedas {s.Coins}";
            for (int i = 0; i < 3; i++) _hudStars[i].color = i < s.Stars ? UiKit.Maiz : UiKit.Line;
            _hudTaquero.text = s.Taquero.Name + " · " + s.Taquero.Perk;

            for (int i = 0; i < 3; i++)
            {
                var c = s.Customers[i];
                bool open = i < lvl.MaxCustomers;
                _cards[i].interactable = open && c != null;
                _cardAvatar[i].gameObject.SetActive(c != null);
                _cardBarRt[i].parent.gameObject.SetActive(c != null);
                if (!open) { _cardName[i].text = "Cerrado en este turno"; _cardOrder[i].text = ""; UiKit.Highlight(_cards[i], UiKit.Limon, false); continue; }
                if (c == null) { _cardName[i].text = "Esperando cliente…"; _cardOrder[i].text = ""; UiKit.Highlight(_cards[i], UiKit.Limon, false); continue; }
                bool ready = s.HasPlateFor(i);
                _cardAvatar[i].color = Avatares[c.Id % Avatares.Length];
                _cardName[i].text = Nombres[(c.Id * 7) % Nombres.Length] + (ready ? $"  <color={UiKit.Hex(UiKit.Limon)}>¡Entrégale!</color>" : " quiere:");
                _cardOrder[i].text = OrderText(c.Order.Meat, c.Order.Extras, false);
                float r = c.PatienceRatio;
                _cardBarRt[i].anchorMax = new Vector2(r, 1f);
                _cardBar[i].color = r > 0.5f ? UiKit.Limon : r > 0.25f ? UiKit.Maiz : UiKit.Chile;
                UiKit.Highlight(_cards[i], UiKit.Limon, ready);
            }

            for (int i = 0; i < _comalSlots.Count; i++)
            {
                var t = s.Comal[i];
                var img = (Image)_comalSlots[i].targetGraphic;
                var label = _comalSlots[i].GetComponentInChildren<Text>();
                if (t == null) { img.color = UiKit.Panel2; label.text = "vacío"; label.color = UiKit.Muted; continue; }
                var st = s.TortillaState(t);
                img.color = st == CookState.Burned ? new Color(0.16f, 0.13f, 0.13f) : st == CookState.Ready ? new Color32(0x2D, 0x4A, 0x2A, 0xFF) : new Color32(0x3A, 0x31, 0x50, 0xFF);
                label.color = UiKit.Cream;
                label.text = st == CookState.Burned ? "Quemada" : st == CookState.Ready ? "¡Caliente!" : $"Calentando {Mathf.CeilToInt(s.Rules.TortillaHotTime - t.Age)}";
            }

            for (int i = 0; i < _grillSlots.Count; i++)
            {
                var m = s.Grill[i];
                var img = (Image)_grillSlots[i].targetGraphic;
                var label = _grillSlots[i].GetComponentInChildren<Text>();
                if (m == null) { img.color = UiKit.Panel2; label.text = "vacío"; label.color = UiKit.Muted; continue; }
                var st = s.MeatState(m);
                img.color = st == CookState.Burned ? new Color(0.16f, 0.13f, 0.13f) : st == CookState.Ready ? new Color32(0x2D, 0x4A, 0x2A, 0xFF) : new Color32(0x3A, 0x31, 0x50, 0xFF);
                label.color = UiKit.Cream;
                string name = m.Meat == Meat.Pastor ? "Pastor" : "Asada";
                label.text = st == CookState.Burned ? name + "\nquemada"
                           : st == CookState.Ready ? $"{name}\n¡lista! ({Mathf.CeilToInt(s.Rules.MeatBurnTime - m.Age)})"
                           : $"{name}\n{Mathf.CeilToInt(s.CookTime - m.Age)} s";
            }

            for (int i = 0; i < _plateButtons.Count; i++)
            {
                var p = s.Plates[i];
                var label = _plateButtons[i].GetComponentInChildren<Text>();
                label.text = p.IsEmpty ? $"Plato {i + 1}\nvacío" : $"Plato {i + 1}\n" + OrderText(p.Meat, p.Extras, p.Tortilla, true);
                UiKit.Highlight(_plateButtons[i], UiKit.Turq, i == s.SelectedPlate);
            }

            // Pista: texto y el boton que toca brilla
            if (_pulsing != null) { _pulsing.transform.localScale = Vector3.one; _pulsing = null; }
            if (lvl.Hints && !s.IsFinished)
            {
                var h = Hints.Next(s);
                _hint.text = "SIGUIENTE:  " + HintText(h);
                _pulsing = HintTarget(h);
                if (_pulsing != null) _pulsing.transform.localScale = Vector3.one * (1f + 0.05f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f)));
            }
        }

        // =============== textos ===============

        static string ExtraName(Extra e)
        {
            switch (e)
            {
                case Extra.Cebolla: return "Cebolla";
                case Extra.Cilantro: return "Cilantro";
                case Extra.Pina: return "Piña";
                case Extra.SalsaVerde: return "S. verde";
                case Extra.SalsaRoja: return "S. roja";
                case Extra.Limon: return "Limón";
                default: return e.ToString();
            }
        }

        static Color ExtraColor(Extra e)
        {
            switch (e)
            {
                case Extra.Cebolla: return new Color32(0xF2, 0xEE, 0xE8, 0xFF);
                case Extra.Cilantro: return new Color32(0x5C, 0xC2, 0x4A, 0xFF);
                case Extra.Pina: return new Color32(0xFF, 0xD8, 0x4A, 0xFF);
                case Extra.SalsaVerde: return new Color32(0x9B, 0xE3, 0x6B, 0xFF);
                case Extra.SalsaRoja: return new Color32(0xFF, 0x7A, 0x6A, 0xFF);
                case Extra.Limon: return new Color32(0xC8, 0xF0, 0x6A, 0xFF);
                default: return Color.white;
            }
        }

        /// <summary>Pedido o plato como texto con colores. onLight = sobre el plato claro (texto oscuro).</summary>
        static string OrderText(Meat meat, Extra extras, bool tortilla, bool onLight = false)
        {
            var parts = new List<string>();
            if (tortilla && onLight) parts.Add("Tortilla");
            if (meat != Meat.None)
            {
                string mc = onLight ? "#8A3A10" : UiKit.Hex(meat == Meat.Pastor ? PastorColor : AsadaColor);
                parts.Add($"<color={mc}>{(meat == Meat.Pastor ? "PASTOR" : "ASADA")}</color>");
            }
            foreach (var e in Extras.All)
                if (Extras.Has(extras, e))
                    parts.Add(onLight ? ExtraName(e) : $"<color={UiKit.Hex(ExtraColor(e))}>{ExtraName(e)}</color>");
            return string.Join(onLight ? "\n" : "  ·  ", parts);
        }

        string HintText(Hint h)
        {
            switch (h.Kind)
            {
                case HintKind.WaitCustomer: return "Espera al siguiente cliente…";
                case HintKind.StartMeat: return $"Toca {(h.Meat == Meat.Pastor ? "Pastor" : "Asada")} para ponerla en la plancha";
                case HintKind.WaitMeat: return "La carne se está cocinando… espera a que se ponga verde";
                case HintKind.TakeMeat: return "¡La carne está lista! Tócala para pasarla al plato";
                case HintKind.AddTortilla: return "Pon una tortilla en el comal";
                case HintKind.WaitTortilla: return "La tortilla se está calentando…";
                case HintKind.TakeTortilla: return "Toca la tortilla caliente para pasarla al plato";
                case HintKind.SelectPlate: return "Toca el plato para elegirlo";
                case HintKind.AddExtra: return $"Agrega {ExtraName(h.Extra)} al plato";
                case HintKind.TrashPlate: return "Tira el plato que no le sirve a nadie";
                case HintKind.Deliver: return "¡Listo! Toca al cliente para entregarle";
                default: return "";
            }
        }

        Component HintTarget(Hint h)
        {
            switch (h.Kind)
            {
                case HintKind.StartMeat: return _meatButtons.TryGetValue(h.Meat, out var mb) ? mb : null;
                case HintKind.TakeMeat: return h.Slot >= 0 && h.Slot < _grillSlots.Count ? _grillSlots[h.Slot] : null;
                case HintKind.AddTortilla: return _tortillaButton;
                case HintKind.TakeTortilla: return h.Slot >= 0 && h.Slot < _comalSlots.Count ? _comalSlots[h.Slot] : null;
                case HintKind.SelectPlate: return h.Plate >= 0 && h.Plate < _plateButtons.Count ? _plateButtons[h.Plate] : null;
                case HintKind.TrashPlate: return _trashButton;
                case HintKind.AddExtra: return _extraButtons.TryGetValue(h.Extra, out var eb) ? eb : null;
                case HintKind.Deliver: return h.Customer >= 0 ? _cards[h.Customer] : null;
                default: return null;
            }
        }
    }
}
