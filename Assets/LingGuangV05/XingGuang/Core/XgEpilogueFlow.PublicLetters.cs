using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    [Serializable]
    public sealed class XgPublicLetterArchive
    {
        public string articleId = "", sourceArticleId = "", deliverySource = "public_saved";
        public int version, savedAt;
        public XgPublicLetterArchive Copy() => (XgPublicLetterArchive)MemberwiseClone();
    }

    public sealed partial class EpilogueState
    {
        // Optional local bookmarks, separate from the single completion-bearing letter receipt.
        // Missing/null in an older save means no bookmarks; never infer a private delivery or read receipt.
        public List<XgPublicLetterArchive> publicArticleArchives = new List<XgPublicLetterArchive>();
        void CopyPublicArticleArchives(EpilogueState source)
        {
            publicArticleArchives = new List<XgPublicLetterArchive>();
            if (source.publicArticleArchives != null)
                foreach (var item in source.publicArticleArchives) if (item != null) publicArticleArchives.Add(item.Copy());
        }
        public bool HasPublicArticle(string id, int version)
        {
            if (publicArticleArchives != null)
                foreach (var item in publicArticleArchives)
                    if (item != null && item.articleId == id && item.sourceArticleId == id && item.version == version
                        && item.deliverySource == "public_saved" && item.savedAt == XgEpilogueFlow.DisplayDate) return true;
            return false;
        }
    }

    public sealed partial class XgEpilogueFlow
    {
        public static bool CanBrowsePublicLetters(XgState s) => CanEnterEpilogue(s) && s.epilogue != null
            && s.epilogue.schemaVersion == 1 && (s.epilogue.phase == "letter" || s.epilogue.phase == "reading"
                || s.epilogue.phase == "completed");

        /// <summary>Explicitly bookmark any authored public article. Never opens or completes the ending letter.</summary>
        public XgEpilogueResult SavePublicArticle(XgEpilogueBinding token, string articleId, int version, string sourceArticleId)
        {
            if (!IsCurrent(token)) return Fail("stale_binding");
            if (!CanBrowsePublicLetters(state)) return Fail("public_letters_unavailable");
            if (string.IsNullOrEmpty(EndingLetterCatalog.Route(articleId)) || sourceArticleId != articleId || version < 1)
                return Fail("invalid_public_article");
            var ep = state.epilogue.Copy();
            foreach (var item in ep.publicArticleArchives)
                if (item.articleId == articleId)
                    return ep.HasPublicArticle(articleId, version) ? Unchanged() : Fail("article_version_locked");
            ep.publicArticleArchives.Add(new XgPublicLetterArchive
            {
                articleId = articleId, sourceArticleId = sourceArticleId, version = version,
                deliverySource = "public_saved", savedAt = DisplayDate
            });
            // The existing transaction handles rollback and revision. In particular, keep phase/letterId,
            // consent/opened/reading position and completion receipt exactly as they were.
            return Save(token, state.releaseOutcome.Copy(), ep, state.chapterComplete);
        }
    }
}
