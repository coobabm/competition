using System;
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.XingGuang.MiniGames;
using LingGuangV05.XingGuang;
using TMPro;

// Call from the existing visible teaching-page fixture, on the common-sense desk.
public static class TeachingLanguageChecks
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static object Field(object value, string name) => value.GetType().GetField(name, Flags).GetValue(value);
    public static void Run(XgDeskGamePage page, Action<bool> setEnglish)
    {
        if (page.SelectedGame != "kaixincidian") throw new InvalidOperationException("Open the real common-sense desk first");
        bool beforeLanguage = XgUi.En;
        var game = Field(page, "game"); var statement = Field(game, "statement");
        var sim = (XgSim)Field(page, "owner");
        var ticket = Field(page, "ticket"); var sequence = Field(page, "sequence");
        var time = Field(game, "left"); var locked = Field(game, "locked");
        int answers = sim.TeachingAnswers, combo = sim.S.combo;
        try
        {
            foreach (bool english in new[] { true, false, true })
            {
                setEnglish(english); page.Refresh();
                if (!Equals(statement, Field(game, "statement")) || !Equals(ticket, Field(page, "ticket")) ||
                    !Equals(sequence, Field(page, "sequence")) || !Equals(time, Field(game, "left")) ||
                    !Equals(locked, Field(game, "locked")) || answers != sim.TeachingAnswers || combo != sim.S.combo)
                    throw new InvalidOperationException("Language switch changed question, countdown, lock, ticket, answer count or streak");
                var yes = (XgBtn)Field(game, "yes"); var no = (XgBtn)Field(game, "no");
                if (yes.label.text != (english ? "True" : "对") || no.label.text != (english ? "False" : "错"))
                    throw new InvalidOperationException("Answer labels did not rebind");
                var phrase = (XgPhrase)statement;
                string expected = english && !string.IsNullOrEmpty(phrase.textEn) ? phrase.textEn : phrase.text;
                if (((TMP_Text)Field(game, "questionText")).text != expected)
                    throw new InvalidOperationException("Current question text did not rebind");
                if ((bool)Field(page, "showingIntroStatus") && ((TMP_Text)Field(page, "status")).text != (english
                    ? "Every answer is one sample, and the pay goes straight to your balance."
                    : "答一题，就是一条样本，钱直接进账。"))
                    throw new InvalidOperationException("Teaching footer did not rebind");
            }
            UnityEngine.Debug.Log("TEACHING_LANGUAGE_CHECKS_PASS: same question, countdown, lock, ticket, answer count and streak");
        }
        finally { setEnglish(beforeLanguage); page.Refresh(); }
    }
}
