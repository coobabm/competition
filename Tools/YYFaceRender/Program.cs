using System;
using System.IO;
using System.IO.Compression;
using LingGuangV05.Core.Chat;
using LingGuangV05.Desktop.YY;

// Offline preview of the procedural YY faces: writes a magnified contact sheet (faces at chat size, scaled up
// with nearest-neighbour so every pixel is visible) and the sticker heads. Usage: dotnet run -- <out dir> [cell] [zoom]
static class Program
{
    static void Main(string[] args)
    {
        string dir = args.Length > 0 ? args[0] : ".";
        int cell = args.Length > 1 ? int.Parse(args[1]) : 32, zoom = args.Length > 2 ? int.Parse(args[2]) : 3;
        Directory.CreateDirectory(dir);
        int cols = 9, rows = (YYFaces.Count + cols - 1) / cols, pitch = cell + 4;
        int w = cols * pitch * zoom, h = rows * pitch * zoom;
        var img = new byte[w * h * 4];
        Fill(img, 0xF4, 0xF7, 0xFB);
        for (int i = 0; i < YYFaces.Count; i++)
        {
            var face = YYFaces.At(i);
            var c = YYFaceArt.DrawFace(face, cell, 0);
            Blit(img, w, c.ToBytes(), cell, (i % cols) * pitch * zoom + 2 * zoom, (i / cols) * pitch * zoom + 2 * zoom, zoom);
        }
        File.WriteAllBytes(Path.Combine(dir, "faces_zoom.png"), Png(img, w, h));
        // the same at 1:1 on a white bubble and a blue bubble
        int w1 = cols * pitch, h1 = rows * pitch * 2;
        var one = new byte[w1 * h1 * 4];
        Fill(one, 0xFF, 0xFF, 0xFF);
        for (int y = rows * pitch; y < h1; y++) for (int x = 0; x < w1; x++) { int k = (y * w1 + x) * 4; one[k] = 18; one[k + 1] = 183; one[k + 2] = 245; one[k + 3] = 255; }
        for (int i = 0; i < YYFaces.Count; i++)
        {
            var b = YYFaceArt.DrawFace(YYFaces.At(i), cell, 0).ToBytes();
            Blit(one, w1, b, cell, (i % cols) * pitch + 2, (i / cols) * pitch + 2, 1);
            Blit(one, w1, b, cell, (i % cols) * pitch + 2, rows * pitch + (i / cols) * pitch + 2, 1);
        }
        File.WriteAllBytes(Path.Combine(dir, "faces_1x.png"), Png(one, w1, h1));
        int s = 96, sc = 6, sw = sc * (s + 8), sh = 2 * (s + 8);
        var st = new byte[sw * sh * 4];
        Fill(st, 0xE8, 0xEC, 0xF2);
        for (int i = 0; i < YYStickers.All.Count; i++)
            Blit(st, sw, YYFaceArt.DrawSticker(YYStickers.All[i], s).ToBytes(), s, (i % sc) * (s + 8) + 4, (i / sc) * (s + 8) + 4, 1);
        File.WriteAllBytes(Path.Combine(dir, "stickers.png"), Png(st, sw, sh));
        Console.WriteLine("wrote " + dir);
    }

    static void Fill(byte[] img, byte r, byte g, byte b) { for (int i = 0; i < img.Length; i += 4) { img[i] = r; img[i + 1] = g; img[i + 2] = b; img[i + 3] = 255; } }

    static void Blit(byte[] dst, int dw, byte[] src, int size, int ox, int oy, int zoom)
    {
        for (int y = 0; y < size * zoom; y++)
            for (int x = 0; x < size * zoom; x++)
            {
                int si = ((y / zoom) * size + x / zoom) * 4, di = ((oy + y) * dw + ox + x) * 4;
                float a = src[si + 3] / 255f;
                for (int k = 0; k < 3; k++) dst[di + k] = (byte)(src[si + k] * a + dst[di + k] * (1 - a));
            }
    }

    static byte[] Png(byte[] rgba, int w, int h)
    {
        var raw = new MemoryStream();
        for (int y = 0; y < h; y++) { raw.WriteByte(0); raw.Write(rgba, y * w * 4, w * 4); }
        var z = new MemoryStream();
        using (var zs = new ZLibStream(z, CompressionLevel.Optimal, true)) raw.WriteTo(zs);
        var o = new MemoryStream();
        o.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var ihdr = new byte[13];
        BE(ihdr, 0, w); BE(ihdr, 4, h); ihdr[8] = 8; ihdr[9] = 6;
        Chunk(o, "IHDR", ihdr); Chunk(o, "IDAT", z.ToArray()); Chunk(o, "IEND", new byte[0]);
        return o.ToArray();
    }

    static void BE(byte[] b, int at, int v) { b[at] = (byte)(v >> 24); b[at + 1] = (byte)(v >> 16); b[at + 2] = (byte)(v >> 8); b[at + 3] = (byte)v; }

    static void Chunk(Stream o, string type, byte[] data)
    {
        var len = new byte[4]; BE(len, 0, data.Length); o.Write(len);
        var td = new byte[4 + data.Length];
        for (int i = 0; i < 4; i++) td[i] = (byte)type[i];
        Array.Copy(data, 0, td, 4, data.Length);
        o.Write(td);
        var crc = new byte[4]; BE(crc, 0, (int)Crc(td)); o.Write(crc);
    }

    static uint Crc(byte[] d)
    {
        uint c = 0xFFFFFFFF;
        foreach (byte b in d) { c ^= b; for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1; }
        return c ^ 0xFFFFFFFF;
    }
}
