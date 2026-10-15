using LingGuangV05.Core.Chat;
using UnityEngine;

namespace LingGuangV05.Desktop.LLM
{
    /// <summary>
    /// Loads the knowledge files (Resources/LingGuang/Rag: qingwen.json = 晴雯's own life, era2016.json = what people
    /// knew in 2016) into <see cref="Rag"/>. Edit the JSON to change what the characters know; see Core/Chat/Rag.cs.
    /// </summary>
    public static class RagLibrary
    {
        public const string QingwenPath = "LingGuang/Rag/qingwen", EraPath = "LingGuang/Rag/era2016";

        public static void Load()
        {
            Rag.Qingwen = Read(QingwenPath);
            Rag.Era = Read(EraPath);
        }

        static RagBook Read(string path)
        {
            var asset = Resources.Load<TextAsset>(path);
            var book = RagBook.Parse(asset != null ? asset.text : null);
            if (asset == null) Debug.LogWarning("[灵光] 知识库缺失：Resources/" + path + ".json");
            else if (book.entries.Count == 0) Debug.LogWarning("[灵光] 知识库是空的或格式不对：Resources/" + path + ".json");
            return book;
        }

        /// <summary>Embeds every entry in the background (LocalEmbedder calls it once its model runs).</summary>
        public static void Warm(LocalEmbedder embedder)
        {
            if (embedder == null) return;
            var texts = new System.Collections.Generic.List<string>();
            foreach (var book in new[] { Rag.Qingwen, Rag.Era })
                foreach (var e in book.entries) { texts.Add(e.zh); if (!string.IsNullOrWhiteSpace(e.en)) texts.Add(e.en); }
            embedder.Warm(texts);
        }
    }
}
