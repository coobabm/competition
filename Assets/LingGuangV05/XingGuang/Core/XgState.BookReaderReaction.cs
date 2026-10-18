using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    [Serializable]
    public sealed class XgBookReaderReaction
    {
        public string key = "", bookId = "", status = "", reply = "";
        public int chapter;
        public bool english;
    }
    public sealed partial class XgState
    {
        // A receipt is added only when the existing local service accepts a request.
        // requested / returned / failed / cancelled / interrupted are distinct; none changes reading progress.
        public List<XgBookReaderReaction> bookReaderReactions = new List<XgBookReaderReaction>();
    }
}
