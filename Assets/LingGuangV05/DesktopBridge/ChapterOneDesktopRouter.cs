using System;
using System.Collections.Generic;
using UnityEngine;

namespace LingGuangV05.Desktop
{
    /// <summary>Scene-local app registry; it owns no simulation or persistence state.</summary>
    [DisallowMultipleComponent]
    public sealed class ChapterOneDesktopRouter : MonoBehaviour
    {
        [SerializeField] private ChapterOneDesktopBridge[] bindings = Array.Empty<ChapterOneDesktopBridge>();
        public IReadOnlyList<ChapterOneDesktopBridge> Bindings => Array.AsReadOnly(bindings);

        public void Configure(ChapterOneDesktopBridge[] appBindings)
        {
            bindings = appBindings != null ? (ChapterOneDesktopBridge[])appBindings.Clone()
                : Array.Empty<ChapterOneDesktopBridge>();
        }

        public ChapterOneDesktopBridge Get(string appId)
        {
            if (string.IsNullOrWhiteSpace(appId)) return null;
            for (int i = 0; i < bindings.Length; i++)
                if (bindings[i] != null && string.Equals(bindings[i].ApplicationId, appId, StringComparison.Ordinal))
                    return bindings[i];
            return null;
        }

        public bool Open(string appId, string tab = null)
        {
            var binding = Get(appId);
            if (binding == null) return false;
            binding.OpenApp(tab);
            return true;
        }
    }
}
