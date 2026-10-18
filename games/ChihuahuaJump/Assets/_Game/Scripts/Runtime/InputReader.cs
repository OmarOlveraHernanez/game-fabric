using UnityEngine;
using UnityEngine.InputSystem;

namespace ChihuahuaJump
{
    /// <summary>
    /// Controles: el trampolin sigue al dedo (o al raton con el boton presionado).
    /// En el editor tambien: A/D o flechas; espacio para empezar.
    /// Candidato a moverse al Core en F2 (modulo Input).
    /// </summary>
    public sealed class InputReader
    {
        readonly Camera _cam;

        /// <summary>X del mundo a la que apunta el dedo/raton, o null si no hay toque.</summary>
        public float? TargetX { get; private set; }
        /// <summary>-1 izquierda, 0 nada, +1 derecha (solo teclado).</summary>
        public int Direction { get; private set; }
        /// <summary>Se toco/pulso en este frame (para empezar o reiniciar).</summary>
        public bool Tapped { get; private set; }

        public InputReader(Camera cam) { _cam = cam; }

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
