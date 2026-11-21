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
        public int version = 4;
        public Logic.CampaignProgress progress = new Logic.CampaignProgress();
        public string taquero = "guero";
        public int[] bestStars; // solo version 1 (prototipo T1): se pasa a progress al cargar
        public int introsSeen;  // hasta la version 3: bit por mision (no alcanzaba para 72 misiones); se pasa a intros
        public int guidedSeen;
        public bool[] intros = new bool[0]; // por mision: el Tio ya explico lo nuevo
        public bool[] guided = new bool[0]; // por mision: ya se hizo el tutorial con la manita

        /// <summary>Ajusta las listas al numero de misiones y, si es un guardado viejo, pasa los bits.</summary>
        public void FitTutorials(int missions)
        {
            if (intros == null || intros.Length != missions) System.Array.Resize(ref intros, missions);
            if (guided == null || guided.Length != missions) System.Array.Resize(ref guided, missions);
            if (version >= 4) return;
            for (int i = 0; i < 31 && i < missions; i++)
            {
                intros[i] |= (introsSeen & (1 << i)) != 0;
                guided[i] |= (guidedSeen & (1 << i)) != 0;
            }
            introsSeen = guidedSeen = 0;
            version = 4;
        }
    }
}
