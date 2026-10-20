using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XgTrainPage
    {
        RectTransform relicAccessCard;
        XgBtn relicAccess;
        TMP_Text relicAccessNote;

        bool CanOpenTrainingRelics => view.FeatureOpen("backpack") && view.FeatureOpen("home");

        void BuildRelicAccess()
        {
            relicAccessCard = ui.Card(sideContent, "RareItemsAccess", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var layout = relicAccessCard.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = 60; layout.flexibleHeight = 0;
            relicAccess = ui.Button(relicAccessCard, "", () =>
            {
                // Check the currently bound save again, including the profile's existing gate.
                if (!CanOpenTrainingRelics) return;
                Fx.Play(XgJuice.Sfx.Id.Click);
                view.ShowTab("backpack");
            }, 15);
            relicAccess.rt.name = "Open rare items";
            relicAccess.rt.anchorMin = new Vector2(0, 1); relicAccess.rt.anchorMax = Vector2.one;
            relicAccess.rt.offsetMin = new Vector2(10, -36); relicAccess.rt.offsetMax = new Vector2(-10, -6);
            relicAccessNote = ui.Text(Rect("Status", relicAccessCard, Vector2.zero, Vector2.one,
                new Vector2(12, 4), new Vector2(-12, -38)), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            relicAccessNote.textWrappingMode = TextWrappingModes.NoWrap;
            UiTip.Add(relicAccessCard, () => T("原有24件稀有道具、装备与图鉴。学会新能力时可三选一；装备后才生效。", "The original 24 rare items, equipment and collection. Learning an ability offers three choices; only equipped items take effect."));
        }

        void RefreshRelicAccess()
        {
            bool available = CanOpenTrainingRelics;
            relicAccess.Set(T("稀有道具 · 打开背包", "Rare items · Open backpack"), available);
            relicAccessNote.text = available
                ? T("拥有 ", "Owned ") + Sim.S.relicOwned.Count + "/" + XgRelics.All.Length
                    + T(" · 已装备 ", " · Equipped ") + Sim.RelicsEquippedCount + "/" + Sim.RelicSlotCapacity
                : Sim.OpeningLocksLabTab("backpack") || Sim.OpeningLocksLabTab("home") ? Sim.OpeningReason
                : !view.FeatureOpen("home") ? T("先和它聊一次，再打开背包。", "Chat with it once to open the backpack.")
                : T("获得首件稀有道具后开放", "Unlocks after your first rare item.");
        }
    }
}
