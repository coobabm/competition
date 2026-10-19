using UnityEngine;
using System.IO;
using System;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Small static helpers the desktop UI shares: animation clip lengths, sprite accent colors, readable textures,
    /// texture loading from disk and random/unique values. Replaces DreamOS's DreamOSInternalTools (2026-10-19): same
    /// GUID and public API.
    /// </summary>
    public class DreamOSInternalTools : MonoBehaviour
    {
        /// <summary>Length in seconds of the named clip in the animator's controller, or -1 if there is none.</summary>
        public static float GetAnimatorClipLength(Animator _animator, string _clipName)
        {
            foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == _clipName) { return clip.length; }
            }

            return -1;
        }

        /// <summary>Average color of the sprite's visible (non-transparent) pixels.</summary>
        public static Color GetSpriteAccentColor(Sprite sprite)
        {
            Texture2D texture = GetReadableTexture(sprite.texture);

            Rect rect = sprite.textureRect;
            Color[] pixels = texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
            Color accentColor = Color.black;

            int count = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a > 0f)
                {
                    accentColor += pixels[i];
                    count++;
                }
            }

            accentColor /= count;
            return accentColor;
        }

        /// <summary>Dark navy on bright colors, white on dark ones (by perceived brightness).</summary>
        public static Color GetAccentMatchColor(Color color)
        {
            float perceivedBrightness = 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;

            if (perceivedBrightness > 0.5f) { return new Color(0.09803922f, 0.1372549f, 0.1764706f); }
            return Color.white;
        }

        /// <summary>A CPU-readable copy of a texture, drawn through a temporary render texture.</summary>
        public static Texture2D GetReadableTexture(Texture2D source)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.sRGB);

            Graphics.Blit(source, renderTex);
            RenderTexture.active = renderTex;

            Texture2D readableTex = new Texture2D(source.width, source.height);
            readableTex.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableTex.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(renderTex);

            return readableTex;
        }

        /// <summary>Loads an image file into a new texture; null if the file is missing or not an image.</summary>
        public static Texture2D LoadTexture(string filePath)
        {
            if (!File.Exists(filePath)) { return null; }

            Texture2D texture = new Texture2D(2, 2);
            return texture.LoadImage(File.ReadAllBytes(filePath)) ? texture : null;
        }

        /// <summary>A random int in [minValue, maxValue) that differs from currentValue (retries until it does).</summary>
        public static int GetRandomUniqueValue(int currentValue, int minValue, int maxValue)
        {
            int value = UnityEngine.Random.Range(minValue, maxValue);

            while (currentValue == value)
            {
                value = UnityEngine.Random.Range(minValue, maxValue);
            }

            return value;
        }

        public static string GenerateUniqueGuid()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
