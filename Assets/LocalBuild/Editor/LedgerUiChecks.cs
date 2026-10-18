#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;

// Embed after the existing Sidebar/Crawler fixture. No scene opening, save/profile
// access, editor update subscription or disk writes: the caller owns all of those.
public static class LedgerUiChecks
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static T Field<T>(object owner,string name)=>(T)owner.GetType().GetField(name,Flags).GetValue(owner);
    static void Require(bool ok,string why){if(!ok)throw new InvalidOperationException("Ledger UI: "+why);}
    static void Bind(XingGuangController ctl,XgSim sim,bool english)
    {
        sim.English=english;
        typeof(XingGuangController).GetProperty("Sim",Flags).GetSetMethod(true).Invoke(ctl,new object[]{sim});
        ctl.View.Bind(ctl);ctl.View.Open("train");ctl.View.Refresh(true);
    }
    static XgSim Fresh()
    {
        var sim=new XgSim();sim.BeginOpening();sim.S.teachingFlow=true;sim.S.handCorrect=12;
        sim.S.labels.Add(new XgLabelCount{dataset=XgMemes.SenseDesk,count=12});
        sim.S.trainingLessonObservedStage=sim.S.stage; // Isolate unrelated stage-entry quizzes.
        sim.S.teachIntroSaid=sim.S.teachThirdSaid=sim.S.teachTreeSaid=sim.S.teachTrainSaid=true;
        sim.S.spiderIntroDone=true;sim.S.unlocked.Add("train.curve");return sim;
    }
    static bool Visible(Component c)=>c!=null&&c.gameObject.activeInHierarchy;
    static void AssertToast(XingGuangView view,bool expected)
    {
        var toast=Field<TMP_Text>(view,"toast");
        Require(toast.transform.parent.gameObject.activeSelf==expected,"toast visibility after rebind/action");
        if(!expected){Require(toast.text=="","stale message text cleared");return;}
        Require(!toast.raycastTarget,"notice must not intercept pointer");
        var root=Field<RectTransform>(view,"root");
        var corners=new Vector3[4];((RectTransform)toast.transform.parent).GetWorldCorners(corners);
        float top=float.NegativeInfinity;foreach(var p in corners)top=Mathf.Max(top,root.InverseTransformPoint(p).y);
        foreach(var target in new[]{(RectTransform)root.Find("Pages"),Field<XgBtn>(view.Train,"train").rt,Field<RectTransform>(view.Train,"ledgerCard")})
        {
            target.GetWorldCorners(corners);float bottom=float.PositiveInfinity;
            foreach(var p in corners)bottom=Mathf.Min(bottom,root.InverseTransformPoint(p).y);
            Require(top<=bottom+.1f,"toast stays below content and controls: "+target.name);
        }
    }
    static void Readable(TMP_Text text,string why)
    {
        Require(Visible(text)&&!string.IsNullOrEmpty(text.text),why+" visible text");
        text.ForceMeshUpdate(true,true);
        Require(!text.isTextOverflowing,why+" no overflow");
        var size=text.GetPreferredValues(text.text,text.rectTransform.rect.width,0);
        Require(size.y<=text.rectTransform.rect.height+.5f,why+" readable height");
        if(text.textWrappingMode==TextWrappingModes.NoWrap)
            Require(text.GetPreferredValues(text.text).x<=text.rectTransform.rect.width+.5f,why+" readable width");
    }
    static void AssertHeader(XingGuangView view,bool detailed,bool money)
    {
        Require(Visible(Field<RectTransform>(view,"moneyRect"))==money,"wallet visibility");
        Require(Visible(Field<TMP_Text>(view,"statIncome"))==detailed,"income visibility");
        Require(Visible(Field<TMP_Text>(view,"statPower"))==detailed,"power visibility");
        if(money)Readable(Field<TMP_Text>(view,"statMoney"),"wallet");
        if(detailed){Readable(Field<TMP_Text>(view,"statIncome"),"income");Readable(Field<TMP_Text>(view,"statPower"),"power");}
    }
    static void AssertBook(XingGuangView view,bool open)
    {
        Require(Visible(Field<RectTransform>(view.Train,"ledgerCard"))==open,"ledger body visibility");
        var seal=Field<XgTrainingSeal>(view.Train,"ledgerSeal");
        Require(Visible(seal)==!open,"ledger seal visibility");
        if(!open){Readable(seal.NameLabel,"seal name");Readable(seal.DescriptionLabel,"seal sentence");Readable(seal.PriceLabel,"seal price");}
        if(open)
        {
            var button=Field<XgBtn>(view.Train,"fundsBook");
            Require(button.label.text==(GameText.IsEnglish?"Accounts":"查看资金账本"),"account button follows live language");
            Readable(button.label,"account button");
        }
        // Transaction prices must not be gated together with detailed account data.
        Readable(Field<TMP_Text>(view.Train,"trainBill"),"training price before payment");
    }
    // setEnglish updates the caller's existing GameText binding. capture saves the LCD
    // PNG and immediately persists its report index. click uses the existing LCD bridge.
    // Call MoveNext once per actual rendered frame, not a tight Editor update loop.
    public static IEnumerator Frames(XingGuangController ctl,Action<bool> setEnglish,
        Action<RectTransform> click,Action<string> capture)
    {
        var runtime=ctl.runtime;
        Require(runtime!=null&&!runtime.useDiskSave&&!runtime.tickEnabled&&!runtime.TestMode,"isolated live-host fixture required");
        Require(setEnglish!=null&&click!=null&&capture!=null,"locale, real pointer and capture callbacks required");
        var originalSim=ctl.Sim;string originalHouse=JsonUtility.ToJson(runtime.Sim.S);bool wasEnabled=ctl.enabled;
        var window=ctl.Window.windowContainer;Vector2 oldSize=window.sizeDelta;string oldTab=ctl.View.Tab;
        ctl.enabled=false; // Freeze simulation/catch-up, retain visible real view and real host.
        try
        {
            for(int fixture=0;fixture<4;fixture++)
            {
                bool english=fixture>=2,large=fixture%2==1;setEnglish(english);
                window.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,large?1400:960);
                window.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,large?900:600);
                string tag="ledger-"+(english?"en":"zh")+"-"+(large?"1400x900":"960x600");
                var house=runtime.Sim.S;house.money=1000;house.unpaidPower=false;house.breakerTripped=false;house.landlordCut=false;house.debtDays=0;house.rentOutstanding=0;
                var fresh=Fresh();string freshJson=JsonUtility.ToJson(fresh.S);Bind(ctl,fresh,english);
                for(int n=0;n<4;n++)yield return null;
                capture(tag+"-01-unbought.png");AssertHeader(ctl.View,false,false);AssertBook(ctl.View,false);AssertToast(ctl.View,false);
                var seal=Field<XgTrainingSeal>(ctl.View.Train,"ledgerSeal");double before=house.money;
                click(seal.Control.rt);
                for(int n=0;n<3;n++)yield return null;
                capture(tag+"-02-tech-details.png");AssertHeader(ctl.View,false,true);
                Require(ctl.View.Tab=="tree"&&house.money==before&&!fresh.Has(XgSim.LedgerNode),"seal pointer navigates without spending");
                var shown=Field<object>(ctl.View.Tree,"detailShown");
                Require(shown!=null&&(bool)shown.GetType().GetMethod("Covers",Flags).Invoke(shown,new object[]{XgSim.LedgerNode}),"real ledger detail card selected");
                Require(Visible(Field<RectTransform>(ctl.View.Tree,"tooltip")),"real ledger detail card visible");

                ctl.View.Open("train");house.money=14;ctl.View.Refresh(true);
                for(int n=0;n<3;n++)yield return null;
                capture(tag+"-03-low-funds.png");AssertHeader(ctl.View,false,false);Readable(Field<TMP_Text>(ctl.View,"fundsWarning"),"low balance warning");
                var node=XgCatalog.Node(XgSim.LedgerNode);double price=fresh.NodeCost(node);
                // Fixture has no coupons/relics: 14 must be insufficient for ordinary ¥15.
                Require(price==15&&!fresh.BuyNode(node.id,ctl.Host)&&house.money==14,"real insufficient purchase preserves funds");
                house.unpaidPower=true;house.billDue=5;ctl.View.Refresh(true);
                for(int n=0;n<3;n++)yield return null;
                capture(tag+"-04-power-arrears.png");AssertHeader(ctl.View,false,false);Readable(Field<TMP_Text>(ctl.View,"fundsWarning"),"power arrears warning");
                house.unpaidPower=false;house.billDue=0;house.rentOutstanding=100;house.debtDays=2;ctl.View.Refresh(true);
                for(int n=0;n<3;n++)yield return null;
                capture(tag+"-05-cut-risk.png");Readable(Field<TMP_Text>(ctl.View,"fundsWarning"),"power cut risk warning");
                house.rentOutstanding=0;house.debtDays=0;house.money=1000;
                before=house.money;Require(fresh.BuyNode(node.id,ctl.Host),"official BuyNode succeeds");
                Require(Math.Abs(before-house.money-price)<1e-8,"real host charges exact current price once");
                double boughtBalance=house.money;Require(!fresh.BuyNode(node.id,ctl.Host)&&house.money==boughtBalance,"duplicate purchase cannot charge twice");
                ctl.View.Refresh(true);for(int n=0;n<4;n++)yield return null;
                capture(tag+"-06-owned.png");AssertHeader(ctl.View,true,true);AssertBook(ctl.View,true);AssertToast(ctl.View,true);
                var oldState=JsonUtility.FromJson<XgState>(freshJson);oldState.ledgerAccessVersion=0;
                var migrated=new XgSim(oldState);Require(migrated.Has(node.id),"old serialized fixture preserves existing header access");
                Bind(ctl,migrated,english);for(int n=0;n<3;n++)yield return null;
                capture(tag+"-07-old-save.png");AssertHeader(ctl.View,true,true);AssertBook(ctl.View,true);AssertToast(ctl.View,false);
                Bind(ctl,new XgSim(JsonUtility.FromJson<XgState>(freshJson)),english);for(int n=0;n<3;n++)yield return null;
                capture(tag+"-08-new-again.png");AssertHeader(ctl.View,false,false);AssertBook(ctl.View,false);AssertToast(ctl.View,false);
                var pages=(RectTransform)Field<RectTransform>(ctl.View,"root").Find("Pages");Vector2 pageMin=pages.offsetMin,pageMax=pages.offsetMax;
                ctl.View.ShowToast(english?"Notice":"提示",30);AssertToast(ctl.View,true);
                Require(pages.offsetMin==pageMin&&pages.offsetMax==pageMax,"frequent notices never resize pages");
                ctl.Window.CloseWindow();for(int n=0;n<3;n++)yield return null;
                AssertToast(ctl.View,false);ctl.Window.OpenWindow();for(int n=0;n<3;n++)yield return null;
                AssertToast(ctl.View,false);
            }
            Debug.Log("LedgerUiChecks PASS: 4 locale/size fixtures, 32 captures, real seal routing, official purchase/duplicate guard, old/new view rebind.");
        }
        finally
        {
            JsonUtility.FromJsonOverwrite(originalHouse,runtime.Sim.S);
            window.sizeDelta=oldSize;setEnglish(originalSim.English);
            Bind(ctl,originalSim,originalSim.English);ctl.View.Open(oldTab);ctl.enabled=wasEnabled;
            if(!ctl.Window.isOn)ctl.Window.OpenWindow();
        }
    }
}
#endif
