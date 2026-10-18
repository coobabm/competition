using System.Collections.Generic;
using LingGuangV05.Runtime;
using Michsky.DreamOS;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace LingGuangV05.Desktop
{
    // Discover hidden scene objects once, then keep them through explicit lifecycle registration.
    // Runtime factories creating never-enabled inactive clones must call RegisterTree on the clone.
    public static class DesktopWindowIndex
    {
        static readonly HashSet<WindowManager> knownWindows=new HashSet<WindowManager>();
        static readonly HashSet<ChapterOneDesktopBridge> knownBindings=new HashSet<ChapterOneDesktopBridge>();
        static List<WindowManager> windows=new List<WindowManager>();
        static List<ChapterOneDesktopBridge> bindings=new List<ChapterOneDesktopBridge>();
        static readonly List<GameObject> roots=new List<GameObject>();
        static readonly List<WindowManager> windowBuffer=new List<WindowManager>();
        static readonly List<ChapterOneDesktopBridge> bindingBuffer=new List<ChapterOneDesktopBridge>();
        static bool seeded,windowsDirty,bindingsDirty;
        static ChapterOneRuntime owner;
        static readonly Unity.Profiling.ProfilerMarker ScanMarker=new Unity.Profiling.ProfilerMarker("DesktopWindowIndex.DiscoverTree");
        public static int WindowScans{get;private set;} public static int BindingScans{get;private set;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            knownWindows.Clear();knownBindings.Clear();windows=new List<WindowManager>();bindings=new List<ChapterOneDesktopBridge>();
            roots.Clear();windowBuffer.Clear();bindingBuffer.Clear();owner=null;seeded=false;windowsDirty=bindingsDirty=false;WindowScans=BindingScans=0;
            SceneManager.sceneLoaded-=SceneLoaded;SceneManager.sceneLoaded+=SceneLoaded;
        }
        static void SceneLoaded(Scene scene,LoadSceneMode mode){DiscoverScene(scene);}
        static void DiscoverScene(Scene scene)
        {
            if(!scene.isLoaded)return;
            roots.Clear();scene.GetRootGameObjects(roots);
            foreach(var root in roots)RegisterTree(root);
            roots.Clear();
        }
        public static void RegisterTree(GameObject root)
        {
            if(root==null)return;
            using(ScanMarker.Auto())
            {
                windowBuffer.Clear();root.GetComponentsInChildren(true,windowBuffer);foreach(var item in windowBuffer)Register(item);windowBuffer.Clear();WindowScans++;
                bindingBuffer.Clear();root.GetComponentsInChildren(true,bindingBuffer);foreach(var item in bindingBuffer)Register(item);bindingBuffer.Clear();BindingScans++;
            }
        }
        public static void Register(WindowManager item){if(item!=null&&knownWindows.Add(item))windowsDirty=true;}
        public static void Register(ChapterOneDesktopBridge item){if(item!=null&&knownBindings.Add(item))bindingsDirty=true;}
        public static void Unregister(WindowManager item){if(knownWindows.Remove(item))windowsDirty=true;}
        public static void Unregister(ChapterOneDesktopBridge item){if(knownBindings.Remove(item))bindingsDirty=true;}
        static void EnsureSeed(ChapterOneRuntime runtime)
        {
            if(!seeded)
            {
                seeded=true;
                for(int i=0;i<SceneManager.sceneCount;i++)DiscoverScene(SceneManager.GetSceneAt(i));
            }
            // A replacement or persistent owner can live outside the ordinary loaded scenes.
            if(owner!=runtime){owner=runtime;if(runtime!=null)RegisterTree(runtime.transform.root.gameObject);}
        }
        public static List<WindowManager> Get(ChapterOneRuntime runtime)
        {
            EnsureSeed(runtime);
            // Never-enabled destroyed objects do not reliably receive OnDestroy; prune their fake-null refs.
            if(knownWindows.RemoveWhere(IsDeadWindow)>0)windowsDirty=true;
            if(windowsDirty){windows=new List<WindowManager>(knownWindows);windowsDirty=false;}
            return windows; // Existing callers finish their immutable snapshot even if registration occurs.
        }
        public static List<ChapterOneDesktopBridge> GetBindings(ChapterOneRuntime runtime)
        {
            EnsureSeed(runtime);
            if(knownBindings.RemoveWhere(IsDeadBinding)>0)bindingsDirty=true;
            if(bindingsDirty){bindings=new List<ChapterOneDesktopBridge>(knownBindings);bindingsDirty=false;}
            return bindings;
        }
        static bool IsDeadWindow(WindowManager item)=>item==null;
        static bool IsDeadBinding(ChapterOneDesktopBridge item)=>item==null;
    }
}
