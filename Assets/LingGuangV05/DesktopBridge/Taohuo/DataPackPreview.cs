using System;
using System.Collections.Generic;
using System.Globalization;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Taohuo
{
    /// <summary>
    /// Product previews of existing, deterministic game teaching samples. These are not original MNIST,
    /// ImageNet or People's Daily records. No new sample rules, live game state, downloaded dataset or model call.
    /// </summary>
    public static class DataPackPreview
    {
        const int MaxSampleAttempts = 64;
        static readonly object SampleGate = new object();
        static Dictionary<string, XgCard> Samples;
        static readonly Color Ink = new Color32(42, 46, 52, 255);

        /// <summary>Candidate-zero seed. Visible collisions may select a later deterministic candidate.</summary>
        public static int SeedFor(string offerId)
        {
            if (string.IsNullOrEmpty(offerId)) throw new ArgumentException("An offer id is required.", nameof(offerId));
            return XgBoardData.Seed(offerId, 0, XgBoardData.Use.Train, 20261007);
        }

        /// <summary>A full serialized copy for provenance/tests; callers cannot mutate the cached sample.</summary>
        public static string SampleDescription(string offerId)
        {
            var offer = XgSim.DataOfferDef(offerId);
            if (offer == null) throw new ArgumentException("Unknown data offer: " + offerId, nameof(offerId));
            return JsonUtility.ToJson(Sample(offer));
        }

        /// <summary>
        /// Locale-independent signatures of exactly the Chinese and English preview contents. Text signatures
        /// exclude seeds, ids, answers and explanations that the preview does not display.
        /// </summary>
        public static string VisibleSampleDescription(string offerId)
        {
            var offer = XgSim.DataOfferDef(offerId);
            if (offer == null) throw new ArgumentException("Unknown data offer: " + offerId, nameof(offerId));
            var card = Sample(offer);
            string zh = VisibleSignature(card, false), en = VisibleSignature(card, true);
            return "zh:" + zh.Length.ToString(CultureInfo.InvariantCulture) + ":" + zh + "\nen:" + en;
        }

        static XgCard Sample(XgDataOffer offer)
        {
            lock (SampleGate)
            {
                if (Samples == null)
                {
                    var offers = new List<XgDataOffer>();
                    foreach (var item in XgSim.DataOfferCatalog)
                        if (item.source == XgDataSource.Public || item.source == XgDataSource.Junk) offers.Add(item);
                    offers.Sort((a, b) => StringComparer.Ordinal.Compare(a.id, b.id));
                    var pending = new Dictionary<string, XgCard>(StringComparer.Ordinal);
                    var chinese = new HashSet<string>(StringComparer.Ordinal);
                    var english = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var item in offers)
                    {
                        XgCard chosen = null;
                        for (int attempt = 0; attempt < MaxSampleAttempts; attempt++)
                        {
                            var candidate = CreateSample(item, attempt);
                            string zh = VisibleSignature(candidate, false), en = VisibleSignature(candidate, true);
                            if (chinese.Contains(zh) || english.Contains(en)) continue;
                            chinese.Add(zh); english.Add(en); chosen = candidate;
                            break;
                        }
                        if (chosen == null)
                            throw new InvalidOperationException("No distinct visible sample after " + MaxSampleAttempts + " candidates: " + item.id);
                        pending.Add(item.id, chosen);
                    }
                    Samples = pending; // Publish only the complete, order-independent selection.
                }
                if (Samples.TryGetValue(offer.id, out var cached)) return cached;
                throw new ArgumentException("No public/junk preview for data offer: " + offer.id, nameof(offer));
            }
        }

        static XgCard CreateSample(XgDataOffer offer, int attempt)
        {
            int seed = XgBoardData.Seed(offer.id, attempt, XgBoardData.Use.Train, 20261007);
            int date = Math.Max(20160601, offer.month * 100 + 1);
            XgCard card;
            // These non-lab datasets already have concept-board teaching samples in the game.
            if (XgCatalog.Desk(offer.datasetId) == null && offer.datasetId != "translate" && offer.datasetId != "crosssentence")
                card = XgBoardData.Make(offer.datasetId, seed, 1, date).source;
            else
            {
                var sim = new XgSim { GoldChance = 0, Today = date };
                sim.S.stage = sim.S.stageVision = sim.S.stageSequence = Math.Min(5, Math.Max(1, offer.stage));
                sim.S.rng = seed;
                card = sim.CreateLabelCard(offer.datasetId);
            }
            if (card == null) throw new InvalidOperationException("Existing game generator returned no sample: " + offer.id);
            return card;
        }

        static string VisibleSignature(XgCard card, bool english)
        {
            string N(int value) => value.ToString(CultureInfo.InvariantCulture);
            if (XgLabelPreviewGraphic.Valid(card.patternA))
                return "grid|" + card.patternA + "|" + (XgLabelPreviewGraphic.Valid(card.patternB) ? card.patternB : "");
            switch (card.dataset)
            {
                // Seeds are included only where the existing Graphic actually uses them to draw the sample.
                case "mnist": return "digit|" + N(card.digit) + "|" + N(card.seed) + "|" + N(card.level) + "|" + N(card.asked);
                case "cifar": case "imagenet":
                    return "captcha|" + N(card.digit) + "|" + N(card.seed) + "|" + N(card.level) + "|" + N(Math.Max(-1, card.shown));
                case "meme": return "face|" + N(card.digit) + "|" + N(card.seed) + "|" + card.line;
                case "go": return "go|" + card.line + "|" + N(card.digit) + "|" + N(card.asked);
                default: return "text|" + Body(card, english);
            }
        }

        public static bool Paint(RectTransform parent, XgDataOffer offer, TMP_FontAsset font)
        {
            if (parent == null || offer == null || font == null || XgCatalog.Dataset(offer.datasetId) == null) return false;
            var card = Sample(offer);
            var root = Area(parent, "Game Data Preview " + offer.id, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            PrologueDesk.Fill(root, new Color32(248, 246, 239, 255), false);
            var stripe = Area(root, "Pack Source", new Vector2(0, 1), Vector2.one, new Vector2(0, -3), Vector2.zero);
            PrologueDesk.Fill(stripe, offer.source == XgDataSource.Junk ? new Color32(156, 100, 53, 255) : new Color32(45, 105, 132, 255), false);
            var picture = Area(root, "Actual Sample", new Vector2(0, .17f), new Vector2(1, 1), new Vector2(6, 3), new Vector2(-6, -7));
            string caption = GameText.T("游戏样本", "Game sample");
            if (XgLabelPreviewGraphic.Valid(card.patternA))
                picture.gameObject.AddComponent<XgLabelPreviewGraphic>().Show(card.patternA, card.patternB);
            else switch (offer.datasetId)
            {
                case "mnist":
                    var digit = picture.gameObject.AddComponent<XgDigitGraphic>(); digit.color = Ink;
                    digit.Show(card.digit, card.seed, card.level);
                    caption = GameText.T("是「" + card.asked + "」吗？", "Is this " + card.asked + "?");
                    break;
                case "cifar": case "imagenet":
                    if (card.shown >= 0)
                    {
                        Area(picture, "A", Vector2.zero, new Vector2(.48f, 1), Vector2.zero, Vector2.zero)
                            .gameObject.AddComponent<XgCaptchaGraphic>().Show(card.digit, card.seed, card.level);
                        Area(picture, "B", new Vector2(.52f, 0), Vector2.one, Vector2.zero, Vector2.zero)
                            .gameObject.AddComponent<XgCaptchaGraphic>().Show(card.shown, card.seed + 7, card.level);
                    }
                    else picture.gameObject.AddComponent<XgCaptchaGraphic>().Show(card.digit, card.seed, card.level);
                    break;
                case "meme":
                    picture.gameObject.AddComponent<XgFaceGraphic>().Show(card.digit, card.seed);
                    caption = card.line;
                    break;
                case "go":
                    picture.gameObject.AddComponent<XgGoGraphic>().Show(card.line, card.digit, card.asked);
                    break;
                default:
                    Text(picture, "Sample Text", Body(card), font, 11, 8);
                    break;
            }
            Text(Area(root, "Caption", Vector2.zero, new Vector2(1, .17f), new Vector2(4, 1), new Vector2(-4, 0)), "Label", caption, font, 9, 7);
            // Existing Xg graphics normally render interactive tasks. Here they are decorative thumbnails only.
            foreach (var graphic in root.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;
            return true;
        }

        static string Body(XgCard card) => Body(card, GameText.IsEnglish);

        static string Body(XgCard card, bool english)
        {
            if (!string.IsNullOrEmpty(card.sourceText) || !string.IsNullOrEmpty(card.candidateText))
                return Visible(card.sourceText, card.sourceTextEn, english) + "\n\n" + Visible(card.candidateText, card.candidateTextEn, english);
            if (card.dataset == "poems" || card.dataset == "news")
                return card.line.Substring(0, Math.Min(card.shown, card.line.Length)) + "＿\n\n"
                    + (english ? "Next: " : "下一字：") + card.askedChar;
            return Visible(card.question, card.questionEn, english);
        }

        static string Visible(string zh, string en, bool english) => english && !string.IsNullOrEmpty(en) ? en : zh ?? "";

        static RectTransform Area(Transform parent, string name, Vector2 min, Vector2 max, Vector2 insetMin, Vector2 insetMax)
            => PrologueDesk.Rect(name, parent, min, max, insetMin, insetMax);

        static void Text(RectTransform parent, string name, string value, TMP_FontAsset font, float size, float minimum)
        {
            var label = Area(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font; label.text = value ?? ""; label.color = Ink; label.fontSize = size; label.richText = false;
            label.enableAutoSizing = true; label.fontSizeMin = minimum; label.fontSizeMax = size;
            label.alignment = TextAlignmentOptions.Center; label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis; label.raycastTarget = false;
        }
    }
}
