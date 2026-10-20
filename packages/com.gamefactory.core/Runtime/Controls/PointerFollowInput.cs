using UnityEngine;
using UnityEngine.InputSystem;

namespace GameFactory.Core.Controls
{
    /// <summary>
    /// Control "seguir el dedo": devuelve la X del mundo a la que apunta el dedo (o el raton con el boton
    /// presionado). En el editor tambien A/D o flechas como direccion, y espacio/enter como toque.
    /// Llamar Poll() una vez por frame antes de leer.
    /// </summary>
    public sealed class PointerFollowInput
    {
        readonly Camera _cam;

        /// <summary>X del mundo a la que apunta el dedo/raton, o null si no hay toque.</summary>
        public float? TargetX { get; private set; }
        /// <summary>-1 izquierda, 0 nada, +1 derecha (solo teclado).</summary>
        public int Direction { get; private set; }
        /// <summary>Se toco/pulso en este frame.</summary>
        public bool Tapped { get; private set; }

        public PointerFollowInput(Camera cam) { _cam = cam; }

        public void Poll()
        {
            float? target = null;
            bool tapped = false;

            var touch = Touchscreen.current;
            if (touch != null)
            {
                var t = touch.primaryTouch;
                if (t.press.isPressed) target = ScreenToWorldX(t.position.ReadValue());
                tapped |= t.press.wasPressedThisFrame;
            }

            var mouse = Mouse.current;
            if (mouse != null && target == null)
            {
                if (mouse.leftButton.isPressed) target = ScreenToWorldX(mouse.position.ReadValue());
                tapped |= mouse.leftButton.wasPressedThisFrame;
            }

            int dir = 0;
            var kb = Keyboard.current;
            if (kb != null)
            {
                bool left = kb.aKey.isPressed || kb.leftArrowKey.isPressed;
                bool right = kb.dKey.isPressed || kb.rightArrowKey.isPressed;
                if (left != right) dir = left ? -1 : 1;
                tapped |= kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame;
            }

            TargetX = target;
            Direction = dir;
            Tapped = tapped;
        }

        float ScreenToWorldX(Vector2 screen) =>
            _cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -_cam.transform.position.z)).x;
    }
}
