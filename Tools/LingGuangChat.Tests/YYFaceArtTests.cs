using System.Collections.Generic;
using LingGuangV05.Core.Chat;
using LingGuangV05.Desktop.YY;
using NUnit.Framework;

namespace LingGuangV05.Tests
{
    /// <summary>The procedural face art is engine-free, so it can be checked here: every face draws and no two look the same.</summary>
    public sealed class YYFaceArtTests
    {
        [Test]
        public void EveryFaceDrawsADistinctPicture()
        {
            var seen = new Dictionary<string, string>();
            foreach (var face in YYFaces.All)
            {
                var canvas = YYFaceArt.DrawFace(face, YYFaceArt.Cell);
                float coverage = canvas.Coverage();
                Assert.Greater(coverage, .2f, face.Id + " is nearly empty");
                Assert.Less(coverage, .95f, face.Id + " fills the whole cell");
                string key = System.Convert.ToBase64String(canvas.ToBytes());
                Assert.IsFalse(seen.ContainsKey(key), face.Id + " looks exactly like " + (seen.ContainsKey(key) ? seen[key] : ""));
                seen[key] = face.Id;
                if (YYFaceArt.Frames(face) > 1)
                    Assert.AreNotEqual(key, System.Convert.ToBase64String(YYFaceArt.DrawFace(face, YYFaceArt.Cell, 1).ToBytes()), face.Id + " second frame");
            }
        }

        [Test]
        public void EveryStickerDrawsAHead()
        {
            foreach (var sticker in YYStickers.All)
                Assert.Greater(YYFaceArt.DrawSticker(sticker, 48).Coverage(), .9f, sticker.Id);
        }
    }
}
