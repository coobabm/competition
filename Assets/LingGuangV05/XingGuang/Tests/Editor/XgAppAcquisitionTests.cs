using NUnit.Framework;
using UnityEngine;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgAppAcquisitionTests
    {
        static XgAppCatalog Apps()=>new XgAppCatalog{apps=new System.Collections.Generic.List<XgAppDefinition>{new XgAppDefinition{id="zhongbao",how="group_link",stage=3,sizeMB=12.6}}};
        static XgSim Fresh(){var s=new XgSim();s.S.stage=3;s.Today=20160720;return s;}
        static XgGroupIntroCatalog Intros(string id="zhongbao_intro",string first="")=>new XgGroupIntroCatalog{scripts=new System.Collections.Generic.List<XgGroupIntroScript>{new XgGroupIntroScript{id=id,trigger=new XgGroupIntroTrigger{stageEnter=3,delaySec=10,firstTime=first}}}};
        [Test] public void FreshGameDoesNotPretendAppsOrIntrosWereUsed()
        {var s=Fresh();Assert.IsTrue(s.DesktopAppInstalled("gamecenter"));Assert.IsFalse(s.DesktopAppInstalled("taohuo"));Assert.IsFalse(s.AppReceipt("zhongbao").offered);Assert.IsFalse(s.IntroProgress("zhongbao_intro").completed);}
        [Test] public void LegacyEntitlementIsSeparateFromAPlayerInstallOrRead()
        {var s=new XgSim(new XgState{gradualSystems=false});var a=s.AppReceipt("zhongbao");Assert.IsTrue(a.installed&&a.legacyGranted);Assert.IsFalse(a.offered||a.downloaded);Assert.AreEqual(0,a.installStep);var p=s.IntroProgress("zhongbao_intro");Assert.IsTrue(p.legacySkipped);Assert.IsFalse(p.completed||p.skipped);}
        [Test] public void DownloadRequiresAnOfferedLinkAndCorrectStage()
        {var s=Fresh();Assert.IsFalse(s.StartDesktopDownload("zhongbao",Apps()));s.AppReceipt("zhongbao").offered=true;s.S.stage=2;Assert.IsFalse(s.StartDesktopDownload("zhongbao",Apps()));s.S.stage=3;Assert.IsTrue(s.StartDesktopDownload("zhongbao",Apps()));}
        [Test] public void PauseAndDisconnectedTimeCannotDownloadBytes()
        {var s=Fresh();s.AppReceipt("zhongbao").offered=true;s.StartDesktopDownload("zhongbao",Apps());s.TickAppAcquisition(1,true,true,Apps());s.TickAppAcquisition(1,false,false,Apps());Assert.AreEqual(0,s.AppReceipt("zhongbao").receivedMB);s.TickAppAcquisition(1,false,true,Apps());Assert.AreEqual(2.5,s.AppReceipt("zhongbao").receivedMB);}
        [Test] public void InstallNeedsCompletedBytesAndThreeSeparateActions()
        {var s=Fresh();var a=s.AppReceipt("zhongbao");a.offered=true;Assert.IsFalse(s.AdvanceDesktopInstall("zhongbao"));s.StartDesktopDownload("zhongbao",Apps());for(int i=0;i<5;i++)s.TickAppAcquisition(1,false,true,Apps());Assert.IsFalse(a.downloaded);s.TickAppAcquisition(.04,false,true,Apps());Assert.IsTrue(a.downloaded);for(int i=0;i<2;i++){Assert.IsTrue(s.AdvanceDesktopInstall("zhongbao"));Assert.IsFalse(a.installed);}Assert.IsTrue(s.AdvanceDesktopInstall("zhongbao"));Assert.IsTrue(a.installed);Assert.IsFalse(s.AdvanceDesktopInstall("zhongbao"));Assert.AreEqual(3,a.installStep);}
        [Test] public void SaveReloadRetainsPartialDownloadAndInstallStep()
        {var s=Fresh();var a=s.AppReceipt("zhongbao");a.offered=true;s.StartDesktopDownload("zhongbao",Apps());s.TickAppAcquisition(1,false,true,Apps());var clone=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(s.S)));Assert.AreEqual(2.5,clone.AppReceipt("zhongbao").receivedMB);Assert.IsFalse(clone.AppReceipt("zhongbao").legacyGranted);Assert.IsTrue(clone.AppReceipt("zhongbao").downloading);}
        [Test] public void IntroDelayOnlyCountsEligibleActiveTime()
        {var s=Fresh();var c=Intros();Assert.AreEqual("",s.NextGroupIntro(c,_=>false));for(int i=0;i<10;i++)s.TickAppAcquisition(1,true,true,Apps());Assert.AreEqual("",s.NextGroupIntro(c,_=>false));for(int i=0;i<10;i++)s.TickAppAcquisition(1,false,true,Apps());Assert.AreEqual("zhongbao_intro",s.NextGroupIntro(c,_=>false));}
        [Test] public void PrivateSampleReadinessCannotStartPublicDialogue()
        {var s=Fresh();var c=Intros("datapack_intro","examBlockedBySamples");s.S.appAcquisition.sampleHelpEligible=true;s.S.appAcquisition.activeSeconds=100;Assert.AreEqual("",s.NextGroupIntro(c,_=>false));s.S.appAcquisition.sampleHelpRequested=true;Assert.AreEqual("",s.NextGroupIntro(c,_=>false));s.S.appAcquisition.activeSeconds+=10;Assert.AreEqual("datapack_intro",s.NextGroupIntro(c,_=>false));}
        [Test] public void LateInstallCanStillUnlockMissedStageIntroduction()
        {var s=Fresh();s.S.stage=5;var c=Intros("enterprise_intro");c.scripts[0].trigger.stageEnter=4;c.scripts[0].trigger.requires="appInstalled:zhongbao";Assert.AreEqual("",s.NextGroupIntro(c,_=>false));s.AppReceipt("zhongbao").installed=true;s.NextGroupIntro(c,_=>false);s.S.appAcquisition.activeSeconds+=10;Assert.AreEqual("enterprise_intro",s.NextGroupIntro(c,_=>false));}
        [Test] public void CompletedOrMigratedIntroductionCannotReplay()
        {var s=Fresh();var c=Intros();s.S.appAcquisition.activeSeconds=100;s.IntroProgress("zhongbao_intro").completed=true;Assert.AreEqual("",s.NextGroupIntro(c,_=>false));s.IntroProgress("zhongbao_intro").completed=false;s.IntroProgress("zhongbao_intro").legacySkipped=true;Assert.AreEqual("",s.NextGroupIntro(c,_=>false));}
        [Test] public void InvalidAppIdsAndNonfiniteTimingHaveNoEffect()
        {var s=Fresh();int count=s.S.appAcquisition.apps.Count;Assert.IsNull(s.AppReceipt("unknown"));Assert.AreEqual(count,s.S.appAcquisition.apps.Count);s.TickAppAcquisition(double.NaN,false,true,Apps());s.TickAppAcquisition(double.PositiveInfinity,false,true,Apps());Assert.AreEqual(0,s.S.appAcquisition.activeSeconds);}
        [TestCase("{\"stage\":4,\"gradualSystems\":true}")]
        [TestCase("{\"stage\":4,\"gradualSystems\":true,\"appAcquisition\":{}}")]
        [TestCase("{\"stage\":4,\"gradualSystems\":true,\"appAcquisition\":{\"version\":1,\"apps\":[],\"intros\":[],\"deferredGroup\":[]}}")]
        public void UnityLegacyJsonAndEmptyPlaceholderPreserveOldEntitlements(string json)
        {
            var s=new XgSim(JsonUtility.FromJson<XgState>(json));
            var roundtrip=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(s.S)));
            foreach(var id in new[]{"taohuo","miaoyu","zhongbao"})
            {var a=roundtrip.AppReceipt(id);Assert.IsTrue(a.installed&&a.legacyGranted,id);Assert.IsFalse(a.offered||a.downloaded,id);Assert.AreEqual(0,a.installStep,id);Assert.IsFalse(roundtrip.IntroProgress(id+"_intro").completed,id);}
        }
        [Test] public void UnityFreshStateRoundtripIsNotMistakenForLegacyPlaceholder()
        {
            var s=new XgSim();s.S.stage=4;
            var clone=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(s.S)));
            Assert.IsTrue(clone.DesktopAppInstalled("gamecenter"));
            foreach(var id in new[]{"taohuo","miaoyu","zhongbao"})Assert.IsFalse(clone.DesktopAppInstalled(id),id);
            Assert.IsFalse(clone.IntroProgress("taohuo_intro").legacySkipped);
        }
    }
}
