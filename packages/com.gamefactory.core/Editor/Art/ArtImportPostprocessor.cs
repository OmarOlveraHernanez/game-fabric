using UnityEditor;
using UnityEngine;

namespace GameFactory.Core.Editor
{
    /// <summary>
    /// Estandar de la fabrica para arte 2D: todo PNG dentro de "Assets/_Game/Art/" se importa como Sprite
    /// con 256 pixeles por unidad, pivote abajo-centro, sin mipmaps y con transparencia.
    /// Asi el artista solo exporta el PNG con el lienzo correcto (ver ARTE.md del juego) y no configura nada.
    /// Los archivos que empiezan con "fondo_" o "suelo_" usan malla completa (para repetirse en tira).
    /// </summary>
    public sealed class ArtImportPostprocessor : AssetPostprocessor
    {
        public const string ArtFolder = "/_Game/Art/";
        public const float PixelsPerUnit = 256f;

        void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.Contains(ArtFolder) || !path.ToLowerInvariant().EndsWith(".png")) return;

            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = PixelsPerUnit;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.maxTextureSize = 2048;

            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            string file = System.IO.Path.GetFileName(path).ToLowerInvariant();
            settings.spriteMeshType = file.StartsWith("fondo_") || file.StartsWith("suelo_")
                ? SpriteMeshType.FullRect
                : SpriteMeshType.Tight;
            ti.SetTextureSettings(settings);
        }
    }
}
