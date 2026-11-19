using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tacos.UI
{
    /// <summary>
    /// Una imagen pintada (fondo) sobre la que se acomodan cosas en pixeles de la imagen.
    /// Todo lo que se pone con Place() escala junto con la imagen.
    /// Cover: llena la pantalla recortando lo que sobre (mapas).
    /// FitWidth: ancho completo; si sobra alto queda arriba (HUD) y si falta se recorta primero la franja de abajo (escena).
    /// </summary>
    public sealed class Stage
    {
        public enum Fit { Cover, FitWidth }

        public readonly float W, H;
        public readonly RectTransform Root;
        public readonly Image Background;
        readonly Fit _fit;
        readonly float _cropBottom;

        public Stage(Transform parent, string name, Sprite background, float width, float height, Fit fit, float cropBottomPx = 0f)
        {
            W = width; H = height; _fit = fit; _cropBottom = cropBottomPx;
            Root = UiKit.NewRect(name, parent);
            Root.anchorMin = Root.anchorMax = new Vector2(0.5f, 0.5f);
            Root.pivot = new Vector2(0.5f, 0.5f);
            Background = UiKit.Picture(Root, "Fondo", background);
            Background.preserveAspect = false;
            UiKit.Stretch(Background.rectTransform);
            if (background == null) { Background.enabled = true; Background.color = UiKit.Panel1; }
        }

        /// <summary>Acomoda la imagen al tamaño del padre. Llamar cada cuadro (es barato).</summary>
        public void Layout()
        {
            var parent = (RectTransform)Root.parent;
            float pw = parent.rect.width, ph = parent.rect.height;
            if (pw <= 0f || ph <= 0f) return;
            float s = _fit == Fit.Cover ? Mathf.Max(pw / W, ph / H) : pw / W;
            float w = W * s, h = H * s;
            Root.sizeDelta = new Vector2(w, h);
            float y = 0f;
            if (_fit == Fit.FitWidth)
            {
                float overflow = h - ph;
                // pegada abajo; si es mas alta que la pantalla, primero se recorta la franja de abajo
                float shift = overflow > 0f ? Mathf.Min(overflow, _cropBottom * s) : 0f;
                y = (h - ph) / 2f - shift;
                if (overflow <= 0f) y = -(ph - h) / 2f;
            }
            Root.anchoredPosition = new Vector2(0f, y);
        }

        public static Rect R(float cx, float cy, float w, float h) => new Rect(cx - w / 2f, cy - h / 2f, w, h);

        /// <summary>Coloca un elemento en un rectangulo en pixeles de la imagen (y hacia abajo).</summary>
        public RectTransform Place(RectTransform rt, Rect r)
        {
            rt.anchorMin = new Vector2(r.xMin / W, 1f - r.yMax / H);
            rt.anchorMax = new Vector2(r.xMax / W, 1f - r.yMin / H);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public Image Pic(string name, Sprite sprite, Rect r, Transform parent = null)
        {
            var img = UiKit.Picture(parent ?? Root, name, sprite);
            Place(img.rectTransform, r);
            return img;
        }

        public Image Img(string name, Sprite sprite, Color color, Rect r, Transform parent = null)
        {
            var img = UiKit.NewRect(name, parent ?? Root).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            Place(img.rectTransform, r);
            return img;
        }

        /// <summary>Zona invisible que se puede tocar.</summary>
        public RectTransform Hotspot(string name, Rect r, Action onTap, Transform parent = null)
        {
            var img = UiKit.NewRect(name, parent ?? Root).gameObject.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            var b = img.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => { UiKit.Clicked?.Invoke(); onTap(); });
            return Place(img.rectTransform, r);
        }

        public Text Label(string text, int size, Color color, Rect r, Transform parent = null, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var t = UiKit.Outlined(parent ?? Root, text, size, color, align);
            Place(t.rectTransform, r);
            return t;
        }
    }
}
