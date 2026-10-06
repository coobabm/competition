using System;
using UnityEngine;

namespace LingGuangV05.Desktop.Games
{
    /// <summary>Native sprite views of the original RGBA artwork; no package or pixel conversion required.</summary>
    public sealed class ChessSprites : IDisposable
    {
        readonly Sprite[] sprites = new Sprite[12];
        // Measured alpha bounds of the authored 1024x1536 sheet, with four pixels of transparent gutter.
        static readonly Rect[] Bounds = {
            new Rect(105,1171,171,228), new Rect(422,1171,181,246), new Rect(736,1169,191,259),
            new Rect(101,783,173,275), new Rect(414,783,196,276), new Rect(751,783,183,293),
            new Rect(103,433,176,233), new Rect(419,432,187,238), new Rect(736,429,197,263),
            new Rect(104,60,176,271), new Rect(411,60,203,269), new Rect(746,59,189,280)
        };
        public bool Ready => sprites[0] != null && sprites[11] != null;

        public ChessSprites()
        {
            var texture = Resources.Load<Texture2D>("LingGuangV05/Chess/ChessPieces");
            if (texture == null) { Debug.LogError("The original chess piece artwork is missing."); return; }
            for (int i = 0; i < sprites.Length; i++)
            {
                Rect rect = Bounds[i];
                rect.x *= texture.width / 1024f; rect.width *= texture.width / 1024f;
                rect.y *= texture.height / 1536f; rect.height *= texture.height / 1536f;
                sprites[i] = Sprite.Create(texture, rect, new Vector2(.5f,.5f), 100, 0, SpriteMeshType.FullRect);
                sprites[i].name = "Chess " + "PRNBQKprnbqk"[i];
            }
        }
        public Sprite Get(char piece) { int index = Index(piece); return index < 0 ? null : sprites[index]; }
        public static int Index(char piece)
        {
            int index = "prnbqk".IndexOf(char.ToLowerInvariant(piece));
            return index < 0 ? -1 : index + (char.IsUpper(piece) ? 0 : 6);
        }
        public void Dispose()
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null) continue;
                if (Application.isPlaying) UnityEngine.Object.Destroy(sprites[i]); else UnityEngine.Object.DestroyImmediate(sprites[i]);
                sprites[i] = null;
            }
        }
    }
}
