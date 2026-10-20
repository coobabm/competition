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
        public void Enter()
        {
            if(active)return;
            active=true;width=Screen.width;height=Screen.height;mode=Screen.fullScreenMode;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            using(var process=System.Diagnostics.Process.GetCurrentProcess())handle=process.MainWindowHandle;
            captured=handle!=IntPtr.Zero&&GetWindowRect(handle,out original);
            if(!captured){active=false;Debug.LogWarning("Compact window: OS handle unavailable; keeping the current window size.");return;}
            if(captured)style=GetWindowLong(handle,-16);
            Screen.SetResolution(288,168,FullScreenMode.Windowed);
            StartCoroutine(Position());
#endif
        }
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        IEnumerator Position()
        {
            yield return null;yield return new WaitForEndOfFrame();
            if(!active||handle==IntPtr.Zero)yield break;
            if(captured)SetWindowLong(handle,-16,style & ~0x00CF0000);
            if(SystemParametersInfo(0x0030,0,out var work,0))
                SetWindowPos(handle,IntPtr.Zero,work.right-300,work.bottom-180,288,168,0x0034);
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
