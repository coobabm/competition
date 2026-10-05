#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using HongmengOS.Aero2010;
using LingGuangV05.Desktop.Casino;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace LingGuangV05.Editor
{
    /// <summary>Run only after the isolated casino fixture. Exercises actual LCD clicks/drag and captures motion phases.</summary>
    public static class CasinoJuicePlayQa
    {
        public static CasinoPlayQa.Report Result;
        public static bool Running;
        static ChapterOneRuntime runtime;
        static XingGuangController controller;
        static CasinoPage Page => UnityEngine.Object.FindAnyObjectByType<CasinoPage>();
        static string Dir => Path.Combine(Application.dataPath,"..","Evidence","CasinoJuice");
        public static void Begin()
        {
            runtime=UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>();
            controller=UnityEngine.Object.FindAnyObjectByType<XingGuangController>();
            if(!Application.isPlaying||runtime==null||runtime.useDiskSave||Page==null)throw new InvalidOperationException("Requires isolated Play fixture and visible casino");
            Result=new CasinoPlayQa.Report();Running=true;ChapterOneQaReloadGuard.LockForPlayQa(70);
            runtime.StartCoroutine(Guard(Run()));
        }
        static IEnumerator Guard(IEnumerator work)
        {
            while(true)
            {
                object next=null;bool more;
                try{more=work.MoveNext();if(more)next=work.Current;}
                catch(Exception e){Check("QA exception",false,e.ToString());break;}
                if(!more){Result.completed=true;break;}
                yield return next;
            }
            Running=false;Write();ChapterOneQaReloadGuard.Release();
        }
        static IEnumerator Run()
        {
            var sim=controller.Sim;
            float scale=Time.timeScale;
            sim.CloseCasinoAd();
            yield return Ready();
            Click("Game roulette");yield return Ready();yield return new WaitForSecondsRealtime(.15f);
            Click("Stake 500");Click("Choice 1");
            for(int round=0;round<2;round++)
            {
                sim.Casino.rng=1;sim.Clock+=3;
                yield return new WaitForSecondsRealtime(.15f);
                double money=runtime.Sim.S.money;int n=sim.Casino.rounds;
                Click("Place Bet");yield return new WaitForSecondsRealtime(1.32f);
                Check("Win committed once "+round,sim.Casino.rounds==n+1&&runtime.Sim.S.money==money+500);
                Check("Global clock unchanged on win "+round,Mathf.Approximately(Time.timeScale,scale));
                bool floaters=Page.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.name=="Float"&&t.gameObject.activeInHierarchy);
                Check("Win floaters visible "+round,floaters);
                Capture(round==0?"big-win-impact":"win-streak");
                yield return new WaitForSecondsRealtime(1.3f);
            }
            sim.Clock+=3;Click("Game blackjack");yield return Ready();
            Click("Stake 50");sim.Casino.rng=12345;yield return new WaitForSecondsRealtime(.15f);
            Click("Place Bet");yield return new WaitForSecondsRealtime(.18f);
            Check("Cards are travelling rather than appearing",Page.GetComponentsInChildren<CasinoCardMotion>().Any(m=>m.Busy&&m.Progress>0&&m.Progress<1));
            Capture("blackjack-dealing-mid");
            yield return Ready();
            Check("Initial four cards have landed",Page.GetComponentsInChildren<CasinoCardMotion>().Count(m=>!m.Busy)==4);
            Capture("blackjack-dealt");
            // Route a real drag through the monitor mapping, not directly through the callback.
            var card=Page.GetComponentsInChildren<CasinoCardMotion>().First(m=>m.name.StartsWith("Card 190"));
            var rt=(RectTransform)card.transform;var target=rt.anchoredPosition;
            var surface=UnityEngine.Object.FindAnyObjectByType<AeroLcdInputSurface>();
            var pos=ChapterOnePlayQa.PhysicalPoint(rt);
            var e=new PointerEventData(EventSystem.current){position=pos,button=PointerEventData.InputButton.Left,pointerId=-1};
            double cash=runtime.Sim.S.money;int count=sim.Casino.blackjack.player.Count;
            surface.OnPointerEnter(e);surface.OnPointerMove(e);surface.OnPointerDown(e);
            e.position=pos+new Vector2(50,15);surface.OnBeginDrag(e);surface.OnDrag(e);
            e.position=pos+new Vector2(90,30);surface.OnDrag(e);
            Check("Card responds to mapped drag",(rt.anchoredPosition-target).sqrMagnitude>1);
            Capture("blackjack-drag");surface.OnPointerUp(e);
            yield return new WaitForSecondsRealtime(.35f);
            Check("Drag returns without changing hand or wallet",(rt.anchoredPosition-target).sqrMagnitude<.1f&&!card.Busy&&count==sim.Casino.blackjack.player.Count&&cash==runtime.Sim.S.money);
            sim.Clock+=1;yield return new WaitForSecondsRealtime(.15f);
            Click("Blackjack stand");yield return new WaitForSecondsRealtime(.06f);
            Capture("dealer-hole-flip");
            float waited=0;
            while(Page.GetComponentsInChildren<CasinoCardMotion>().Any(m=>m.Busy)&&waited<5)
            {waited+=Time.unscaledDeltaTime;yield return null;}
            Check("Dealer reveal and extra deals finish",!Page.GetComponentsInChildren<CasinoCardMotion>().Any(m=>m.Busy));
            yield return new WaitForSecondsRealtime(1.6f);
            Check("Clock restored unchanged after effects",Mathf.Approximately(Time.timeScale,scale));
            Capture("blackjack-settled");
        }
        static void Click(string name)
        {
            var b=Page.GetComponentsInChildren<Button>(true).First(x=>x.name==name&&x.gameObject.activeInHierarchy);
            ChapterOnePlayQa.Click((RectTransform)b.transform);
        }
        static IEnumerator Ready()
        {
            float waited=0;
            while(Page!=null&&Page.GetComponentsInChildren<CasinoCardMotion>().Any(m=>m.Busy)&&waited<6)
            {waited+=Time.unscaledDeltaTime;yield return null;}
            yield return null; // let the 10 Hz button refresh observe the final pose
            yield return new WaitForSecondsRealtime(.12f);
        }
        static void Check(string name,bool ok,string detail="")
        {
            Result.checks.Add(new CasinoPlayQa.Check{name=name,passed=ok,detail=detail});
            if(ok)Result.passed++;else Result.failed++;Write();
        }
        static void Capture(string name){Directory.CreateDirectory(Dir);ScreenCapture.CaptureScreenshot(Path.Combine(Dir,name+".png"));}
        static void Write(){Directory.CreateDirectory(Dir);File.WriteAllText(Path.Combine(Dir,"play-feedback.json"),JsonUtility.ToJson(Result,true));}
    }
}
#endif
