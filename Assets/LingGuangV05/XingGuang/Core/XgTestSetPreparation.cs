using System;
using System.Collections.Generic;
using System.Text;

namespace LingGuangV05.XingGuang
{
    // Main-thread incremental equivalent of XgBoardData.TestSet. Private work is never a live cache entry.
    public sealed class XgTestSetPreparation
    {
        public readonly string Dataset;
        public readonly int Level, Today, Salt;
        public readonly XgBoardData.Use Purpose;
        HashSet<string> signatures;
        List<string> signatureParts = new List<string>();
        StringBuilder signatureText = new StringBuilder();
        Func<string, bool> containsSignature;
        List<XgBoardCard> accepted = new List<XgBoardCard>(XgBoardData.TestSize);
        List<XgBoardCard> spare = new List<XgBoardCard>();
        XgBoardData.VocabularyPreparation vocabulary;
        bool vocabularyChecked;
        int trainIndex, candidateIndex, spareIndex;
        public bool ReusedTrainingSignatures { get; }
        public int TrainingGenerated => ReusedTrainingSignatures ? 0 : trainIndex;
        public int CandidatesGenerated => candidateIndex;
        public bool Complete { get; private set; }
        public List<XgBoardCard> Result => Complete ? accepted : null;
        public XgTestSetPreparation(string dataset, int level, int today, int salt, XgBoardData.Use purpose = XgBoardData.Use.Diagnostic)
        {
            Dataset = dataset; Level = level; Today = today; Salt = salt; Purpose = purpose;
            ReusedTrainingSignatures = XgBoardData.TryGetTrainSignatures(dataset, level, today, salt, out containsSignature);
            if (ReusedTrainingSignatures) { trainIndex = XgBoardData.PoolLimit; signatures = null; }
            else { signatures = new HashSet<string>(); containsSignature = signatures.Contains; }
        }
        public bool Matches(string dataset, int level, int today, int salt, XgBoardData.Use purpose = XgBoardData.Use.Diagnostic)
            => Dataset == dataset && Level == level && Today == today && Salt == salt && Purpose == purpose;
        // A unit is one vocabulary step, training card, candidate, or fallback copy. No lock spans a frame.
        public void Advance(int quota)
        {
            while (!Complete && quota > 0)
            {
                if (!vocabularyChecked)
                { vocabulary = XgBoardData.PrepareVocabulary(Dataset); vocabularyChecked = true; }
                if (vocabulary != null)
                {
                    if (vocabulary.AdvanceOne()) vocabulary = null;
                    quota--; continue;
                }
                if (trainIndex < XgBoardData.PoolLimit)
                {
                    signatures.Add(BuildSignature(XgBoardData.Make(Dataset, XgBoardData.Seed(Dataset, trainIndex, XgBoardData.Use.Train, Salt), Level, Today)));
                    trainIndex++; quota--; continue;
                }
                if (accepted.Count < XgBoardData.TestSize && candidateIndex < XgBoardData.TestSize * 4)
                {
                    var card = XgBoardData.Make(Dataset, XgBoardData.Seed(Dataset, candidateIndex, Purpose, Salt), Level, Today);
                    if (containsSignature(BuildSignature(card))) { if (spare.Count < XgBoardData.TestSize) spare.Add(card); }
                    else accepted.Add(card);
                    candidateIndex++; quota--; continue;
                }
                if (accepted.Count < XgBoardData.TestSize && spareIndex < spare.Count)
                { accepted.Add(spare[spareIndex++]); quota--; continue; }
                Complete = true;
                signatures = null; containsSignature = null; spare = null; // Release construction buffers/lookups; retain only the complete result.
                signatureParts = null; signatureText = null;
            }
        }

        // Private scratch space belongs to this preparation only. Preserve the original culture-sensitive
        // integer text and ordinal sort (sorting features themselves would change duplicate/order semantics).
        // Keep XgBoardData.Signature unchanged as the synchronous reference and existing-cache contract.
        string BuildSignature(XgBoardCard card)
        {
            signatureParts.Clear();
            foreach (var feature in card.features)
            {
                signatureText.Clear();
                signatureText.Append(feature.name).Append('@').Append(feature.x).Append(',')
                    .Append(feature.y).Append(',').Append(feature.seq);
                signatureParts.Add(signatureText.ToString());
            }
            signatureParts.Sort(string.CompareOrdinal);
            signatureText.Clear();
            signatureText.Append(card.region).Append('|').Append(card.truth).Append('|');
            for (int i = 0; i < signatureParts.Count; i++)
            {
                if (i > 0) signatureText.Append(';');
                signatureText.Append(signatureParts[i]);
            }
            return signatureText.ToString();
        }
    }
}
