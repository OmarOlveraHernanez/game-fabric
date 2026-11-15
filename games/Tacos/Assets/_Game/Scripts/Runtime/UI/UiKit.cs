using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Tacos.UI
{
    /// <summary>
    /// Ayudas para armar interfaz uGUI por codigo (sin prefabs). Copia adaptada de la de Rescate Papi:
    /// cuando ambos juegos esten estables se extrae una sola version al Core (regla de extraccion).
    /// Medidas en pixeles de referencia 1920x1080 (se escala segun el alto de la pantalla).
    /// </summary>
    public static class UiKit
    {
        // Paleta: calle de noche con neon (ver tacos-concepto)
        public static readonly Color Night = new Color32(0x15, 0x12, 0x1F, 0xFF);
        public static readonly Color Panel1 = new Color32(0x22, 0x1D, 0x33, 0xFF);
        public static readonly Color Panel2 = new Color32(0x2C, 0x26, 0x42, 0xFF);
        public static readonly Color Line = new Color32(0x3E, 0x36, 0x59, 0xFF);
        public static readonly Color Cream = new Color32(0xF6, 0xEF, 0xE4, 0xFF);
        public static readonly Color Muted = new Color32(0xA7, 0x9F, 0xBD, 0xFF);
        public static readonly Color Ink = new Color32(0x1A, 0x16, 0x26, 0xFF);
        public static readonly Color Chile = new Color32(0xFF, 0x4F, 0x3F, 0xFF);
        public static readonly Color Maiz = new Color32(0xFF, 0xC5, 0x3D, 0xFF);
        public static readonly Color Limon = new Color32(0x8F, 0xDC, 0x4F, 0xFF);
        public static readonly Color Turq = new Color32(0x2F, 0xC6, 0xB8, 0xFF);
        public static readonly Color Plato = new Color32(0xEF, 0xE6, 0xD6, 0xFF);

        static Font _font;
        static Sprite _rounded, _star;

        public static Font Font => _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            EnsureEventSystem();
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null || Object.FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        public static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>Coloca desde la esquina superior izquierda del padre: x, y hacia abajo, ancho y alto.</summary>
        public static RectTransform TopLeft(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static Image Panel(Transform parent, string name, Color color, bool rounded, bool blocksTouches)
        {
            var img = NewRect(name, parent).gameObject.AddComponent<Image>();
            img.color = color;
            if (rounded)
            {
                img.sprite = RoundedSprite;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 1.2f; // esquinas de ~20 px de referencia
            }
            img.raycastTarget = blocksTouches;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var t = NewRect("Texto", parent).gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.color = color;
            t.alignment = align;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Texto que ocupa todo el padre con un margen.</summary>
        public static Text FillLabel(Transform parent, string text, int size, Color color, TextAnchor align, float margin = 12f)
        {
            var t = Label(parent, text, size, color, align);
            var rt = Stretch((RectTransform)t.transform);
            rt.offsetMin = new Vector2(margin, margin);
            rt.offsetMax = new Vector2(-margin, -margin);
            return t;
        }

        /// <summary>Boton redondeado con texto. El contorno sirve para resaltar (elegido o pista).</summary>
        public static Button MakeButton(Transform parent, string name, string text, Color background, Color textColor,
                                        int fontSize, System.Action onClick)
        {
            var img = Panel(parent, name, background, true, true);
            var outline = img.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0f);
            outline.effectDistance = new Vector2(6f, -6f);
            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.35f);
            colors.fadeDuration = 0.05f;
            button.colors = colors;
            // text == null: sin etiqueta (el que llama arma su contenido). "" = etiqueta vacia que se llena despues.
            if (text != null) FillLabel(img.transform, text, fontSize, textColor, TextAnchor.MiddleCenter, 8f);
            button.onClick.AddListener(() => onClick?.Invoke());
            return button;
        }

        public static void Highlight(Component c, Color color, bool on)
        {
            var o = c.GetComponent<Outline>();
            if (o != null) o.effectColor = on ? color : new Color(0f, 0f, 0f, 0f);
        }

        public static Image Star(Transform parent, string name)
        {
            var img = NewRect(name, parent).gameObject.AddComponent<Image>();
            img.sprite = StarSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        public static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

        static Sprite RoundedSprite
        {
            get
            {
                if (_rounded != null) return _rounded;
                const int S = 64, R = 24;
                var tex = new Texture2D(S, S, TextureFormat.RGBA32, false)
                    { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "UiRounded" };
                var px = new Color32[S * S];
                for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    var c = new Vector2(Mathf.Clamp(p.x, R, S - R), Mathf.Clamp(p.y, R, S - R));
                    float a = Mathf.Clamp01(R - Vector2.Distance(p, c) + 0.5f);
                    px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                _rounded = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f, 0,
                                         SpriteMeshType.FullRect, new Vector4(R, R, R, R));
                return _rounded;
            }
        }

        /// <summary>Estrella blanca de 5 puntas (se colorea con Image.color).</summary>
        static Sprite StarSprite
        {
            get
            {
                if (_star != null) return _star;
                const int S = 64;
                var pts = new Vector2[10];
                for (int i = 0; i < 10; i++)
                {
                    float ang = Mathf.PI / 2f + i * Mathf.PI / 5f;
                    float r = i % 2 == 0 ? 30f : 12.5f;
                    pts[i] = new Vector2(32f + r * Mathf.Cos(ang), 32f + r * Mathf.Sin(ang));
                }
                var tex = new Texture2D(S, S, TextureFormat.RGBA32, false)
                    { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "UiStar" };
                var px = new Color32[S * S];
                for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    // 4 sub-muestras por pixel para suavizar el borde
                    int inside = 0;
                    for (int sy = 0; sy < 2; sy++)
                    for (int sx = 0; sx < 2; sx++)
                        if (InPolygon(new Vector2(x + 0.25f + sx * 0.5f, y + 0.25f + sy * 0.5f), pts)) inside++;
                    px[y * S + x] = new Color32(255, 255, 255, (byte)(inside * 255 / 4));
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                _star = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
                return _star;
            }
        }

        static bool InPolygon(Vector2 p, Vector2[] poly)
        {
            bool c = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    c = !c;
            return c;
        }
    }
}
