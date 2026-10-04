using System;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop
{
    [Serializable] public sealed class NativeAppText { public string id; public TMP_Text control; }
    [Serializable] public sealed class NativeAppButton { public string id; public ButtonManager control; }
    [Serializable] public sealed class NativeAppInput { public string id; public TMP_InputField control; }
    [Serializable] public sealed class NativeAppImage { public string id; public Image control; }
    [Serializable] public sealed class NativeAppPage { public string id; public GameObject page; public ButtonManager tabButton; }
    [Serializable] public sealed class NativeAppBindings
    {
        public NativeAppText[] texts=Array.Empty<NativeAppText>();
        public NativeAppButton[] buttons=Array.Empty<NativeAppButton>();
        public NativeAppInput[] inputs=Array.Empty<NativeAppInput>();
        public NativeAppImage[] images=Array.Empty<NativeAppImage>();
        public NativeAppPage[] pages=Array.Empty<NativeAppPage>();
        public RectTransform boardArea;
        public TMP_FontAsset font;
    }
}
