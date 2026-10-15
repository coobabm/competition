using LingGuangV05.Core;
using LingGuangV05.Runtime;
using Michsky.DreamOS;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Extends the existing desktop menu without replacing its original actions.</summary>
    public sealed class XgWallpaperMenu : MonoBehaviour
    {
        XgBrainWallpaper wallpaper;
        XgBrainWallpaperPresentation presentation;
        XingGuangController controller;
        ContextMenuContent menu;
        ContextMenuContent.MenuItem divider, view, quiet;
        bool installed;
        Image menuBackground;

        void Start()
        {
            wallpaper = GetComponent<XgBrainWallpaper>();
            presentation = GetComponent<XgBrainWallpaperPresentation>();
            controller = FindAnyObjectByType<XingGuangController>();
            menu = transform.parent.Find("Desktop Context Menu")?.GetComponent<ContextMenuContent>();
            divider = new ContextMenuContent.MenuItem { contextItemType = ContextMenuContent.ContextItemType.Separator };
            view = new ContextMenuContent.MenuItem();
            quiet = new ContextMenuContent.MenuItem();
            view.onClick.AddListener(OpenProfile);
            quiet.onClick.AddListener(() => presentation.SetQuiet(!presentation.Quiet));
            // Native text uses dark Aero ink. Give it the light Win7 menu surface,
            // independently of the taskbar's blue glass theme.
            menuBackground = menu?.contextManager?.contentRect.Find("Background")?.GetComponent<Image>();
            if (menuBackground != null)
            {
                var theme = menuBackground.GetComponent<UIManagerElement>();
                if (theme != null) theme.useCustomColor = true;
                menuBackground.color = new Color32(246, 248, 251, 255);
            }
        }

        void OpenProfile()
        {
            if (!wallpaper.Reveal.Visible || controller == null || controller.runtime == null) return;
            var router = controller.runtime.GetComponent<ChapterOneDesktopRouter>();
            if (router != null) router.Open(XingGuangController.AppId, "home");
        }

        void Update()
        {
            if (menu == null) return;
            bool visible = wallpaper.Reveal.Visible && wallpaper.Reveal.Age >= XgWallpaperReveal.CompleteSeconds;
            if (visible != installed)
            {
                if (menu.contextManager != null && menu.contextManager.isOn) menu.contextManager.Close();
                if (visible) { menu.menuItems.Add(divider); menu.menuItems.Add(view); menu.menuItems.Add(quiet); installed = true; }
                else { RemoveItems(); presentation.SetQuiet(false); }
            }
            view.itemText = GameText.T("查看", "View");
            quiet.itemText = presentation.Quiet ? GameText.T("安静（已开启，点击恢复）", "Quiet (on — click to restore)") : GameText.T("安静", "Quiet");
        }

        void RemoveItems()
        {
            if (menu != null) { menu.menuItems.Remove(divider); menu.menuItems.Remove(view); menu.menuItems.Remove(quiet); }
            installed = false;
        }

        void OnDestroy() { RemoveItems(); }
    }
}
