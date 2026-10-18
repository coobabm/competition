using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.CaseMenu
{
    public sealed partial class CaseMenuHud
    {
        RectTransform playerHelpSettingsCard, playerHelpCard, playerHelpContent;
        Button playerHelpEntry, playerHelpBack;
        readonly Button[] playerHelpDetails = new Button[4];
        readonly TMP_Text[] playerHelpDetailLabels = new TMP_Text[4], playerHelpDetailBodies = new TMP_Text[4];
        readonly bool[] playerHelpExpanded = new bool[4];
        ScrollRect playerHelpScroll;
        Scrollbar playerHelpScrollbar;
        TMP_Text playerHelpEntryLabel, playerHelpHeading, playerHelpContext, playerHelpMain,
            playerHelpBackLabel;
        bool playerHelpEnglish;
        Vector2 playerHelpSize;
        GameObject playerHelpLastSelection;

        public bool IsPlayerHelpOpen => playerHelpCard != null && playerHelpCard.gameObject.activeSelf;
        public bool IsPlayerHelpGpuExpanded => IsPlayerHelpOpen && playerHelpExpanded[0];

        Button BuildPlayerHelpEntry(RectTransform settingsCard)
        {
            playerHelpSettingsCard = settingsCard;
            var image = Panel("PlayerHelp", settingsCard, Control, true);
            Place(image.rectTransform, new Vector2(0, 1), new Vector2(200, -557), new Vector2(142, 44));
            playerHelpEntry = image.gameObject.AddComponent<Button>(); Style(playerHelpEntry, image);
            playerHelpEntryLabel = Label("Label", image.transform, "", 18, Ink);
            Stretch(playerHelpEntryLabel.rectTransform); playerHelpEntryLabel.alignment = TextAlignmentOptions.Midline;
            playerHelpEntry.onClick.AddListener(() => { uiClick?.Invoke(); OpenPlayerHelp(); });
            RefreshPlayerHelpEntry();
            return playerHelpEntry;
        }

        void RefreshPlayerHelpEntry()
        { if (playerHelpEntryLabel != null) playerHelpEntryLabel.text = T("帮助", "Help"); }

        // This entry is deliberately independent of the simulation, stages, money and save data.
        public void OpenPlayerHelp()
        {
            if (!IsSettingsOpen || playerHelpSettingsCard == null) return;
            if (playerHelpCard == null) BuildPlayerHelp();
            System.Array.Clear(playerHelpExpanded, 0, playerHelpExpanded.Length);
            playerHelpSettingsCard.gameObject.SetActive(false);
            playerHelpCard.gameObject.SetActive(true);
            playerHelpCard.SetAsLastSibling();
            RefreshPlayerHelp(true);
            playerHelpScroll.StopMovement(); playerHelpScroll.verticalNormalizedPosition = 1;
            EventSystem.current?.SetSelectedGameObject(playerHelpBack.gameObject);
        }

        public void ClosePlayerHelp() => ClosePlayerHelp(true);

        void ClosePlayerHelp(bool restoreFocus)
        {
            bool wasOpen = IsPlayerHelpOpen;
            if (playerHelpCard != null) playerHelpCard.gameObject.SetActive(false);
            if (playerHelpSettingsCard != null) playerHelpSettingsCard.gameObject.SetActive(true);
            System.Array.Clear(playerHelpExpanded, 0, playerHelpExpanded.Length);
            RefreshPlayerHelpEntry();
            if (wasOpen && restoreFocus && IsSettingsOpen)
                EventSystem.current?.SetSelectedGameObject(playerHelpEntry.gameObject);
        }

        void BuildPlayerHelp()
        {
            playerHelpCard = Panel("PlayerHelpCard", settingsGroup.transform, Surface, true).rectTransform;
            playerHelpHeading = HelpText("Audience", playerHelpCard, 28, Ink);
            playerHelpContext = HelpText("Context", playerHelpCard, 16, Muted);
            var clip = Panel("PlayerHelpViewport", playerHelpCard, Surface, true).rectTransform;
            clip.gameObject.AddComponent<RectMask2D>();
            playerHelpContent = Rect("PlayerHelpContent", clip);
            playerHelpContent.anchorMin = new Vector2(0, 1); playerHelpContent.anchorMax = Vector2.one;
            playerHelpContent.pivot = new Vector2(.5f, 1);
            playerHelpScroll = clip.gameObject.AddComponent<ScrollRect>();
            playerHelpScroll.viewport = clip; playerHelpScroll.content = playerHelpContent;
            playerHelpScroll.horizontal = false; playerHelpScroll.vertical = true;
            playerHelpScroll.inertia = false; playerHelpScroll.scrollSensitivity = 36;
            playerHelpScroll.movementType = ScrollRect.MovementType.Clamped;
            playerHelpMain = HelpText("FormalExplanations", playerHelpContent, 20, Ink);
            playerHelpMain.richText = true;
            string[] names = { "GpuSupplement", "SixStages", "TrainingWorkflow", "ContextAndTraining" };
            string[] bodyNames = { "GpuExplanation", "SixStagesExplanation", "TrainingWorkflowExplanation", "ContextAndTrainingExplanation" };
            for (int i = 0; i < playerHelpDetails.Length; i++)
            {
                int index = i;
                var detail = Panel(names[i], playerHelpContent, Control, true);
                var button = playerHelpDetails[i] = detail.gameObject.AddComponent<Button>(); Style(button, detail);
                var label = playerHelpDetailLabels[i] = HelpText("Label", detail.transform, 18, Ink);
                Stretch(label.rectTransform);
                label.rectTransform.offsetMin = new Vector2(14, 6);
                label.rectTransform.offsetMax = new Vector2(-14, -6);
                label.alignment = TextAlignmentOptions.MidlineLeft;
                button.onClick.AddListener(() =>
                {
                    playerHelpExpanded[index] = !playerHelpExpanded[index];
                    RefreshPlayerHelp(true); uiClick?.Invoke();
                });
                playerHelpDetailBodies[i] = HelpText(bodyNames[i], playerHelpContent, 20, Ink);
            }
            var track = Panel("PlayerHelpScrollTrack", playerHelpCard, Control, true);
            var handle = Panel("Handle", track.transform, Muted, true); Stretch(handle.rectTransform);
            playerHelpScrollbar = track.gameObject.AddComponent<Scrollbar>();
            playerHelpScrollbar.handleRect = handle.rectTransform;
            playerHelpScrollbar.direction = Scrollbar.Direction.BottomToTop;
            Style(playerHelpScrollbar, handle);
            playerHelpScroll.verticalScrollbar = playerHelpScrollbar;
            playerHelpScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            var back = Panel("PlayerHelpBack", playerHelpCard, Control, true);
            playerHelpBack = back.gameObject.AddComponent<Button>(); Style(playerHelpBack, back);
            playerHelpBackLabel = Label("Label", back.transform, "", 18, Ink);
            Stretch(playerHelpBackLabel.rectTransform); playerHelpBackLabel.alignment = TextAlignmentOptions.Midline;
            playerHelpBack.onClick.AddListener(() => { uiClick?.Invoke(); ClosePlayerHelp(); });
            Link(playerHelpBack, playerHelpDetails[3], playerHelpScrollbar);
            Link(playerHelpScrollbar, playerHelpBack, playerHelpDetails[0]);
            for (int i = 0; i < playerHelpDetails.Length; i++)
                Link(playerHelpDetails[i], i == 0 ? (Selectable)playerHelpScrollbar : playerHelpDetails[i - 1],
                    i == 3 ? playerHelpBack : playerHelpDetails[i + 1]);
            playerHelpCard.gameObject.SetActive(false);
        }

        TMP_Text HelpText(string name, Transform parent, float size, Color color)
        {
            var text = Label(name, parent, "", size, color);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.alignment = TextAlignmentOptions.TopLeft;
            return text;
        }

        // Reflow on language or window changes only, not every frame.
        void LateUpdate()
        {
            if (!IsPlayerHelpOpen) return;
            RefreshPlayerHelp(false);
            // Keep a keyboard-selected supplement in view, without moving a pointer reader.
            var selected = EventSystem.current?.currentSelectedGameObject;
            bool changed = selected != playerHelpLastSelection;
            playerHelpLastSelection = selected;
            var selectedDetail = System.Array.Find(playerHelpDetails, button => button != null && button.gameObject == selected);
            if (changed && selectedDetail != null)
            {
                float top = -selectedDetail.GetComponent<RectTransform>().anchoredPosition.y;
                float bottom = top + selectedDetail.GetComponent<RectTransform>().rect.height;
                float visibleTop = playerHelpContent.anchoredPosition.y;
                float visibleBottom = visibleTop + playerHelpScroll.viewport.rect.height;
                if (top < visibleTop) playerHelpContent.anchoredPosition = new Vector2(0, top);
                else if (bottom > visibleBottom)
                    playerHelpContent.anchoredPosition = new Vector2(0, Mathf.Max(0, bottom - playerHelpScroll.viewport.rect.height));
            }
        }

        void RefreshPlayerHelp(bool force)
        {
            Vector2 available = canvasRoot.rect.size;
            bool english = GameText.IsEnglish;
            if (!force && playerHelpSize == available && playerHelpEnglish == english) return;
            playerHelpSize = available; playerHelpEnglish = english;
            float oldTop = playerHelpContent.anchoredPosition.y;
            float width = Mathf.Max(240, Mathf.Min(980, available.x - 64));
            float height = Mathf.Max(240, Mathf.Min(820, available.y - 64));
            Place(playerHelpCard, new Vector2(.5f, .5f), Vector2.zero, new Vector2(width, height));
            playerHelpHeading.text = PlayerKnowledgeCopy.Audience(english);
            playerHelpContext.text = PlayerKnowledgeCopy.Context(english);
            playerHelpMain.text = "<b>" + PlayerKnowledgeCopy.Title1(english) + "</b>\n\n" + PlayerKnowledgeCopy.Body1(english)
                + "\n\n<b>" + PlayerKnowledgeCopy.Title2(english) + "</b>\n\n" + PlayerKnowledgeCopy.Body2(english)
                + "\n\n<b>" + PlayerKnowledgeCopy.Title4(english) + "</b>\n\n" + PlayerKnowledgeCopy.Summary4(english);
            string[] titles = { PlayerKnowledgeCopy.Title3(english), PlayerKnowledgeCopy.Title4(english), PlayerKnowledgeCopy.Title5(english), PlayerKnowledgeCopy.Title6(english) };
            string[] bodies = { PlayerKnowledgeCopy.Body3(english), PlayerKnowledgeCopy.Body4(english), PlayerKnowledgeCopy.Body5(english), PlayerKnowledgeCopy.Body6(english) };
            for (int i = 0; i < playerHelpDetails.Length; i++)
            {
                playerHelpDetailLabels[i].text = (playerHelpExpanded[i] ? "− " : "+ ") + T("可展开阅读：", "Optional reading: ") + titles[i];
                playerHelpDetailBodies[i].text = bodies[i];
                playerHelpDetailBodies[i].gameObject.SetActive(playerHelpExpanded[i]);
            }
            playerHelpBackLabel.text = T("返回设置", "Back to settings");
            float inner = width - 56;
            float headingHeight = HelpHeight(playerHelpHeading, inner, 36);
            HelpPlace(playerHelpHeading.rectTransform, 28, 20, inner, headingHeight);
            float contextHeight = HelpHeight(playerHelpContext, inner, 28);
            HelpPlace(playerHelpContext.rectTransform, 28, 28 + headingHeight, inner, contextHeight);
            float top = 42 + headingHeight + contextHeight;
            float viewportHeight = Mathf.Max(24, height - top - 82);
            float textWidth = inner - 24;
            HelpPlace(playerHelpScroll.viewport, 28, top, textWidth, viewportHeight);
            HelpPlace((RectTransform)playerHelpScrollbar.transform, width - 40, top, 12, viewportHeight);
            float mainHeight = HelpHeight(playerHelpMain, textWidth, 0);
            HelpPlace(playerHelpMain.rectTransform, 0, 0, textWidth, mainHeight);
            float y = mainHeight + 24;
            for (int i = 0; i < playerHelpDetails.Length; i++)
            {
                float buttonHeight = HelpHeight(playerHelpDetailLabels[i], textWidth - 28, 28) + 16;
                HelpPlace((RectTransform)playerHelpDetails[i].transform, 0, y, textWidth, buttonHeight);
                y += buttonHeight + 18;
                float bodyHeight = HelpHeight(playerHelpDetailBodies[i], textWidth, 0);
                HelpPlace(playerHelpDetailBodies[i].rectTransform, 0, y, textWidth, bodyHeight);
                if (playerHelpExpanded[i]) y += bodyHeight + 24;
            }
            playerHelpContent.sizeDelta = new Vector2(0, y);
            playerHelpContent.anchoredPosition = new Vector2(0, Mathf.Clamp(oldTop, 0, Mathf.Max(0, playerHelpContent.sizeDelta.y - viewportHeight)));
            Place((RectTransform)playerHelpBack.transform, Vector2.zero, new Vector2(28, 22), new Vector2(210, 44));
        }

        static float HelpHeight(TMP_Text text, float width, float minimum)
            => Mathf.Max(minimum, Mathf.Ceil(text.GetPreferredValues(text.text, Mathf.Max(1, width), Mathf.Infinity).y) + 4);

        static void HelpPlace(RectTransform rect, float left, float top, float width, float height)
            => Place(rect, new Vector2(0, 1), new Vector2(left, -top), new Vector2(width, height));
    }
}
