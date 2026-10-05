using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
namespace LingGuangV05.UI.Tests
{
    public class CasinoMotionTests
    {
        static Type Find(string name) => Type.GetType(name + ", Assembly-CSharp", true);
        [Test] public void CasinoHitStopNeverChangesTheGlobalClock()
        {
            float before=Time.timeScale; var go=new GameObject("Local hit-stop test");
            try
            {
                var type=Find("LingGuangV05.Desktop.XingGuang.XgJuice");
                var fx=go.AddComponent(type);
                var local=type.GetField("LocalHitStop"); Assert.IsNotNull(local);
                local.SetValue(fx,true);type.GetMethod("HitStop").Invoke(fx,new object[]{60f});
                Assert.AreEqual(before,Time.timeScale);
                Assert.IsTrue((bool)type.GetProperty("IsHitStopped").GetValue(fx));
            }
            finally{UnityEngine.Object.DestroyImmediate(go);Time.timeScale=before;}
        }
        [TestCase(true)] [TestCase(false)]
        public void CardsTravelFromShoeThroughAnIntermediatePoseAndSettle(bool reveal)
        {
            var parent=new GameObject("Table",typeof(RectTransform));
            try
            {
                var go=new GameObject("Card",typeof(RectTransform));go.transform.SetParent(parent.transform,false);
                var rt=(RectTransform)go.transform;var target=new Vector2(180,-200);rt.anchoredPosition=target;
                var front=new GameObject("Front",typeof(RectTransform));front.transform.SetParent(rt,false);
                var back=new GameObject("Back",typeof(RectTransform));back.transform.SetParent(rt,false);
                var type=Find("LingGuangV05.Desktop.Casino.CasinoCardMotion");var motion=go.AddComponent(type);
                type.GetField("Front").SetValue(motion,(RectTransform)front.transform);
                type.GetField("Back").SetValue(motion,(RectTransform)back.transform);
                var shoe=new Vector2(680,-100);
                type.GetMethod("Deal").Invoke(motion,new object[]{shoe,0f,reveal});
                Assert.AreEqual(shoe,rt.anchoredPosition);Assert.IsFalse(front.activeSelf);
                type.GetMethod("Step").Invoke(motion,new object[]{.05f});
                Assert.AreNotEqual(shoe,rt.anchoredPosition);Assert.AreNotEqual(target,rt.anchoredPosition);
                for(int i=0;i<35;i++)type.GetMethod("Step").Invoke(motion,new object[]{.05f});
                Assert.AreEqual(target,rt.anchoredPosition);Assert.AreEqual(Vector3.one,rt.localScale);
                Assert.AreEqual(reveal,front.activeSelf);Assert.AreEqual(!reveal,back.activeSelf);
                Assert.IsFalse((bool)type.GetProperty("Busy").GetValue(motion));
            }
            finally{UnityEngine.Object.DestroyImmediate(parent);}
        }

        [Test] public void DisablingMidDealRestoresPoseAndInputWithoutRestartingFlight()
        {
            var go=new GameObject("Interrupted card",typeof(RectTransform));
            try
            {
                var rt=(RectTransform)go.transform;rt.anchoredPosition=new Vector2(100,-80);
                var type=Find("LingGuangV05.Desktop.Casino.CasinoCardMotion");
                var motion=(Behaviour)go.AddComponent(type);
                type.GetMethod("Deal").Invoke(motion,new object[]{new Vector2(600,-50),0f,true});
                type.GetMethod("Step").Invoke(motion,new object[]{.05f});
                motion.enabled=false;
                // EditMode does not dispatch ordinary MonoBehaviour lifecycle; exercise the same handler there.
                if(!Application.isPlaying)type.GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(motion,null);
                Assert.AreEqual(new Vector2(100,-80),rt.anchoredPosition);
                Assert.IsFalse((bool)type.GetProperty("Busy").GetValue(motion));
                Assert.IsTrue(go.GetComponent<CanvasGroup>().blocksRaycasts);
                motion.enabled=true;type.GetMethod("Step").Invoke(motion,new object[]{.05f});
                Assert.AreEqual(new Vector2(100,-80),rt.anchoredPosition);
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }

        [Test] public void HoleCardStaysHiddenThroughDelayThenFlipsBeforeFinishing()
        {
            var go=new GameObject("Hole card",typeof(RectTransform));
            try
            {
                var front=new GameObject("Front",typeof(RectTransform));front.transform.SetParent(go.transform,false);
                var back=new GameObject("Back",typeof(RectTransform));back.transform.SetParent(go.transform,false);
                var type=Find("LingGuangV05.Desktop.Casino.CasinoCardMotion");var motion=go.AddComponent(type);
                type.GetField("Front").SetValue(motion,(RectTransform)front.transform);type.GetField("Back").SetValue(motion,(RectTransform)back.transform);
                type.GetMethod("Rest").Invoke(motion,new object[]{false});
                type.GetMethod("Reveal").Invoke(motion,new object[]{.2f});
                for(int i=0;i<3;i++)type.GetMethod("Step").Invoke(motion,new object[]{.05f});
                Assert.IsFalse(front.activeSelf);Assert.IsTrue(back.activeSelf);
                for(int i=0;i<2;i++)type.GetMethod("Step").Invoke(motion,new object[]{.05f});
                Assert.Less(go.transform.localScale.x,1f,"visible edge-on flip, not a teleport");
                for(int i=0;i<8;i++)type.GetMethod("Step").Invoke(motion,new object[]{.05f});
                Assert.IsTrue(front.activeSelf);Assert.IsFalse(back.activeSelf);Assert.AreEqual(Vector3.one,go.transform.localScale);
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }

        [Test] public void ClosedPageDiscardsQueuedPayoutInsteadOfCelebratingAgain()
        {
            var go=new GameObject("Closed casino tab");
            try
            {
                var type=Find("LingGuangV05.Desktop.Casino.CasinoPage");var page=(Behaviour)go.AddComponent(type);
                var pending=type.GetField("pendingPayout",BindingFlags.Instance|BindingFlags.NonPublic);
                pending.SetValue(page,Activator.CreateInstance(pending.FieldType));
                page.enabled=false;
                if(!Application.isPlaying)type.GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(page,null);
                Assert.IsNull(pending.GetValue(page));
                page.enabled=true;Assert.IsNull(pending.GetValue(page));
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }

        [Test] public void RepeatedTabInitReusesPersistentAudioPool()
        {
            var type=Find("LingGuangV05.Desktop.XingGuang.XgJuice").GetNestedType("Sfx");
            var field=type.GetField("sources",BindingFlags.Static|BindingFlags.NonPublic);
            object original=field.GetValue(null);
            var host=new GameObject("Persistent casino audio test");var tab=new GameObject("Transient casino tab test");
            try
            {
                // Temporarily isolate the static slot; never destroy/replace live game's original sources.
                field.SetValue(null,null);
                type.GetMethod("Init").Invoke(null,new object[]{host,true});var first=field.GetValue(null);
                type.GetMethod("Init").Invoke(null,new object[]{tab,false});
                type.GetMethod("Init").Invoke(null,new object[]{host,true});
                Assert.AreSame(first,field.GetValue(null));Assert.AreEqual(6,host.GetComponents<AudioSource>().Length);
                Assert.AreEqual(0,tab.GetComponents<AudioSource>().Length);
            }
            finally{field.SetValue(null,original);UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(tab);}
        }
    }
}
