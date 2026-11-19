using System;
using Tacos.Logic;
using Tacos.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Tacos
{
    /// <summary>
    /// El Tio enseña a jugar.
    /// 1) La primera vez que juegas una mision (aunque ya tengas avance de antes), explica lo nuevo (LevelConfig.Intro)
    ///    y espera a que toques "¡Va!". Se guarda en TacosData.introsSeen.
    /// 2) En las misiones con pistas (1 y 3), la primera vez (TacosData.guidedSeen): todo se oscurece menos lo que hay que tocar,
    ///    una manita lo señala y no se puede tocar otra cosa. La paciencia no baja mientras tanto.
    /// </summary>
    public sealed class TutorialOverlay
    {
        readonly RectTransform _root, _guide;
        KitchenScene _scene;
        /// <summary>Al cambiar de puesto se arma otra cocina: la manita sigue a la nueva.</summary>
        public KitchenScene Scene { set => _scene = value; }
        readonly Image[] _blockers = new Image[4];
        readonly Image _hand, _tio, _bubble, _introVeil;
        readonly Text _text;
        readonly Button _okButton;
        Action _onIntroClosed, _onGuideDone;
        KitchenSimulation _sim;
        bool _guided;
        int _guidedUntil;
        float _doneMessage;

        public bool ShowingIntro { get; private set; }

        public TutorialOverlay(RectTransform game, KitchenScene scene)
        {
            _scene = scene;
            _root = UiKit.Stretch(UiKit.NewRect("Tutorial", game));
            _guide = UiKit.Stretch(UiKit.NewRect("Guia", _root));
            for (int i = 0; i < 4; i++)
                _blockers[i] = UiKit.Panel(_guide, "Sombra " + i, new Color(0.03f, 0.02f, 0.07f, 0.66f), false, true);
            _hand = UiKit.Picture(_guide, "Manita", TacosArt.Get("ui_mano"));
            _hand.rectTransform.pivot = new Vector2(0.08f, 0.1f); // la punta del dedo
            _hand.rectTransform.anchorMin = _hand.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _hand.rectTransform.sizeDelta = new Vector2(150f, 150f);

            _introVeil = UiKit.Panel(_root, "VeloIntro", new Color(0.03f, 0.02f, 0.07f, 0.7f), false, true);
            UiKit.Stretch(_introVeil.rectTransform);

            _tio = UiKit.Picture(_root, "Tio", TacosArt.Taquero("tio", TacosArt.Pose.Quieto));
            _tio.rectTransform.pivot = new Vector2(0.5f, 0f);
            _tio.rectTransform.sizeDelta = new Vector2(300f, 420f);
            _bubble = UiKit.Panel(_root, "Globo", new Color(1f, 0.98f, 0.94f, 0.98f), true, false);
            _bubble.rectTransform.sizeDelta = new Vector2(640f, 190f);
            _text = UiKit.Label(_bubble.transform, "", 28, UiKit.Ink, TextAnchor.UpperLeft);
            var trt = UiKit.Stretch(_text.rectTransform); trt.offsetMin = new Vector2(28f, 70f); trt.offsetMax = new Vector2(-28f, -20f);
            _text.verticalOverflow = VerticalWrapMode.Truncate;
            _okButton = UiKit.MakeButton(_bubble.transform, "Va", "¡Va!", UiKit.Chile, Color.white, 30, CloseIntro);
            var ob = (RectTransform)_okButton.transform;
            ob.anchorMin = ob.anchorMax = ob.pivot = new Vector2(1f, 0f);
            ob.anchoredPosition = new Vector2(-20f, 16f); ob.sizeDelta = new Vector2(150f, 58f);
            _root.gameObject.SetActive(false);
        }

        /// <summary>Empieza una mision: intro = el Tio explica lo nuevo; guide = tutorial con la manita (si la mision tiene pistas).</summary>
        public void Begin(KitchenSimulation sim, int mission, bool intro, bool guide, Action onIntroClosed, Action onGuideDone)
        {
            _sim = sim;
            _onIntroClosed = onIntroClosed;
            _onGuideDone = onGuideDone;
            _guided = guide && sim.Level.Hints;
            _guidedUntil = mission == 0 ? 3 : 2; // tacos guiados
            _doneMessage = 0f;
            ShowingIntro = intro && !string.IsNullOrEmpty(sim.Level.Intro);
            sim.PausePatience = _guided;
            _root.gameObject.SetActive(ShowingIntro || _guided);
            _root.SetAsLastSibling();
            if (ShowingIntro) ShowIntro(sim.Level.Intro);
        }

        public void End()
        {
            ShowingIntro = false;
            _guided = false;
            if (_sim != null) _sim.PausePatience = false;
            _root.gameObject.SetActive(false);
        }

        void ShowIntro(string text)
        {
            _guide.gameObject.SetActive(false);
            _introVeil.enabled = true;
            _okButton.gameObject.SetActive(true);
            _text.text = text;
            PlaceTio(false, true);
        }

        void CloseIntro()
        {
            ShowingIntro = false;
            _introVeil.enabled = false;
            _okButton.gameObject.SetActive(false);
            _root.gameObject.SetActive(_guided);
            _onIntroClosed?.Invoke();
        }

        /// <summary>Tio abajo a la izquierda o a la derecha, con su globo hacia el centro.</summary>
        void PlaceTio(bool left, bool center = false)
        {
            var t = _tio.rectTransform;
            t.anchorMin = t.anchorMax = new Vector2(left ? 0f : 1f, 0f);
            t.anchoredPosition = new Vector2(left ? 170f : -170f, -10f);
            var b = _bubble.rectTransform;
            b.anchorMin = b.anchorMax = new Vector2(left ? 0f : 1f, 0f);
            b.pivot = new Vector2(left ? 0f : 1f, 0f);
            b.anchoredPosition = new Vector2(left ? 320f : -320f, center ? 300f : 150f);
        }

        /// <summary>Cada cuadro: sigue la pista actual (oscurece todo menos lo que hay que tocar).</summary>
        public void Update(KitchenSimulation s, Hint? hint, string hintText)
        {
            if (s != _sim) return;
            if (_doneMessage > 0f)
            {
                _doneMessage -= Time.unscaledDeltaTime;
                if (_doneMessage <= 0f && !_guided && !ShowingIntro) _root.gameObject.SetActive(false);
            }
            if (ShowingIntro || !_guided) return;

            if (s.Served >= _guidedUntil || s.IsFinished)
            {
                _guided = false;
                s.PausePatience = false;
                _guide.gameObject.SetActive(false);
                _text.text = "¡Ya le agarraste, mijo! Ahora tú solo. Fíjate en el globito de cada cliente.";
                _doneMessage = 2.8f;
                _onGuideDone?.Invoke();
                return;
            }

            _guide.gameObject.SetActive(true);
            var target = hint.HasValue ? _scene.HintTarget(hint.Value) : null;
            _text.text = hintText ?? "";
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                // Esperando (cliente, carne o tortilla): sin hueco y sin bloquear
                foreach (var b in _blockers) b.enabled = false;
                _hand.enabled = false;
                PlaceTio(false);
                return;
            }

            // Hueco: lo que hay que tocar y, si se arrastra, tambien de donde a donde (para poder arrastrar dentro)
            var path = hint.HasValue ? _scene.HintDrag(hint.Value) : null;
            Rect hole = Bounds(target);
            if (path.HasValue) { hole = Union(hole, Bounds(path.Value.from)); hole = Union(hole, Bounds(path.Value.to)); }
            const float pad = 14f;
            Vector2 min = hole.min - new Vector2(pad, pad), max = hole.max + new Vector2(pad, pad);
            var r = _guide.rect;
            SetBlock(0, r.xMin, max.y, r.xMax, r.yMax);   // arriba
            SetBlock(1, r.xMin, r.yMin, r.xMax, min.y);   // abajo
            SetBlock(2, r.xMin, min.y, min.x, max.y);     // izquierda
            SetBlock(3, max.x, min.y, r.xMax, max.y);     // derecha

            _hand.enabled = _hand.sprite != null;
            if (path.HasValue)
            {
                // La manita hace el movimiento: aprieta, arrastra y suelta, una y otra vez
                Vector2 a0 = Bounds(path.Value.from).center, a1 = Bounds(path.Value.to).center;
                float t = Mathf.Repeat(Time.unscaledTime, 1.8f);
                float k = t < 0.3f ? 0f : t < 1.3f ? Mathf.SmoothStep(0f, 1f, (t - 0.3f) / 1f) : 1f;
                _hand.rectTransform.anchoredPosition = Vector2.Lerp(a0, a1, k) + new Vector2(-8f, -8f);
                float press = t < 0.3f ? Mathf.Lerp(1f, 0.85f, t / 0.3f) : t < 1.3f ? 0.85f : Mathf.Lerp(0.85f, 1f, (t - 1.3f) / 0.5f);
                _hand.rectTransform.localScale = new Vector3(press, press, 1f);
                _hand.color = new Color(1f, 1f, 1f, t > 1.55f ? 1f - (t - 1.55f) / 0.25f : 1f);
            }
            else
            {
                var tb = Bounds(target);
                float bob = 14f * Mathf.Sin(Time.unscaledTime * 6f);
                _hand.rectTransform.anchoredPosition = new Vector2(tb.xMax - 10f + bob, tb.yMax - 10f + bob);
                _hand.rectTransform.localScale = Vector3.one;
                _hand.color = Color.white;
            }

            float cx = (min.x + max.x) / 2f;
            PlaceTio(cx > r.center.x);
        }

        /// <summary>Rectangulo de algo de la escena en coordenadas de este overlay.</summary>
        Rect Bounds(RectTransform t)
        {
            var corners = new Vector3[4];
            t.GetWorldCorners(corners);
            Vector2 a = _guide.InverseTransformPoint(corners[0]), b = _guide.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        static Rect Union(Rect a, Rect b) => Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));

        void SetBlock(int i, float x0, float y0, float x1, float y1)
        {
            var b = _blockers[i];
            b.enabled = x1 > x0 && y1 > y0;
            var rt = b.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(x0, y0);
            rt.sizeDelta = new Vector2(Mathf.Max(0f, x1 - x0), Mathf.Max(0f, y1 - y0));
        }
    }
}
