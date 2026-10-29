using UnityEngine;

namespace RescatePapi.UI
{
    /// <summary>Ajusta el RectTransform a Screen.safeArea (evita el notch y las esquinas redondeadas).</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        Rect _applied;
        Vector2Int _screen;

        void Awake() => Apply();

        void Update()
        {
            if (Screen.safeArea != _applied || _screen.x != Screen.width || _screen.y != Screen.height) Apply();
        }

        void Apply()
        {
            var rt = (RectTransform)transform;
            _applied = Screen.safeArea;
            _screen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;
            Vector2 min = _applied.position, max = _applied.position + _applied.size;
            min.x /= Screen.width; max.x /= Screen.width;
            min.y /= Screen.height; max.y /= Screen.height;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
