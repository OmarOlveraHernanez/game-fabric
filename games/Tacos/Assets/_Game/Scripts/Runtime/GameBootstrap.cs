using GameFactory.Core;
using Tacos.UI;
using UnityEngine;

namespace Tacos
{
    /// <summary>Punto de entrada: se ejecuta solo al cargar la escena y arma el juego por codigo.</summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            Application.targetFrameRate = 60;            // Android usa 30 por defecto
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            var cam = Camera.main;
            if (cam == null) cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UiKit.Night;
            new GameObject("Tacos").AddComponent<TacosGame>();
            Debug.Log($"[Tacos] Core v{FactoryInfo.CoreVersion} - app v{Application.version}");
        }
    }

    /// <summary>Lo que se guarda entre partidas (tacos.json). Subir version si cambia el formato.</summary>
    [System.Serializable]
    public class TacosData
    {
        public int version = 1;
        public int[] bestStars = new int[3];
        public string taquero = "guero";
    }
}
