using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;
namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XingGuangView
    {
        RectTransform profileSections, profileMoreOverlay, profileMorePanel;
        string profileSection = "home", profileMoreTab = "", profileMoreSection = "";
        XgSim profileMoreOwner;
        XgBtn profileOverview, profileMore, profileCards, profileBackpack, profileFinal, profileAtlas, profilePersonaArchive;

        void BuildProfileSections(RectTransform area)
        {
            foreach (var page in new XgPage[] { Home, Cards, Backpack, Finale })
                page.root.offsetMax += new Vector2(0, -44);
            profileSections = Rect("Profile sections", area, new Vector2(0, 1), Vector2.one, new Vector2(0, -36), Vector2.zero);
            profileOverview = ui.Button(profileSections, "", () => OpenProfileSection("home"), 14);
            profileMore = ui.Button(profileSections, "", ToggleProfileMore, 14);
            PlaceTopLeft(profileOverview, 0, 0, 180, 34);
            profilePersonaArchive = ui.Button(profileSections, "", OpenPersonaArchive, 14);
            profilePersonaArchive.rt.gameObject.name = "Open personality archive";
            PlaceTopLeft(profilePersonaArchive, 190, 0, 220, 34);
            profileMore.rt.anchorMin = profileMore.rt.anchorMax = Vector2.one;
            profileMore.rt.offsetMin = new Vector2(-160, -34);
            profileMore.rt.offsetMax = Vector2.zero;

            // Only the menu is new. Destinations retain their existing page instances and ShowTab routes.
            // Keep the top strip available to toggle More or return home; a click elsewhere dismisses the menu.
            profileMoreOverlay = Rect("Profile more", area, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -44));
            var outside = Panel(profileMoreOverlay, Color.clear);
            var dismiss = profileMoreOverlay.gameObject.AddComponent<Button>();
            dismiss.transition = Selectable.Transition.None; dismiss.targetGraphic = outside;
            dismiss.onClick.AddListener(HideProfileMore);
            profileMorePanel = ui.Card(profileMoreOverlay, "Menu", Vector2.one, Vector2.one, new Vector2(-288, -160), Vector2.zero);
            // The panel itself catches empty-space clicks, so only the area outside it dismisses.
            var menuSurface = profileMorePanel.gameObject.AddComponent<Button>();
            menuSurface.transition = Selectable.Transition.None; menuSurface.targetGraphic = profileMorePanel.GetComponent<Image>();
            profileCards = ui.Button(profileMorePanel, "", () => OpenProfileSection("cards"), 14);
            profileBackpack = ui.Button(profileMorePanel, "", () => OpenProfileSection("backpack"), 14);
            profileFinal = ui.Button(profileMorePanel, "", () => OpenProfileSection("final"), 14);
            profileAtlas = ui.Button(profileMorePanel, "", OpenAtlas, 14);
            profileAtlas.rt.gameObject.name = "Open unified atlas";
            profileMoreOverlay.gameObject.SetActive(false);
            controller.Window.onMinimize.AddListener(HideProfileMore);
            controller.Window.onClose.AddListener(HideProfileMore);
        }

        void ToggleProfileMore()
        {
            RefreshProfileSections();
            if (tab != "home" || !FeatureOpen("home")) return;
            bool show = !profileMoreOverlay.gameObject.activeSelf;
            profileMoreOverlay.gameObject.SetActive(show);
            if (show) profileMoreOverlay.SetAsLastSibling();
        }

        void HideProfileMore()
        {
            if (profileMoreOverlay != null) profileMoreOverlay.gameObject.SetActive(false);
        }

        void OpenProfileSection(string id)
        {
            HideProfileMore();
            ShowTab(id);
        }

        void RefreshProfileSections()
        {
            if (profileSections == null) return;
            // A menu is transient UI, never save state. Do not carry it across pages or save rebinding.
            if (!ReferenceEquals(profileMoreOwner, Sim) || profileMoreTab != tab || profileMoreSection != profileSection || tab != "home" || !Visible)
                HideProfileMore();
            profileMoreOwner = Sim; profileMoreTab = tab; profileMoreSection = profileSection;
            profileOverview.Set(profileSection == "home" ? T("近况与性格", "About me") : T("返回名字页", "Back to profile"), true);
            profilePersonaArchive.Set(Sim.PersonaArchiveUnlocked ? T("性格档案", "Personality archive") : T("性格档案 · 未解锁", "Personality archive · Locked"), true);

            // Preserve the original visibility/interaction rules: cards and awards share one existing page.
            profileCards.Set(T("闪卡与成就", "Cards & awards"), true);
            profileAtlas.Set(T("图鉴", "Atlas"), true);
            bool backpack = FeatureOpen("backpack"), finale = FeatureOpen("final");
            bool fresh = backpack && Sim.RelicsFresh && profileSection != "backpack";
            string badge = fresh ? " <color=#E24B4A>●</color>" : "";
            profileMore.Set(T("更多", "More") + badge, true);
            profileBackpack.Set(T("背包", "Backpack") + badge, backpack);
            profileBackpack.Show(backpack);
            profileFinal.Set(T("终章", "Finale"), finale);
            profileFinal.Show(finale);

            float y = 12;
            PlaceTopLeft(profileCards, 12, y, 264, 40); y += 46;
            PlaceTopLeft(profileAtlas, 12, y, 264, 40); y += 46;
            if (backpack) { PlaceTopLeft(profileBackpack, 12, y, 264, 40); y += 46; }
            if (finale) { PlaceTopLeft(profileFinal, 12, y, 264, 40); y += 46; }
            profileMorePanel.offsetMin = new Vector2(-288, -y - 6);
        }
    }
}
