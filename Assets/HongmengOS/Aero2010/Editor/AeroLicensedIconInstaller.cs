using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Michsky.DreamOS;
using TMPro;
namespace HongmengOS.Aero2010.Editor
{
 public static class AeroLicensedIconInstaller
 {
  public static int Install()
  {
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
   if(EditorApplication.isPlaying||scene.path!=AeroDesktopStyleEditor.ScenePath)throw new InvalidOperationException("2010desktop edit mode required");
   var source=GameObject.Find("hongmengos 2010 Desktop Source");if(source==null)throw new InvalidOperationException("LCD setup required");
   var library=AssetDatabase.LoadAssetAtPath<AppLibrary>("Assets/HongmengOS/Aero2010/Theme/Aero 2010 Apps.asset");
   if(library==null)throw new InvalidOperationException("Aero App Library required");
   var lookup=new Dictionary<Sprite,string>();
   foreach(var app in library.apps){if(!File.Exists(Path.Combine(Application.streamingAssetsPath,"HongmengOS-Aero2010/Oxygen",app.appTitle+".png")))throw new FileNotFoundException(app.appTitle);foreach(var sprite in new[]{app.appIconBig,app.appIconMedium,app.appIconSmall})if(sprite!=null)lookup[sprite]=app.appTitle;}
   var bindings=new List<AeroLicensedIconLoader.ImageBinding>();
   foreach(var image in source.GetComponentsInChildren<Image>(true))if(image.sprite!=null&&lookup.TryGetValue(image.sprite,out string key))bindings.Add(new AeroLicensedIconLoader.ImageBinding{image=image,iconKey=key});
   var loader=source.GetComponent<AeroLicensedIconLoader>()??source.AddComponent<AeroLicensedIconLoader>();loader.originalLibrary=library;loader.bindings=bindings.ToArray();
   var system=source.transform.Find("Desktop/Apps & Windows/Settings/Container/Content/Main Content/About/Content/Aero System Information");
   if(system!=null&&system.Find("Icon Attribution")==null){var t=new GameObject("Icon Attribution",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();t.transform.SetParent(system,false);t.gameObject.layer=5;t.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");t.fontSize=16;t.color=new Color32(61,91,120,255);t.text="Icons: Oxygen Team and contributors · LGPL-3.0-or-later\nLicense texts and editable artwork sources are included with this desktop.";t.raycastTarget=false;t.rectTransform.anchorMin=t.rectTransform.anchorMax=new Vector2(0,1);t.rectTransform.pivot=new Vector2(0,1);t.rectTransform.anchoredPosition=new Vector2(0,-704);t.rectTransform.sizeDelta=new Vector2(900,48);}
   EditorUtility.SetDirty(loader);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return bindings.Count;
  }
 }
}
