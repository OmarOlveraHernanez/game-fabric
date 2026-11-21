using System;
using System.Collections.Generic;
using GameFactory.Core.Audio;
using GameFactory.Core.Save;
using Tacos.Logic;
using Tacos.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Tacos
{
    /// <summary>
    /// Controla las pantallas (mapa de zonas, calle de misiones, tienda, juego y final), el guardado y el sonido.
    /// Solo dibuja la simulacion y le manda comandos; las reglas viven en Tacos.Logic.
    /// Partes: TacosGame.Maps (mapas), TacosGame.Hud (HUD, power-ups, franja del taquero), TutorialOverlay, KitchenScene.
    /// Medidas: pixeles de referencia con 1080 de alto.
    /// </summary>
    public sealed partial class TacosGame : MonoBehaviour
    {
        SaveFile<TacosData> _saveFile;
        TacosData _data;
        int _mission;
        CampaignProgress P => _data.progress;
        /// <summary>
        /// MODO PRUEBA: todos los mundos y misiones abiertos para probar en la tablet.
        /// ¡PONER EN false ANTES DE PUBLICAR! (pendiente en claude/mundos-puestos.md, "Antes de publicar")
        /// </summary>
        public static readonly bool TodoAbierto = true;

        /// <summary>La mision (de este mundo) se puede jugar.</summary>
        bool Unlocked(int mission) => TodoAbierto || P.IsUnlocked(G(mission));

        // Mundos: cada uno con sus 12 misiones (LevelConfig.Worlds). El guardado las numera seguidas: mundo * 12 + mision.
        const int PerWorld = 12;
        int _playWorld;
        LevelConfig[] Missions => LevelConfig.Worlds[_playWorld];
        int G(int mission) => _playWorld * PerWorld + mission;
        static int AllMissions { get { int n = 0; foreach (var w in LevelConfig.Worlds) n += w.Length; return n; } }

        /// <summary>Estrellas de un mundo (las del mapa de su calle).</summary>
        int WorldStars(int world)
        {
            int n = 0;
            for (int i = 0; i < PerWorld; i++) { int g = world * PerWorld + i; if (g < P.stars.Length) n += P.stars[g]; }
            return n;
        }
        TaqueroDef _taq = TaqueroDef.Guero;
        KitchenSimulation _sim;

        RectTransform _root, _game, _end, _shop;
        Text _toast;
        GameObject _toastPanel;
        float _toastTime;
        bool _paused; // franja del power-up o explicacion del Tio: el turno no avanza

        KitchenScene _scene;
        TutorialOverlay _tutorial;
        TacosSounds _snd;
        static AudioService Audio => AudioService.Instance;

        // fin

        void Awake()
        {
            _saveFile = new SaveFile<TacosData>("tacos");
            _data = _saveFile.Load();
            if (_data.progress == null) _data.progress = new CampaignProgress();
            _data.progress.Normalize(AllMissions, UpgradeDef.All.Length);
            if (_data.version < 2)
            {
                // Guardado del prototipo T1: sus 3 turnos son las misiones 1 a 3
                if (_data.bestStars != null)
                    for (int i = 0; i < _data.bestStars.Length && i < 3; i++) _data.progress.stars[i] = Math.Max(_data.progress.stars[i], _data.bestStars[i]);
                _data.version = 2;
                _saveFile.Save(_data);
            }
            if (_data.version < 3)
            {
                // Version 3: ahora se arrastra. El tutorial de las misiones 1 y 3 vuelve a salir una vez para enseñarlo.
                _data.introsSeen &= ~(1 | 4);
                _data.guidedSeen &= ~(1 | 4);
                _data.version = 3;
                _saveFile.Save(_data);
            }
            if (_data.version < 4) { _data.FitTutorials(AllMissions); _saveFile.Save(_data); }
            _data.FitTutorials(AllMissions);
            _taq = Array.Find(TaqueroDef.Starters, t => t.Id == _data.taquero) ?? TaqueroDef.Guero; // se guarda el lugar como taquero
            _mission = FirstOpenMission();

            var canvas = UiKit.CreateCanvas("TacosCanvas", 10);
            canvas.transform.SetParent(transform, false);
            UiKit.Stretch((RectTransform)UiKit.Panel(canvas.transform, "Fondo", UiKit.Night, false, false).transform);
            _snd = new TacosSounds();
            UiKit.StarOverride = TacosArt.Get("ui_estrella");
            UiKit.Clicked = () => Audio.PlaySfx(_snd.Click, 0.35f);
            _root = UiKit.Stretch(UiKit.NewRect("ZonaSegura", canvas.transform));
            _root.gameObject.AddComponent<SafeArea>();

            BuildGame();
            BuildWorld();
            BuildZone();
            _zoneBuiltFor = _playWorld;
            BuildShop();
            BuildEnd();
            ShowWorld();
        }

        /// <summary>La primera mision abierta que todavia no tiene 3 estrellas (para empezar ahi).</summary>
        int FirstOpenMission()
        {
            int m = 0;
            for (int i = 0; i < Missions.Length; i++)
                if (Unlocked(i)) { m = i; if (P.stars[G(i)] < 3) break; }
            return m;
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

        /// <summary>Caja pegada a una esquina: ancla (0..1), posicion y tamaño en px de referencia.</summary>
        static RectTransform Corner(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        // =============== construccion ===============

        void BuildGame()
        {
            _game = UiKit.Stretch(UiKit.NewRect("Juego", _root));
            _scene = new KitchenScene(_game, Do);
            _scene.Notice += text => { Toast(text, UiKit.Maiz); Audio.PlaySfx(_snd.Error, 0.4f); };
            BuildHud();

            var toast = UiKit.Panel(_game, "Aviso", new Color(UiKit.Ink.r, UiKit.Ink.g, UiKit.Ink.b, 0.92f), true, false);
            BoxOf(toast, 0.3f, 0.7f, 470f, 76f);
            _toastPanel = toast.gameObject;
            _toast = UiKit.FillLabel(toast.transform, "", 30, UiKit.Cream, TextAnchor.MiddleCenter, 10f);
            _toastPanel.SetActive(false);

            BuildCutIn();
            _tutorial = new TutorialOverlay(_game, _scene);
        }

        // =============== flujo ===============

        void ShowScreen(RectTransform screen)
        {
            _world.gameObject.SetActive(screen == _world);
            _zone.gameObject.SetActive(screen == _zone);
            _shop.gameObject.SetActive(screen == _shop);
            _game.gameObject.SetActive(screen == _game);
            _end.gameObject.SetActive(false);
        }

        void ShowShop()
        {
            ShowScreen(_shop);
            RefreshShop();
        }

        void ToggleSound()
        {
            Audio.Muted = !Audio.Muted;
            RefreshWorld();
            RefreshZone();
        }

        static readonly string[] SaidTacos =
        {
            "¡Qué rico!", "¡Uuuy, qué buenos!", "¡Así me gustan!", "¡Están de lujo!", "¡Mmm, qué sabor!", "¡Ya me hacía falta!",
            "¡Con todo, como debe ser!", "¡Bien servido!", "¡Otro igualito, eh!", "¡Qué buena salsa!"
        };
        static readonly string[] SaidGarnachas =
        {
            "¡Qué rica garnacha!", "¡Uy, bien doradita!", "¡Así como en mi pueblo!", "¡Está de rechupete!", "¡Con todo y crema, qué rico!",
            "¡Qué buena sazón!", "¡Bien calientita!", "¡Mmm, ya me hacía falta!", "¡Esto sí es comida!", "¡Gracias, marchanta!"
        };
        static readonly string[] SaidElotes =
        {
            "¡Qué rico elotito!", "¡Bien preparado, como me gusta!", "¡Con harta mayonesa, eso!", "¡Uy, del que pica!", "¡Sabe a domingo en la plaza!",
            "¡Calientito, qué rico!", "¡Así se preparan!", "¡Gracias, joven!", "¡El mejor esquite de la plaza!", "¡Mmm, con su limoncito!"
        };
        static readonly string[] SaidPozole =
        {
            "¡Qué rico pozolito!", "¡Bien calientito, como debe ser!", "¡Con su limoncito, qué rico!", "¡Así lo hacía mi abuela!", "¡Esto revive a cualquiera!",
            "¡Uy, qué buen caldo!", "¡Bien servido, eh!", "¡Con todo y rábano, eso!", "¡El mejor pozole del barrio!", "¡Gracias, señito!"
        };
        static readonly string[] SaidTamales =
        {
            "¡Qué rico tamalito!", "¡Bien esponjadito!", "¡Con su atolito, qué rico!", "¡Así me gusta empezar el día!", "¡Uy, calientito!",
            "¡La guajolota del día!", "¡Sabe a Candelaria!", "¡Gracias, marchante!", "¡Están de rechupete!", "¡Mmm, qué buen champurrado!"
        };
        static readonly string[] SaidFeria =
        {
            "¡Qué rico todo!", "¡Esto sí es feria!", "¡Uy, de lo mejor de la feria!", "¡Ya me hacía falta un antojito!", "¡Bien preparado!",
            "¡A la próxima traigo a la familia!", "¡Qué sabor, joven!", "¡Mejor que los juegos mecánicos!", "¡Gracias, me voy a la rueda!", "¡De lujo!"
        };
        static readonly string[][] SaidBy = { SaidTacos, SaidGarnachas, SaidElotes, SaidPozole, SaidTamales, SaidFeria };
        static string[] Reactions(Stall s) => SaidBy[(int)s];

        static string BaseName(Base b) =>
            b == Base.Gordita ? "una gordita" : b == Base.Quesadilla ? "una quesadilla" : b == Base.Sope ? "un sope"
            : b == Base.PlatoChico ? "un plato chico" : b == Base.PlatoGrande ? "un plato grande"
            : b == Base.Hoja ? "una hoja" : b == Base.Guajolota ? "un bolillo" : "una tortilla";

        static string RuleText(MissionRule r) =>
            r == MissionRule.NoErrors ? "cero errores" : r == MissionRule.NoBurn ? "sin quemar nada" : null;

        void StartTurn()
        {
            if (!Unlocked(_mission)) return;
            _data.taquero = TaqueroDef.Starters[TaqueroDef.SlotOf(_taq)].Id;
            _taq = TaqueroDef.For(Missions[_mission].Stall)[TaqueroDef.SlotOf(_taq)];
            _sim = new KitchenSimulation(Missions[_mission], _taq, Environment.TickCount, P.BuildRules(null, Missions[_mission].Stall));
            TacosArt.Stall = _sim.Level.Stall;
            for (int i = 0; i < PowerUps.All.Length && i < _powerIcons.Length; i++) _powerIcons[i].sprite = TacosArt.PowerUp(PowerUps.All[i]);
            if (_scene.Stall != _sim.Level.Stall)
            {
                // Otro puesto: se arma su cocina (solo existe una a la vez)
                UnityEngine.Object.Destroy(_scene.Root.gameObject);
                _scene = new KitchenScene(_game, Do, StallLayout.For(_sim.Level.Stall));
                _scene.Notice += text => { Toast(text, UiKit.Maiz); Audio.PlaySfx(_snd.Error, 0.4f); };
                _scene.Root.SetSiblingIndex(0);
                _tutorial.Scene = _scene;
            }
            _sim.OrderServed += (slot, pay, tip) =>
            {
                var at = _scene.CustomerPoint(slot);
                // Lo que dice el cliente al recibir su comida (cada puesto tiene sus frases) y, aparte, la racha
                var said = Reactions(_sim.Level.Stall);
                _scene.Popup(said[UnityEngine.Random.Range(0, said.Length)], at + new Vector2(0f, -44f), UiKit.Cream, 28);
                string combo = _sim.Streak >= 3 ? $"  ¡Racha x{_sim.Streak}!" : "";
                _scene.Popup("+" + pay + combo, at, tip >= 4 ? UiKit.Maiz : UiKit.Limon, combo.Length > 0 ? 38 : 34);
                Audio.PlaySfx(_snd.Cobro);
                PulseCoins();
                _scene.MessNapkin();
            };
            _sim.OrderWrong += slot => { Toast("¡Eso no pedí!", UiKit.Chile); Audio.PlaySfx(_snd.Error); _scene.Shake(); _scene.MessSpill(); };
            _sim.CustomerLeft += slot => { Toast("Un cliente se cansó de esperar", UiKit.Chile); Audio.PlaySfx(_snd.Error, 0.7f); _scene.Shake(); };
            _sim.CustomerArrived += slot => Audio.PlaySfx(_snd.Cliente, 0.6f);
            _sim.BossArrived += slot =>
            {
                // Entrada del jefe final: aviso grande y la cumbia se detiene un instante (reusa el corte de power-up)
                Toast($"¡Llegó {_sim.Level.BossName}! Pide {_sim.Level.BossItems} platos", UiKit.Maiz);
                _scene.Popup("¡JEFE FINAL!", _scene.CustomerPoint(slot) + new Vector2(0f, -110f), UiKit.Chile, 46);
                Audio.PlaySfx(_snd.Jefe, 0.9f);
                _scene.Shake();
            };
            _sim.BossServed += () =>
            {
                Toast($"¡{_sim.Level.BossName.Split(',')[0]} quedó encantado! +{_sim.Rules.BossBonus} de bono", UiKit.Limon);
                _scene.Popup("¡LO LOGRASTE!", _scene.CustomerPoint(_sim.BossSlot) + new Vector2(0f, -110f), UiKit.Limon, 46);
                Audio.PlaySfx(_snd.Cobro);
            };
            _sim.ItemBurned += () => { Toast(_sim.Level.Stall == Stall.Taqueria ? "¡Se quemó una tortilla!" : "¡Se quemó una masa!", UiKit.Chile); Audio.PlaySfx(_snd.Quemado); };
            _sim.PotRefilled += m => Audio.PlaySfx(_snd.Listo, 0.6f);
            _sim.OrderChanged += slot => { _scene.Popup("¡Siempre no, mejor otro!", _scene.CustomerPoint(slot), new Color32(0xFF, 0xB0, 0x5A, 0xFF), 26); Audio.PlaySfx(_snd.Cliente, 0.8f); };
            _sim.StreakBroken += n => _scene.Popup($"Se rompió la racha de {n}", new Vector2(768, 300), UiKit.Chile, 28);
            _sim.PickupSpawned += p => Audio.PlaySfx(_snd.PowerUpCae, 0.8f);
            _sim.PowerUpGained += OnPowerUpGained;
            _sim.PowerUpUsed += OnPowerUpUsed;
            _sim.Finished += EndTurn;
            var big = UpgradeDef.For(_sim.Level.Stall)[3].Id; // la de "porciones de mas" de ese mundo
            _scene.Setup(_sim, P.Owns(Upgrade.Paciencia), P.Owns(Upgrade.ComalRapido), P.Owns(big));
            SetupHud();
            ShowScreen(_game);
            _toastTime = 0f;
            _toastPanel.SetActive(false);
            _paused = false;
            int m = G(_mission);
            _tutorial.Begin(_sim, _mission, !_data.intros[m], !_data.guided[m],
                () => { _paused = false; _data.intros[m] = true; _saveFile.Save(_data); },
                () => { _data.guided[m] = true; _saveFile.Save(_data); });
            if (_tutorial.ShowingIntro) _paused = true;
            Audio.PlayMusic(_snd.MusicGame);
            Refresh();
        }

        void EndTurn()
        {
            _tutorial.End();
            int stars = _sim.Stars;
            P.Record(G(_mission), stars, _sim.Coins);
            _saveFile.Save(_data);
            Audio.PlaySfx(_snd.Fin);
            var st = _sim.Level.Stars;
            bool last = _mission >= Missions.Length - 1;
            string rule = RuleText(_sim.Level.Rule);
            string title = _sim.BossFailed ? "¡Se fue el jefe final!" : stars == 3 ? "¡De leyenda!" : stars > 0 ? "¡Buen turno!" : _sim.RuleBroken ? "Se rompió la regla" : "Se acabó el turno";
            string info = $"Estrellas a las {st[0]}, {st[1]} y {st[2]} monedas." +
                (_sim.BossFailed ? $" Para las estrellas hay que dejar contento a {_sim.Level.BossName.Split(',')[0]}."
                 : _sim.RuleBroken ? $" Esta misión pedía {rule}." : "") +
                $"\nAhora tienes {P.wallet} monedas para mejoras. " +
                (last ? (stars > 0 ? $"¡Terminaste el mundo {_playWorld + 1}!" : "Es la última misión de este mundo.")
                      : Unlocked(_mission + 1) ? "Ya puedes pasar a la siguiente misión." : "Necesitas 1 estrella para abrir la siguiente.");
            ShowResults(title, stars, info, !last && Unlocked(_mission + 1));
            _end.gameObject.SetActive(true);
        }

        void Do(Command cmd)
        {
            if (_sim == null || _sim.IsFinished || _paused) return;
            var r = _sim.Apply(cmd);
            if (r == CommandResult.Ok) { ActionFeedback(cmd); _scene.Mess(cmd); }
            string msg = Message(r);
            if (msg != null) Toast(msg, UiKit.Chile);
            Refresh();
        }

        void ActionFeedback(Command cmd)
        {
            switch (cmd.Type)
            {
                case CommandType.AddTortilla:
                case CommandType.TakeTortilla:
                    Audio.PlaySfx(_snd.Tortilla); break;
                case CommandType.RefillMeat:
                    Audio.PlaySfx(_snd.Plancha, 0.8f); break;
                case CommandType.ServeMeat:
                    Audio.PlaySfx(_snd.Tortilla, 0.8f, 0.8f); break;
                case CommandType.ToggleExtra:
                    Audio.PlaySfx(_snd.ExtraPop, 0.8f, UnityEngine.Random.Range(0.9f, 1.15f)); break;
            }
        }

        void Toast(string text, Color color)
        {
            _toast.text = text;
            _toast.color = color;
            _toastPanel.SetActive(true);
            _toastTime = 1.4f;
        }

        static string TamalName(Meat m) =>
            m == Meat.TamalVerde ? "verde" : m == Meat.TamalRojo ? "rojo" : m == Meat.TamalRajas ? "de rajas" : "dulce";

        static string NeedBaseText =>
            TacosArt.Stall == Stall.Pozole ? "Primero arrastra un plato de la pila a la tabla"
            : TacosArt.Stall == Stall.Tamales ? "Primero arrastra una hoja o un bolillo a la tabla"
            : "Primero pasa una tortilla caliente del domo al plato";

        static string OneOfText => TacosArt.Stall == Stall.Tamales ? "Un solo atole por combo" : "Una sola salsa por taco";

        static string Message(CommandResult r)
        {
            switch (r)
            {
                case CommandResult.ComalFull: return "El domo está lleno de tortillas";
                case CommandResult.TortillaCold: return "Todavía está fría";
                case CommandResult.TortillaBurned: return "Tortilla quemada a la basura";
                case CommandResult.NoFreePlate: return "No hay plato libre";
                case CommandResult.PotEmpty: return "Se acabó: toca el cazo para rellenarlo";
                case CommandResult.PotRefilling: return "Se está cociendo, espera tantito";
                case CommandResult.PotFull: return "Los cazos ya están llenos";
                case CommandResult.NeedTortillaFirst: return NeedBaseText;
                case CommandResult.PlatesFull: return "Los platos ya tienen carne: entrega o tira uno";
                case CommandResult.NeedMeatFirst: return "Primero pon la carne en el plato";
                case CommandResult.OneSalsaOnly: return OneOfText;
                case CommandResult.FirstMayo: return "¡Primero la mayonesa! Si no, no se pega";
                case CommandResult.NotForThis: return "Eso no va con ese antojo";
                case CommandResult.NoPlateReady: return "Ningún plato está listo para este cliente";
                case CommandResult.NoPowerUp: return "Todavía no tienes ese power-up: llena tu racha";
                case CommandResult.PowerUpActive: return "Ya está funcionando";
                case CommandResult.NoCustomer: return null;
                default: return null;
            }
        }

        // =============== cada cuadro ===============

        void Update()
        {
            _scene.Layout();
            LayoutMaps();
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                if (_game.gameObject.activeSelf || _shop.gameObject.activeSelf) ShowZone();
                else if (_zone.gameObject.activeSelf) ShowWorld();
            }
            AnimateMaps();
            if (_shop.gameObject.activeSelf) UpdateShop();
            if (_end.gameObject.activeSelf) UpdateResults();
            if (_sim == null) return;
            UpdateCutIn();
            if (!_sim.IsFinished && !_paused) _sim.Tick(Time.deltaTime);
            if (_toastTime > 0f)
            {
                _toastTime -= Time.deltaTime;
                if (_toastTime <= 0f) _toastPanel.SetActive(false);
            }
            Refresh();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && _sim != null && !_sim.IsFinished) ShowZone(); // el turno no corre en segundo plano
        }

        void Refresh()
        {
            if (_sim == null) return;
            var s = _sim;
            RefreshHud(s);
            _scene.Refresh(s);
            Hint? h = s.Level.Hints && !s.IsFinished ? Hints.Next(s) : (Hint?)null;
            _scene.ShowHint(h);
            _tutorial.Update(s, h, h.HasValue ? HintText(h.Value) : null);
        }

        // =============== textos ===============

        static string ExtraName(Extra e)
        {
            switch (e)
            {
                case Extra.Cebolla: return "Cebolla";
                case Extra.Cilantro: return "Cilantro";
                case Extra.Pina: return "Piña";
                case Extra.SalsaVerde: return "Salsa verde";
                case Extra.SalsaRoja: return "Salsa roja";
                case Extra.Limon: return "Limón";
                case Extra.Crema: return "Crema";
                case Extra.Queso: return "Queso";
                case Extra.Lechuga: return "Lechuga";
                case Extra.Mayonesa: return "Mayonesa";
                case Extra.ChilePolvo: return "Chile en polvo";
                case Extra.Picante: return "Salsa picante";
                case Extra.Rabano: return "Rábano";
                case Extra.Oregano: return "Orégano";
                case Extra.Champurrado: return "Champurrado";
                case Extra.AtoleArroz: return "Atole de arroz";
                case Extra.AtoleFresa: return "Atole de fresa";
                default: return e.ToString();
            }
        }

        string HintText(Hint h)
        {
            switch (h.Kind)
            {
                case HintKind.WaitCustomer: return "Espera tantito, ya viene el siguiente cliente…";
                case HintKind.ServeMeat:
                    if (_sim != null && h.Meat >= Meat.PozoleRojo && h.Meat <= Meat.PozoleVerde)
                    {
                        foreach (var pl in _sim.Plates)
                            if (pl.Meat == h.Meat && pl.NeedsScoop) return "¡Es plato grande! Ponle OTRO cucharón de la misma olla.";
                        return $"Quiere {KitchenScene.MeatName(h.Meat).ToLower()}: arrastra un cucharón de su olla al plato.";
                    }
                    if (h.Meat >= Meat.TamalVerde) return $"Quiere tamal {TamalName(h.Meat)}: arrástralo de la vaporera al plato.";
                    return h.Meat == Meat.Pastor ? "Mira su globito: quiere pastor. Arrastra la carne del trompo al plato."
                    : h.Meat == Meat.Elote ? "Quiere elote: arrástralo de la olla grande a la charolita."
                    : h.Meat == Meat.Esquite || h.Meat == Meat.EloteAsado ? $"Quiere {KitchenScene.MeatName(h.Meat).ToLower()}: arrástralo de su olla a la charolita."
                    : h.Meat >= Meat.Chicharron ? $"Quiere {KitchenScene.MeatName(h.Meat).ToLower()}: arrastra el guisado de su cazuela a la masa."
                    : $"Quiere {KitchenScene.MeatName(h.Meat).ToLower()}: arrastra la carne de su cazo al plato.";
                case HintKind.RefillMeat: return h.Meat == Meat.Pastor ? "Se acabó el pastor: toca el trompo para rebanar más." : "Ese cazo está vacío: tócalo para rellenarlo.";
                case HintKind.WaitMeat: return "La carne se está cociendo… espera tantito.";
                case HintKind.AddTortilla:
                    if (_sim != null && _sim.Level.StallDef.InstantBase)
                        return $"Mira su globito: arrastra {BaseName(h.Command.HasValue ? (Base)h.Command.Value.Arg : Base.PlatoChico)} de su pila a la tabla.";
                    if (_sim != null && _sim.Level.Stall == Stall.Garnachas)
                        return $"Mira qué masa pidió: arrastra {BaseName(h.Command.HasValue ? (Base)h.Command.Value.Arg : Base.Sope)} de su pila al comal.";
                    return "Primero la tortilla: arrastra una de la pila al domo para calentarla.";
                case HintKind.WaitTortilla: return _sim != null && _sim.Level.Stall == Stall.Garnachas ? "Se está cociendo… cuando un lado se dore, la volteas." : "Se está calentando… cuando brille, tócala.";
                case HintKind.FlipItem: return "¡Ya se doró de abajo! Tócala para voltearla.";
                case HintKind.TakeTortilla: return _sim != null && _sim.Level.Stall == Stall.Garnachas ? "¡Ya quedó de los dos lados! Arrástrala al plato." : "¡Ya brilla! Arrástrala al plato.";
                case HintKind.SelectPlate: return "Toca este plato para trabajar en él.";
                case HintKind.AddExtra:
                    if (h.Extra == Extra.Mayonesa) return "¡Primero la mayonesa! Arrástrala al elote; si no, el queso y el chile no se pegan.";
                    if (h.Extra >= Extra.Champurrado) return $"Y su atole: arrastra el {ExtraName(h.Extra).ToLower()} al plato.";
                    return $"Ahora lo que pide encima: arrastra {ExtraName(h.Extra).ToLower()} al plato.";
                case HintKind.TrashPlate: return "Ese plato no le sirve a nadie: arrástralo al bote.";
                case HintKind.Deliver: return _sim != null && _sim.Level.Stall != Stall.Taqueria ? "¡Ya quedó! Arrastra el plato al cliente." : "¡Ya quedó! Arrastra el plato al cliente para entregarle su taco.";
                default: return "";
            }
        }
    }
}
