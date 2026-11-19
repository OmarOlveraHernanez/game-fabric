using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tacos.UI
{
    /// <summary>
    /// Hace que algo se pueda arrastrar. Tocar sin arrastrar sigue siendo clic (el Button de al lado).
    /// Begin devuelve false si en este momento no hay nada que arrastrar (por ejemplo, un cazo vacio).
    /// </summary>
    public sealed class DragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Func<bool> Begin;
        public Action<Vector2> Move;   // posicion en pantalla
        public Action<Vector2> Drop;   // posicion en pantalla al soltar
        public Action Cancel;          // se apago a medio arrastre (sin soltar en ningun lado)
        bool _active;

        public void OnBeginDrag(PointerEventData e)
        {
            _active = Begin != null && Begin();
            if (_active) Move?.Invoke(e.position);
        }

        public void OnDrag(PointerEventData e)
        {
            if (_active) Move?.Invoke(e.position);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!_active) return;
            _active = false;
            Drop?.Invoke(e.position);
        }

        // Bug de las tortillas flotando: si lo que arrastras desaparece a medio camino (la tortilla se quemo, se acabo la mision),
        // Unity ya no manda OnEndDrag y el dibujo se quedaba pegado en el aire. Aqui se cancela.
        void OnDisable()
        {
            if (!_active) return;
            _active = false;
            Cancel?.Invoke();
        }
    }
}
