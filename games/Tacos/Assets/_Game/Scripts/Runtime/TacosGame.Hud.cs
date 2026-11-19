using Tacos.Logic;
using Tacos.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Tacos
{
    /// <summary>HUD del turno: tiempo, monedas, estrellas, racha, barra de power-ups y la franja anime del taquero.</summary>
    public sealed partial class TacosGame
    {
        Text _hudTurno, _hudTiempo, _hudMonedas, _hudRule, _hudRacha;
        Image[] _hudStars;
        RectTransform _rachaBox, _coinsRt;
        Image _rachaFill, _salsaBadge;
        Text _salsaText;
        float _coinPulse;

        // barra de power-ups (abajo a la izquierda)
        RectTransform _powerBar;
        readonly Button[] _powerButtons = new Button[4];
        readonly Image[] _powerIcons = new Image[4], _powerGlow = new Image[4], _powerTimer = new Image[4];
        readonly Text[] _powerCount = new Text[4];
        readonly float[] _powerPop = new float[4];

        // franja del power-up
        RectTransform _cutIn, _cutBand;
        Image _cutVeil, _cutTaquero, _cutIcon;
        Text _cutTitle, _cutSub;
        float _cutT = -1f;
        const float CutInTime = 1.7f;

        void BuildHud()
        {
            var hudBg = UiKit.Panel(_game, "HUDFondo", new Color(UiKit.Night.r, UiKit.Night.g, UiKit.Night.b, 0.84f), true, false);
            BoxOf(hudBg, 0f, 1f, 10f, 74f, 24f);
            var hud = Box(UiKit.NewRect("HUD", _game), 0f, 1f, 12f, 70f, 56f);
            _hudTurno = UiKit.Label(hud, "", 28, UiKit.Maiz, TextAnchor.MiddleLeft);
            BoxOf(_hudTurno, 0f, 0.25f, 0f, 70f);
            _hudTiempo = UiKit.Label(hud, "", 36, UiKit.Maiz, TextAnchor.MiddleCenter);
            BoxOf(_hudTiempo, 0.25f, 0.34f, 0f, 70f);
            var coinIcon = UiKit.NewRect("Moneda", hud).gameObject.AddComponent<Image>();
            coinIcon.sprite = TacosArt.Coin != null ? TacosArt.Coin : UiKit.CircleSprite;
            coinIcon.color = TacosArt.Coin != null ? Color.white : UiKit.Maiz; coinIcon.raycastTarget = false;
            BoxOf(coinIcon, 0.345f, 0.375f, 10f, 50f);
            coinIcon.preserveAspect = true;
            _hudMonedas = UiKit.Label(hud, "", 34, UiKit.Cream, TextAnchor.MiddleLeft);
            _coinsRt = BoxOf(_hudMonedas, 0.38f, 0.47f, 0f, 70f);
            _hudStars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                _hudStars[i] = UiKit.Star(hud, "Estrella");
                BoxOf(_hudStars[i], 0.47f + i * 0.028f, 0.497f + i * 0.028f, 12f, 46f);
            }
            _hudRule = UiKit.Label(hud, "", 22, UiKit.Muted, TextAnchor.MiddleLeft);
            BoxOf(_hudRule, 0.56f, 0.66f, 0f, 70f);

            // Racha: llama + texto + barra que carga el siguiente power-up
            var racha = UiKit.Panel(hud, "Racha", new Color(0.16f, 0.09f, 0.07f, 0.95f), true, false);
            _rachaBox = BoxOf(racha, 0.66f, 0.92f, 6f, 58f, 10f);
            var flame = UiKit.Picture(racha.transform, "Llama", TacosArt.Get("ui_racha"));
            UiKit.TopLeft(flame.rectTransform, 8f, 4f, 50f, 50f);
            _hudRacha = UiKit.Label(racha.transform, "", 24, new Color32(0xFF, 0xBE, 0x50, 0xFF), TextAnchor.MiddleLeft);
            var hr = _hudRacha.rectTransform; hr.anchorMin = new Vector2(0f, 0f); hr.anchorMax = new Vector2(0.5f, 1f);
            hr.offsetMin = new Vector2(62f, 0f); hr.offsetMax = Vector2.zero;
            var barBg = UiKit.Panel(racha.transform, "Carga", new Color(0.32f, 0.2f, 0.16f), true, false);
            var bb = barBg.rectTransform; bb.anchorMin = new Vector2(0.5f, 0.35f); bb.anchorMax = new Vector2(0.96f, 0.65f);
            bb.offsetMin = bb.offsetMax = Vector2.zero;
            _rachaFill = UiKit.Panel(barBg.transform, "Relleno", new Color32(0xFF, 0x8C, 0x28, 0xFF), true, false);

            var menuBtn = UiKit.MakeButton(hud, "Menu", "II", UiKit.Panel2, UiKit.Cream, 28, ShowZone);
            BoxOf(menuBtn, 0.93f, 1f, 4f, 62f);

            // Salsa de la abuela activa: aviso de propina doble junto a las monedas
            _salsaBadge = UiKit.Panel(_game, "PropinaDoble", new Color(0.55f, 0.12f, 0.08f, 0.95f), true, false);
            Corner(_salsaBadge.rectTransform, new Vector2(0.5f, 1f), new Vector2(-40f, -92f), new Vector2(330f, 50f));
            _salsaText = UiKit.FillLabel(_salsaBadge.transform, "", 24, UiKit.Maiz, TextAnchor.MiddleCenter, 4f);

            // Barra de power-ups
            _powerBar = UiKit.Panel(_game, "PowerUps", new Color(UiKit.Night.r, UiKit.Night.g, UiKit.Night.b, 0.9f), true, false).rectTransform;
            UiKit.Highlight(_powerBar, UiKit.Maiz, false);
            for (int i = 0; i < 4; i++)
            {
                var p = (PowerUp)i;
                var b = UiKit.MakeButton(_powerBar, "PowerUp " + p, null, UiKit.Panel2, UiKit.Cream, 20, () => Do(Command.UsePowerUp(p)));
                b.GetComponent<Image>().sprite = UiKit.CircleSprite;
                b.GetComponent<Image>().type = Image.Type.Simple;
                _powerButtons[i] = b;
                var rt = (RectTransform)b.transform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = new Vector2(92f, 92f);
                _powerGlow[i] = UiKit.NewRect("Brillo", b.transform).gameObject.AddComponent<Image>();
                _powerGlow[i].sprite = UiKit.GlowSprite; _powerGlow[i].raycastTarget = false;
                var g = _powerGlow[i].rectTransform; g.anchorMin = g.anchorMax = new Vector2(0.5f, 0.5f); g.sizeDelta = new Vector2(150f, 150f);
                _powerGlow[i].transform.SetAsFirstSibling();
                _powerTimer[i] = UiKit.NewRect("Tiempo", b.transform).gameObject.AddComponent<Image>();
                _powerTimer[i].sprite = UiKit.RingSprite; _powerTimer[i].raycastTarget = false;
                _powerTimer[i].type = Image.Type.Filled; _powerTimer[i].fillMethod = Image.FillMethod.Radial360;
                _powerTimer[i].fillOrigin = (int)Image.Origin360.Top;
                UiKit.Stretch(_powerTimer[i].rectTransform);
                _powerIcons[i] = UiKit.Picture(b.transform, "Icono", TacosArt.PowerUp(p));
                var ic = _powerIcons[i].rectTransform; ic.anchorMin = Vector2.zero; ic.anchorMax = Vector2.one;
                ic.offsetMin = new Vector2(10f, 10f); ic.offsetMax = new Vector2(-10f, -10f);
                var cnt = UiKit.Panel(b.transform, "Cuantos", UiKit.Chile, true, false);
                Corner(cnt.rectTransform, new Vector2(1f, 1f), new Vector2(8f, 8f), new Vector2(36f, 36f));
                _powerCount[i] = UiKit.FillLabel(cnt.transform, "", 22, Color.white, TextAnchor.MiddleCenter, 0f);
            }
        }

        void SetupHud()
        {
            var lvl = _sim.Level;
            bool powers = lvl.PowerUps.Length > 0;
            _rachaBox.gameObject.SetActive(powers);
            _powerBar.gameObject.SetActive(powers);
            int n = 0;
            for (int i = 0; i < 4; i++)
            {
                bool on = System.Array.IndexOf(lvl.PowerUps, (PowerUp)i) >= 0;
                _powerButtons[i].gameObject.SetActive(on);
                if (on) ((RectTransform)_powerButtons[i].transform).anchoredPosition = new Vector2(14f + n++ * 104f, 0f);
                _powerPop[i] = 0f;
            }
            Corner(_powerBar, Vector2.zero, new Vector2(14f, 14f), new Vector2(18f + n * 104f, 112f));
            _cutT = -1f;
            _cutIn.gameObject.SetActive(false);
        }

        void RefreshHud(KitchenSimulation s)
        {
            var lvl = s.Level;
            _hudTurno.text = $"{_mission + 1}. {lvl.Name}";
            int secs = Mathf.CeilToInt(s.TimeLeft);
            _hudTiempo.text = $"{secs / 60}:{secs % 60:00}";
            _hudTiempo.color = s.TimeLeft < 10f ? UiKit.Chile : UiKit.Maiz;
            _hudMonedas.text = s.Coins.ToString();
            if (_coinPulse > 0f) _coinPulse -= Time.unscaledDeltaTime;
            float cp = 1f + 0.25f * Mathf.Clamp01(_coinPulse / 0.3f);
            _coinsRt.localScale = new Vector3(cp, cp, 1f);
            for (int i = 0; i < 3; i++) _hudStars[i].color = i < s.Stars ? UiKit.Maiz : UiKit.Line;
            string rule = RuleText(lvl.Rule);
            _hudRule.text = rule == null ? "" : $"Regla: {rule}\n" + (s.RuleBroken ? $"<color={UiKit.Hex(UiKit.Chile)}>ROTA</color>" : $"<color={UiKit.Hex(UiKit.Limon)}>vas bien</color>");

            if (lvl.PowerUps.Length > 0)
            {
                _hudRacha.text = s.Streak > 0 ? $"RACHA x{s.Streak}" : "RACHA";
                var f = _rachaFill.rectTransform;
                f.anchorMin = Vector2.zero; f.anchorMax = new Vector2(Mathf.Clamp01(s.Charge), 1f);
                f.offsetMin = f.offsetMax = Vector2.zero;
                float t = Time.unscaledTime;
                for (int i = 0; i < 4; i++)
                {
                    if (!_powerButtons[i].gameObject.activeSelf) continue;
                    var p = (PowerUp)i;
                    int n = s.Stock(p);
                    float active = p == PowerUp.Nopal ? s.NopalLeft / s.Rules.NopalTime : p == PowerUp.Salsa ? s.SalsaLeft / s.Rules.SalsaTime : 0f;
                    _powerIcons[i].color = n > 0 || active > 0f ? Color.white : new Color(0.45f, 0.42f, 0.5f, 0.75f);
                    _powerCount[i].transform.parent.gameObject.SetActive(n > 0);
                    _powerCount[i].text = n.ToString();
                    _powerGlow[i].enabled = n > 0;
                    _powerGlow[i].color = new Color(1f, 0.85f, 0.45f, 0.55f + 0.3f * Mathf.Sin(t * 5f + i));
                    _powerTimer[i].enabled = active > 0f;
                    _powerTimer[i].fillAmount = active;
                    _powerTimer[i].color = p == PowerUp.Nopal ? new Color(0.55f, 0.85f, 1f) : UiKit.Maiz;
                    if (_powerPop[i] > 0f) _powerPop[i] -= Time.unscaledDeltaTime;
                    float sc = 1f + 0.3f * Mathf.Clamp01(_powerPop[i] / 0.4f);
                    _powerButtons[i].transform.localScale = new Vector3(sc, sc, 1f);
                }
            }
            bool salsa = s.SalsaLeft > 0f;
            _salsaBadge.gameObject.SetActive(salsa);
            if (salsa) _salsaText.text = $"¡Propina doble! {Mathf.CeilToInt(s.SalsaLeft)} s";
        }

        void PulseCoins() => _coinPulse = 0.3f;

        void OnPowerUpGained(PowerUp p, bool fromPickup)
        {
            _powerPop[(int)p] = 0.4f;
            Audio.PlaySfx(_snd.Listo);
            Toast($"¡Ganaste {PowerUps.Name(p, TacosArt.Stall)}! Tócalo abajo", UiKit.Maiz);
        }

        // =============== franja anime del taquero ===============

        void BuildCutIn()
        {
            _cutIn = UiKit.Stretch(UiKit.NewRect("FranjaPowerUp", _game));
            _cutVeil = UiKit.Panel(_cutIn, "Velo", new Color(0.04f, 0.02f, 0.08f, 0.6f), false, true);
            UiKit.Stretch(_cutVeil.rectTransform);
            _cutBand = UiKit.NewRect("Franja", _cutIn);
            _cutBand.anchorMin = new Vector2(-0.2f, 0.5f); _cutBand.anchorMax = new Vector2(1.2f, 0.5f);
            _cutBand.sizeDelta = new Vector2(0f, 360f);
            _cutBand.localRotation = Quaternion.Euler(0f, 0f, 6f);
            var edge = UiKit.Panel(_cutBand, "Orilla", UiKit.Maiz, false, false); UiKit.Stretch(edge.rectTransform);
            var inner = UiKit.Panel(_cutBand, "Dentro", new Color32(0x28, 0x16, 0x3C, 0xF5), false, false);
            var irt = UiKit.Stretch(inner.rectTransform); irt.offsetMin = new Vector2(0f, 16f); irt.offsetMax = new Vector2(0f, -16f);

            _cutTaquero = UiKit.Picture(_cutIn, "Taquero", null);
            Corner(_cutTaquero.rectTransform, new Vector2(0.24f, 0.5f), Vector2.zero, new Vector2(520f, 640f));
            _cutIcon = UiKit.Picture(_cutIn, "Icono", null);
            Corner(_cutIcon.rectTransform, new Vector2(0.84f, 0.66f), Vector2.zero, new Vector2(220f, 220f));
            _cutTitle = UiKit.Outlined(_cutIn, "", 88, UiKit.Maiz, TextAnchor.MiddleCenter, 5f);
            _cutTitle.font = UiKit.Display;
            _cutTitle.horizontalOverflow = HorizontalWrapMode.Overflow;
            Corner(_cutTitle.rectTransform, new Vector2(0.64f, 0.5f), new Vector2(0f, 20f), new Vector2(1000f, 120f));
            _cutSub = UiKit.Outlined(_cutIn, "", 30, UiKit.Cream);
            Corner(_cutSub.rectTransform, new Vector2(0.64f, 0.5f), new Vector2(0f, -70f), new Vector2(900f, 50f));
            _cutIn.gameObject.SetActive(false);
        }

        void OnPowerUpUsed(PowerUp p)
        {
            _cutTaquero.sprite = TacosArt.Taquero(_taq.Id, TacosArt.Pose.Festeja);
            _cutTaquero.enabled = _cutTaquero.sprite != null;
            _cutIcon.sprite = TacosArt.PowerUp(p);
            _cutIcon.enabled = _cutIcon.sprite != null;
            _cutTitle.text = "¡" + PowerUps.Name(p, TacosArt.Stall).ToUpper() + "!";
            _cutSub.text = $"{_taq.Name}: {PowerUps.Effect(p, TacosArt.Stall)}";
            _cutT = 0f;
            _paused = true;
            _cutIn.gameObject.SetActive(true);
            _cutIn.SetAsLastSibling();
            Audio.PlaySfx(_snd.Fin);
        }

        void UpdateCutIn()
        {
            if (_cutT < 0f) return;
            _cutT += Time.unscaledDeltaTime;
            float t = _cutT;
            // entra rapido, se queda y sale
            float inK = Mathf.Clamp01(t / 0.22f), outK = Mathf.Clamp01((t - (CutInTime - 0.25f)) / 0.25f);
            float ease = 1f - Mathf.Pow(1f - inK, 3f);
            float x = Mathf.Lerp(-1400f, 0f, ease) + Mathf.Lerp(0f, 1400f, outK * outK);
            _cutBand.anchoredPosition = new Vector2(x, 0f);
            _cutTaquero.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-900f, 0f, ease) - outK * 900f, 0f);
            _cutTitle.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(1200f, 0f, ease) + outK * 1200f, 20f);
            _cutSub.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(1200f, 0f, ease) + outK * 1200f, -70f);
            float bob = 1f + 0.06f * Mathf.Sin(t * 12f);
            _cutIcon.rectTransform.localScale = new Vector3(bob, bob, 1f) * (1f - outK);
            _cutVeil.color = new Color(0.04f, 0.02f, 0.08f, 0.6f * (1f - outK));
            if (t >= CutInTime)
            {
                _cutT = -1f;
                _cutIn.gameObject.SetActive(false);
                _paused = _tutorial.ShowingIntro;
            }
        }
    }
}
