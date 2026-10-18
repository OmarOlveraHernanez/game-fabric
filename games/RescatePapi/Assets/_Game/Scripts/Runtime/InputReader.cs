using UnityEngine.InputSystem;

namespace RescatePapi
{
    /// <summary>
    /// Lee "tocar" y "mantener" de pantalla tactil, raton o teclado (espacio / flecha arriba).
    /// Candidato a moverse al Core en F2 (modulo Input).
    /// </summary>
    public sealed class InputReader
    {
        public bool PressedThisFrame { get; private set; }
        public bool Held { get; private set; }

        public void Poll()
        {
            bool pressed = false, held = false;

            var touch = Touchscreen.current;
            if (touch != null)
            {
                var p = touch.primaryTouch.press;
                pressed |= p.wasPressedThisFrame;
                held |= p.isPressed;
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                pressed |= mouse.leftButton.wasPressedThisFrame;
                held |= mouse.leftButton.isPressed;
            }

            var kb = Keyboard.current;
            if (kb != null)
            {
                pressed |= kb.spaceKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame;
                held |= kb.spaceKey.isPressed || kb.upArrowKey.isPressed;
            }

            PressedThisFrame = pressed;
            Held = held;
        }
    }
}
