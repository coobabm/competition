using UnityEditor;

namespace LingGuangV05.EditorTools
{
    /// <summary>
    /// The drawn icon PNGs (Resources/LingGuangV05/Icons and the 888 casino art) are shown far smaller than they are drawn,
    /// so they import with mipmaps and trilinear filtering (no shimmer at 24 px), full alpha and no lossy compression.
    /// </summary>
    sealed class XgIconImportSettings : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.Contains("/Resources/LingGuangV05/Icons/") && !path.Contains("/Resources/LingGuangV05/Casino/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.filterMode = UnityEngine.FilterMode.Trilinear;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 512;
        }
    }
}
