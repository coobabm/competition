using LingGuangV05.Runtime;
using LingGuangV05.Desktop.Tieba;

namespace LingGuangV05.Desktop.Bodu
{
    /// <summary>
    /// 「三体」 on 摆渡: the login page of the novel's online game. The invitation code is refused twice and lets the
    /// stubborn in on the third try. Inside, a world of three suns: each era the sky gives a clue (a sun rising as
    /// usual, flying stars, a dark sky) and the player chooses to 浸泡 (live and work, for a stable era) or 脱水 (dry
    /// out and wait, for a chaotic one). Soaking in a chaotic era ends the civilisation; three stable eras lived take
    /// it to the atomic age, and then the three suns rise together. Hidden cards (下班以后): the login, the code, the
    /// first era lived, and the seed of civilisation.
    /// </summary>
    public sealed partial class BoduView
    {
        const int SantiEras = 3;
        static readonly string[] SantiAges = { "青铜时代", "蒸汽时代", "原子时代" }, SantiAgesEn = { "the Bronze Age", "the Age of Steam", "the Atomic Age" };
        int santiTries, santiCiv, santiEras, santiClue;
        string santiNote = "", santiNoteEn = "";
        bool santiOver;
        System.Random santiRng;

        void Santi()
        {
            TiebaHub.Lab()?.EarnSecret("life.santi.login");
            Line("<b>" + T("三体", "Three Body") + "</b>", 26, Ink);
            Line(T("需要 V 装备与邀请码。", "Requires a V-suit and an invitation code."), 18, Muted);
            Line("<color=#1A0DAB><u>" + T("【输入邀请码】", "[Enter invitation code]") + "</u></color>", 18, Ink, SantiCode);
            if (santiTries == 1) Line("<color=#C00000>" + T("邀请码无效。本游戏只对有缘人开放。", "Invalid invitation code. This game is only open to those it was meant for.") + "</color>", 17, Ink);
            else if (santiTries >= 2) Line("<color=#C00000>" + T("邀请码无效。……你很执着。", "Invalid invitation code. …You are persistent.") + "</color>", 17, Ink);
        }

        void SantiCode()
        {
            santiTries++;
            if (santiTries >= 3)
            {
                TiebaHub.Lab()?.EarnSecret("life.santi.code");
                santiTries = 0;
                page = "santi.game";
                if (santiCiv == 0) SantiNewCivilisation(T("……进来吧。", "…Come in."), "…Come in.");
            }
            signature = "";
        }

        void SantiNewCivilisation(string note, string noteEn)
        {
            if (santiRng == null) santiRng = new System.Random();
            santiCiv = santiCiv == 0 ? 137 + santiRng.Next(48) : santiCiv + 1;
            santiEras = 0; santiOver = false;
            santiNote = note; santiNoteEn = noteEn;
            SantiNextSky();
        }

        /// <summary>The clue of the next era: 0 a sun rising as usual (mostly stable), 1 flying stars (mostly chaotic), 2 a dark sky (either).</summary>
        void SantiNextSky() => santiClue = santiRng.Next(3);

        void SantiGame()
        {
            Line("<b>" + T("三体", "Three Body") + "</b>  <size=15><color=#787878>" + T("第 " + santiCiv + " 号文明 · " + (santiEras > 0 ? SantiAges[santiEras - 1] : "石器时代"), "Civilisation #" + santiCiv + " · " + (santiEras > 0 ? SantiAgesEn[santiEras - 1] : "the Stone Age")) + "</color></size>", 24, Ink);
            if (santiNote.Length > 0) Line(GameText.IsEnglish ? santiNoteEn : santiNote, 17, Ink);
            if (santiOver)
            {
                Line("<color=#1A0DAB><u>" + T("【重新开始】", "[Start again]") + "</u></color>", 18, Ink, () => { SantiNewCivilisation(T("文明的种子仍在，它将重新启动……", "The seed of civilisation remains; it will start again…"), "The seed of civilisation remains; it will start again…"); signature = ""; });
                Line("<color=#1A0DAB><u>" + T("【退出游戏】", "[Leave the game]") + "</u></color>", 16, Ink, () => { page = "santi"; signature = ""; });
                return;
            }
            string[] sky = { "太阳升起来了，大小和昨天一样。", "天边出现了两颗飞星。", "天色昏暗，什么也看不清。" };
            string[] skyEn = { "The sun is rising, the same size as yesterday.", "Two flying stars appear on the horizon.", "The sky is dim; nothing can be seen." };
            Line("<color=#787878>" + T("天象：", "The sky: ") + "</color>" + T(sky[santiClue], skyEn[santiClue]), 18, Ink);
            Line("<color=#1A0DAB><u>" + T("【浸泡】", "[Soak]") + "</u></color>  <size=14><color=#787878>" + T("相信是恒纪元：复活，劳作", "trust it is a stable era: revive and work") + "</color></size>", 18, Ink, () => SantiChoose(true));
            Line("<color=#1A0DAB><u>" + T("【脱水】", "[Dehydrate]") + "</u></color>  <size=14><color=#787878>" + T("相信是乱纪元：脱水，存进仓库", "trust it is a chaotic era: dry out and wait in the store") + "</color></size>", 18, Ink, () => SantiChoose(false));
            Line("<color=#1A0DAB><u>" + T("【退出游戏】", "[Leave the game]") + "</u></color>", 16, Ink, () => { page = "santi"; signature = ""; });
        }

        void SantiChoose(bool soak)
        {
            var lab = TiebaHub.Lab();
            // The clue is right four times in five; a dark sky is a coin toss.
            double stableChance = santiClue == 0 ? .8 : santiClue == 1 ? .2 : .5;
            bool stable = santiRng.NextDouble() < stableChance;
            if (stable && soak)
            {
                santiEras++;
                santiNote = "恒纪元！人们从仓库里出来，浸泡复活，文明进入了" + SantiAges[santiEras - 1] + "。";
                santiNoteEn = "A stable era! People come out of the stores, soak, revive, and the civilisation reaches " + SantiAgesEn[santiEras - 1] + ".";
                lab?.EarnSecret("life.santi.play");
                if (santiEras >= SantiEras)
                {
                    lab?.EarnSecret("life.santi.civ");
                    santiNote += "\n……然后，三颗太阳同时升起。三日凌空。第 " + santiCiv + " 号文明，在原子时代毁灭了。";
                    santiNoteEn += "\n…Then three suns rise together. Civilisation #" + santiCiv + " was destroyed in the Atomic Age.";
                    santiOver = true;
                }
            }
            else if (stable)
            {
                santiNote = "这是恒纪元，你们却躺在仓库里。一个纪元白白过去了。";
                santiNoteEn = "It was a stable era and you stayed dried out in the stores. A whole era wasted.";
            }
            else if (!soak)
            {
                santiNote = "乱纪元来了：烈日之后是严寒。好在人们都脱了水，熬过去了。";
                santiNoteEn = "A chaotic era: blazing heat, then deep frost. Luckily everyone was dried out, and it passed.";
                lab?.EarnSecret("life.santi.play");
            }
            else
            {
                santiNote = "乱纪元！烈日下，浸泡的人们化为灰烬。第 " + santiCiv + " 号文明毁灭了。";
                santiNoteEn = "A chaotic era! Under the blazing suns, everyone who soaked burned to ash. Civilisation #" + santiCiv + " was destroyed.";
                santiOver = true;
            }
            if (!santiOver) SantiNextSky();
            signature = "";
        }
    }
}
