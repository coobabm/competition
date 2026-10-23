using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem;
#endif
namespace LingGuangV05.Desktop.XingGuang
{
    // Changes only this game's existing OS window; restores its previous mode on leaving.
    public sealed class XgIdleNativeWindow : MonoBehaviour
    {
        bool active;
        bool topmost;
        public Vector2 RequestedSize => XgIdleWindow.WindowSize;
        public bool Topmost => topmost;
        // The design size is in DPI-logical pixels; the canvas scaler fills whatever the window becomes.
        Vector2 PixelSize => RequestedSize*(Screen.dpi>96?Mathf.Min(Screen.dpi/96f,3):1);
        int width,height;
        FullScreenMode mode;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [StructLayout(LayoutKind.Sequential)] struct Bounds { public int left,top,right,bottom; }
        [StructLayout(LayoutKind.Sequential)] struct Point { public int x,y; }
        [StructLayout(LayoutKind.Sequential)] struct MonitorInfo { public int size; public Bounds monitor,work; public uint flags; }
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int w,int hgt,uint flags);
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h,out Bounds b);
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr h,int index);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr h,int index,int value);
        [DllImport("user32.dll")] static extern bool GetCursorPos(out Point p);
        [DllImport("user32.dll")] static extern IntPtr MonitorFromWindow(IntPtr h,uint flags);
        [DllImport("user32.dll",EntryPoint="GetMonitorInfoW")] static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
        IntPtr handle;
        Bounds original;
        int style;
        bool captured;
        // A drag moves the window from Unity's own loop; a native caption drag would block it and swallow the button-up.
        bool dragging;
        Point dragCursor;
        Bounds dragWindow;
#endif
        // Off by default; only the player's explicit menu choice raises the compact window above others.
        public void SetTopmost(bool value)
        {
            topmost=value;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if(active&&captured)ApplyTopmost(value);
#endif
        }
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        void ApplyTopmost(bool value){SetWindowPos(handle,new IntPtr(value?-1:-2),0,0,0,0,0x0013);}
#endif
        public void Enter()
        {
            if(active)return;
            active=true;width=Screen.width;height=Screen.height;mode=Screen.fullScreenMode;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            using(var process=System.Diagnostics.Process.GetCurrentProcess())handle=process.MainWindowHandle;
            captured=handle!=IntPtr.Zero&&GetWindowRect(handle,out original);
            if(!captured){active=false;Debug.LogWarning("Compact window: OS handle unavailable; keeping the current window size.");return;}
            if(captured)style=GetWindowLong(handle,-16);
            var size=PixelSize;Screen.SetResolution((int)size.x,(int)size.y,FullScreenMode.Windowed);
            StartCoroutine(Position());
#endif
        }
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        IEnumerator Position()
        {
            yield return null;yield return new WaitForEndOfFrame();
            if(!active||handle==IntPtr.Zero)yield break;
            if(captured)SetWindowLong(handle,-16,style & ~0x00CF0000);
            // The work area of the monitor this window is on (nearest), not only the primary one.
            var info=new MonitorInfo{size=Marshal.SizeOf(typeof(MonitorInfo))};
            var monitor=MonitorFromWindow(handle,2);
            if(monitor!=IntPtr.Zero&&GetMonitorInfo(monitor,ref info))
            {
                var work=info.work;
                var size=PixelSize;int w=(int)size.x,h=(int)size.y;
                int x=Math.Max(work.left,work.right-w-16),y=Math.Max(work.top,work.bottom-h-16);
                SetWindowPos(handle,IntPtr.Zero,x,y,w,h,0x0034);
            }
            if(topmost)ApplyTopmost(true);
        }
#endif
        public void Restore()
        {
            if(!active)return;active=false;StopAllCoroutines();EndDrag();
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            Screen.SetResolution(width,height,mode);
            if(captured){if(topmost)ApplyTopmost(false);SetWindowLong(handle,-16,style);if(isActiveAndEnabled)StartCoroutine(RestorePosition());}
#endif
        }
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        IEnumerator RestorePosition()
        {
            yield return null;yield return new WaitForEndOfFrame();
            if(active||!captured)yield break;
            if(mode==FullScreenMode.Windowed)SetWindowPos(handle,IntPtr.Zero,original.left,original.top,original.right-original.left,original.bottom-original.top,0x0034);
        }
#endif
        public void BeginDrag()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            dragging=active&&handle!=IntPtr.Zero&&GetCursorPos(out dragCursor)&&GetWindowRect(handle,out dragWindow);
#endif
        }
        public void EndDrag()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            dragging=false;
#endif
        }
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // Screen-space cursor deltas: the pointer's window-relative position changes as the window follows it.
        void Update()
        {
            if(!dragging)return;
            if(!active||Mouse.current==null||!Mouse.current.leftButton.isPressed){dragging=false;return;}
            if(!GetCursorPos(out var p))return;
            SetWindowPos(handle,IntPtr.Zero,dragWindow.left+p.x-dragCursor.x,dragWindow.top+p.y-dragCursor.y,0,0,0x0015);
        }
#endif
        void OnDisable(){EndDrag();}
        void OnDestroy(){Restore();}
    }
    public sealed class XgIdleDragHandle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public XgIdleNativeWindow Window;
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)Window?.BeginDrag();}
        public void OnPointerUp(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)Window?.EndDrag();}
    }
}
