using UnityEngine;
using System.IO;
using System;

namespace Michsky.DreamOS
{
    public class DreamOSInternalTools : MonoBehaviour
    {
        public static float GetAnimatorClipLength(Animator _animator, string _clipName)
        {
            float _lengthValue = -1;
            RuntimeAnimatorController _rac = _animator.runtimeAnimatorController;

            for (int i = 0; i < _rac.animationClips.Length; i++)
            {
                if (_rac.animationClips[i].name == _clipName)
                {
                    _lengthValue = _rac.animationClips[i].length;
                    break;
                }
            }

            return _lengthValue;
        }

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

        public static Color GetAccentMatchColor(Color color)
        {
            float perceivedBrightness = 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
            Color contrastingColor;

            if (perceivedBrightness > 0.5f) { contrastingColor = new Color(0.09803922f, 0.1372549f, 0.1764706f); }
            else { contrastingColor = Color.white; }

            return contrastingColor;
        }

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

        public static Texture2D LoadTexture(string filePath)
        {
            Texture2D Tex2D;
            byte[] fileData;

            if (File.Exists(filePath))
            {
                fileData = File.ReadAllBytes(filePath);
                Tex2D = new Texture2D(2, 2);

                if (Tex2D.LoadImage(fileData))
                    return Tex2D;
            }

            return null;
        }

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
            Guid uniqueGuid = Guid.NewGuid();
            string uniqueString = uniqueGuid.ToString();
            return uniqueString;
        }
    }
}