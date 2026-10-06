using System;
using NUnit.Framework;
using UnityEngine;

namespace LingGuangV05.UI.Tests
{
    public class DesktopLocalizationTests
    {
        [Test]
        public void DisplayFacadeExistsForDynamicPresentation()
        {
            Assert.That(Type.GetType("LingGuangV05.Runtime.GameText, LingGuangV05.Runtime"), Is.Not.Null,
                "Dynamic game UI needs a shared facade over the native language manager.");
        }

        [Test]
        public void OwnedChineseAndEnglishAssetsExistWithoutEditingVendorAssets()
        {
            Assert.That(UnityEditor.AssetDatabase.LoadMainAssetAtPath("Assets/LingGuangV05/Localization/Chinese.asset"), Is.Not.Null);
            Assert.That(UnityEditor.AssetDatabase.LoadMainAssetAtPath("Assets/LingGuangV05/Localization/English.asset"), Is.Not.Null);
        }
    }
}
