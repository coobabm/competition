using System.Collections;
using LingGuangV05.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// The prologue's pace and its skips (玩家反馈 F1, 技能树与自动化 v4 §7.1). Every scripted wait, cursor move, click and
    /// typing runs at <see cref="OpeningPace"/> of its written length; the story and its lines are unchanged, and spoken
    /// thoughts still type along their voice. A click (anywhere, released within <see cref="ClickSeconds"/>) hurries the
    /// current segment: its text appears at once and its waits shrink to <see cref="HurryPace"/>. Holding the mouse button
    /// or Space for <see cref="HoldToSkip"/> fills a ring in the corner and skips the segment: its remaining steps run to
    /// their end state in a few frames behind a dark veil — no click or open is dispatched, a question it was waiting on
    /// takes the answer the story goes on with (the 360 box is ignored, 0.txt is opened, 否 to the shutdown). Esc asks
    /// once whether to skip the whole opening, first playthrough included; it then runs to the 灵光.exe setup, which is
    /// never skipped (the name and personality are the player's). Nothing is saved before the setup, so a skip can never
    /// leave half a prologue in a save.
    /// </summary>
    public sealed partial class PrologueDirector
    {
        /// <summary>Scripted animation and waits run at this share of their written length (QA sets 1 to time the written pace).</summary>
        public static float OpeningPace = .6f;
        /// <summary>A click shortens the rest of the segment's waits to this share again.</summary>
        public const float HurryPace = .4f;
        public const float HoldToSkip = .8f, ClickSeconds = .3f;
        const float SkipSpeed = 400;
        /// <summary>Steps that start a segment: a skip or a hurry ends at the next of these.</summary>
        static readonly string[] Segments = { "title", "virus", "txt", "hijack", "grab", "shutdown", "extract", "setup", "done" };

        bool pacing, hurry, skipping, skipAll, confirmingSkip;
        float pressedAt = -1, segmentStartedAt, runStartedAt;
        bool pressBySpace;
        RectTransform skipHint, skipRing, skipVeil, skipConfirm;
        Image ringFill;
        CanvasGroup veilGroup;
        TMP_Text confirmYes, confirmNo;

        /// <summary>The prologue is in its scripted part (before the setup), where its pace and skips apply.</summary>
        public bool Pacing => pacing;
        /// <summary>The current segment is being skipped (or the whole opening is).</summary>
        public bool Skipping => skipping || skipAll;
        public bool Hurried => hurry;

        float Speed => !pacing ? 1 : Skipping ? SkipSpeed : hurry ? 1 / (OpeningPace * HurryPace) : 1 / OpeningPace;
        /// <summary>The scripted clock: unscaled time at the prologue's pace.</summary>
        float Dt => Time.unscaledDeltaTime * Speed;

        IEnumerator W(float seconds) { for (float t = 0; t < seconds; t += Dt) yield return null; }

        IEnumerator Mv(Vector2 to, float seconds)
        {
            var cursor = desk.Cursor;
            if (cursor == null) yield break;
            Vector2 from = cursor.anchoredPosition;
            Vector2 bend = new Vector2(-(to - from).y, (to - from).x) * .08f;
            for (float t = 0; t < seconds && !Skipping; t += Dt)
            {
                float k = Mathf.SmoothStep(0, 1, t / seconds);
                cursor.anchoredPosition = Vector2.Lerp(from, to, k) + bend * Mathf.Sin(k * Mathf.PI);
                yield return null;
            }
            cursor.anchoredPosition = to;
        }

        IEnumerator Ck(int times = 1)
        {
            for (int i = 0; i < times; i++)
            {
                if (desk.Cursor != null) desk.Cursor.localScale = Vector3.one * .85f;
                yield return W(.07f);
                if (desk.Cursor != null) desk.Cursor.localScale = Vector3.one;
                yield return W(.09f);
            }
        }

        /// <summary>Typing at the prologue's pace; a hurried or skipped segment shows the whole text at once.</summary>
        IEnumerator Ty(TMP_Text label, string start, string text, float perChar)
        {
            for (int i = 0; i <= text.Length; i++)
            {
                if (label == null) yield break;
                if (hurry || Skipping) { label.text = start + text; yield break; }
                label.text = start + text.Substring(0, i);
                yield return W(perChar);
            }
        }

        void BeginPacing()
        {
            pacing = true; hurry = skipping = skipAll = confirmingSkip = false; pressedAt = -1;
            runStartedAt = segmentStartedAt = Time.unscaledTime;
            BuildSkipUi();
        }

        void EndPacing()
        {
            pacing = hurry = skipping = skipAll = false; pressedAt = -1;
            CloseSkipConfirm();
            foreach (var rt in new[] { skipHint, skipRing, skipVeil, skipConfirm }) if (rt != null) PrologueDesk.RemoveTransient(rt.gameObject);
            skipHint = skipRing = skipVeil = skipConfirm = null; ringFill = null; veilGroup = null;
        }

        /// <summary>Called by <see cref="SetStep"/>: a new segment starts at the written pace, and a one-segment skip ends.</summary>
        void PaceStep(string step)
        {
            if (System.Array.IndexOf(Segments, step) < 0) return;
            Debug.Log("[PrologueTiming] " + step + " at " + (Time.unscaledTime - runStartedAt).ToString("0.00") + " s");
            segmentStartedAt = Time.unscaledTime;
            hurry = false;
            skipping = false;
            if (step == "setup" || step == "done") { EndPacing(); return; }
            if (!skipAll) HideVeil();
        }

        /// <summary>Every frame while the scripted prologue runs: Esc, a click, a hold.</summary>
        void TickSkipInput()
        {
            if (!pacing || run == null) return;
            if (veilGroup != null && !Skipping && veilGroup.alpha > 0)
            {
                veilGroup.alpha = Mathf.MoveTowards(veilGroup.alpha, 0, Time.unscaledDeltaTime / .25f);
                if (veilGroup.alpha <= 0) skipVeil.gameObject.SetActive(false);
            }
            if (skipAll) return;
            var keyboard = Keyboard.current; var mouse = Mouse.current;
            if (confirmingSkip) { TickSkipConfirm(keyboard, mouse); return; }
            if (EscapeKey.TryConsume()) { OpenSkipConfirm(); return; }
            bool mouseDown = mouse != null && mouse.leftButton.isPressed, spaceDown = keyboard != null && keyboard.spaceKey.isPressed;
            if (pressedAt < 0)
            {
                if (mouse != null && mouse.leftButton.wasPressedThisFrame) { pressedAt = Time.unscaledTime; pressBySpace = false; }
                else if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) { pressedAt = Time.unscaledTime; pressBySpace = true; }
            }
            if (pressedAt >= 0)
            {
                bool held = pressBySpace ? spaceDown : mouseDown;
                float k = (Time.unscaledTime - pressedAt) / HoldToSkip;
                if (!held)
                {
                    // A click hurries this segment (a click left over from the previous segment does not).
                    if (Time.unscaledTime - pressedAt <= ClickSeconds && pressedAt >= segmentStartedAt + .2f && !skipping) hurry = true;
                    pressedAt = -1; ShowRing(0);
                }
                else if (k >= 1 && !skipping) { pressedAt = -1; ShowRing(0); SkipSegment(); }
                else ShowRing(k < .2f ? 0 : k);
            }
        }

        /// <summary>Runs the rest of this segment to its end state (Esc's "skip the opening" runs every segment).</summary>
        void SkipSegment()
        {
            if (!pacing || Step == "setup" || Step == "done") return;
            skipping = true; hurry = false;
            Debug.Log("[PrologueTiming] skip " + Step + (skipAll ? " (opening)" : ""));
            VoiceLines.Stop();
            if (thinking != null) { StopCoroutine(thinking); thinking = null; }
            if (thoughtGroup != null) thoughtGroup.alpha = 0;
            ShowVeil();
        }

        void SkipOpening()
        {
            CloseSkipConfirm();
            skipAll = true;
            SkipSegment();
        }

        // ───────────── the corner hint, the ring, the veil and the question ─────────────

        void BuildSkipUi()
        {
            if (screenRoot == null || desk == null) return;
            EndPacingUiOnly();
            skipHint = PrologueDesk.Rect("Skip Hint", screenRoot, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-620, 10), new Vector2(-24, 40));
            var hint = desk.Text(skipHint, T("点击 加速 · 按住 跳过本段 · Esc 跳过开场", "Click: faster · Hold: skip this part · Esc: skip the opening"), 17, new Color(.85f, .88f, .92f, .55f), TextAlignmentOptions.MidlineRight);
            hint.raycastTarget = false;
            skipRing = PrologueDesk.Rect("Skip Ring", screenRoot, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-92, 48), new Vector2(-36, 104));
            var back = PrologueDesk.Fill(skipRing, new Color(1, 1, 1, .18f), false); back.sprite = PrologueDesk.Circle();
            var fill = PrologueDesk.Rect("Fill", skipRing, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ringFill = PrologueDesk.Fill(fill, new Color32(95, 227, 181, 230), false);
            ringFill.sprite = PrologueDesk.Circle(); ringFill.type = Image.Type.Filled; ringFill.fillMethod = Image.FillMethod.Radial360; ringFill.fillOrigin = 2; ringFill.fillAmount = 0;
            var hole = PrologueDesk.Rect("Hole", skipRing, Vector2.zero, Vector2.one, new Vector2(7, 7), new Vector2(-7, -7));
            PrologueDesk.Fill(hole, new Color(.03f, .04f, .06f, .9f), false).sprite = PrologueDesk.Circle();
            desk.Text(PrologueDesk.Rect("Label", hole, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "»", 22, Color.white, TextAlignmentOptions.Center).raycastTarget = false;
            skipRing.gameObject.SetActive(false);
            skipVeil = PrologueDesk.Rect("Skip Veil", screenRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            PrologueDesk.Fill(skipVeil, new Color(.02f, .03f, .04f, .9f), false);
            veilGroup = skipVeil.gameObject.AddComponent<CanvasGroup>(); veilGroup.blocksRaycasts = false; veilGroup.alpha = 0;
            desk.Text(PrologueDesk.Centered("Text", skipVeil, Vector2.zero, new Vector2(900, 60)), T("跳过……", "Skipping…"), 30, new Color(.85f, .9f, .95f), TextAlignmentOptions.Center).raycastTarget = false;
            skipVeil.gameObject.SetActive(false);
            skipHint.SetAsLastSibling();
        }

        void EndPacingUiOnly()
        {
            if (screenRoot == null) return;
            for (int i = screenRoot.childCount - 1; i >= 0; i--)
            {
                var child = screenRoot.GetChild(i);
                if (child.name == "Skip Hint" || child.name == "Skip Ring" || child.name == "Skip Veil" || child.name == "Skip Confirm") PrologueDesk.RemoveTransient(child.gameObject);
            }
        }

        void ShowRing(float k)
        {
            if (skipRing == null) return;
            bool on = k > 0;
            if (skipRing.gameObject.activeSelf != on) skipRing.gameObject.SetActive(on);
            if (on) { ringFill.fillAmount = Mathf.Clamp01(k); skipRing.SetAsLastSibling(); }
        }

        void ShowVeil()
        {
            if (skipVeil == null) return;
            skipVeil.gameObject.SetActive(true); skipVeil.SetAsLastSibling(); veilGroup.alpha = 1;
            if (skipHint != null) skipHint.SetAsLastSibling();
        }

        void HideVeil() { /* fades out in TickSkipInput once the skip is over */ }

        bool confirmLockedInput;

        void OpenSkipConfirm()
        {
            if (screenRoot == null || desk == null || skipConfirm != null) return;
            confirmingSkip = true;
            skipConfirm = PrologueDesk.Rect("Skip Confirm", screenRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            PrologueDesk.Fill(skipConfirm, new Color(0, 0, 0, .55f), false);
            var card = PrologueDesk.Centered("Card", skipConfirm, Vector2.zero, new Vector2(720, 270));
            PrologueDesk.Fill(card, new Color32(17, 19, 23, 245), false);
            desk.Text(PrologueDesk.Rect("Title", card, new Vector2(0, 1), Vector2.one, new Vector2(36, -78), new Vector2(-36, -24)), T("跳过开场？", "Skip the opening?"), 32, new Color32(242, 244, 247, 255), TextAlignmentOptions.MidlineLeft);
            desk.Text(PrologueDesk.Rect("Body", card, new Vector2(0, 0), Vector2.one, new Vector2(36, 96), new Vector2(-36, -86)),
                T("开场讲它从哪儿来，约一分钟。跳过后直接到起名，名字和性格仍由你来定。", "The opening shows where it came from, about a minute. Skipping goes straight to naming it; the name and personality are still yours to choose."),
                20, new Color32(151, 153, 158, 255), TextAlignmentOptions.TopLeft);
            confirmYes = ConfirmButton(card, "Skip", T("跳过开场（Enter）", "Skip (Enter)"), new Vector2(-200, 24), new Color32(95, 227, 181, 255), new Color32(6, 8, 12, 255));
            confirmNo = ConfirmButton(card, "Watch", T("继续看（Esc）", "Keep watching (Esc)"), new Vector2(-36, 24), new Color32(40, 44, 52, 255), new Color32(242, 244, 247, 255));
            // The desktop under the question takes no clicks meanwhile (unless the story already holds the mouse).
            if (!hijacked && surface != null) { surface.SetInputEnabled(false); confirmLockedInput = true; }
            if (skipHint != null) skipHint.gameObject.SetActive(false);
        }

        TMP_Text ConfirmButton(RectTransform card, string name, string label, Vector2 rightBottom, Color fill, Color ink)
        {
            var rt = PrologueDesk.Rect(name, card, new Vector2(1, 0), new Vector2(1, 0), rightBottom + new Vector2(-150, 0), rightBottom + new Vector2(0, 50));
            PrologueDesk.Fill(rt, fill, false);
            var text = desk.Text(PrologueDesk.Rect("Label", rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), label, 18, ink, TextAlignmentOptions.Center);
            text.raycastTarget = false;
            return text;
        }

        void TickSkipConfirm(Keyboard keyboard, Mouse mouse)
        {
            if (EscapeKey.TryConsume()) { CloseSkipConfirm(); return; }
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)) { SkipOpening(); return; }
            if (mouse == null || !mouse.leftButton.wasReleasedThisFrame) return;
            var at = mouse.position.ReadValue();
            if (confirmYes != null && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)confirmYes.transform.parent, at, null)) SkipOpening();
            else if (confirmNo != null && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)confirmNo.transform.parent, at, null)) CloseSkipConfirm();
        }

        void CloseSkipConfirm()
        {
            confirmingSkip = false;
            if (skipConfirm != null) PrologueDesk.RemoveTransient(skipConfirm.gameObject);
            skipConfirm = null; confirmYes = confirmNo = null;
            if (confirmLockedInput) { confirmLockedInput = false; if (surface != null && !hijacked) surface.SetInputEnabled(true); }
            if (skipHint != null && pacing) skipHint.gameObject.SetActive(true);
            pressedAt = -1;
        }
    }
}
