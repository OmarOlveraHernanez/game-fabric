using ChihuahuaJump.Logic;
using UnityEngine;

namespace ChihuahuaJump
{
    /// <summary>
    /// F1: arma la escena del prototipo por codigo (camara vertical, piso, paredes del templo).
    /// En F3 se reemplaza por escena/prefabs con arte estilo codice.
    /// </summary>
    public static class GameBootstrap
    {
        public static readonly Color SkyColor = new Color(0.96f, 0.91f, 0.80f);   // papel amate claro
        public static readonly Color GroundColor = new Color(0.55f, 0.36f, 0.22f); // tierra
        public static readonly Color WallColor = new Color(0.62f, 0.50f, 0.36f);   // piedra del templo

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            Application.targetFrameRate = 60;            // Android usa 30 por defecto
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            var config = new GameConfig();
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
            }
            // Mostrar todo el ancho del campo (+ margen) y al menos 16 unidades de alto
            float aspect = Mathf.Max(0.3f, cam.aspect);
            float size = Mathf.Max(config.VisibleTopY * 0.5f, (config.HalfWidth + 0.15f) / aspect);
            cam.orthographic = true;
            cam.orthographicSize = size;
            cam.transform.position = new Vector3(0f, size - 0.3f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SkyColor;

            Sprite square = CreateSquareSprite();
            CreateRect("Ground", square, GroundColor, new Vector2(0f, -3f), new Vector2(40f, 3f), 0);
            float wallW = 20f;
            CreateRect("WallLeft", square, WallColor, new Vector2(-config.HalfWidth - wallW * 0.5f, -3f), new Vector2(wallW, 60f), 1);
            CreateRect("WallRight", square, WallColor, new Vector2(config.HalfWidth + wallW * 0.5f, -3f), new Vector2(wallW, 60f), 1);

            var sim = new BounceSimulation(config, System.Environment.TickCount);
            var view = new GameObject("Game").AddComponent<GameView>();
            view.Init(sim, square, cam);
        }

        static void CreateRect(string name, Sprite sprite, Color color, Vector2 bottomCenter, Vector2 size, int order)
        {
            var sr = new GameObject(name).AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            sr.transform.position = new Vector3(bottomCenter.x, bottomCenter.y, 0f);
            sr.transform.localScale = new Vector3(size.x, size.y, 1f);
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
