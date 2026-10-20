using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
#endif
namespace LingGuangV05.Desktop.XingGuang
{
    // Changes only this game's existing OS window; restores its previous mode on leaving.
    public sealed class XgIdleNativeWindow : MonoBehaviour
    {
        bool active;
        bool mini;
        public Vector2 RequestedSize => XgIdleWindow.WindowSize(mini);
        int width,height;
        FullScreenMode mode;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [StructLayout(LayoutKind.Sequential)] struct Bounds { public int left,top,right,bottom; }
        [DllImport("user32.dll")] static extern bool SystemParametersInfo(uint a,uint b,out Bounds c,uint d);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int w,int hgt,uint flags);
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h,out Bounds b);
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr h,int index);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr h,int index,int value);
        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h,uint msg,IntPtr w,IntPtr l);
        IntPtr handle;
        Bounds original;
        int style;
        bool captured;
#endif
        public void SetMini(bool value)
        {
            if(mini==value)return;
            mini=value;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if(active&&captured)
            {
                StopAllCoroutines();
                var size=RequestedSize;Screen.SetResolution((int)size.x,(int)size.y,FullScreenMode.Windowed);
                StartCoroutine(Position(false));
            }
#endif
        }
        public void Enter()
        {
            if(active)return;
            active=true;width=Screen.width;height=Screen.height;mode=Screen.fullScreenMode;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            using(var process=System.Diagnostics.Process.GetCurrentProcess())handle=process.MainWindowHandle;
            captured=handle!=IntPtr.Zero&&GetWindowRect(handle,out original);
            if(!captured){active=false;Debug.LogWarning("Compact window: OS handle unavailable; keeping the current window size.");return;}
            if(captured)style=GetWindowLong(handle,-16);
            var size=RequestedSize;Screen.SetResolution((int)size.x,(int)size.y,FullScreenMode.Windowed);
            StartCoroutine(Position(true));
#endif
        }
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        IEnumerator Position(bool initial)
        {
            yield return null;yield return new WaitForEndOfFrame();
            if(!active||handle==IntPtr.Zero)yield break;
            if(captured)SetWindowLong(handle,-16,style & ~0x00CF0000);
            if(SystemParametersInfo(0x0030,0,out var work,0))
            {
                var size=RequestedSize;int w=(int)size.x,h=(int)size.y;
                Bounds current=default;bool keep=!initial&&GetWindowRect(handle,out current);
                int x=keep?current.left:work.right-w-12,y=keep?current.top:work.bottom-h-12;
                x=Math.Max(work.left,Math.Min(x,work.right-w));y=Math.Max(work.top,Math.Min(y,work.bottom-h));
                SetWindowPos(handle,IntPtr.Zero,x,y,w,h,0x0034);
            }
        }
#endif
        public void Restore()
        {
            if(!active)return;active=false;StopAllCoroutines();
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            Screen.SetResolution(width,height,mode);
            if(captured){SetWindowLong(handle,-16,style);if(isActiveAndEnabled)StartCoroutine(RestorePosition());}
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
        public void Drag()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if(active&&handle!=IntPtr.Zero){ReleaseCapture();SendMessage(handle,0x00A1,new IntPtr(2),IntPtr.Zero);}
#endif
        }
        void OnDestroy(){Restore();}
    }
    public sealed class XgIdleDragHandle : MonoBehaviour, IPointerDownHandler
    {
        public XgIdleNativeWindow Window;
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)Window?.Drag();}
    }
}
