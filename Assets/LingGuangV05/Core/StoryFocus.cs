using System;

namespace LingGuangV05.Core
{
    /// <summary>
    /// A story moment is on screen (剧情修订 2026-10-16 §二): a beat with lines, a cutscene, the origin film, an ability's
    /// cutscene, the AI joining YY, the epiphany, the love question. While it plays, nothing else pops up: life-event cards,
    /// platform notices, tray bubbles, toasts, flash cards and other people's YY popups wait (their messages still land);
    /// they come back <see cref="SettleSeconds"/> after it ends. The story's own popups always pass.
    /// One place decides: <see cref="Active"/> (set by the story presenter). Callers ask <see cref="Allows"/>.
    /// </summary>
    public static class StoryFocus
    {
        /// <summary>Seconds of calm after a story moment before held popups start again.</summary>
        public const double SettleSeconds = 3;

        /// <summary>The desktop's reading (set by the story presenter). Unset outside the game: never focused.</summary>
        public static Func<bool> Probe;
        public static Func<bool> ExclusiveProbe;
        public static bool Exclusive => ExclusiveProbe != null && ExclusiveProbe();
        static object notificationOwner;static double notificationAfter;
        static readonly System.Collections.Generic.List<object> notificationWaiters=new System.Collections.Generic.List<object>();
        public static bool TakeNotification(object owner,double now){if(owner==null)return false;if(ReferenceEquals(notificationOwner,owner))return !Active; if(!notificationWaiters.Exists(x=>ReferenceEquals(x,owner)))notificationWaiters.Add(owner);if(Active||now<notificationAfter||notificationOwner!=null||!ReferenceEquals(notificationWaiters[0],owner))return false;notificationWaiters.RemoveAt(0);notificationOwner=owner;return true;}
        public static void ReleaseNotification(object owner,double now){notificationWaiters.RemoveAll(x=>ReferenceEquals(x,owner));if(!ReferenceEquals(notificationOwner,owner))return;notificationOwner=null;notificationAfter=now+1.5;}



        /// <summary>A story moment is playing, or ended less than <see cref="SettleSeconds"/> ago.</summary>
        public static bool Active
        {
            get
            {
                if(Exclusive)return true;
                var probe = Probe;
                if (probe == null) return false;
                try { return probe(); }
                catch (Exception) { return false; }
            }
        }

        /// <summary>A story popup always passes; anything else only when no story moment is on screen.</summary>
        public static bool Allows(bool story) => !Exclusive && (story || !Active);
    }
}
