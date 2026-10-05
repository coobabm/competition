using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HongmengOS.Retro.Editor
{
    /// <summary>
    /// Original, hand-constructed 32-pixel desktop artwork. Coordinates use a
    /// top-left origin; there are no downloaded images or platform-brand assets.
    /// Run explicitly after compilation; importing this helper changes no assets.
    /// </summary>
    public static class RetroIconBuilder
    {
        private const string OutputDirectory = "Assets/HongmengOS/Retro/Icons/Generated";
        private static readonly Color32 Ink = C(32, 34, 39);
        private static readonly Color32 Shadow = C(79, 80, 79);
        private static readonly Color32 Grey = C(157, 161, 159);
        private static readonly Color32 Silver = C(192, 195, 185);
        private static readonly Color32 Paper = C(239, 238, 210);
        private static readonly Color32 White = C(255, 255, 242);
        private static readonly Color32 Navy = C(0, 0, 128);
        private static readonly Color32 Blue = C(35, 94, 199);
        private static readonly Color32 Sky = C(100, 188, 236);
        private static readonly Color32 Cyan = C(121, 238, 238);
        private static readonly Color32 Teal = C(0, 128, 128);
        private static readonly Color32 Green = C(0, 120, 57);
        private static readonly Color32 Lime = C(106, 195, 70);
        private static readonly Color32 Yellow = C(255, 222, 80);
        private static readonly Color32 Gold = C(183, 140, 39);
        private static readonly Color32 Red = C(189, 48, 54);
        private static readonly Color32 Pink = C(239, 122, 120);
        private static readonly Color32 Purple = C(119, 74, 157);

        [MenuItem("HongmengOS/Retro/Build Original Pixel Icons")]
        public static void BuildFromMenu()
        {
            Debug.Log("Built " + BuildAssets().Count + " original HongmengOS retro icons.");
        }

        public static Dictionary<string, Sprite> BuildAssets()
        {
            var drawings = new Dictionary<string, Action<Pixels>>(StringComparer.Ordinal)
            {
                { "Calculator", Calculator }, { "Commander", Commander },
                { "Game Hub", GameHub }, { "Mail", Mail },
                { "Messaging", Messaging }, { "Music Player", MusicPlayer },
                { "Notepad", Notepad }, { "Photo Gallery", PhotoGallery },
                { "Reminder", Reminder }, { "Settings", Settings },
                { "Video Player", VideoPlayer }, { "Web Browser", WebBrowser },
                { "Widget Library", WidgetLibrary }
            };
            Directory.CreateDirectory(OutputDirectory);
            var result = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, Action<Pixels>> drawing in drawings)
            {
                var canvas = new Pixels();
                drawing.Value(canvas);
                string path = OutputDirectory + "/Retro-" + drawing.Key.Replace(' ', '-') + ".png";
                var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false, false);
                try
                {
                    texture.SetPixels32(canvas.Data);
                    texture.Apply(false, false);
                    File.WriteAllBytes(path, texture.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Cannot import " + path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32;
                importer.spritePivot = new Vector2(0.5f, 0.5f);
                importer.spriteBorder = Vector4.zero;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.isReadable = false;
                importer.filterMode = FilterMode.Point;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 32;
                importer.sRGBTexture = true;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) throw new InvalidOperationException("Missing sprite at " + path);
                result.Add(drawing.Key, sprite);
            }
            AssetDatabase.SaveAssets();
            return result;
        }

        private static void Calculator(Pixels p)
        {
            p.Rect(8, 4, 19, 26, Shadow);
            p.Bevel(6, 2, 19, 26, Silver);
            p.Rect(9, 5, 13, 7, Ink); p.Rect(10, 6, 11, 5, Lime);
            p.Rect(11, 7, 2, 1, Green); p.Rect(15, 7, 1, 3, Green);
            p.Rect(18, 7, 2, 1, Green); p.Rect(18, 9, 2, 1, Green); p.Dot(20, 10, Green);
            for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++)
                p.Bevel(9 + x * 4, 14 + y * 4, 3, 3, Paper);
            p.Bevel(21, 14, 3, 3, Red); p.Bevel(21, 18, 3, 7, Blue);
            p.Rect(10, 26, 7, 1, White);
        }

        private static void Commander(Pixels p)
        {
            Monitor(p, 3, 3, 25, 20);
            p.Rect(6, 6, 19, 13, Ink);
            p.Line(8, 9, 10, 11, Lime); p.Line(10, 11, 8, 13, Lime);
            p.Rect(13, 13, 5, 1, Lime);
            p.Rect(8, 7, 13, 1, Shadow); p.Rect(7, 16, 16, 1, Green);
            Keyboard(p, 4, 27, 24, 4);
        }

        private static void GameHub(Pixels p)
        {
            p.Line(17, 0, 17, 4, Ink); p.Line(17, 4, 23, 4, Ink);
            p.Line(23, 4, 23, 8, Ink); p.Line(23, 8, 17, 8, Ink); p.Line(17, 8, 17, 12, Ink);
            p.Poly(Ink, 6, 11, 25, 11, 29, 16, 31, 26, 27, 29, 23, 28, 20, 23, 11, 23, 8, 28, 4, 29, 1, 25, 3, 16);
            p.Poly(Silver, 6, 12, 24, 12, 28, 17, 29, 25, 27, 27, 24, 26, 20, 21, 11, 21, 7, 26, 4, 27, 3, 24, 5, 16);
            p.Line(6, 13, 23, 13, White); p.Line(4, 18, 6, 14, White);
            p.Rect(7, 15, 3, 9, Ink); p.Rect(4, 18, 9, 3, Ink);
            p.Rect(8, 16, 1, 2, Grey); p.Rect(5, 19, 2, 1, Grey);
            p.Ellipse(22, 15, 4, 4, Red); p.Dot(23, 16, Pink);
            p.Ellipse(25, 20, 4, 4, Blue); p.Dot(26, 21, Sky);
            p.Rect(14, 19, 3, 2, Shadow); p.Rect(18, 19, 3, 2, Shadow);
            p.Rect(14, 15, 6, 1, Grey);
        }

        private static void Mail(Pixels p)
        {
            p.Rect(4, 9, 27, 20, Shadow);
            p.Rect(2, 7, 27, 20, Ink); p.Rect(3, 8, 25, 18, Yellow);
            p.Line(3, 25, 15, 16, Gold); p.Line(27, 25, 15, 16, Gold);
            p.Poly(Paper, 3, 8, 27, 8, 15, 19);
            p.Line(3, 8, 15, 19, Gold); p.Line(15, 19, 27, 8, Gold);
            p.Line(4, 9, 14, 18, White);
            p.Rect(22, 3, 8, 9, Paper); p.Rect(23, 4, 6, 7, Red);
            p.Rect(24, 5, 4, 1, Pink); p.Rect(25, 7, 2, 2, Yellow);
            p.Line(25, 2, 30, 2, Shadow); p.Line(29, 5, 31, 5, Shadow);
        }

        private static void Messaging(Pixels p)
        {
            p.Rect(3, 3, 22, 17, Shadow); p.Bevel(1, 1, 22, 17, Silver);
            p.Rect(3, 3, 18, 3, Navy); p.Rect(4, 4, 7, 1, White); p.Rect(18, 4, 2, 1, Silver);
            p.Rect(3, 7, 18, 9, White); p.Rect(5, 9, 12, 1, Blue); p.Rect(5, 12, 8, 1, Blue);
            p.Poly(Ink, 5, 17, 9, 17, 5, 21); p.Poly(White, 6, 17, 8, 17, 6, 19);
            p.Rect(10, 13, 22, 16, Shadow); p.Bevel(8, 11, 22, 16, Silver);
            p.Rect(10, 13, 18, 3, Teal); p.Rect(11, 14, 6, 1, White); p.Rect(25, 14, 2, 1, Silver);
            p.Rect(10, 17, 18, 8, Paper); p.Rect(12, 19, 13, 1, Shadow); p.Rect(12, 22, 8, 1, Shadow);
            p.Poly(Ink, 23, 26, 27, 26, 27, 30); p.Poly(Paper, 24, 26, 26, 26, 26, 28);
        }

        private static void MusicPlayer(Pixels p)
        {
            p.Ellipse(2, 7, 24, 24, Ink); p.Ellipse(3, 8, 22, 22, Silver);
            p.Poly(White, 4, 14, 8, 10, 13, 18, 11, 20);
            p.Poly(Sky, 13, 18, 20, 10, 23, 14, 15, 21);
            p.Poly(Pink, 13, 22, 20, 28, 17, 29, 11, 23);
            p.Line(4, 21, 10, 21, Grey); p.Line(17, 21, 24, 21, Grey);
            p.Ellipse(10, 16, 8, 8, Grey); p.Ellipse(11, 17, 6, 6, White); p.Ellipse(12, 18, 4, 4, Ink);
            p.Rect(21, 4, 3, 16, Ink); p.Rect(28, 2, 3, 15, Ink);
            p.Poly(Ink, 21, 4, 30, 1, 30, 7, 21, 10);
            p.Poly(Blue, 22, 5, 29, 3, 29, 5, 22, 8);
            p.Ellipse(16, 17, 8, 6, Ink); p.Ellipse(23, 14, 8, 6, Ink);
            p.Rect(18, 18, 3, 1, Blue); p.Rect(25, 15, 3, 1, Blue);
        }

        private static void Notepad(Pixels p)
        {
            p.Rect(6, 4, 22, 26, Shadow); p.Rect(4, 2, 22, 26, Ink); p.Rect(5, 3, 20, 24, Paper);
            p.Rect(5, 3, 20, 4, Blue); p.Rect(6, 4, 18, 1, Sky);
            for (int x = 8; x < 24; x += 4) { p.Rect(x, 1, 2, 5, Ink); p.Rect(x, 2, 1, 3, White); }
            p.Rect(8, 9, 1, 15, Pink);
            for (int y = 11; y < 25; y += 4) p.Rect(10, y, 12, 1, Sky);
            p.Poly(Ink, 23, 12, 28, 15, 19, 29, 14, 31, 14, 26);
            p.Poly(Yellow, 23, 14, 26, 16, 18, 28, 16, 27);
            p.Line(24, 15, 17, 27, Gold); p.Poly(Paper, 15, 28, 18, 29, 15, 30);
            p.Poly(Red, 24, 12, 28, 14, 27, 16, 23, 14); p.Dot(14, 30, Ink);
        }

        private static void PhotoGallery(Pixels p)
        {
            p.Rect(2, 3, 25, 22, Ink); p.Rect(3, 4, 23, 20, Paper);
            p.Rect(4, 5, 21, 17, Purple); p.Ellipse(17, 7, 5, 5, Pink);
            p.Rect(6, 8, 25, 22, Shadow); p.Rect(4, 6, 25, 22, Ink); p.Rect(5, 7, 23, 20, White);
            p.Rect(7, 9, 19, 14, Sky); p.Ellipse(19, 10, 5, 5, Yellow);
            p.Poly(Green, 7, 20, 13, 13, 20, 22, 7, 22);
            p.Poly(Lime, 12, 22, 21, 15, 26, 20, 26, 22);
            p.Poly(White, 11, 15, 13, 13, 15, 16, 13, 15);
            p.Rect(7, 23, 19, 1, Shadow); p.Rect(9, 25, 13, 1, Silver);
        }

        private static void Reminder(Pixels p)
        {
            p.Rect(4, 4, 23, 25, Shadow); p.Bevel(2, 2, 23, 25, Paper);
            p.Rect(4, 4, 19, 6, Red); p.Rect(5, 5, 17, 1, Pink);
            p.Rect(7, 1, 3, 6, Ink); p.Rect(17, 1, 3, 6, Ink);
            p.Rect(8, 2, 1, 4, Silver); p.Rect(18, 2, 1, 4, Silver);
            for (int y = 12; y <= 22; y += 5) for (int x = 5; x <= 20; x += 5) p.Rect(x, y, 3, 3, Grey);
            p.Rect(10, 12, 3, 3, Blue);
            p.Ellipse(14, 14, 18, 18, Ink); p.Ellipse(15, 15, 16, 16, Gold); p.Ellipse(17, 17, 12, 12, White);
            p.Rect(22, 18, 1, 5, Ink); p.Line(22, 23, 26, 25, Ink); p.Dot(22, 23, Red);
            p.Dot(22, 17, Shadow); p.Dot(28, 23, Shadow); p.Dot(22, 28, Shadow); p.Dot(17, 23, Shadow);
        }

        private static void Settings(Pixels p)
        {
            Monitor(p, 2, 2, 26, 21);
            p.Rect(5, 5, 20, 13, Navy); p.Rect(6, 6, 18, 2, Blue); p.Rect(7, 6, 6, 1, White);
            p.Rect(6, 9, 18, 8, Paper);
            for (int x = 9; x <= 21; x += 6) p.Rect(x, 10, 1, 6, Shadow);
            p.Bevel(7, 11, 5, 3, Silver); p.Bevel(13, 13, 5, 3, Silver); p.Bevel(19, 10, 5, 3, Silver);
            Keyboard(p, 2, 27, 23, 4);
            p.Poly(Ink, 24, 18, 29, 18, 31, 22, 31, 28, 29, 31, 25, 31, 23, 28, 23, 22);
            p.Poly(Silver, 25, 19, 28, 19, 30, 22, 30, 28, 28, 30, 25, 30, 24, 28, 24, 22);
            p.Line(27, 19, 27, 24, Shadow); p.Line(24, 24, 30, 24, Shadow); p.Line(25, 20, 25, 23, White);
            p.Rect(26, 27, 2, 2, Grey);
        }

        private static void VideoPlayer(Pixels p)
        {
            p.Rect(5, 8, 27, 23, Shadow); p.Rect(3, 6, 27, 23, Ink); p.Rect(7, 7, 19, 21, Navy);
            for (int y = 8; y < 29; y += 5) { p.Rect(4, y, 2, 3, White); p.Rect(27, y, 2, 3, White); }
            p.Rect(8, 8, 17, 3, Blue); p.Rect(8, 24, 17, 3, Blue);
            p.Poly(Shadow, 13, 12, 23, 18, 13, 24); p.Poly(White, 12, 11, 22, 17, 12, 23);
            p.Line(13, 12, 13, 21, Cyan);
            p.Ellipse(1, 1, 13, 13, Ink); p.Ellipse(2, 2, 11, 11, Silver);
            p.Ellipse(5, 5, 5, 5, Ink); p.Dot(7, 3, White); p.Dot(3, 7, White); p.Dot(11, 7, Shadow); p.Dot(7, 11, Shadow);
            p.Ellipse(15, 0, 12, 12, Ink); p.Ellipse(16, 1, 10, 10, Silver); p.Ellipse(19, 4, 4, 4, Ink);
            p.Dot(21, 2, White); p.Dot(17, 5, White); p.Dot(24, 5, Shadow);
        }

        private static void WebBrowser(Pixels p)
        {
            p.Ellipse(2, 1, 26, 26, Ink); p.Ellipse(3, 2, 24, 24, Blue);
            p.Ellipse(7, 2, 16, 24, Sky); p.Ellipse(8, 3, 14, 22, Blue);
            p.Line(3, 13, 26, 13, Sky); p.Line(6, 6, 23, 6, Sky); p.Line(5, 20, 24, 20, Sky);
            p.Line(14, 2, 14, 25, Sky);
            p.Poly(Lime, 7, 5, 12, 4, 13, 7, 10, 10, 8, 9, 6, 7);
            p.Poly(Green, 9, 13, 13, 14, 15, 19, 12, 24, 10, 20);
            p.Poly(Lime, 18, 6, 23, 8, 25, 13, 20, 12, 18, 15, 16, 11);
            p.Bevel(12, 16, 20, 15, Silver); p.Rect(14, 18, 16, 3, Navy);
            p.Rect(15, 19, 5, 1, White); p.Rect(27, 19, 2, 1, Silver);
            p.Rect(14, 22, 16, 7, White); p.Rect(16, 23, 12, 1, Blue); p.Rect(16, 26, 8, 1, Shadow);
            p.Poly(Ink, 25, 23, 30, 27, 28, 27, 30, 30, 28, 31, 26, 28, 25, 30);
            p.Poly(White, 26, 25, 28, 26, 27, 27, 29, 30, 28, 30, 26, 27);
        }

        private static void WidgetLibrary(Pixels p)
        {
            SmallWindow(p, 1, 2, 14, 13, Blue);
            p.Rect(4, 8, 8, 4, White); p.Rect(5, 9, 2, 2, Red); p.Rect(8, 9, 3, 1, Grey);
            SmallWindow(p, 17, 2, 14, 13, Teal);
            p.Rect(20, 8, 2, 4, Lime); p.Rect(23, 6, 2, 6, Green); p.Rect(26, 9, 2, 3, Blue);
            SmallWindow(p, 1, 18, 14, 13, Purple);
            p.Ellipse(4, 23, 8, 6, White); p.Rect(7, 23, 1, 3, Navy); p.Rect(7, 26, 3, 1, Navy);
            SmallWindow(p, 17, 18, 14, 13, Red);
            p.Poly(Gold, 24, 22, 25, 24, 29, 24, 27, 26, 28, 29, 24, 28, 21, 29, 22, 26, 20, 24, 23, 24);
            p.Poly(Yellow, 24, 22, 25, 25, 28, 25, 26, 26, 27, 28, 24, 27, 22, 28, 23, 26, 21, 25, 24, 25);
        }

        private static void Monitor(Pixels p, int x, int y, int width, int height)
        {
            p.Rect(x + 2, y + 2, width, height, Shadow);
            p.Bevel(x, y, width, height, Silver);
            p.Rect(x + 2, y + 2, width - 4, height - 6, Shadow);
            p.Rect(x + width - 5, y + height - 3, 2, 1, Lime);
            p.Rect(x + 3, y + height - 3, 5, 1, Grey);
            p.Rect(x + width / 2 - 3, y + height, 7, 3, Shadow);
            p.Rect(x + width / 2 - 2, y + height, 5, 2, Silver);
            p.Bevel(x + width / 2 - 7, y + height + 2, 15, 3, Silver);
        }

        private static void Keyboard(Pixels p, int x, int y, int width, int height)
        {
            p.Bevel(x, y, width, height, Silver);
            for (int keyX = x + 3; keyX < x + width - 2; keyX += 3) p.Dot(keyX, y + 1, Shadow);
            p.Rect(x + 6, y + 2, width - 12, 1, Shadow);
        }

        private static void SmallWindow(Pixels p, int x, int y, int width, int height, Color32 title)
        {
            p.Bevel(x, y, width, height, Silver); p.Rect(x + 2, y + 2, width - 4, 3, title);
            p.Rect(x + 3, y + 3, 4, 1, White); p.Dot(x + width - 4, y + 3, White);
        }

        private static Color32 C(byte r, byte g, byte b) { return new Color32(r, g, b, 255); }

        private sealed class Pixels
        {
            public readonly Color32[] Data = new Color32[32 * 32];

            public void Dot(int x, int y, Color32 color)
            {
                if (x >= 0 && x < 32 && y >= 0 && y < 32) Data[(31 - y) * 32 + x] = color;
            }
            public void Rect(int x, int y, int width, int height, Color32 color)
            {
                for (int yy = y; yy < y + height; yy++)
                    for (int xx = x; xx < x + width; xx++) Dot(xx, yy, color);
            }
            public void Line(int x0, int y0, int x1, int y1, Color32 color)
            {
                int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
                int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
                int error = dx + dy;
                while (true)
                {
                    Dot(x0, y0, color);
                    if (x0 == x1 && y0 == y1) break;
                    int twice = 2 * error;
                    if (twice >= dy) { error += dy; x0 += sx; }
                    if (twice <= dx) { error += dx; y0 += sy; }
                }
            }
            public void Bevel(int x, int y, int width, int height, Color32 fill)
            {
                Rect(x, y, width, height, Ink); Rect(x + 1, y + 1, width - 2, height - 2, fill);
                Line(x + 1, y + 1, x + width - 2, y + 1, White);
                Line(x + 1, y + 1, x + 1, y + height - 2, White);
                Line(x + 1, y + height - 2, x + width - 2, y + height - 2, Shadow);
                Line(x + width - 2, y + 1, x + width - 2, y + height - 2, Shadow);
            }
            public void Ellipse(int x, int y, int width, int height, Color32 color)
            {
                double rx = width / 2.0, ry = height / 2.0;
                for (int yy = y; yy < y + height; yy++)
                    for (int xx = x; xx < x + width; xx++)
                    {
                        double nx = (xx + 0.5 - x - rx) / rx, ny = (yy + 0.5 - y - ry) / ry;
                        if (nx * nx + ny * ny <= 1.0) Dot(xx, yy, color);
                    }
            }
            public void Poly(Color32 color, params int[] points)
            {
                if (points.Length < 6 || points.Length % 2 != 0) throw new ArgumentException("Polygon needs x/y pairs.");
                int count = points.Length / 2;
                // This tiny fixed canvas deliberately favors legibility over a scanline accelerator.
                for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                {
                    bool inside = false;
                    for (int i = 0, j = count - 1; i < count; j = i++)
                    {
                        int xi = points[i * 2], yi = points[i * 2 + 1];
                        int xj = points[j * 2], yj = points[j * 2 + 1];
                        if ((yi > y) != (yj > y) && x < (double)(xj - xi) * (y - yi) / (yj - yi) + xi)
                            inside = !inside;
                    }
                    if (inside) Dot(x, y, color);
                }
                for (int i = 0; i < count; i++)
                {
                    int next = (i + 1) % count;
                    Line(points[i * 2], points[i * 2 + 1], points[next * 2], points[next * 2 + 1], color);
                }
            }
        }
    }
}
