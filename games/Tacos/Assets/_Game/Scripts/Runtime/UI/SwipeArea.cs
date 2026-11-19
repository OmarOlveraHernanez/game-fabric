using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tacos.UI
{
    /// <summary>
    /// Recibe el arrastre horizontal (tambien el que empieza sobre un boton hijo) y lo pasa como pixeles de referencia.
    /// Un toque sin arrastrar sigue siendo clic en el boton.
    /// </summary>
    public sealed class SwipeArea : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Action<float> Dragged;   // delta en unidades del canvas
        public Action<float> Released;  // velocidad aproximada al soltar
        public bool Dragging { get; private set; }
        float _velocity;

        float Scale()
        {
            var canvas = GetComponentInParent<Canvas>();
            return canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        }

        public void OnBeginDrag(PointerEventData e) { Dragging = true; _velocity = 0f; }

        public void OnDrag(PointerEventData e)
        {
            float dx = e.delta.x / Scale();
            _velocity = Mathf.Lerp(_velocity, dx / Mathf.Max(Time.unscaledDeltaTime, 0.001f), 0.5f);
            Dragged?.Invoke(dx);
        }

        public void OnEndDrag(PointerEventData e)
        {
            Dragging = false;
            Released?.Invoke(_velocity);
        }
    }
}
