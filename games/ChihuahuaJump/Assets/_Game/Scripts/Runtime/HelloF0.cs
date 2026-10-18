using GameFactory.Core;
using UnityEngine;

namespace ChihuahuaJump
{
    /// <summary>
    /// F0: pantalla de verificacion. Demuestra que el juego compila, usa el Core
    /// y corre en Android. Se elimina al iniciar F1.
    /// </summary>
    public class HelloF0 : MonoBehaviour
    {
        GUIStyle _style;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Spawn()
        {
            if (Camera.main != null) Camera.main.backgroundColor = new Color(0.86f, 0.66f, 0.42f); // color chihuahua
            var go = new GameObject(nameof(HelloF0));
            go.AddComponent<HelloF0>();
            DontDestroyOnLoad(go);
            Debug.Log($"[ChihuahuaJump] F0 OK - Core v{FactoryInfo.CoreVersion} - app v{Application.version}");
        }

        void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.Max(24, Screen.height / 20),
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
                _style.normal.textColor = new Color(0.15f, 0.1f, 0.05f);
            }

            string text = "Chihuahua Jump\nF0 OK\n\n"
                        + $"Core v{FactoryInfo.CoreVersion}\n"
                        + $"App v{Application.version}\n"
                        + $"{SystemInfo.deviceModel}\n"
                        + $"{Screen.width}x{Screen.height}";
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), text, _style);
        }
    }
}
