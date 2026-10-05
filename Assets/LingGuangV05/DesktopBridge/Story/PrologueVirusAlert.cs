using System.Collections;
using LingGuangV05.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// The first thing after boot: 360 reports a severe virus and asks whether to clean it. The player has the mouse,
    /// but 是 cannot be pressed. The first two presses make the whole box jump aside; on the third, 否 swaps into 是's
    /// place under the pointer and the press lands on 否. The risk is ignored and 0.txt appears (Step 1).
    /// It is the SI's first move, before it takes the mouse outright.
    /// </summary>
    public sealed partial class PrologueDirector
    {
        const int VirusDodges = 2;
        static readonly Color VirusGreen = new Color32(46, 160, 67, 255), VirusRed = new Color32(210, 60, 42, 255);

        RectTransform virusBox;

        IEnumerator VirusAlert()
        {
            bool answered = false;
            int presses = 0;
            var client = desk.Window("Prologue Virus Alert", L("virus_title"), new Vector2(0, 40), new Vector2(560, 300), out virusBox);
            spawned.Add(virusBox.gameObject);
            // 360's own green frame instead of the Aero blue.
            PrologueDesk.Fill(virusBox, VirusGreen);
            var bar = virusBox.Find("Title") as RectTransform;
            PrologueDesk.Fill(bar, VirusGreen);
            PrologueDesk.Fill(bar.Find("Shine") as RectTransform, new Color32(78, 190, 96, 255), false);
            var barText = bar.Find("Text").GetComponent<TMP_Text>();
            barText.color = Color.white;
            // Closing the box is not an answer.
            virusBox.Find("Title/Close").GetComponent<Button>().onClick.RemoveAllListeners();

            var shield = PrologueDesk.Centered("Shield", client, new Vector2(-205, 62), new Vector2(64, 64));
            PrologueDesk.Fill(shield, VirusRed, false);
            desk.Text(PrologueDesk.Rect("Mark", shield, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "!", 44, Color.white, TextAlignmentOptions.Center);
            desk.Text(PrologueDesk.Centered("Head", client, new Vector2(55, 82), new Vector2(420, 40)), "<b>" + L("virus_head") + "</b>", 26, VirusRed, TextAlignmentOptions.MidlineLeft);
            desk.Text(PrologueDesk.Centered("Body", client, new Vector2(55, 8), new Vector2(420, 110)), L("virus_body"), 17, PrologueDesk.Ink, TextAlignmentOptions.TopLeft);

            Vector2 yesAt = new Vector2(80, -98), noAt = new Vector2(205, -98);
            var yes = desk.Button(client, L("yes"), yesAt, new Vector2(110, 38), null, VirusGreen);
            yes.GetComponentInChildren<TMP_Text>().color = Color.white;
            var no = desk.Button(client, L("no"), noAt, new Vector2(110, 38), () => answered = true);
            // 是 never clicks: pressing it only moves things away from the pointer.
            var dodge = yes.gameObject.AddComponent<PrologueDodge>();
            dodge.Pressed = () => presses++;
            int handled = 0;

            Think(L("m_virus"), 4);
            while (!answered)
            {
                if (presses > handled)
                {
                    handled++;
                    if (handled <= VirusDodges)
                    {
                        yield return Dodge(virusBox);
                        Think(L(handled == 1 ? "m_dodge_1" : "m_dodge_2"), 2.5f);
                    }
                    else
                    {
                        // 否 jumps into 是's place under the pointer, and the press lands on it.
                        ((RectTransform)no.transform).anchoredPosition = yesAt;
                        ((RectTransform)yes.transform).anchoredPosition = noAt;
                        no.targetGraphic.color = new Color32(200, 214, 230, 255);
                        yield return PrologueDesk.Wait(.45f);
                        answered = true;
                        Think(L("m_dodge_3"), 2.5f);
                        yield return PrologueDesk.Wait(1.6f);
                    }
                }
                yield return null;
            }

            Close(ref virusBox);
            desk.Popup(L("who_360"), L("virus_ignored"), 6);
            yield return PrologueDesk.Wait(.8f);
            yield return ThinkAndWait(L("m_ignored"), 1.5f);
        }

        /// <summary>The box slides a short way in a direction that keeps it on the desktop.</summary>
        IEnumerator Dodge(RectTransform box)
        {
            var parent = (RectTransform)box.parent;
            Vector2 half = parent.rect.size / 2 - box.rect.size / 2 - new Vector2(20, 60);
            Vector2 from = box.anchoredPosition, to = from;
            for (int tries = 0; tries < 12; tries++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2);
                var candidate = from + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(170f, 240f);
                if (Mathf.Abs(candidate.x) <= half.x && Mathf.Abs(candidate.y) <= half.y) { to = candidate; break; }
                to = new Vector2(Mathf.Clamp(candidate.x, -half.x, half.x), Mathf.Clamp(candidate.y, -half.y, half.y));
            }
            for (float t = 0; t < .14f; t += Time.unscaledDeltaTime)
            {
                float k = 1 - (1 - t / .14f) * (1 - t / .14f);
                box.anchoredPosition = Vector2.Lerp(from, to, k);
                yield return null;
            }
            box.anchoredPosition = to;
        }
    }

    /// <summary>Reports a left press on a control that must never complete a click.</summary>
    public sealed class PrologueDodge : MonoBehaviour, IPointerDownHandler
    {
        public System.Action Pressed;

        public void OnPointerDown(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left) Pressed?.Invoke();
        }
    }
}
