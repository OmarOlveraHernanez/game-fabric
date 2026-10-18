using RescatePapi.Logic;
using UnityEngine;

namespace RescatePapi
{
    /// <summary>
    /// Arma la escena del prototipo por codigo (camara horizontal, suelo, runner).
    /// Despues se reemplaza por escena/prefabs con arte estilo codice.
    /// </summary>
    public static class GameBootstrap
    {
        public static readonly Color SkyColor = new Color(0.96f, 0.91f, 0.80f);   // papel amate claro
        public static readonly Color GroundColor = new Color(0.55f, 0.36f, 0.22f); // tierra

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            Application.targetFrameRate = 60;            // Android usa 30 por defecto
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 5f;                    // 10 unidades de alto visibles
            cam.transform.position = new Vector3(0f, 3.5f, -10f); // suelo (y=0) cerca de la parte baja
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SkyColor;

            Sprite square = CreateSquareSprite();

            var ground = new GameObject("Ground").AddComponent<SpriteRenderer>();
            ground.sprite = square;
            ground.color = GroundColor;
            ground.transform.position = new Vector3(0f, -2f, 0f);
            ground.transform.localScale = new Vector3(60f, 2f, 1f);

            var sim = new RunnerSimulation(new RunnerConfig(), System.Environment.TickCount);
            var view = new GameObject("Runner").AddComponent<RunnerView>();
            view.Init(sim, square);
        }

        /// <summary>Sprite blanco de 1x1 unidad con pivote abajo-centro (se colorea por SpriteRenderer).</summary>
        static Sprite CreateSquareSprite()
        {
            var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0f), 1f);
        }
    }
}
