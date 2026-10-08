using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LingGuangV05.UI.Tests
{
    public sealed class AeroWatermarkLayerTests
    {
        [Test]
        public void MainDesktopWatermarkStaysBetweenWallpaperAndApplications()
        {
            var preview = EditorSceneManager.OpenPreviewScene("Assets/LingGuangV05/Scenes/Main.unity");
            try
            {
                var mark = preview.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Single(transform => transform.name == "Aero Edition Mark");
                var wallpaper = mark.parent.Find("Wallpaper");
                var applications = mark.parent.Find("Apps & Windows");

                // Exercise the shipped scene's startup adapter rather than saving
                // editor-computed layout values back into the authored scene asset.
                var bridgeType = Type.GetType("HongmengOS.Aero2010.AeroLcdSourceBridge, Assembly-CSharp", true);
                var bridge = preview.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                    .Single(component => component != null && component.GetType() == bridgeType);
                bridgeType.GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(bridge, null);

                Assert.That(wallpaper, Is.Not.Null);
                Assert.That(applications, Is.Not.Null);
                Assert.That(mark.GetSiblingIndex(), Is.GreaterThan(wallpaper.GetSiblingIndex()),
                    "The edition mark must remain visible above the wallpaper.");
                Assert.That(mark.GetSiblingIndex(), Is.LessThan(applications.GetSiblingIndex()),
                    "Desktop decoration must not render over maximized application windows.");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }
    }
}
