using RescatePapi.Logic;
using UnityEngine;

namespace RescatePapi
{
    /// <summary>
    /// Arma la escena por codigo: camara horizontal, fondo, suelo y runner.
    /// Usa los sprites de Assets/_Game/Art/Resources/Sprites si existen; si no, formas de color.
    /// </summary>
    public static class GameBootstrap
    {
        public static readonly Color SkyColor = new Color(0.68f, 0.87f, 0.73f);    // verde menta: igual al borde de arriba de fondo_lejano
        const float CameraHalfHeight = 4f;   // antes 5: se ve un 25% mas cerca
        const float CameraY = 2.8f;          // ve de y=-1.2 a y=6.8
        const float PlayerLeftMargin = 2f;   // unidades entre Papi y el borde izquierdo
        // Parallax: fraccion de la velocidad del suelo con que se mueve cada capa. Bajos a proposito:
        // capas lejanas casi quietas y selva lenta reducen el mareo (antes 0.08 y 0.3).
        const float FarParallax = 0.03f;
        const float MidParallax = 0.15f;
        public static readonly Color GroundColor = new Color(0.48f, 0.29f, 0.16f); // tierra

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
            cam.orthographicSize = CameraHalfHeight;      // 8 unidades de alto visibles (personajes mas grandes en pantalla)
            float halfW = cam.orthographicSize * Mathf.Max(1f, cam.aspect);
            // Papi queda a PlayerLeftMargin del borde izquierdo en cualquier proporcion de pantalla;
            // asi todo lo demas del ancho es camino visible por delante
            float camX = new RunnerConfig().PlayerX + halfW - PlayerLeftMargin;
            cam.transform.position = new Vector3(camX, CameraY, -10f); // suelo (y=0) cerca de la parte baja
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SkyColor;
            float viewHalfWidth = halfW + Mathf.Abs(camX) + 1f;

            var sprites = new SpriteSet();
            Sprite square = CreateSquareSprite();

            // Fondos con parallax (si hay arte) y suelo
            var strips = new System.Collections.Generic.List<(ScrollingStrip strip, float factor)>();
            AddStrip(strips, sprites, "fondo_lejano", -1f, FarParallax, -20, viewHalfWidth);
            AddStrip(strips, sprites, "fondo_medio", -1f, MidParallax, -10, viewHalfWidth);
            bool hasGround = AddStrip(strips, sprites, "suelo_tierra", -2f, 1f, 1, viewHalfWidth); // base en y=-2: el borde del pasto (512 px arriba) queda en y=0
            if (!hasGround)
            {
                var ground = new GameObject("Ground").AddComponent<SpriteRenderer>();
                ground.sprite = square;
                ground.color = GroundColor;
                ground.transform.position = new Vector3(0f, -2f, 0f);
                ground.transform.localScale = new Vector3(60f, 2f, 1f);
            }

            var sim = new RunnerSimulation(new RunnerConfig(), System.Environment.TickCount);
            var view = new GameObject("Runner").AddComponent<RunnerView>();
            view.Init(sim, square, sprites, strips.ConvertAll(s => s.strip));
        }

        static bool AddStrip(System.Collections.Generic.List<(ScrollingStrip, float)> list, SpriteSet sprites,
                             string key, float bottomY, float factor, int order, float viewHalfWidth)
        {
            var frames = sprites.Get(key);
            if (frames == null) return false;
            list.Add((ScrollingStrip.Create(key, frames[0], bottomY, factor, order, viewHalfWidth), factor));
            return true;
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
