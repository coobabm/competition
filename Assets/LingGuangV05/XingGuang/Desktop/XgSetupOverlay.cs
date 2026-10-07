using System.Collections;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// Prologue Step 8 (design v1.1 §6): the first time the exe opens it asks for a name, how it calls itself, how it
    /// calls you and one sentence of personality. The sentence is read once (local model if running, else offline),
    /// the save starts, and the home page shows one bulb, 是 / 否 and "{name}：尚无智能".
    /// </summary>
    public sealed class XgSetupOverlay
    {
        readonly XingGuangView view;
        readonly XgUi ui;
        readonly TMP_FontAsset font;
        readonly RectTransform root, form, home;
        readonly TMP_InputField name, selfCustom, callCustom, mood;
        readonly TMP_Text title, error, bulb, homeLine;
        readonly List<TMP_Text> labels = new List<TMP_Text>();
        readonly List<(string value, XgBtn b)> selfChips = new List<(string, XgBtn)>(), callChips = new List<(string, XgBtn)>();
        readonly XgBtn done, yes, no;
        string self = PrologueProfile.SelfChoices[0], call = PrologueProfile.CallChoices[0];
        bool reading, homePending;

        static string L(string key) => PrologueContent.L(key);

        public XgSetupOverlay(XingGuangView owner, XgUi kit, TMP_FontAsset fontAsset, RectTransform parent)
        {
            view = owner; ui = kit; font = fontAsset;
            root = Rect("Setup", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Panel(root, new Color32(3, 6, 10, 238));

            form = Rect("Form", root, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-330, -260), new Vector2(330, 260));
            Panel(form, XgDark.Line);
            Panel(Rect("Inner", form, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -1)), XgDark.Card).raycastTarget = false;
            title = ui.Text(Strip("Title", form, 14, 40, 24, 24), "", 26, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            float y = 66;
            labels.Add(ui.Text(Strip("NameLabel", form, y, 24, 24, 24), "", 16, XgDark.Muted, TextAlignmentOptions.MidlineLeft));
            name = Input(Strip("Name", form, y + 26, 38, 24, 24), 16, false);
            y += 76;
            labels.Add(ui.Text(Strip("SelfLabel", form, y, 24, 24, 24), "", 16, XgDark.Muted, TextAlignmentOptions.MidlineLeft));
            selfCustom = Chips(form, y + 26, PrologueProfile.SelfChoices, selfChips, v => self = v);
            y += 76;
            labels.Add(ui.Text(Strip("CallLabel", form, y, 24, 24, 24), "", 16, XgDark.Muted, TextAlignmentOptions.MidlineLeft));
            callCustom = Chips(form, y + 26, PrologueProfile.CallChoices, callChips, v => call = v);
            y += 76;
            labels.Add(ui.Text(Strip("MoodLabel", form, y, 24, 24, 24), "", 16, XgDark.Muted, TextAlignmentOptions.MidlineLeft));
            mood = Input(Strip("Mood", form, y + 26, 38, 24, 24), 60, false);
            error = ui.Text(Rect("Error", form, Vector2.zero, new Vector2(1, 0), new Vector2(24, 18), new Vector2(-220, 62)), "", 14, XgDark.Bad, TextAlignmentOptions.MidlineLeft);
            done = ui.Button(form, "", Submit, 18);
            done.rt.anchorMin = done.rt.anchorMax = new Vector2(1, 0); done.rt.offsetMin = new Vector2(-204, 18); done.rt.offsetMax = new Vector2(-24, 62);

            home = Rect("Home", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Panel(home, XgDark.Page);
            bulb = ui.Text(Rect("Bulb", home, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-120, -20), new Vector2(120, 220)), "●", 200, new Color32(40, 60, 72, 255), TextAlignmentOptions.Center);
            yes = ui.Button(home, "", () => Answer(true), 26);
            yes.rt.anchorMin = yes.rt.anchorMax = new Vector2(.5f, .5f); yes.rt.offsetMin = new Vector2(-170, -110); yes.rt.offsetMax = new Vector2(-20, -40);
            no = ui.Button(home, "", () => Answer(false), 26);
            no.rt.anchorMin = no.rt.anchorMax = new Vector2(.5f, .5f); no.rt.offsetMin = new Vector2(20, -110); no.rt.offsetMax = new Vector2(170, -40);
            homeLine = ui.Text(Rect("Line", home, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-300, -170), new Vector2(300, -130)), "", 16, XgDark.Muted, TextAlignmentOptions.Center);
            root.gameObject.SetActive(false);
        }

        public bool Visible => root.gameObject.activeSelf;

        TMP_InputField Input(RectTransform box, int limit, bool small)
        {
            Panel(box, XgDark.Button);
            var area = Rect("Text Area", box, Vector2.zero, Vector2.one, new Vector2(10, 4), new Vector2(-10, -4));
            area.gameObject.AddComponent<RectMask2D>();
            var placeholder = ui.Text(Rect("Placeholder", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", small ? 14 : 17, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            var text = ui.Text(Rect("Text", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", small ? 14 : 17, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            text.richText = false; text.textWrappingMode = TextWrappingModes.NoWrap;
            var field = box.gameObject.AddComponent<TMP_InputField>();
            field.textViewport = area; field.textComponent = text; field.placeholder = placeholder;
            field.fontAsset = font; field.pointSize = small ? 14 : 17; field.characterLimit = limit; field.richText = false;
            field.lineType = TMP_InputField.LineType.SingleLine;
            return field;
        }

        TMP_InputField Chips(RectTransform parent, float y, string[] values, List<(string, XgBtn)> into, System.Action<string> pick)
        {
            var row = Strip("Chips", parent, y, 38, 24, 24);
            float x = 0;
            TMP_InputField custom = null;
            foreach (var value in values)
            {
                string v = value;
                var b = ui.Button(row, v, null, 16);
                b.button.onClick.AddListener(() => { pick(v); if (custom != null) custom.text = ""; Paint(); });
                b.rt.anchorMin = new Vector2(0, 0); b.rt.anchorMax = new Vector2(0, 1); b.rt.offsetMin = new Vector2(x, 0); b.rt.offsetMax = new Vector2(x + 70, 0);
                into.Add((v, b));
                x += 78;
            }
            var box = Rect("Custom", row, Vector2.zero, new Vector2(1, 1), new Vector2(x, 0), Vector2.zero);
            custom = Input(box, 8, true);
            custom.onValueChanged.AddListener(t => { if (t.Trim().Length > 0) pick(t.Trim()); else if (into.Count > 0) pick(into[0].Item1); Paint(); });
            return custom;
        }

        void Paint()
        {
            foreach (var (v, b) in selfChips) b.Set(Shown(v, true), true, v == self ? XgDark.Accent : (Color?)null, v == self ? Color.white : (Color?)null);
            foreach (var (v, b) in callChips) b.Set(Shown(v, false), true, v == call ? XgDark.Accent : (Color?)null, v == call ? Color.white : (Color?)null);
        }

        static string Shown(string value, bool selfRow)
        {
            if (!GameText.IsEnglish) return value;
            var zh = selfRow ? PrologueProfile.SelfChoices : PrologueProfile.CallChoices;
            var en = selfRow ? PrologueProfile.SelfChoicesEn : PrologueProfile.CallChoicesEn;
            int i = System.Array.IndexOf(zh, value);
            return i >= 0 ? en[i] : value;
        }

        /// <summary>Shown while the prologue waits for its setup, and once afterwards for the home page.</summary>
        public void Refresh(ChapterOneRuntime runtime)
        {
            bool prologue = runtime != null && runtime.Sim != null && runtime.Sim.InPrologue && !runtime.TestMode;
            bool on = prologue || homePending;
            if (root.gameObject.activeSelf != on) root.gameObject.SetActive(on);
            if (!on) return;
            root.SetAsLastSibling();
            form.gameObject.SetActive(!homePending);
            home.gameObject.SetActive(homePending);
            title.text = L("setup_title");
            string[] keys = { "setup_name", "setup_self", "setup_call", "setup_mood" };
            for (int i = 0; i < labels.Count; i++) labels[i].text = L(keys[i]);
            ((TMP_Text)mood.placeholder).text = L("setup_mood_hint");
            ((TMP_Text)selfCustom.placeholder).text = L("setup_custom");
            ((TMP_Text)callCustom.placeholder).text = L("setup_custom");
            done.Set(reading ? L("setup_reading") : L("setup_done"), !reading, XgDark.Accent, Color.white);
            yes.Set(L("yes"), true, XgDark.Card, XgDark.Ink);
            no.Set(L("no"), true, XgDark.Card, XgDark.Ink);
            if (homePending) homeLine.text = runtime.Sim.S.aiName + T("：", ": ") + L("no_intelligence");
            Paint();
        }

        void Submit()
        {
            if (reading) return;
            var runtime = view.Controller.runtime;
            var draft = new PrologueProfile { name = name.text, self = self, callMe = call, personality = mood.text };
            string problem = PrologueProfile.Problem(draft, GameText.IsEnglish);
            if (problem != null) { error.text = problem; return; }
            error.text = "";
            view.StartCoroutine(ReadAndSave(runtime, draft));
        }

        /// <summary>The sentence is read once: by the local model if it answers within a few seconds, otherwise offline.</summary>
        IEnumerator ReadAndSave(ChapterOneRuntime runtime, PrologueProfile draft)
        {
            reading = true; Refresh(runtime);
            PrologueProfile read = null;
            var llm = LingGuangV05.Desktop.LLM.LocalLlm.Instance;
            if (llm != null && llm.Ready)
            {
                bool answered = false;
                var messages = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("system", PrologueProfile.ModelPrompt(GameText.IsEnglish)),
                    new KeyValuePair<string, string>("user", draft.personality.Trim()),
                };
                llm.Chat(messages, 80, .2f, reply => { read = PrologueProfile.ParseModel(reply, draft.personality.Trim()); answered = true; });
                for (float t = 0; t < 8 && !answered; t += Time.unscaledDeltaTime) yield return null;
            }
            if (read == null) read = PrologueProfile.Read(draft.personality.Trim());
            read.name = draft.name; read.self = draft.self; read.callMe = draft.callMe; read.personality = draft.personality;
            reading = false;
            if (runtime.CompletePrologue(read)) homePending = true;
            else error.text = runtime.SaveStatus;
            Refresh(runtime);
        }

        /// <summary>The first 是 / 否: the bulb lights for a moment, then 摆渡众包 opens on the labelling desk.</summary>
        void Answer(bool answer)
        {
            view.StartCoroutine(Light(answer));
        }

        IEnumerator Light(bool answer)
        {
            bulb.color = XgDark.Gold;
            view.Juice.Play(answer ? XgJuice.Sfx.Id.Ding : XgJuice.Sfx.Id.Click);
            for (float t = 0; t < .8f; t += Time.unscaledDeltaTime) yield return null;
            homePending = false;
            Refresh(view.Controller.runtime);
            // 灵光 lands on 概览 behind 摆渡众包, so closing the crowd window shows the overview first.
            view.ShowTab("home");
            view.Controller.OpenCrowd("label");
        }
    }
}
