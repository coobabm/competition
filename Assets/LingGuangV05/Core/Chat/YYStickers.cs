using System;
using System.Collections.Generic;

namespace LingGuangV05.Core.Chat
{
    /// <summary>One 斗图 sticker: an original drawn head with a bold caption, sent as an image message.</summary>
    public sealed class YYSticker
    {
        public readonly string Id;
        public readonly string Zh;
        public readonly string En;
        /// <summary>The meme does not exist before this in-game date (null = always available).</summary>
        public readonly DateTime? From;
        /// <summary>Which drawn head the renderer uses (see YYFaceArt).</summary>
        public readonly string Head;

        internal YYSticker(string id, string zh, string en, string head, DateTime? from)
        { Id = id; Zh = zh; En = en; Head = head; From = from; }

        public string Caption(bool english) => english ? En : Zh;
        public bool AvailableOn(DateTime date) => From == null || date.Date >= From.Value.Date;
        /// <summary>Plain-text stand-in for previews and logs, e.g. "[斗图:我也是醉了]".</summary>
        public string PreviewText(bool english) => english ? "[Sticker: " + En + "]" : "[斗图:" + Zh + "]";
    }

    /// <summary>The 斗图 sticker set. Captions are 2015–2016 memes; dated ones only appear once the meme exists.</summary>
    public static class YYStickers
    {
        static readonly YYSticker[] all =
        {
            new YYSticker("zuile", "我也是醉了", "I'm drunk on this", "dizzy", null),
            new YYSticker("luanyong", "然而并没有什么卵用", "And yet it's no use at all", "deadpan", null),
            new YYSticker("xiade", "吓得我都…", "Scared me so much I…", "shock", null),
            new YYSticker("xiaochuan", "友谊的小船说翻就翻", "The friendship boat capsized", "boat", null),
            new YYSticker("honghuang", "洪荒之力", "Primordial power!", "flex", new DateTime(2016, 8, 8)),
            new YYSticker("lanshou", "蓝瘦香菇", "So sad, wanna cry", "cry", new DateTime(2016, 10, 10)),
            new YYSticker("fang", "我好方", "I'm so square (lost)", "square", null),
            new YYSticker("xinliku", "宝宝心里苦", "Baby is bitter inside", "pout", null),
            new YYSticker("666", "666", "666", "thumbs", null),
            new YYSticker("wofu", "我服", "I give up", "kneel", null),
            new YYSticker("zhenxiang", "真相只有一个", "There's only one truth", "glasses", null),
            new YYSticker("chigua", "吃瓜群众", "Just eating melon here", "melon", null),
        };

        public static IReadOnlyList<YYSticker> All => all;

        public static YYSticker Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var s in all) if (s.Id == id) return s;
            return null;
        }

        /// <summary>Stickers whose meme exists on this in-game date, in picker order.</summary>
        public static List<YYSticker> AvailableOn(DateTime date)
        {
            var list = new List<YYSticker>();
            foreach (var s in all) if (s.AvailableOn(date)) list.Add(s);
            return list;
        }
    }
}
