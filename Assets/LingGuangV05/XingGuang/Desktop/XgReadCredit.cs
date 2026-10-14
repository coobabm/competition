using LingGuangV05.XingGuang;
using TMPro;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// Bridge from the resident spider's feet to the lab: a word it plants a foot on is read, and a new word counts as a
    /// sample (<see cref="XgSim.ResidentRead"/>, 1 word = 1 sample). Kept out of XgResidentSpider so the animation code
    /// only makes one call.
    /// </summary>
    public static class XgReadCredit
    {
        /// <summary>The characters <paramref name="count"/> from <paramref name="first"/> of a laid-out text, or null if they are not there.</summary>
        public static string WordOf(TMP_Text text, int first, int count)
        {
            if (text == null || count <= 0) return null;
            var info = text.textInfo;
            if (info == null || first < 0 || first + count > info.characterCount) return null;
            var chars = new char[count];
            for (int i = 0; i < count; i++) chars[i] = info.characterInfo[first + i].character;
            return new string(chars);
        }

        /// <summary>Credits the word under a foot. True when it counted as a new sample.</summary>
        public static bool Read(XgSim sim, TMP_Text text, int first, int count)
            => sim != null && sim.ResidentRead(WordOf(text, first, count));
    }
}
