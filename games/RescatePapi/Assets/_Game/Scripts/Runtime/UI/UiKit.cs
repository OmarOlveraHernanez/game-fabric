using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace RescatePapi.UI
{
    /// <summary>
    /// Ayudas para armar interfaz uGUI por codigo (sin prefabs): canvas, paneles redondeados, textos y botones.
    /// Pensado para extraerse al Core cuando un segundo juego lo use (regla de extraccion).
    /// Medidas en pixeles de referencia 1920x1080 (se escala segun el alto de la pantalla).
    /// </summary>
    public static class UiKit
    {
        // Paleta (ver ARTE.md)
        public static readonly Color Ink = new Color32(0x1A, 0x14, 0x10, 0xFF);
        public static readonly Color Amate = new Color32(0xF2, 0xE6, 0xCC, 0xFF);
        public static readonly Color Gold = new Color32(0xF2, 0xC1, 0x4E, 0xFF);
        public static readonly Color Turq = new Color32(0x1F, 0x8A, 0x8A, 0xFF);
        public static readonly Color Grana = new Color32(0xB0, 0x30, 0x2A, 0xFF);

        static Font _font;
        static Sprite _rounded;
        static readonly List<RaycastResult> Hits = new List<RaycastResult>();

        public static Font Font => _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        /// <summary>Canvas a pantalla completa que escala con el alto (1080 de referencia).</summary>
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

        /// <summary>Ocupa todo el padre.</summary>
        public static RectTransform Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>Ancla y pivote en el mismo punto (0..1), con posicion y tamano en pixeles de referencia.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>Rectangulo de color, redondeado o no. blocksTouches = absorbe los toques.</summary>
        public static Image Panel(Transform parent, string name, Color color, bool rounded, bool blocksTouches)
        {
            var img = NewRect(name, parent).gameObject.AddComponent<Image>();
            img.color = color;
            if (rounded)
            {
                img.sprite = RoundedSprite;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 0.5f; // esquinas de ~48 px de referencia
            }
            img.raycastTarget = blocksTouches;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color,
                                 TextAnchor align = TextAnchor.MiddleCenter, bool outline = true)
        {
            var t = NewRect("Texto", parent).gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false; // los textos no roban toques al juego
            if (outline)
            {
                var o = t.gameObject.AddComponent<Outline>();
                o.effectColor = Ink;
                float d = Mathf.Max(2f, size * 0.05f);
                o.effectDistance = new Vector2(d, -d);
            }
            return t;
        }

        public static Image Icon(Transform parent, Sprite sprite)
        {
            var img = NewRect("Icono", parent).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Boton redondeado con contorno de tinta y texto centrado.</summary>
        public static Button MakeButton(Transform parent, string text, Color background, Color textColor, int fontSize,
                                        System.Action onClick)
        {
            var img = Panel(parent, "Boton " + text, background, true, true);
            var outline = img.gameObject.AddComponent<Outline>();
            outline.effectColor = Ink;
            outline.effectDistance = new Vector2(5f, -5f);
            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.05f;
            button.colors = colors;
            var label = Label(img.transform, text, fontSize, textColor, TextAnchor.MiddleCenter, false);
            Stretch((RectTransform)label.transform);
            button.onClick.AddListener(() => onClick?.Invoke());
            return button;
        }

        public static Text ButtonLabel(Button b) => b.GetComponentInChildren<Text>();

        /// <summary>Hay algun elemento de interfaz que recibe toques en esa posicion de pantalla.</summary>
        public static bool IsOverUI(Vector2 screenPosition)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            var data = new PointerEventData(es) { position = screenPosition };
            Hits.Clear();
            es.RaycastAll(data, Hits);
            return Hits.Count > 0;
        }

        /// <summary>Posicion del dedo o raton que esta presionando (si hay).</summary>
        public static bool TryGetPressedPointer(out Vector2 position)
        {
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed)
            {
                position = touch.primaryTouch.position.ReadValue();
                return true;
            }
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                position = mouse.position.ReadValue();
                return true;
            }
            position = default;
            return false;
        }

        /// <summary>Sprite blanco 64x64 con esquinas redondeadas, listo para 9-slice.</summary>
        static Sprite RoundedSprite
        {
            get
            {
                if (_rounded != null) return _rounded;
                const int S = 64, R = 24;
                var tex = new Texture2D(S, S, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "UiRounded"
                };
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
    }
}
