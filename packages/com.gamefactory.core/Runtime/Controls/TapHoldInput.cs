using UnityEngine.InputSystem;

namespace GameFactory.Core.Controls
{
    /// <summary>
    /// Control de un dedo "tocar / mantener": pantalla tactil, raton (boton izquierdo) o teclado
    /// (espacio / flecha arriba). Llamar Poll() una vez por frame antes de leer.
    /// </summary>
    public sealed class TapHoldInput
    {
        /// <summary>Se empezo a presionar en este frame.</summary>
        public bool PressedThisFrame { get; private set; }
        /// <summary>Se esta manteniendo presionado.</summary>
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
