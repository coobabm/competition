#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using LingGuangV05.Desktop.YY;
using UnityEngine;

// Prepared isolated PlayMode coroutine; no authored messages are delivered and no runtime/save owner is loaded.
public static class LifeGroupPlaybackChecks
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static T Get<T>(YYChatHub hub, string field) => (T)typeof(YYChatHub).GetField(field, Hidden).GetValue(hub);
    static void Set(YYChatHub hub, string field, object value) => typeof(YYChatHub).GetField(field, Hidden).SetValue(hub, value);
    static void State(YYChatHub hub, YYState state) => typeof(YYChatHub).GetProperty("S").GetSetMethod(true).Invoke(hub, new object[] { state });
    static void Check(bool ok, string label) { if (!ok) throw new Exception(label); }
    public static IEnumerator Frames()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Run in an isolated PlayMode fixture");
        var random = UnityEngine.Random.state;
        var go = new GameObject("Life group playback lifecycle fixture");
        var hub = go.AddComponent<YYChatHub>();
        IEnumerator stale = null;
        try
        {
            var saved = new YYState(); State(hub, saved);
            var queue = Get<Queue<string>>(hub, "presetReplyQueue");
            queue.Enqueue("queued fixture; never delivered");
            Set(hub, "presetReplyOwner", saved);
            go.SetActive(false);
            Check(!hub.GroupScriptPlaying && queue.Count == 1, "Disable clears playing flag and preserves pending order");
            go.SetActive(true);
            Check(hub.GroupScriptPlaying && Get<Coroutine>(hub, "presetReplyRoutine") != null, "Reenable resumes pending coroutine");
            int oldGeneration = Get<int>(hub, "presetReplyGeneration");
            stale = (IEnumerator)typeof(YYChatHub).GetMethod("DeliverPresetReplies", Hidden).Invoke(hub, new object[] { saved, oldGeneration });
            Check(stale.MoveNext(), "Old iterator reached its waiting boundary");
            hub.enabled = false;
            Check(!hub.GroupScriptPlaying && !Get<bool>(hub, "presetReplyRunning"), "Component disable also clears running");
            hub.enabled = true;
            var resumed = Get<Coroutine>(hub, "presetReplyRoutine");
            Check(hub.GroupScriptPlaying && resumed != null && Get<int>(hub, "presetReplyGeneration") != oldGeneration,
                "Component reenable starts a distinct generation");
            (stale as IDisposable)?.Dispose(); stale = null;
            Check(hub.GroupScriptPlaying && ReferenceEquals(resumed, Get<Coroutine>(hub, "presetReplyRoutine")) && queue.Count == 1,
                "Disposing old iterator cannot erase current playback or its queue");
            hub.enabled = false;
            State(hub, new YYState());
            hub.enabled = true;
            Check(!hub.GroupScriptPlaying && queue.Count == 0, "Queued replies from another save are discarded before resume");
            // No rendered frame elapses with an active delivery coroutine; no Receive/runtime call is needed.
            Debug.Log("LIFE_GROUP_PLAYBACK_PASS lifecycle and stale-finally checks. No authored delivery, save IO or physical UI acceptance.");
        }
        finally
        {
            go.SetActive(false);
            (stale as IDisposable)?.Dispose();
            UnityEngine.Object.Destroy(go);
            UnityEngine.Random.state = random;
        }
        yield return null;
    }
}
#endif
