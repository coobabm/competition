using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LingGuangV05.UI.Tests
{
    public class RemovedYyIntroTests
    {
        [Test]
        public void LaoZhouStartsWithoutTheRemovedTutorialMessage()
        {
            var asset = AssetDatabase.LoadMainAssetAtPath("Assets/LingGuangV05/Content/Native/LaoZhou.asset");
            Assert.That(asset, Is.Not.Null);
            var messages = new SerializedObject(asset).FindProperty("messageList");
            for (int i = 0; i < messages.arraySize; i++)
                Assert.That(messages.GetArrayElementAtIndex(i).FindPropertyRelative("messageContent").stringValue,
                    Does.Not.StartWith("我是老周。先去邮件里标注"));
        }
        [Test]
        public void RemovedOpeningHasNoEnglishCatalogEntry()
        {
            var catalog = Resources.Load<TextAsset>("Localization/GameplayText");
            Assert.That(catalog.text, Does.Not.Contain("I'm Lao Zhou. Start by labeling in Mail"));
        }
    }
}
