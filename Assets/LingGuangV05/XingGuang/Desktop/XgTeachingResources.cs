using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using UnityEngine;
namespace LingGuangV05.Desktop.XingGuang
{
    static class XgTeachingResources
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Load()
        {
            var asset = Resources.Load<TextAsset>("LingGuangV05/Teaching/minigame_packs");
            if (asset == null) { Debug.LogError("Missing minigame_packs.json; retaining built-in packs"); return; }
            try
            {
                var file = JsonUtility.FromJson<XgTeachingPackFile>(asset.text);
                string error;
                if (Valid(file, out error)) XgTeachingPacks.All = file.packs;
                else Debug.LogError("Invalid teaching pack manifest: " + error + "; retaining built-in packs");
            }
            catch (Exception e) { Debug.LogError("Cannot parse teaching pack manifest; retaining built-in packs: " + e.Message); }
        }
        static bool SafeId(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            foreach (char c in id) if (!(c >= 'a' && c <= 'z') && !(c >= '0' && c <= '9') && c != '_') return false;
            return true;
        }
        static bool Valid(XgTeachingPackFile file, out string error)
        {
            error = "expected 20 packs";
            if (file == null || file.packs == null || file.packs.Length != 20) return false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            bool sudoku = false;
            foreach (var p in file.packs)
            {
                error = "null pack, unsafe ID or duplicate ID";
                if (p == null || !SafeId(p.id) || !ids.Add(p.id)) return false;
                error = p.id + ": missing required bilingual content or dataset";
                if (!SafeId(p.dataset) || string.IsNullOrWhiteSpace(p.name) || string.IsNullOrWhiteSpace(p.nameEn) ||
                    string.IsNullOrWhiteSpace(p.play) || string.IsNullOrWhiteSpace(p.playEn)) return false;
                error = p.id + ": stage must be 1 through 6";
                if (p.stage < 1 || p.stage > 6) return false;
                if (p.id == "sudoku")
                {
                    error = "sudoku must be purchased in stage 2 and use logic data";
                    if (p.stage != 2 || !p.buy || p.dataset != "logic") return false;
                    sudoku = true;
                }
            }
            error = "missing sudoku";
            if (!sudoku) return false;
            error = null;
            return true;
        }
    }
}
