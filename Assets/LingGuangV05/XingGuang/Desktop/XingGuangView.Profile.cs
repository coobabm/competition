using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;
namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XingGuangView
    {
        RectTransform profileSections;
        string profileSection = "home";
        XgBtn profileOverview, profileCards, profileBackpack, profileFinal;
        void BuildProfileSections(RectTransform area)
        {
            foreach (var page in new XgPage[] { Home, Cards, Backpack, Finale })
                page.root.offsetMax += new Vector2(0, -44);
            profileSections = Rect("Profile sections", area, new Vector2(0, 1), Vector2.one, new Vector2(0, -36), Vector2.zero);
            profileOverview = ui.Button(profileSections, "", () => ShowTab("home"), 14);
            profileCards = ui.Button(profileSections, "", () => ShowTab("cards"), 14);
            profileBackpack = ui.Button(profileSections, "", () => ShowTab("backpack"), 14);
            profileFinal = ui.Button(profileSections, "", () => ShowTab("final"), 14);
            PlaceTopLeft(profileOverview, 0, 0, 120, 34);
            PlaceTopLeft(profileCards, 128, 0, 120, 34);
            PlaceTopLeft(profileBackpack, 256, 0, 120, 34);
            PlaceTopLeft(profileFinal, 384, 0, 120, 34);
        }
        void RefreshProfileSections()
        {
            if (profileSections == null) return;
            profileOverview.Set(T("近况与性格", "About me"), true);
            profileCards.Set(T("闪卡与成就", "Cards & awards"), true);
            // 背包 (rare items) sits here once the first one is offered; it slides into its slot so 终章 never leaves a gap.
            bool backpack = FeatureOpen("backpack");
            profileBackpack.Set(T("背包", "Backpack") + (Sim.RelicsFresh && profileSection != "backpack" ? " <color=#E24B4A>●</color>" : ""), backpack);
            profileBackpack.rt.gameObject.SetActive(backpack);
            PlaceTopLeft(profileFinal, backpack ? 384 : 256, 0, 120, 34);
            profileFinal.Set(T("终章", "Finale"), FeatureOpen("final"));
            profileFinal.rt.gameObject.SetActive(FeatureOpen("final"));
        }
    }
}
