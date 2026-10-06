using System;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Core.Chat;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using UnityEngine;

namespace LingGuangV05.Desktop.Media
{
    /// <summary>
    /// Receive-only notifications. History restoration and the player's own sends never enter this service.
    /// Bursts coalesce by conversation; unread histories remain authoritative, not this bounded UI queue.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DesktopNotifications : MonoBehaviour
    {
        const int MaxPending = 12;
        sealed class Pending
        {
            public string channel, sender, body;
            public int count = 1;
            public Action open;
        }

        readonly List<Pending> pending = new List<Pending>();
        readonly DesktopNotificationBudget budget = new DesktopNotificationBudget();
        ChapterOneRuntime runtime;
        ChapterOneSim bound;
        XingGuangController lab;
        StoryDesktopPresenter story;
        PrologueDirector prologue;
        OriginCurtain curtain;
        int overflow;

        public static DesktopNotifications Install(ChapterOneRuntime runtime)
        {
            if (runtime == null || runtime.TestMode || runtime.Sim == null) return null;
            var service = runtime.GetComponent<DesktopNotifications>() ?? runtime.gameObject.AddComponent<DesktopNotifications>();
            service.runtime = runtime;
            service.CheckSave();
            return service;
        }

        /// <summary>DreamOS keeps isOn true when minimized; the root CanvasGroup owns actual visibility.</summary>
        public static bool IsWindowVisible(Component view)
        {
            if (view == null || !view.gameObject.activeInHierarchy) return false;
            var window = view.GetComponentInParent<Michsky.DreamOS.WindowManager>();
            if (window == null || !window.isOn) return false;
            var group = window.GetComponent<CanvasGroup>();
            return group == null || group.alpha > .01f;
        }

        public static void Notify(ChapterOneRuntime runtime, string channel, string sender, string body,
            bool visibleConversation = false, Action onClick = null)
        {
            if (string.IsNullOrWhiteSpace(body)) return;
            var service = Install(runtime);
            if (service == null) return;
            // The arriving bubble itself is the visible reminder. Never cover the conversation or steal focus.
            if (visibleConversation && !service.Held()) { service.Sound(); return; }
            channel = channel ?? "";
            sender = sender ?? "";
            string text = YYFaces.ToPlainText(body, GameText.IsEnglish);
            text = text.Length > 180 ? text.Substring(0, 180) + "…" : text;
            var item = service.pending.Find(p => p.channel == channel && p.sender == sender);
            if (item == null)
            {
                if (service.pending.Count >= MaxPending) { service.overflow++; return; }
                item = new Pending { channel = channel, sender = sender };
                service.pending.Add(item);
            }
            else item.count++;
            item.body = text;
            item.open = onClick;
        }

        /// <summary>Also used when an existing native tray notice actually appears.</summary>
        public static void Ping()
        {
            var runtime = FindAnyObjectByType<ChapterOneRuntime>();
            var service = Install(runtime);
            if (service != null) service.Sound();
        }

        void CheckSave()
        {
            if (ReferenceEquals(bound, runtime.Sim)) return;
            bound = runtime.Sim;
            pending.Clear(); overflow = 0; budget.Reset();
        }

        bool Held()
        {
            if (story == null) story = GetComponent<StoryDesktopPresenter>();
            if (prologue == null) prologue = GetComponent<PrologueDirector>();
            if (curtain == null) curtain = GetComponent<OriginCurtain>();
            return (story != null && story.NotificationsHeld) || (prologue != null && (prologue.Running || prologue.EndingPlaying))
                || AutoLabelEpiphany.Playing || AiJoinsYy.Playing || LoveQuestionCutscene.Playing
                || (curtain != null && curtain.Playing);
        }

        void Sound()
        {
            if (lab == null) lab = GetComponent<XingGuangController>() ?? FindAnyObjectByType<XingGuangController>();
            bool muted = lab != null && lab.Sim != null && lab.Sim.S.mute;
            if (!budget.TrySound(Time.unscaledTimeAsDouble, muted, Held())) return;
            XgJuice.Sfx.Init(runtime.gameObject, true);
            XgJuice.Sfx.Play(XgJuice.Sfx.Id.Ding, 1, .65f);
        }

        void Update()
        {
            if (runtime == null || runtime.Sim == null || runtime.TestMode) return;
            CheckSave();
            if (pending.Count == 0 && overflow == 0) return;
            if (Held()) return;
            var desk = PrologueDirector.Desk;
            var tray = desk != null && desk.layer != null ? desk.layer.GetComponent<TrayPopups>() : null;
            if (desk == null || desk.layer == null) return; // Wait for the existing desktop, never create another shell.
            if (!budget.TryPopup(Time.unscaledTimeAsDouble, false, tray != null && tray.IsBusy)) return;
            Pending item;
            if (pending.Count > 0) { item = pending[0]; pending.RemoveAt(0); }
            else
            {
                item = new Pending { channel = Lang.T("消息"), sender = "",
                    body = GameText.F("另有 {0} 条新消息，请查看应用的未读标记。", "{0} more new messages. Check the apps' unread badges.", overflow) };
                overflow = 0;
            }
            if (tray == null) tray = desk.layer.gameObject.AddComponent<TrayPopups>();
            tray.desk = desk;
            string title = item.channel + (item.sender.Length > 0 ? " · " + item.sender : "");
            string body = (item.count > 1 ? GameText.F("{0} 条新消息\n", "{0} new messages\n", item.count) : "") + item.body;
            var save = bound;
            tray.EnqueueNotification(title, body, 6, item.open,
                () => this != null && runtime != null && !runtime.TestMode && ReferenceEquals(runtime.Sim, save), Held);
        }
    }
}
