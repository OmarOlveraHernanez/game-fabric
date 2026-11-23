using Tacos.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Tacos
{
    /// <summary>
    /// Pantalla de resultados con el estilo del juego (antes eran botones lisos):
    /// el taquero festejando, un cartel de madera con foquitos, liston con el titulo, estrellas que caen en sus huecos
    /// una por una, lo que hiciste con iconos, y botones de madera pintada.
    /// </summary>
    public sealed partial class TacosGame
    {
        Stage _endStage;
        Image _endCook, _endPanel;
        Text _endTitle, _endInfo, _endServed, _endCoins, _endStreak;
        readonly Image[] _endStars = new Image[3];
        RectTransform _nextGroup;
        int _endStarCount;
        float _endT;

        static readonly float[] EndStarX = { 730, 860, 990 };

        void BuildEnd()
        {
            _end = UiKit.Stretch(UiKit.NewRect("Fin", _root));
            UiKit.Stretch((RectTransform)UiKit.Panel(_end, "Velo", new Color(0.05f, 0.03f, 0.08f, 0.78f), false, true).transform);
            _endStage = new Stage(_end, "Resultados", null, 1536, 864, Stage.Fit.FitWidth);
            _endStage.Background.color = new Color(0f, 0f, 0f, 0f);
            var st = _endStage;

            _endCook = st.Pic("Taquero", null, Stage.R(300, 480, 400, 540));

            // Cartel colgado con foquitos
            _endPanel = st.Pic("Cartel", TacosArt.Get("ui_cartel"), Stage.R(860, 450, 880, 580));
            _endPanel.preserveAspect = false;
            if (_endPanel.sprite == null) { _endPanel.sprite = UiKit.RoundedSpritePublic; _endPanel.color = new Color(0.32f, 0.2f, 0.12f); }
            _endPanel.rectTransform.pivot = new Vector2(0.5f, 1f);

            var liston = st.Pic("Liston", TacosArt.Get("ui_liston"), Stage.R(860, 170, 760, 196));
            liston.preserveAspect = false;
            _endTitle = st.Label("", 46, Color.white, Stage.R(860, 156, 520, 70));
            _endTitle.font = UiKit.Display;
            FitText(_endTitle, 46);

            for (int i = 0; i < 3; i++)
            {
                float size = i == 1 ? 150f : 124f, y = i == 1 ? 300f : 318f;
                st.Pic("Hueco " + i, TacosArt.Get("ui_estrella_hueco"), Stage.R(EndStarX[i], y, size, size));
                _endStars[i] = st.Pic("Estrella " + i, TacosArt.Get("ui_estrella") ?? UiKit.StarOverride, Stage.R(EndStarX[i], y - 2, size * 0.86f, size * 0.86f));
            }

            // Lo que hiciste: platos servidos, monedas y mejor racha
            _endServed = EndStat(st, "ing_taco", 690);
            _endCoins = EndStat(st, "ui_moneda", 860);
            _endStreak = EndStat(st, "ui_racha", 1030);

            _endInfo = st.Label("", 22, new Color32(0xFF, 0xF0, 0xD2, 0xFF), Stage.R(860, 600, 700, 74));
            FitText(_endInfo, 22);
            BuildDoubleCoins(st);

            EndButton(st, "btn_rojo", "‹ Calle", 560, 230, ShowZone);
            EndButton(st, "btn_madera", "Repetir", 830, 260, StartTurn);
            _nextGroup = EndButton(st, "btn_verde", "Siguiente ›", 1110, 300, () =>
            {
                _mission = Mathf.Min(_mission + 1, Missions.Length - 1);
                StartTurn();
            });
        }

        Text EndStat(Stage st, string icon, float x)
        {
            var ic = st.Pic("Icono " + icon, TacosArt.Get(icon), Stage.R(x, 450, 70, 64));
            ic.preserveAspect = true;
            var t = st.Label("", 38, UiKit.Maiz, Stage.R(x, 512, 170, 50));
            t.font = UiKit.Display;
            return t;
        }

        /// <summary>Boton de madera pintada: dibujo, texto encima y se toca todo.</summary>
        RectTransform EndButton(Stage st, string art, string text, float x, float w, System.Action onTap)
        {
            var group = UiKit.NewRect("Boton " + text, st.Root);
            UiKit.Stretch(group);
            var img = st.Pic("Fondo", TacosArt.Get(art), Stage.R(x, 760, w, 96), group);
            img.preserveAspect = false;
            if (img.sprite == null) { img.sprite = UiKit.RoundedSpritePublic; img.color = UiKit.Chile; }
            var label = st.Label(text, 34, Color.white, Stage.R(x, 756, w - 40, 60), group);
            label.font = UiKit.Display;
            FitText(label, 34);
            // al salir de resultados puede tocar un anuncio entre misiones (cada 3, y nunca con "Quitar anuncios")
            st.Hotspot("Tocar", Stage.R(x, 760, w, 96), () => { UiKit.Clicked?.Invoke(); AfterResults(onTap); }, group);
            return group;
        }

        void ShowResults(string title, int stars, string info, bool canNext)
        {
            _endTitle.text = title;
            _endInfo.text = info;
            _endServed.text = _sim.Served.ToString();
            _endCoins.text = _sim.Coins.ToString();
            _endStreak.text = "x" + _sim.BestStreak;
            _endStarCount = stars;
            _endCook.sprite = TacosArt.Taquero(_taq.Id, stars > 0 ? TacosArt.Pose.Festeja : TacosArt.Pose.Quieto);
            _endCook.enabled = _endCook.sprite != null;
            _endCook.preserveAspect = true;
            _nextGroup.gameObject.SetActive(canNext);
            foreach (var s in _endStars) s.enabled = false;
            _endT = 0f;
        }

        /// <summary>El cartel baja meciendose y las estrellas caen en sus huecos una por una.</summary>
        void UpdateResults()
        {
            _endStage.Layout();
            _endT += Time.unscaledDeltaTime;
            float drop = Mathf.Clamp01(_endT / 0.45f);
            float swing = (1f - drop) * 8f + 1.2f * Mathf.Sin(_endT * 1.6f) * Mathf.Clamp01(_endT - 0.4f);
            _endPanel.rectTransform.localRotation = Quaternion.Euler(0f, 0f, swing);
            for (int i = 0; i < 3; i++)
            {
                float t0 = 0.5f + i * 0.35f;
                bool on = i < _endStarCount && _endT >= t0;
                if (on && !_endStars[i].enabled) Audio.PlaySfx(_snd.Estrella, 0.8f);
                _endStars[i].enabled = on && _endStars[i].sprite != null;
                if (!on) continue;
                float k = Mathf.Clamp01((_endT - t0) / 0.3f);
                float sc = k < 1f ? Mathf.Lerp(2f, 1f, k * k) : 1f + 0.04f * Mathf.Sin(_endT * 3f + i);
                _endStars[i].rectTransform.localScale = new Vector3(sc, sc, 1f);
            }
            float bob = 1f + 0.02f * Mathf.Sin(_endT * 4f);
            _endCook.rectTransform.localScale = new Vector3(bob, bob, 1f);
        }
    }
}
