using System;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        /// <summary>Handwriting desk: drawings handed in, how many it read right, and its last 20 results ('1' read right, '0' not), oldest first.</summary>
        public int handwritingDrawn, handwritingRight;
        public string handwritingRecent = "";
    }

    /// <summary>What happened to one drawing handed in on the 手写数字 desk (<see cref="XgSim.SubmitHandwriting"/>).</summary>
    public struct XgHandwritingResult
    {
        /// <summary>It was counted: a sample was added and the pay settled.</summary>
        public bool accepted;
        /// <summary>Not counted: empty, a dot or a tick, or the desk is not open.</summary>
        public bool rejected;
        public int guess;
        public double confidence;
        /// <summary>Its reading, taken before it learned this drawing, matched the digit that was asked.</summary>
        public bool hit;
        public double seconds, pay, bonus;
        public int samples;
    }

    /// <summary>
    /// 手写数字 as a drawing task: the player writes the asked digit, the recogniser reads it, and the drawing is handed in as
    /// one hand-labelled sample of the digits dataset (labelled with the asked digit). It is paid like any hand label
    /// (never fined, by the rule in XgSim.MiniPay.cs) with a speed bonus for being read right within <see cref="HandwritingFastSeconds"/>, and the recogniser
    /// remembers it, so it reads this player's hand better the more drawings come in.
    /// </summary>
    public sealed partial class XgSim
    {
        public const string HandwritingDesk = "mnist";
        public const double HandwritingFastSeconds = MiniFastSeconds;
        public const int HandwritingWindow = 20;

        XgHandwritingRecognizer handwriting;
        /// <summary>How many saved drawings the live recogniser was built from (a changed save list rebuilds it).</summary>
        int handwritingBuiltFrom = -1;

        /// <summary>The recogniser: built-in templates plus every drawing in the save (shared with the mini-game hall).</summary>
        public XgHandwritingRecognizer Handwriting
        {
            get
            {
                XgMiniActionMemory.Repair(S, XgMiniActionMemory.Capacity * 3);
                if (handwriting == null || handwritingBuiltFrom != S.miniActionDrawings.Count)
                {
                    handwriting = new XgHandwritingRecognizer(); handwritingBuiltFrom = S.miniActionDrawings.Count;
                    // Only the newest drawings the memory holds are learned; older ones stay in the save for when the memory grows again.
                    for (int i = Math.Max(0, S.miniActionDrawings.Count - HandwritingCapacity); i < S.miniActionDrawings.Count; i++)
                    { var d = S.miniActionDrawings[i]; handwriting.Teach(d.digit, XgHandwriting.Extract(XgMiniActionMemory.Decode(d.raster)), HandwritingCapacity); }
                }
                return handwriting;
            }
        }

        /// <summary>Live reading of a drawing in progress: nothing is learned, paid or counted. Not valid for an unusable raster.</summary>
        public XgHandwritingGuess ReadHandwriting(bool[] raster)
        {
            var feature = XgHandwriting.Extract(raster);
            return feature == null ? default : Handwriting.Recognize(feature);
        }

        /// <summary>Keeps one labelled drawing in the save (newest 100) and teaches the live recogniser the same.</summary>
        public void LearnHandwriting(int digit, bool[] raster)
        {
            var model = Handwriting;
            int before = S.miniActionDrawings.Count;
            XgMiniActionMemory.Remember(S, digit, raster, HandwritingCapacity);
            if (S.miniActionDrawings.Count == before && before < HandwritingCapacity) return; // refused (bad digit or raster)
            model.Teach(digit, XgHandwriting.Extract(raster), HandwritingCapacity);
            handwritingBuiltFrom = S.miniActionDrawings.Count;
        }

        /// <summary>Drawings it remembers from this player.</summary>
        public int HandwritingMemory { get { var model = Handwriting; return model.LearnedCount; } }

        /// <summary>Share of the last (up to) 20 drawings it read right; <paramref name="count"/> is how many results that covers.</summary>
        public double HandwritingAccuracy(out int count)
        {
            string r = S.handwritingRecent ?? "";
            count = 0; int right = 0;
            foreach (char c in r) { count++; if (c == '1') right++; }
            return count == 0 ? 0 : right / (double)count;
        }

        /// <summary>The last 20 results, oldest first, as true (read right) / false.</summary>
        public bool[] HandwritingRecent()
        {
            string r = S.handwritingRecent ?? "";
            var list = new bool[r.Length];
            for (int i = 0; i < r.Length; i++) list[i] = r[i] == '1';
            return list;
        }

        /// <summary>
        /// Hands in a drawing of <paramref name="asked"/> (0–9). <paramref name="seconds"/> is how long it took before the
        /// recogniser first read it right (the page times it); the bonus only applies to a right reading within five seconds.
        /// </summary>
        public XgHandwritingResult SubmitHandwriting(int asked, bool[] raster, double seconds, IXgHost host)
        {
            var result = new XgHandwritingResult { guess = -1 };
            if (host == null || asked < 0 || asked > 9 || !DeskOpen(HandwritingDesk) || !XgHandwriting.Usable(raster)) { result.rejected = true; return result; }
            var feature = XgHandwriting.Extract(raster);
            if (feature == null) { result.rejected = true; return result; }
            var model = Handwriting;
            var guess = model.Recognize(feature);
            result.guess = guess.digit; result.confidence = guess.confidence;
            result.hit = guess.digit == asked;
            result.seconds = double.IsNaN(seconds) || seconds < 0 ? 0 : seconds;

            // Settled by the shared desk-game rule (XgSim.MiniPay.cs): the desk's hand pay, a speed bonus only for a right
            // reading, no fine (a drawing is never wrong), the shared combo untouched.
            var action = SettleMiniAction(HandwritingDesk, true, host, result.hit ? result.seconds : -1, false);
            result.pay = action.pay; result.bonus = action.bonus;
            result.samples = action.samples;

            // It learns the drawing under the asked label, in the save and in the live recogniser.
            LearnHandwriting(asked, raster);
            S.handwritingDrawn++; if (result.hit) S.handwritingRight++;
            string recent = (S.handwritingRecent ?? "") + (result.hit ? "1" : "0");
            S.handwritingRecent = recent.Length > HandwritingWindow ? recent.Substring(recent.Length - HandwritingWindow) : recent;
            result.accepted = true;
            return result;
        }
    }
}
