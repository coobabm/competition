#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
// Local Editor diagnostics only. Call from Edit Mode before any Play-mode QA.
public static class EditorSearchWarmup
{
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Initialize the local search index in Edit Mode.");
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("UnityEditor.Search.SearchDatabase", false)).FirstOrDefault(t => t != null);
        if (type == null) throw new InvalidOperationException("Editor SearchDatabase type unavailable");
        var method = type.GetMethod("GetDefaultSearchDatabase", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null, Type.EmptyTypes, null);
        if (method == null) throw new InvalidOperationException("Editor default search initializer unavailable");
        var database = method.Invoke(null, null);
        if (database == null) throw new InvalidOperationException("Editor default search index was not created");
        Debug.Log("EDITOR_SEARCH_WARMUP_PASS: " + database);
    }
}
#endif
