using System.Collections;
using System.IO;
using HongmengOS.Aero2010;
using Michsky.DreamOS;
using UnityEngine;
using UnityEngine.UI;

public sealed class AeroIconLifetimeQa : MonoBehaviour
{
    [System.Serializable] public class Result { public bool complete; public bool cloneUsesRuntime; public bool cloneRestored; public bool originalsRestored; public bool noMissingSprites; }
    public Result report = new Result();
    public void Run() { StartCoroutine(Check()); }
    IEnumerator Check()
    {
        var loader = FindFirstObjectByType<AeroLicensedIconLoader>();
        var source = loader.transform.Find("Desktop/Desktop List/Settings/Icon Background/Icon");
        var originalLibrary = loader.originalLibrary;
        var originalBindings = loader.bindings;
        var clone = Instantiate(source.gameObject, loader.transform);
        clone.name = "QA Runtime Icon Clone";
        var app = clone.GetComponent<AppElement>();
        loader.RefreshBindings();
        report.cloneUsesRuntime = app.appLibrary == loader.RuntimeLibrary && clone.GetComponent<Image>().sprite.name.StartsWith("Oxygen runtime icon");
        Destroy(loader);
        yield return null;
        yield return null;
        report.cloneRestored = app.appLibrary == originalLibrary;
        report.originalsRestored = true;
        report.noMissingSprites = clone.GetComponent<Image>().sprite != null && !clone.GetComponent<Image>().sprite.name.StartsWith("Oxygen runtime icon");
        foreach (var b in originalBindings)
        {
            if (b.image == null) continue;
            report.originalsRestored &= b.image.sprite != null && !b.image.sprite.name.StartsWith("Oxygen runtime icon");
        }
        report.complete = true;
        File.WriteAllText("/Users/kk/competition/Evidence/HongmengOS-Aero2010/aero-icon-lifetime-qa.json", JsonUtility.ToJson(report, true));
        Destroy(clone);
    }
}
