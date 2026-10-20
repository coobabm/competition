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
        XgBtn profileMore, profileCards, profileBackpack, profileFinal, profileAtlas, profilePersonaArchive, profileMemories;

        void BuildProfileSections(RectTransform area)
        {
            foreach (var page in new XgPage[] { Home, Cards, Backpack, Finale })
                page.root.offsetMax += new Vector2(0, -44);
            profileSections = Rect("Profile sections", area, new Vector2(0, 1), Vector2.one, new Vector2(0, -36), Vector2.zero);
            profileMore = ui.Button(profileSections, "", ToggleProfileMore, 14);
            profilePersonaArchive = ui.Button(profileSections, "", OpenPersonaArchive, 14);
            profilePersonaArchive.rt.gameObject.name = "Open personality archive";
            PlaceTopLeft(profilePersonaArchive, 0, 0, 220, 34);
            profileMemories = ui.Button(profileSections, "", OpenProfileMemories, 14);
            profileMemories.rt.gameObject.name = "Open remembered moments";
            profileMemories.rt.anchorMin = new Vector2(0, 1); profileMemories.rt.anchorMax = Vector2.one;
            profileMemories.rt.offsetMin = new Vector2(230, -34);
            profileMemories.rt.offsetMax = new Vector2(-170, 0);
            profileMemories.label.enableAutoSizing = true;
            profileMemories.label.fontSizeMin = 11; profileMemories.label.fontSizeMax = 14;
            profileMore.rt.anchorMin = profileMore.rt.anchorMax = Vector2.one;
            profileMore.rt.offsetMin = new Vector2(-160, -34);
            profileMore.rt.offsetMax = Vector2.zero;

            // Only the menu is new. Destinations retain their existing page instances and ShowTab routes.
            // Keep More in the top strip; the player-named sidebar entry returns home.
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

        void OpenProfileMemories()
        {
            HideProfileMore();
            if (!Visible || tab != "home") return;
            controller.GetComponent<XgEndingPreparationHost>()?.OpenProfileMemories();
        }

        void RefreshProfileSections()
        {
            if (profileSections == null) return;
            // A menu is transient UI, never save state. Do not carry it across pages or save rebinding.
            if (!ReferenceEquals(profileMoreOwner, Sim) || profileMoreTab != tab || profileMoreSection != profileSection || tab != "home" || !Visible)
                HideProfileMore();
            profileMoreOwner = Sim; profileMoreTab = tab; profileMoreSection = profileSection;
            float actionWidth = Mathf.Max(0, profileSections.rect.width - 20);
            float archiveWidth = Mathf.Min(220, actionWidth * .38f);
            float moreWidth = Mathf.Min(160, actionWidth * .28f);
            PlaceTopLeft(profilePersonaArchive, 0, 0, archiveWidth, 34);
            profilePersonaArchive.label.enableAutoSizing = true;
            profilePersonaArchive.label.fontSizeMin = 11; profilePersonaArchive.label.fontSizeMax = 14;
            profileMemories.rt.offsetMin = new Vector2(archiveWidth + 10, -34);
            profileMemories.rt.offsetMax = new Vector2(-moreWidth - 10, 0);
            profileMore.rt.offsetMin = new Vector2(-moreWidth, -34);
            profilePersonaArchive.Set(Sim.PersonaArchiveUnlocked ? T("性格档案", "Personality archive") : T("性格档案 · 未解锁", "Personality archive · Locked"), true);
            var memories = controller.GetComponent<XgEndingPreparationHost>();
            int memoryCount = memories != null ? memories.ProfileMemoryCount : 0;
            bool memoriesAvailable = memories != null && memories.CanOpenProfileMemories;
            profileMemories.Set(T("它记得（" + memoryCount + "）", "It remembers (" + memoryCount + ")"), memoriesAvailable);
            profileMemories.Show(memoriesAvailable);

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
