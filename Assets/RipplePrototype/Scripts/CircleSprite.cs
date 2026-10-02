using UnityEngine;

/// 运行时生成一个抗锯齿的白色圆形 Sprite（直径 = 1 世界单位），原型阶段不需要任何美术资源。
public static class CircleSprite
{
    static Sprite cached;

    public static Sprite Get()
    {
        if (cached != null) return cached;

        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        float r = size * 0.5f;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x + 0.5f - r, dy = y + 0.5f - r;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Clamp01(r - d); // 1 像素宽的柔边
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        cached = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return cached;
    }
}
