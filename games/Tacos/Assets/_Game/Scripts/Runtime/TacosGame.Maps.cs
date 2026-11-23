using System.Collections.Generic;
using Tacos.Logic;
using Tacos.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Tacos
{
    /// <summary>
    /// Mapas: (1) puestos flotantes, una isla por mundo (un puesto distinto cada uno) (como Plantas vs Zombies); (2) la calle de la zona con sus 12 misiones.
    /// Fondos en Resources/Sprites (mapa_islas, mapa_zona1), posiciones en pixeles de esas imagenes (1536 x 864).
    /// </summary>
    public sealed partial class TacosGame
    {
        RectTransform _world, _zone;
        Stage _worldStage, _zoneStage;
        Text _worldWallet, _worldStars, _worldMsg, _worldSound, _zoneSound, _zoneWallet, _zoneStars;
        float _worldMsgTime;

        // Mundos: una isla por zona, de izquierda a derecha, unidas por puentes. Se desliza para pasar de una a otra.
        sealed class ZoneDef { public string Name, Island; public int Stars, Chiles; public bool Ready; }
        // Un puesto por mundo: cada uno trae una sola mecanica nueva (ver claude/mundos-puestos.md). Los que no estan listos dicen "Proximamente".
        static readonly ZoneDef[] Zones =
        {
            new ZoneDef { Name = "Mundo 1 · Taquería", Island = "isla_callejero", Stars = 0, Chiles = 1, Ready = true },
            new ZoneDef { Name = "Mundo 2 · Garnachas", Island = "isla_garnachas", Stars = 24, Chiles = 2, Ready = true },
            new ZoneDef { Name = "Mundo 3 · Elotes", Island = "isla_elotes", Stars = 50, Chiles = 3, Ready = true },
            new ZoneDef { Name = "Mundo 4 · Pozolería", Island = "isla_pozole", Stars = 76, Chiles = 3, Ready = true },
            new ZoneDef { Name = "Mundo 5 · Tamales", Island = "isla_tamales", Stars = 100, Chiles = 4, Ready = true },
            new ZoneDef { Name = "Mundo 6 · La Feria", Island = "isla_feria", Stars = 125, Chiles = 5, Ready = true },
        };
        const float ZoneGap = 900f;
        RectTransform _strip;
        SwipeArea _swipe;
        float _worldScroll;
        int _worldIndex;
        readonly List<Image> _islands = new List<Image>(), _locks = new List<Image>(), _dots = new List<Image>();
        // Puentes entre islas: se mecen con las islas; los que llevan a un mundo cerrado se ven en sombra
        readonly List<Image> _bridges = new List<Image>();
        readonly List<Image[]> _bridgeLights = new List<Image[]>();
        readonly List<bool> _bridgeOpen = new List<bool>();
        readonly List<Text> _zoneInfo = new List<Text>();
        Text _tapToEnter;

        // calle de misiones
        // Cada mundo tiene su calle de misiones: dibujo de fondo y donde va cada una de las 12 (pixeles de ese dibujo)
        static readonly string[] StreetArt = { "mapa_zona1", "mapa_garnachas", "mapa_elotes", "mapa_pozole", "mapa_tamales", "mapa_feria" };
        static readonly Vector2[][] WorldSpots =
        {
            new[]
            {
                new Vector2(318, 765), new Vector2(421, 716), new Vector2(525, 666), new Vector2(615, 603), new Vector2(670, 502), new Vector2(734, 412),
                new Vector2(840, 368), new Vector2(945, 323), new Vector2(1051, 279), new Vector2(1156, 234), new Vector2(1261, 188), new Vector2(1365, 140)
            },
            // Tianguis de garnachas: las plataformas de piedra del caminito (la 2 va entre las dos primeras)
            new[]
            {
                new Vector2(198, 770), new Vector2(322, 764), new Vector2(448, 730), new Vector2(430, 650), new Vector2(602, 568), new Vector2(773, 483),
                new Vector2(884, 391), new Vector2(1035, 334), new Vector2(1176, 252), new Vector2(1331, 205), new Vector2(1316, 132), new Vector2(1410, 100)
            },
            // Plaza de los elotes: las 9 plataformas del caminito y 3 lugares entre ellas
            new[]
            {
                new Vector2(75, 800), new Vector2(174, 756), new Vector2(272, 712), new Vector2(478, 642), new Vector2(579, 603), new Vector2(680, 565),
                new Vector2(869, 483), new Vector2(958, 443), new Vector2(1048, 403), new Vector2(1222, 330), new Vector2(1115, 258), new Vector2(1294, 195)
            },
            // Mercado de la pozoleria: las 10 plataformas del empedrado y 2 lugares mas (entre la 3 y la 4, y al final)
            new[]
            {
                new Vector2(190, 785), new Vector2(312, 708), new Vector2(462, 640), new Vector2(540, 600), new Vector2(612, 560), new Vector2(772, 497),
                new Vector2(925, 420), new Vector2(1017, 325), new Vector2(1130, 257), new Vector2(1240, 197), new Vector2(1360, 122), new Vector2(1440, 72)
            },
            // Esquina de los tamales al amanecer: 11 plataformas de la banqueta y una entre la 2 y la 3
            new[]
            {
                new Vector2(222, 750), new Vector2(337, 690), new Vector2(420, 660), new Vector2(490, 630), new Vector2(605, 553), new Vector2(737, 490),
                new Vector2(873, 428), new Vector2(1012, 360), new Vector2(1153, 307), new Vector2(1238, 240), new Vector2(1282, 148), new Vector2(1370, 82)
            },
            // La feria de noche: 11 plataformas del camino y una entre la 4 y la 5
            new[]
            {
                new Vector2(155, 805), new Vector2(355, 725), new Vector2(320, 605), new Vector2(508, 545), new Vector2(590, 505), new Vector2(670, 470),
                new Vector2(830, 410), new Vector2(990, 345), new Vector2(1165, 288), new Vector2(1275, 215), new Vector2(1385, 145), new Vector2(1448, 88)
            }
        };
        Vector2[] MissionSpots => WorldSpots[Mathf.Min(_playWorld, WorldSpots.Length - 1)];
        int _zoneBuiltFor = -1;
        readonly List<Image> _nodeDisc = new List<Image>(), _nodeGlow = new List<Image>();
        readonly List<Text> _nodeNum = new List<Text>();
        readonly List<Image[]> _nodeStars = new List<Image[]>();
        Image _nodeTaquero;

        // tarjeta de la mision
        Text _cardName, _cardDesc, _cardRule, _cardPerk;
        readonly Text[] _cardGoals = new Text[3];
        readonly List<Image> _cardMeats = new List<Image>(), _cardPowers = new List<Image>();
        readonly List<Button> _cardTaqueros = new List<Button>();

        static readonly Color Gold = new Color32(0xFF, 0xC5, 0x3D, 0xFF);
        static readonly Color LockedColor = new Color32(0x46, 0x3E, 0x5C, 0xFF);

        // =============== construccion ===============

        void BuildWorld()
        {
            _world = UiKit.Stretch(UiKit.NewRect("MapaZonas", _root));
            _worldStage = new Stage(_world, "Cielo", TacosArt.Get("cielo_mapa"), 1536, 864, Stage.Fit.Cover);

            // Area que se arrastra (todo el cielo) con la tira de islas adentro
            var view = UiKit.Stretch(UiKit.NewRect("Mundos", _world));
            var catcher = view.gameObject.AddComponent<Image>();
            catcher.color = new Color(0f, 0f, 0f, 0f);
            _swipe = view.gameObject.AddComponent<SwipeArea>();
            _swipe.Dragged = dx => _worldScroll = Mathf.Clamp(_worldScroll - dx, -200f, (Zones.Length - 1) * ZoneGap + 200f);
            _swipe.Released = v => GoToZone(Mathf.RoundToInt((_worldScroll - v * 0.12f) / ZoneGap));
            _strip = UiKit.NewRect("Islas", view);
            _strip.anchorMin = _strip.anchorMax = _strip.pivot = new Vector2(0.5f, 0.5f);
            _strip.sizeDelta = Vector2.zero;

            for (int i = 0; i < Zones.Length - 1; i++)
            {
                var bridge = UiKit.Picture(_strip, "Puente " + i, TacosArt.Get("puente"));
                bridge.preserveAspect = false;
                Corner(bridge.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(i * ZoneGap + ZoneGap / 2f, 10f), new Vector2(ZoneGap - 380f, 150f));
                _bridges.Add(bridge);
                _bridgeOpen.Add(true);
                // lucecitas que cruzan el puente abierto (como gente que va de un puesto al otro)
                var lights = new Image[3];
                for (int l = 0; l < lights.Length; l++)
                {
                    lights[l] = UiKit.Picture(_strip, "Luz puente " + i + "." + l, UiKit.GlowSprite);
                    lights[l].raycastTarget = false;
                    Corner(lights[l].rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 46f));
                }
                _bridgeLights.Add(lights);
            }
            for (int i = 0; i < Zones.Length; i++)
            {
                int z = i;
                var def = Zones[i];
                var island = UiKit.Picture(_strip, "Isla " + i, TacosArt.Get(def.Island));
                island.raycastTarget = true;
                var btn = island.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => { UiKit.Clicked?.Invoke(); TapZone(z); });
                Corner(island.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(i * ZoneGap, 60f), new Vector2(600f, 600f));
                _islands.Add(island);
                var lk = UiKit.Picture(island.transform, "Candado", TacosArt.Get("ui_candado"));
                Corner(lk.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(110f, 140f));
                _locks.Add(lk);

                // Letrero colgante debajo de la isla
                var sign = UiKit.Picture(_strip, "Letrero " + i, TacosArt.Get("ui_letrero"));
                sign.preserveAspect = false;
                // Tambien se entra tocando el letrero (los jugadores lo intentaban)
                sign.raycastTarget = true;
                var signBtn = sign.gameObject.AddComponent<Button>();
                signBtn.transition = Selectable.Transition.None;
                signBtn.onClick.AddListener(() => { UiKit.Clicked?.Invoke(); TapZone(z); });
                Corner(sign.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(i * ZoneGap, -330f), new Vector2(470f, 200f));
                var name = UiKit.Outlined(sign.transform, def.Name, 26, new Color32(0xFF, 0xF0, 0xD2, 0xFF));
                Corner(name.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -2f), new Vector2(440f, 40f));
                for (int c = 0; c < 5; c++)
                {
                    var ch = UiKit.Picture(sign.transform, "Chile " + c, TacosArt.Get("ui_chile"));
                    Corner(ch.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-80f + c * 40f, -40f), new Vector2(36f, 38f));
                    ch.color = c < def.Chiles ? Color.white : new Color(0.25f, 0.2f, 0.28f, 0.55f);
                }
                var info = UiKit.Outlined(sign.transform, "", 20, new Color32(0xFF, 0xE6, 0xBE, 0xFF));
                Corner(info.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -76f), new Vector2(440f, 30f));
                _zoneInfo.Add(info);
            }
            _tapToEnter = UiKit.Outlined(_strip, "Toca el puesto o su letrero para entrar", 24, UiKit.Turq);
            Corner(_tapToEnter.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -200f), new Vector2(460f, 36f));

            // Flechas y puntitos
            foreach (var (dir, x) in new[] { (-1, 0f), (1, 1f) })
            {
                int d = dir;
                var arrow = UiKit.MakeButton(_world, "Flecha " + dir, dir < 0 ? "‹" : "›", new Color(0.08f, 0.07f, 0.12f, 0.8f), UiKit.Maiz, 56, () => GoToZone(_worldIndex + d));
                arrow.GetComponent<Image>().sprite = UiKit.CircleSprite;
                arrow.GetComponent<Image>().type = Image.Type.Simple;
                Corner((RectTransform)arrow.transform, new Vector2(x, 0.5f), new Vector2(dir < 0 ? 70f : -70f, 40f), new Vector2(96f, 96f));
            }
            for (int i = 0; i < Zones.Length; i++)
            {
                var dot = UiKit.NewRect("Punto " + i, _world).gameObject.AddComponent<Image>();
                dot.sprite = UiKit.CircleSprite; dot.raycastTarget = false;
                Corner(dot.rectTransform, new Vector2(0.5f, 0f), new Vector2((i - (Zones.Length - 1) / 2f) * 34f, 22f), new Vector2(16f, 16f));
                _dots.Add(dot);
            }

            // Encabezado: titulo de neon, sonido, cartera
            var title = UiKit.Picture(_world, "Titulo", TacosArt.Get("titulo"));
            Corner(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(420f, 200f));
            if (title.sprite == null)
            {
                var t = UiKit.Outlined(_world, "ANTOJITOS NEÓN", 56, UiKit.Maiz);
                t.font = UiKit.Display;
                Corner(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(800f, 80f));
            }
            BuildWallet(_world, out _worldStars, out _worldWallet);
            var snd = UiKit.MakeButton(_world, "Sonido", "", new Color(0.08f, 0.07f, 0.12f, 0.86f), UiKit.Cream, 24, ToggleSound);
            Corner((RectTransform)snd.transform, new Vector2(0f, 1f), new Vector2(18f, -16f), new Vector2(200f, 62f));
            _removeAdsWorld = BuildRemoveAdsButton(_world, new Vector2(0f, 1f), new Vector2(18f, -90f));
            _worldSound = snd.GetComponentInChildren<Text>();

            var msg = UiKit.Panel(_world, "Mensaje", new Color(UiKit.Ink.r, UiKit.Ink.g, UiKit.Ink.b, 0.94f), true, false);
            var mo = msg.gameObject.AddComponent<Outline>(); mo.effectColor = UiKit.Maiz; mo.effectDistance = new Vector2(3f, -3f);
            Corner(msg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(900f, 90f));
            _worldMsg = UiKit.FillLabel(msg.transform, "", 28, UiKit.Cream, TextAnchor.MiddleCenter, 10f);
            msg.gameObject.SetActive(false);

            // Recordatorio visible mientras todo esta abierto para probar
            if (TodoAbierto)
            {
                var dev = UiKit.Outlined(_world, "MODO PRUEBA · todo abierto", 20, UiKit.Chile);
                Corner(dev.rectTransform, new Vector2(0f, 0f), new Vector2(170f, 26f), new Vector2(320f, 30f));
            }
        }

        bool ZoneOpen(int z) => Zones[z].Ready && z < LevelConfig.Worlds.Length && (TodoAbierto || P.TotalStars >= Zones[z].Stars);

        void GoToZone(int z) => _worldIndex = Mathf.Clamp(z, 0, Zones.Length - 1);

        void TapZone(int z)
        {
            if (z != _worldIndex) { GoToZone(z); return; }
            if (ZoneOpen(z))
            {
                if (_playWorld != z) { _playWorld = z; _mission = FirstOpenMission(); }
                ShowZone();
                return;
            }
            var def = Zones[z];
            Audio.PlaySfx(_snd.Error, 0.4f);
            WorldMessage(P.TotalStars < def.Stars
                ? $"Junta {def.Stars} estrellas para abrir {def.Name.Split('·')[1].Trim()} (llevas {P.TotalStars})"
                : "¡Próximamente! Estamos cocinando este puesto");
        }

        /// <summary>Estrellas y monedas arriba a la derecha.</summary>
        void BuildWallet(RectTransform parent, out Text stars, out Text coins)
        {
            var box = UiKit.Panel(parent, "Cartera", new Color(0.08f, 0.07f, 0.12f, 0.86f), true, false);
            Corner(box.rectTransform, new Vector2(1f, 1f), new Vector2(-18f, -16f), new Vector2(380f, 62f));
            var star = UiKit.Star(box.transform, "Estrella"); star.color = UiKit.Maiz; star.preserveAspect = true;
            UiKit.TopLeft(star.rectTransform, 14f, 9f, 44f, 44f);
            stars = UiKit.Label(box.transform, "", 28, UiKit.Cream, TextAnchor.MiddleLeft);
            UiKit.TopLeft(stars.rectTransform, 64f, 0f, 130f, 62f);
            var coin = UiKit.NewRect("Moneda", box.transform).gameObject.AddComponent<Image>();
            coin.sprite = TacosArt.Coin != null ? TacosArt.Coin : UiKit.CircleSprite; coin.color = TacosArt.Coin != null ? Color.white : UiKit.Maiz;
            coin.raycastTarget = false; coin.preserveAspect = true;
            UiKit.TopLeft(coin.rectTransform, 196f, 7f, 48f, 48f);
            coins = UiKit.Label(box.transform, "", 28, UiKit.Cream, TextAnchor.MiddleLeft);
            UiKit.TopLeft(coins.rectTransform, 244f, 0f, 130f, 62f);
        }

        void BuildZone()
        {
            _zone = UiKit.Stretch(UiKit.NewRect("CalleMisiones", _root));
            _zoneStage = new Stage(_zone, "Calle", TacosArt.Get(StreetArt[Mathf.Min(_playWorld, StreetArt.Length - 1)]), 1536, 864, Stage.Fit.Cover);
            var st = _zoneStage;

            // Caminito de puntos entre misiones
            for (int i = 0; i < MissionSpots.Length - 1; i++)
                for (int k = 1; k < 5; k++)
                {
                    var p = Vector2.Lerp(MissionSpots[i], MissionSpots[i + 1], k / 5f);
                    st.Img($"Punto {i}.{k}", UiKit.CircleSprite, new Color(1f, 0.85f, 0.4f, 0.8f), Stage.R(p.x, p.y, 10, 10));
                }

            _nodeTaquero = st.Pic("TaqueroMapa", null, Stage.R(0, 0, 10, 10));
            for (int i = 0; i < MissionSpots.Length; i++)
            {
                int m = i;
                var c = MissionSpots[i];
                _nodeGlow.Add(st.Img("Brillo " + i, UiKit.GlowSprite, new Color(1f, 0.85f, 0.4f, 0.9f), Stage.R(c.x, c.y, 125, 125)));
                // Azulejo de talavera (antes un circulo amarillo liso); si falta el dibujo, el circulo de siempre
                Image disc;
                if (TacosArt.Get("ui_mision") != null)
                {
                    disc = st.Pic("Mision " + i, TacosArt.Get("ui_mision"), Stage.R(c.x, c.y, 74, 74));
                    disc.preserveAspect = true;
                }
                else
                {
                    disc = st.Img("Mision " + i, UiKit.CircleSprite, Gold, Stage.R(c.x, c.y, 70, 70));
                    var o = disc.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0.16f, 0.08f, 0.04f); o.effectDistance = new Vector2(4f, -4f);
                }
                _nodeDisc.Add(disc);
                _nodeNum.Add(st.Label((i + 1).ToString(), TacosArt.Get("ui_mision") != null ? 24 : 30, UiKit.Ink, Stage.R(c.x, c.y - 1, 70, 70))); // azulejo mas chico: numero mas chico
                _nodeNum[i].GetComponents<Outline>()[0].enabled = false;
                _nodeNum[i].GetComponents<Outline>()[1].enabled = false;
                var stars = new Image[3];
                for (int s = 0; s < 3; s++)
                {
                    stars[s] = UiKit.Star(st.Root, "Estrella " + i + "." + s);
                    st.Place(stars[s].rectTransform, Stage.R(c.x - 28 + s * 28, c.y + 46 - (s == 1 ? 5 : 0), 28, 28));
                }
                _nodeStars.Add(stars);
                st.Hotspot("TocarMision " + i, Stage.R(c.x, c.y, 96, 96), () => SelectMission(m));
            }

            // Encabezado
            var back = UiKit.MakeButton(_zone, "AlMapa", "‹ Mundos", new Color(0.08f, 0.07f, 0.12f, 0.88f), UiKit.Cream, 26, ShowWorld);
            Corner((RectTransform)back.transform, new Vector2(0f, 1f), new Vector2(18f, -16f), new Vector2(170f, 62f));
            var title = UiKit.Panel(_zone, "Titulo", new Color(0.08f, 0.07f, 0.12f, 0.88f), true, false);
            Corner(title.rectTransform, new Vector2(0f, 1f), new Vector2(200f, -16f), new Vector2(430f, 62f));
            UiKit.FillLabel(title.transform, Zones[Mathf.Min(_playWorld, Zones.Length - 1)].Name, 26, UiKit.Maiz, TextAnchor.MiddleCenter, 4f);
            BuildWallet(_zone, out _zoneStars, out _zoneWallet);
            var shop = UiKit.MakeButton(_zone, "Tienda", "Mejoras", UiKit.Turq, UiKit.Ink, 26, ShowShop);
            Corner((RectTransform)shop.transform, new Vector2(1f, 1f), new Vector2(-410f, -16f), new Vector2(170f, 62f));
            var snd = UiKit.MakeButton(_zone, "SonidoZona", "", new Color(0.08f, 0.07f, 0.12f, 0.88f), UiKit.Cream, 22, ToggleSound);
            Corner((RectTransform)snd.transform, new Vector2(1f, 1f), new Vector2(-596f, -16f), new Vector2(170f, 62f));
            _zoneSound = snd.GetComponentInChildren<Text>();

            BuildMissionCard();
        }

        void BuildMissionCard()
        {
            var card = UiKit.Panel(_zone, "TarjetaMision", new Color(0.08f, 0.07f, 0.12f, 0.94f), true, true);
            var cardRt = Corner(card.rectTransform, new Vector2(1f, 0f), new Vector2(-18f, 18f), new Vector2(640f, 380f));
            var co = card.gameObject.AddComponent<Outline>(); co.effectColor = UiKit.Turq; co.effectDistance = new Vector2(3f, -3f);
            var t = card.transform;

            _cardName = UiKit.Label(t, "", 34, UiKit.Cream, TextAnchor.UpperLeft);
            UiKit.TopLeft(_cardName.rectTransform, 24f, 16f, 410f, 46f);
            _cardDesc = UiKit.Label(t, "", 22, new Color(0.82f, 0.78f, 0.9f), TextAnchor.UpperLeft);
            UiKit.TopLeft(_cardDesc.rectTransform, 24f, 64f, 592f, 64f);
            _cardRule = UiKit.Label(t, "", 22, UiKit.Chile, TextAnchor.UpperLeft);
            UiKit.TopLeft(_cardRule.rectTransform, 24f, 126f, 592f, 30f);

            for (int i = 0; i < 3; i++)
            {
                var s = UiKit.Star(t, "Meta " + i); s.color = UiKit.Maiz;
                UiKit.TopLeft(s.rectTransform, 24f + i * 110f, 160f, 34f, 34f);
                _cardGoals[i] = UiKit.Label(t, "", 24, UiKit.Cream, TextAnchor.MiddleLeft);
                UiKit.TopLeft(_cardGoals[i].rectTransform, 62f + i * 110f, 160f, 70f, 34f);
            }
            var meats = UiKit.Label(t, "Carnes", 18, UiKit.Muted, TextAnchor.MiddleLeft);
            UiKit.TopLeft(meats.rectTransform, 360f, 140f, 120f, 24f);
            for (int i = 0; i < 4; i++)
            {
                var m = UiKit.Picture(t, "Carne " + i, null);
                UiKit.TopLeft(m.rectTransform, 360f + i * 52f, 162f, 48f, 40f);
                _cardMeats.Add(m);
            }
            var pw = UiKit.Label(t, "Power-ups", 18, UiKit.Muted, TextAnchor.MiddleLeft);
            UiKit.TopLeft(pw.rectTransform, 24f, 204f, 160f, 24f);
            for (int i = 0; i < 4; i++)
            {
                var p = UiKit.Picture(t, "PowerUp " + i, null);
                UiKit.TopLeft(p.rectTransform, 140f + i * 50f, 200f, 44f, 34f);
                _cardPowers.Add(p);
            }

            var cooks = TaqueroDef.For(Missions[0].Stall); // cada puesto con sus cocineros
            for (int i = 0; i < cooks.Length; i++)
            {
                var taq = cooks[i];
                var b = UiKit.MakeButton(t, "Taquero " + taq.Id, null, UiKit.Panel2, UiKit.Cream, 20, () => { _taq = taq; RefreshZone(); });
                UiKit.TopLeft((RectTransform)b.transform, 24f + i * 106f, 244f, 96f, 116f);
                var face = UiKit.Picture(b.transform, "Retrato", TacosArt.Taquero(taq.Id, TacosArt.Pose.Quieto));
                var fr = face.rectTransform; fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one;
                fr.offsetMin = new Vector2(4f, 4f); fr.offsetMax = new Vector2(-4f, -4f);
                _cardTaqueros.Add(b);
            }
            _cardPerk = UiKit.Label(t, "", 18, UiKit.Muted, TextAnchor.UpperLeft);
            UiKit.TopLeft(_cardPerk.rectTransform, 350f, 238f, 270f, 50f);
            var how = UiKit.MakeButton(t, "ComoSeJuega", "¿Cómo se juega?", UiKit.Panel2, UiKit.Turq, 18, () =>
            {
                _data.intros[G(0)] = false; _data.guided[G(0)] = false; // repite el tutorial de la mision 1 del mundo
                _mission = 0;
                StartTurn();
            });
            UiKit.TopLeft((RectTransform)how.transform, 440f, 14f, 180f, 40f);
            // cada puesto con su grito (la calle se arma de nuevo al cambiar de mundo)
            var stall = Missions[0].Stall;
            string grito = new[] { "¡A TAQUEAR!", "¡A GARNACHEAR!", "¡A ELOTEAR!", "¡A POZOLEAR!", "¡A TAMALEAR!", "¡A LA FERIA!" }[(int)stall];
            var go = UiKit.MakeButton(t, "ATaquear", grito, UiKit.Chile, Color.white, 30, StartTurn);
            UiKit.TopLeft((RectTransform)go.transform, 350f, 292f, 266f, 70f);
            BuildPowerUpOffer();
        }

        // =============== mostrar ===============

        void ShowWorld()
        {
            _sim = null;
            ShowScreen(_world);
            Audio.PlayMusic(_snd.MusicMenu);
            RefreshWorld();
        }

        /// <summary>Al cambiar de mundo se arma la calle de ese mundo (su dibujo y sus 12 lugares).</summary>
        void EnsureZone()
        {
            if (_zoneBuiltFor == _playWorld) return;
            int index = _zone != null ? _zone.GetSiblingIndex() : -1;
            if (_zone != null) UnityEngine.Object.Destroy(_zone.gameObject);
            _nodeDisc.Clear(); _nodeGlow.Clear(); _nodeNum.Clear(); _nodeStars.Clear();
            _cardMeats.Clear(); _cardPowers.Clear(); _cardTaqueros.Clear();
            BuildZone();
            if (index >= 0) _zone.SetSiblingIndex(index);
            _zoneBuiltFor = _playWorld;
        }

        void ShowZone()
        {
            EnsureZone();
            _taq = TaqueroDef.For(Missions[0].Stall)[TaqueroDef.SlotOf(_taq)]; // el mismo lugar con el cocinero de este puesto
            _sim = null;
            _tutorial.End();
            ShowScreen(_zone);
            if (!Unlocked(_mission)) _mission = FirstOpenMission();
            Audio.PlayMusic(_snd.MusicMenu);
            RefreshZone();
        }

        float _lastMissionTap = -10f;

        /// <summary>Un toque elige la mision; dos toques seguidos en la misma la empiezan.</summary>
        void SelectMission(int m)
        {
            if (!Unlocked(m)) { Audio.PlaySfx(_snd.Error, 0.5f); return; }
            bool twice = m == _mission && Time.unscaledTime - _lastMissionTap < 0.5f;
            _lastMissionTap = Time.unscaledTime;
            _mission = m;
            RefreshZone();
            if (twice) { _lastMissionTap = -10f; StartTurn(); }
        }

        void WorldMessage(string text)
        {
            _worldMsg.text = text;
            _worldMsg.transform.parent.gameObject.SetActive(true);
            _worldMsgTime = 2.6f;
        }

        void RefreshWorld()
        {
            if (_world == null) return;
            _worldStars.text = $"{P.TotalStars}/{AllMissions * 3}";
            _worldWallet.text = P.wallet.ToString();
            _worldSound.text = Audio.Muted ? "Sonido: no" : "Sonido: sí";
            for (int z = 0; z < Zones.Length; z++)
            {
                bool open = ZoneOpen(z);
                _islands[z].color = open ? Color.white : new Color(0.5f, 0.46f, 0.66f, 1f);
                _locks[z].enabled = !open && _locks[z].sprite != null;
                // El puente que llega a este mundo: en sombra mientras no se pueda pasar
                // (en modo prueba todo esta abierto, asi que todos se ven prendidos)
                if (z > 0)
                {
                    bool canCross = open;
                    _bridgeOpen[z - 1] = canCross;
                    _bridges[z - 1].color = canCross ? Color.white : new Color(0.32f, 0.3f, 0.42f, 0.9f);
                }
                int done = 0;
                for (int i = 0; i < PerWorld; i++) { int g = z * PerWorld + i; if (g < P.stars.Length && P.stars[g] > 0) done++; }
                _zoneInfo[z].text = open ? $"★ {WorldStars(z)}/{PerWorld * 3}   ·   {done} de {PerWorld} misiones"
                    : !Zones[z].Ready && P.TotalStars >= Zones[z].Stars ? "¡Próximamente!" : $"★ {Zones[z].Stars} para abrir";
            }
        }

        void RefreshZone()
        {
            if (_zone == null) return;
            _zoneStars.text = $"{WorldStars(_playWorld)}/{Missions.Length * 3}";
            _zoneWallet.text = P.wallet.ToString();
            _zoneSound.text = Audio.Muted ? "Sonido: no" : "Sonido: sí";
            for (int i = 0; i < Missions.Length; i++)
            {
                bool open = Unlocked(i), sel = i == _mission;
                if (TacosArt.Get("ui_mision") != null)
                {
                    // abierto, ya con estrellas (con su papel picado) o de piedra si esta cerrado; el elegido brilla y tiene al cocinero
                    _nodeDisc[i].sprite = !open ? TacosArt.Get("ui_mision_cerrada") : P.stars[G(i)] > 0 ? TacosArt.Get("ui_mision_hecha") : TacosArt.Get("ui_mision");
                    _nodeDisc[i].color = Color.white;
                }
                else _nodeDisc[i].color = sel ? UiKit.Turq : open ? Gold : LockedColor;
                _nodeNum[i].color = open ? UiKit.Ink : TacosArt.Get("ui_mision") != null ? new Color(0.25f, 0.23f, 0.27f) : new Color(0.7f, 0.66f, 0.78f);
                _nodeGlow[i].enabled = sel;
                for (int s = 0; s < 3; s++)
                {
                    _nodeStars[i][s].enabled = open && P.stars[G(i)] > 0;
                    _nodeStars[i][s].color = s < P.stars[G(i)] ? UiKit.Maiz : new Color(0.24f, 0.2f, 0.3f);
                }
            }
            var c = MissionSpots[_mission];
            _nodeTaquero.sprite = TacosArt.Taquero(_taq.Id, TacosArt.Pose.Festeja);
            _nodeTaquero.enabled = _nodeTaquero.sprite != null;
            // Parado arriba de su mision; en las de hasta arriba no cabia y desaparecia: ahi se para a un lado
            float tx = c.x, ty = c.y - 110f;
            if (ty - 80f < 12f) { ty = Mathf.Max(92f, c.y - 20f); tx = c.x > 1300f ? c.x - 120f : c.x + 120f; }
            _zoneStage.Place(_nodeTaquero.rectTransform, Stage.R(tx, ty, 120, 160));

            var lvl = Missions[_mission];
            _cardName.text = $"{_mission + 1}. {lvl.Name}";
            _cardDesc.text = lvl.Description;
            string rule = RuleText(lvl.Rule);
            _cardRule.text = rule == null ? "" : "Regla: " + rule;
            for (int i = 0; i < 3; i++) _cardGoals[i].text = lvl.Stars[i].ToString();
            var meats = lvl.OnlyMeat != Meat.None ? new[] { lvl.OnlyMeat } : lvl.Meats;
            for (int i = 0; i < _cardMeats.Count; i++)
            {
                _cardMeats[i].sprite = i < meats.Length ? TacosArt.Meat(meats[i]) : null;
                _cardMeats[i].enabled = _cardMeats[i].sprite != null;
            }
            for (int i = 0; i < _cardPowers.Count; i++)
            {
                TacosArt.Stall = lvl.Stall;
                _cardPowers[i].sprite = i < lvl.PowerUps.Length ? TacosArt.PowerUp(lvl.PowerUps[i]) : null;
                _cardPowers[i].enabled = _cardPowers[i].sprite != null;
            }
            for (int i = 0; i < _cardTaqueros.Count; i++)
                UiKit.Highlight(_cardTaqueros[i], UiKit.Turq, TaqueroDef.For(Missions[0].Stall)[i] == _taq);
            _cardPerk.text = $"{_taq.Name}: {_taq.Perk}";
        }

        void LayoutMaps()
        {
            if (_world.gameObject.activeSelf)
            {
                _worldStage.Layout();
                // El cielo se mueve poquito al pasar de isla. Con 6 mundos se salia de la imagen (al llegar a la feria
                // se veia negro a la derecha): ahora se agranda un poco y el recorrido nunca pasa de su orilla.
                var sky = _worldStage.Root;
                sky.sizeDelta *= 1.18f;
                float room = Mathf.Max(0f, sky.sizeDelta.x - ((RectTransform)sky.parent).rect.width);
                float k = Mathf.Clamp01(_worldScroll / Mathf.Max(1f, (Zones.Length - 1) * ZoneGap));
                sky.anchoredPosition += new Vector2((0.5f - k) * room, 0f);
            }
            if (_zone.gameObject.activeSelf) _zoneStage.Layout();
        }

        float IslandBob(int z, float t) => 12f * Mathf.Sin(t * 1.3f + z * 1.7f);

        /// <summary>
        /// Los puentes cuelgan entre dos islas que flotan: siguen su vaiven, se inclinan hacia la que esta mas abajo
        /// y se mecen tantito. Los abiertos llevan lucecitas que van y vienen; los cerrados quedan quietos y en sombra.
        /// </summary>
        void AnimateBridges(float t)
        {
            for (int i = 0; i < _bridges.Count; i++)
            {
                float a = IslandBob(i, t), b = IslandBob(i + 1, t);
                bool open = _bridgeOpen[i];
                float swing = open ? 5f * Mathf.Sin(t * 2.1f + i) : 1.5f * Mathf.Sin(t * 0.9f + i);
                float x = i * ZoneGap + ZoneGap / 2f, y = 10f + (a + b) / 2f - Mathf.Abs(swing) * 0.6f;
                var rt = _bridges[i].rectTransform;
                rt.anchoredPosition = new Vector2(x, y);
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(b - a, ZoneGap - 380f) * Mathf.Rad2Deg + swing * 0.15f);
                rt.localScale = new Vector3(1f, 1f + 0.03f * Mathf.Sin(t * 2.1f + i), 1f);
                var lights = _bridgeLights[i];
                for (int l = 0; l < lights.Length; l++)
                {
                    lights[l].enabled = open;
                    if (!open) continue;
                    // van de una isla a la otra y regresan, cada una a su ritmo
                    float k = 0.5f + 0.5f * Mathf.Sin(t * (0.45f + 0.12f * l) + l * 2.1f + i);
                    float half = (ZoneGap - 420f) / 2f;
                    float lx = x - half + k * 2f * half;
                    float ly = y + (b - a) * (k - 0.5f) + 28f - 18f * Mathf.Sin(k * Mathf.PI); // siguen la inclinacion y la panza del puente
                    lights[l].rectTransform.anchoredPosition = new Vector2(lx, ly);
                    float pulse = 0.55f + 0.35f * Mathf.Sin(t * 5f + l * 1.3f);
                    lights[l].color = l == 1 ? new Color(1f, 0.55f, 0.85f, pulse) : new Color(1f, 0.85f, 0.4f, pulse);
                }
            }
        }

        void AnimateMaps()
        {
            float t = Time.unscaledTime;
            if (_world.gameObject.activeSelf)
            {
                float dt = Time.unscaledDeltaTime;
                if (!_swipe.Dragging) _worldScroll = Mathf.Lerp(_worldScroll, _worldIndex * ZoneGap, 1f - Mathf.Exp(-10f * dt));
                _strip.anchoredPosition = new Vector2(-_worldScroll, 0f);
                for (int z = 0; z < _islands.Count; z++)
                {
                    float dist = Mathf.Abs(z * ZoneGap - _worldScroll) / ZoneGap;
                    float k = Mathf.Lerp(1f, 0.8f, Mathf.Clamp01(dist));
                    _islands[z].rectTransform.localScale = new Vector3(k, k, 1f);
                    _islands[z].rectTransform.anchoredPosition = new Vector2(z * ZoneGap, 60f + IslandBob(z, t));
                    _dots[z].color = z == _worldIndex ? UiKit.Maiz : new Color(0.55f, 0.5f, 0.65f);
                }
                AnimateBridges(t);
                _tapToEnter.enabled = _worldIndex == 0;
                var c = _tapToEnter.color; c.a = 0.55f + 0.45f * Mathf.Sin(t * 3f); _tapToEnter.color = c;
                if (_worldMsgTime > 0f)
                {
                    _worldMsgTime -= dt;
                    if (_worldMsgTime <= 0f) _worldMsg.transform.parent.gameObject.SetActive(false);
                }
            }
            if (_zone.gameObject.activeSelf && _mission < _nodeGlow.Count)
            {
                float k = 1f + 0.12f * Mathf.Sin(t * 4f);
                _nodeGlow[_mission].rectTransform.localScale = new Vector3(k, k, 1f);
                _nodeTaquero.rectTransform.localScale = new Vector3(1f, 1f + 0.03f * Mathf.Sin(t * 5f), 1f);
            }
        }
    }
}
