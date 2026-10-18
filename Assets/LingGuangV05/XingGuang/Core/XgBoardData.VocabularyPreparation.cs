using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public static partial class XgBoardData
    {
        // Mirror Make's early returns and FromCard's non-Words branches. In particular danmu and
        // translate are text desks but never consult Vocabulary when generating test cards.
        internal static VocabularyPreparation PrepareVocabulary(string dataset)
        {
            switch (dataset)
            {
                case "xor": case "parallel": case "longtext": case "crosssentence":
                case "translate": case "mnist": case "poems": case "news": case "logic":
                case "arith": case "cifar": case "imagenet": case "meme": case "go": case "danmu":
                    return null;
            }
            lock (vocabularies) if (vocabularies.ContainsKey(dataset)) return null;
            return new VocabularyPreparation(dataset);
        }

        // The owner is one pending test-set job, never a static/global work queue. Dropping that job
        // cancels all remaining work; no callback or worker can publish it after cancellation.
        internal sealed class VocabularyPreparation
        {
            readonly string dataset;
            IEnumerator<int> work;
            internal VocabularyPreparation(string dataset)
            { this.dataset = dataset; work = Build().GetEnumerator(); }

            // One Pick, one unique bigram count, or one final vocabulary candidate per unit.
            internal bool AdvanceOne()
            {
                if (work == null) return true;
                bool alreadyComplete;
                lock (vocabularies) alreadyComplete = vocabularies.ContainsKey(dataset);
                // A synchronous request may have filled the original singleton while we were partial.
                if (alreadyComplete || !work.MoveNext())
                { work.Dispose(); work = null; return true; }
                return false;
            }

            IEnumerable<int> Build()
            {
                var counts = new Dictionary<string, int>();
                var r = new Random(dataset.GetHashCode() & 0x7fffffff);
                var phrases = new HashSet<string>();
                // Keep the original RNG, number of calls, default includeFamily=true and HashSet
                // insertion/enumeration order. Do not accumulate counts in Pick occurrence order.
                for (int i = 0; i < 4000; i++)
                {
                    var p = XgMemes.Pick(dataset, r, XgMemes.MaxLevel, 20161231, "");
                    if (p != null) phrases.Add(p.text);
                    yield return 0;
                }
                foreach (var text in phrases)
                    foreach (var w in Bigrams(text))
                    {
                        counts.TryGetValue(w, out int n); counts[w] = n + 1;
                        yield return 0;
                    }
                var known = new HashSet<string>();
                foreach (var pair in counts)
                {
                    if (pair.Value >= 2) known.Add(pair.Key);
                    yield return 0;
                }
                // Only the finished equivalent vocabulary is shared. Original Vocabulary remains
                // unchanged and always builds synchronously when no complete entry exists.
                lock (vocabularies)
                    if (!vocabularies.ContainsKey(dataset)) vocabularies.Add(dataset, known);
            }
        }
    }
}
