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
        public static readonly Color SkyColor = new Color(0.95f, 0.90f, 0.80f);    // amate
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
            cam.orthographicSize = 5f;                    // 10 unidades de alto visibles
            cam.transform.position = new Vector3(0f, 3.5f, -10f); // suelo (y=0) cerca de la parte baja
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SkyColor;
            float viewHalfWidth = cam.orthographicSize * Mathf.Max(1f, cam.aspect) + 1f;

            var sprites = new SpriteSet();
            Sprite square = CreateSquareSprite();

            // Fondos con parallax (si hay arte) y suelo
            var strips = new System.Collections.Generic.List<(ScrollingStrip strip, float factor)>();
            AddStrip(strips, sprites, "fondo_lejano", -1f, 0.08f, -20, viewHalfWidth);
            AddStrip(strips, sprites, "fondo_medio", -1f, 0.3f, -10, viewHalfWidth);
            bool hasGround = AddStrip(strips, sprites, "suelo_tierra", -1f, 1f, 1, viewHalfWidth);
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
