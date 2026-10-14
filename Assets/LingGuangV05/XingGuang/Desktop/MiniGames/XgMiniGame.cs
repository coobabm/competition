using System;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang.MiniGames
{
    /// <summary>Native minigame surface; its owner (XgDeskGamePage) alone controls life cycle and settlement.</summary>
    public abstract class XgMiniGame : IDisposable
    {
        protected readonly XgMiniContext C;
        protected XgMiniGame(XgMiniContext context) { C = context; }
        public abstract void Build();
        public virtual void Tick(float dt) { }
        public virtual void Dispose() { }
    }

    public sealed class XgMiniContext
    {
        public const float Width = 900, Height = 480;
        public readonly RectTransform Root;
        public readonly XgUi Ui;
        public readonly XgSim Sim;
        public readonly IXgHost Host;
        public readonly string Id;
        public readonly System.Random Random;
        readonly Func<bool> active;
        readonly Func<double> skill;
        readonly Action<string> say;
        readonly Action<bool, double> report;
        bool disposed;
        public bool IsActive => !disposed && Root != null && Root.gameObject.activeInHierarchy && active();
        public double Skill => Math.Max(0, Math.Min(1, skill()));
        public XgMiniContext(RectTransform root, XgUi ui, XgSim sim, IXgHost host, string id,
            Func<bool> active, Func<double> skill, Action<string> say, Action<bool, double> report)
        {
            Root = root; Ui = ui; Sim = sim; Host = host; Id = id;
            this.active = active; this.skill = skill; this.say = say; this.report = report;
            Random = new System.Random(unchecked(Environment.TickCount ^ id.GetHashCode()));
        }
        public static void DestroyObject(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }
        public void Close() { disposed = true; }
        public void Say(string text) { if (!disposed) say(text); }
        /// <summary>One answered item of the desk (see XgSim.SettleMiniAction): the game never decides how much it is worth.</summary>
        public void Report(bool correct) { if (IsActive) report(correct, -1); }
        /// <summary>The same, with how long the answer took; a quick right answer earns the speed bonus.</summary>
        public void ReportTimed(bool correct, double seconds) { if (IsActive) report(correct, seconds); }
        public bool KeyDown(Key key) => IsActive && Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
        public bool KeyHeld(Key key) => IsActive && Keyboard.current != null && Keyboard.current[key].isPressed;
        public RectTransform Rect(string name, float x, float y, float w, float h) => XgUi.TopLeft(name, Root, x, y, w, h);
        public Image Box(string name, float x, float y, float w, float h, Color color)
        {
            var image = XgUi.Panel(Rect(name, x, y, w, h), color); image.raycastTarget = false; return image;
        }
        public TMP_Text Text(string name, float x, float y, float w, float h, string text, float size = 20, Color? color = null)
        {
            var t = Ui.Text(Rect(name, x, y, w, h), text, size, color ?? XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.enableAutoSizing = true; t.fontSizeMin = Math.Min(11, size); t.fontSizeMax = size;
            return t;
        }
        public XgBtn Button(string name, float x, float y, float w, float h, string text, Action click, float size = 20)
        {
            var b = Ui.Button(Root, text, () => { if (IsActive) click?.Invoke(); }, size);
            b.rt.name = name; XgUi.PlaceTopLeft(b, x, y, w, h);
            b.label.textWrappingMode = TextWrappingModes.Normal;
            b.label.overflowMode = TextOverflowModes.Ellipsis;
            b.label.enableAutoSizing = true; b.label.fontSizeMin = Math.Min(12, size); b.label.fontSizeMax = size;
            b.button.navigation = new Navigation { mode = Navigation.Mode.None };
            return b;
        }
        public XgMiniPointer Pointer(RectTransform rect, Action<Vector2> down, Action<Vector2> drag = null, Action<Vector2> up = null)
        {
            var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.raycastTarget = true;
            var p = rect.gameObject.AddComponent<XgMiniPointer>();
            p.Context = this; p.Down = down; p.Drag = drag; p.Up = up;
            return p;
        }
    }

    /// <summary>EventSystem coordinates, local top-left pixels; no screen-coordinate or global mouse hooks.</summary>
    public sealed class XgMiniPointer : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public XgMiniContext Context;
        public Action<Vector2> Down, Drag, Up;
        bool held;
        Vector2 Point(PointerEventData e)
        {
            var rect = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, e.position, e.pressEventCamera, out var p);
            return new Vector2(p.x - rect.rect.xMin, rect.rect.yMax - p.y);
        }
        public void OnPointerDown(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || Context == null) return;
            Context.Ui.window?.FocusToWindow();
            if (!Context.IsActive) return;
            held = true; Down?.Invoke(Point(e));
        }
        public void OnDrag(PointerEventData e) { if (held && Context != null && Context.IsActive) Drag?.Invoke(Point(e)); }
        public void OnPointerUp(PointerEventData e) { if (held && Context != null && Context.IsActive) Up?.Invoke(Point(e)); held = false; }
        void OnDisable() { held = false; }
    }
}
