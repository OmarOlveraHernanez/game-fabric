using GameFactory.Core;
using UnityEngine;

namespace __GAME__
{
    /// <summary>
    /// Punto de entrada del juego: se ejecuta solo al cargar la escena.
    /// Plantilla: muestra una pantalla de verificacion. Reemplazar por el juego.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        GUIStyle _style;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            Application.targetFrameRate = 60;            // Android usa 30 por defecto
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            if (Camera.main != null) Camera.main.backgroundColor = new Color(0.96f, 0.91f, 0.80f);
            var go = new GameObject(nameof(GameBootstrap));
            go.AddComponent<GameBootstrap>();
            DontDestroyOnLoad(go);
            Debug.Log($"[__GAME__] OK - Core v{FactoryInfo.CoreVersion} - app v{Application.version}");
        }

        void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.Max(24, Mathf.Min(Screen.width, Screen.height) / 14),
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
                _style.normal.textColor = new Color(0.15f, 0.1f, 0.05f);
            }
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height),
                $"__TITLE__\nNuevo juego de la fabrica\n\nCore v{FactoryInfo.CoreVersion}  -  App v{Application.version}", _style);
        }
    }
}
