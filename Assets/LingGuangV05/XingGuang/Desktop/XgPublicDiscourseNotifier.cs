using LingGuangV05.Core;
using LingGuangV05.Desktop.Bodu;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>One optional public-reading prompt per stage; never creates private chats or installs apps.</summary>
    public sealed class XgPublicDiscourseNotifier : MonoBehaviour
    {
        XingGuangController owner;
        ChapterOneSim boundSave;
        XgSim boundLab;
        BoduView browser;
        XgEndingPreparationHost preparation;
        float nextCheck;
        public static void Install(XingGuangController controller)
        {
            var c = controller.GetComponent<XgPublicDiscourseNotifier>() ?? controller.gameObject.AddComponent<XgPublicDiscourseNotifier>();
            c.owner = controller;
        }
        public static void InitializeSave(XgSim sim, bool fresh)
        {
            if (sim.S.publicDiscourseDelivery == null) sim.S.publicDiscourseDelivery = new XgPublicDiscourseDeliveryState();
            XgPublicDiscourseSchedule.Initialize(sim.S.publicDiscourseDelivery, sim.S.stage, fresh);
        }
        bool Current => owner != null && ReferenceEquals(owner.runtime?.Sim, boundSave)
            && ReferenceEquals(owner.Sim, boundLab) && owner.BoundTo(boundSave);
        bool ImportantEventActive => Current && (boundLab.LifeEventOpen || boundLab.LifeEventResultPending);
        // Only presentation conflicts may hide a visible notice. A pointer-down must keep its target alive.
        bool PresentationHeld => !Current || StoryFocus.Active || InnerVoice.Busy || boundLab.StagePresentationHeld
            || XgLifeHostActivity.GamePaused || XgLifeHostActivity.ReadingWindowVisible
            || PlayerWindowState.IsMinimized || SaveSlotsPanel.IsOpen
            || (owner.View != null && owner.View.Visible && owner.View.Train != null && owner.View.Train.ExamOpen)
            || (preparation != null && preparation.Showing)
            || (LingGuangV05.Desktop.YY.YYChatHub.Instance != null && LingGuangV05.Desktop.YY.YYChatHub.Instance.GroupScriptPlaying)
            || (owner.Epilogue != null && owner.Epilogue.HoldingSimulation);
        static bool InputBusy()
        {
            var mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed || mouse.middleButton.isPressed)) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) return true;
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected == null || !selected.activeInHierarchy) return false;
            var text = selected.GetComponentInParent<TMP_InputField>();
            if (text != null && text.isActiveAndEnabled && text.isFocused) return true;
            var legacy = selected.GetComponentInParent<UnityEngine.UI.InputField>();
            return legacy != null && legacy.isActiveAndEnabled && legacy.isFocused;
        }
        void Update()
        {
            if (owner?.runtime?.Sim == null || owner.Sim == null || owner.runtime.TestMode) return;
            if (!ReferenceEquals(boundSave, owner.runtime.Sim) || !ReferenceEquals(boundLab, owner.Sim))
            {
                boundSave = owner.runtime.Sim; boundLab = owner.Sim; browser = null; preparation = owner.GetComponent<XgEndingPreparationHost>(); nextCheck = 0;
                InitializeSave(boundLab, false);
            }
            if (!Current || boundSave.InPrologue) return;
            var delivery = boundLab.S.publicDiscourseDelivery;
            var desk = PrologueDirector.Desk;
            var tray = desk?.layer == null ? null : desk.layer.GetComponent<TrayPopups>();
            // Tray activity and interaction reset the safe quiet interval before a new prompt is scheduled.
            // Never put tray.IsBusy in PresentationHeld: the notice itself owns that busy state.
            bool held = PresentationHeld || ImportantEventActive || InputBusy() || (tray != null && tray.IsBusy);
            XgPublicDiscourseSchedule.Advance(delivery, boundLab.S.stage, Time.unscaledDeltaTime,
                !held && Application.isFocused && !PlayerWindowState.IsMinimized, held);
            if (held || !Application.isFocused || Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 1;
            if (tray == null || !tray.isActiveAndEnabled || tray.IsBusy || !desk.layer.gameObject.activeInHierarchy) return;
            var catalog = XgPublicDiscourseDesktop.Catalog;
            if (catalog == null) return;
            foreach (var row in catalog.All)
            {
                var context = XgPublicDiscourseDesktop.Context(row.platform);
                if (!XgPublicDiscourseSchedule.Eligible(delivery, row, context, catalog)) continue;
                var token = XgPublicDiscourseDesktop.Capture();
                var id = row.id; var platform = row.platform;
                // This active native tray accepts the notice synchronously. Only then record delivery.
                tray.EnqueueNotification(GameText.T("公开讨论", "Public discussion"), row.Title(GameText.IsEnglish), 6,
                    () =>
                    {
                        if (!token.Current) return;
                        if (browser == null) browser = FindAnyObjectByType<BoduView>(FindObjectsInactive.Include);
                        browser?.OpenPublicDiscourse(platform, id);
                    // A pending life event cancels this optional notice so the important tray can proceed.
                    }, () => token.Current && !ImportantEventActive, () => PresentationHeld);
                if (XgPublicDiscourseSchedule.RecordDelivered(delivery, row, context, catalog)) owner.runtime.MarkDirty();
                break;
            }
        }
    }
}
