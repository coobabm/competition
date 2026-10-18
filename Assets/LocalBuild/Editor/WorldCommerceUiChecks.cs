using System;
using System.Linq;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.Taohuo;
using LingGuangV05.Desktop.XingGuang;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public static class WorldCommerceUiChecks
{
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static FieldInfo Field(object o,string n){for(var t=o.GetType();t!=null;t=t.BaseType){var f=t.GetField(n,Flags);if(f!=null)return f;}throw new Exception(n);}
 static void Set(object o,string n,object v)=>Field(o,n).SetValue(o,v);
 static object Get(object o,string n)=>Field(o,n).GetValue(o);
 static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,Flags).Invoke(o,args);
 static void Check(bool yes,string why){if(!yes)throw new Exception(why);}
 static RectTransform Rect(Transform p,string n,Vector2 size){var g=new GameObject(n,typeof(RectTransform));g.transform.SetParent(p,false);var r=(RectTransform)g.transform;r.sizeDelta=size;return r;}
 [MenuItem("LingGuang/Checks/Commerce UI")]
 public static void Run(){RunLocale(false);RunLocale(true);Debug.Log("WorldCommerceUiChecks PASS: actual shop/tech handlers and labels, one wallet, legacy/reload ownership, actual funds window due/partial/settled and no debit on refresh, both languages.");}
 static void RunLocale(bool en)
 {
  using(var language=GameText.Bind(()=>en?GameText.English:GameText.Chinese)){
  Check(UnityEngine.Object.FindAnyObjectByType<XingGuangController>()==null,"Run in an isolated editor without a live controller");
  var old=typeof(XingGuangController).GetProperty("Instance").GetValue(null);
  var go=new GameObject("CommerceUiIsolated",typeof(RectTransform));go.SetActive(false);
  try{
   var rt=go.AddComponent<ChapterOneRuntime>();rt.enabled=false;var economy=new ChapterOneSim();rt.SetSimulationForTests(economy);typeof(ChapterOneRuntime).GetProperty("TestMode").SetValue(rt,false); // Disk saving remains disabled by SetSimulationForTests.
   economy.S.prologue=2;economy.S.appInstallState=2;economy.S.money=100000;
   var controller=go.AddComponent<XingGuangController>();controller.enabled=false;controller.runtime=rt;
   var sim=new XgSim();sim.English=en;sim.S.stage=6;sim.S.teachingFlow=false;sim.S.unlocked.Add("train.curve");typeof(XingGuangController).GetProperty("Sim").SetValue(controller,sim);typeof(XingGuangController).GetProperty("Host").SetValue(controller,new XingGuangHost(rt));
   var shopGo=Rect(go.transform,"Shop",new Vector2(1200,800)).gameObject;var shop=shopGo.AddComponent<TaohuoView>();shop.enabled=false;Set(shop,"holder",(RectTransform)shopGo.transform);var shopWindow=shopGo.AddComponent<Michsky.DreamOS.WindowManager>();shopWindow.enabled=false;shopWindow.isOn=true;Set(shop,"window",shopWindow);Set(shop,"runtime",rt);Set(shop,"lab",controller);
   go.SetActive(true);Check(UnityEngine.Object.FindAnyObjectByType<XingGuangController>()==controller,"shop resolves isolated controller");
   Check(shop.EnsureReady(),"actual shop builds");Call(shop,"Show","data");Call(shop,"Update");
   var list=(RectTransform)Get(shop,"list");var paid=XgSim.PersonaPacks.Where(p=>p.price>0).Take(2).ToArray();Check(paid.Length==2,"two paid packs available");
   Button ShopButton(string id)=>list.GetComponentsInChildren<Button>(true).First(b=>b.name=="Buy"&&b.transform.parent.parent.name=="persona."+id);
   var first=ShopButton(paid[0].id);double before=economy.S.money;first.onClick.Invoke();Call(shop,"UpdateLive",economy);
   Check(sim.OwnsPersonaPack(paid[0].id)&&Math.Abs(before-economy.S.money-paid[0].price)<.001,"actual shop button charges exact price once");
   Check(first.GetComponentInChildren<TMP_Text>().text==(en?"Owned":"已拥有"),"shop owned button label");before=economy.S.money;first.onClick.Invoke();Check(economy.S.money==before,"repeat shop click never charges");
   var area=Rect(go.transform,"Tech",new Vector2(1300,800));var juice=go.AddComponent<XgJuice>();juice.enabled=false;juice.Reduced=true;juice.Muted=true;juice.Init(area,area,TMP_Settings.defaultFontAsset);
   var host=new PageHost(controller,juice);var tree=new XgTechTreePage();tree.Init(host,new XgUi(TMP_Settings.defaultFontAsset,null));tree.Build(area);tree.Refresh();
   XgTechNodeView Node(string id)=>area.GetComponentsInChildren<XgTechNodeView>(true).First(v=>v.Covers("persona."+id));
   Check(Node(paid[0].id).cost.text.Contains("✓"),"shop purchase paints real tech card owned");
   before=economy.S.money;var node=Node(paid[1].id);var pointer=new PointerEventData(null){button=PointerEventData.InputButton.Left};node.OnPointerDown(pointer);node.OnPointerClick(pointer);tree.Refresh();Call(shop,"UpdateLive",economy);
   Check(sim.OwnsPersonaPack(paid[1].id)&&Math.Abs(before-economy.S.money-paid[1].price)<.001,"actual tech pointer handler charges exact price once");Check(Node(paid[1].id).cost.text.Contains("✓")&&ShopButton(paid[1].id).GetComponentInChildren<TMP_Text>().text==(en?"Owned":"已拥有"),"tech purchase paints both stores owned");
   // Unity's actual save serializer: legacy node-only ownership, then round trip after the UI purchases.
   var restored=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S)));restored.English=en;
   Check(restored.OwnsPersonaPack(paid[0].id)&&restored.OwnsPersonaPack(paid[1].id),"Unity save round-trip retains both purchases");
   var legacy=new XgSim(JsonUtility.FromJson<XgState>("{\"stage\":6,\"unlocked\":[\"persona."+paid[0].id+"\"],\"personaPacks\":null}"));legacy.English=en;Check(legacy.OwnsPersonaPack(paid[0].id),"legacy node ownership imported");
   typeof(XingGuangController).GetProperty("Sim").SetValue(controller,legacy);Call(shop,"UpdateLive",economy);Check(ShopButton(paid[0].id).GetComponentInChildren<TMP_Text>().text==(en?"Owned":"已拥有"),"actual shop label after legacy save binding");
   CheckFunds(go,rt,en);
  }finally{UnityEngine.Object.DestroyImmediate(go);typeof(XingGuangController).GetProperty("Instance").SetValue(null,old);}
  }
 }
 static void CheckFunds(GameObject go,ChapterOneRuntime rt,bool en)
 {
  var sim=rt.Sim;GameCalendar.Advance(sim.S,GameCalendar.DayIndex(new DateTime(2016,6,2)));sim.SettleCalendar();sim.S.money=5000;sim.Tick(sim.Config.rentDaySeconds);
  Check(sim.Debt>0,"real rent invoice created");double due=sim.Debt;var book=go.AddComponent<FundsPanel>();book.enabled=false;double before=sim.S.money;book.Open();
  var text=(TMP_Text)Get(book,"summary");Check(((GameObject)Get(book,"panel")).activeSelf&&text.text.Contains(due.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)),"actual funds Open displays amount due");Check(sim.S.money==before,"opening ledger does not pay rent");
  Check(sim.TransferRent(100,out _),"partial rent transfer");Call(book,"Refresh");Check(text.text.Contains((due-100).ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)),"visible remaining balance after partial transfer");
  var loaded=new ChapterOneSim(JsonUtility.FromJson<GameState>(JsonUtility.ToJson(sim.S)));rt.SetSimulationForTests(loaded);Call(book,"Refresh");Check(text.text.Contains((due-100).ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)),"ledger survives real Unity partial-debt save reload");
  Check(loaded.TransferRent(loaded.Debt,out _),"settle remaining rent");Call(book,"Refresh");Check(text.text.Contains(en?"Rent settled":"房租已结清")&&!text.text.Contains(en?"Rent still due":"待付房租"),"settled ledger clears debt label");
  before=loaded.S.money;Call(book,"Refresh");Check(loaded.S.money==before,"refresh never transfers again");
  if(en)foreach(var field in new[]{"title","summary","hint","entries"})Check(!System.Text.RegularExpressions.Regex.IsMatch(((TMP_Text)Get(book,field)).text,"[\\u4e00-\\u9fff]"),"English ledger field "+field);
 }
 sealed class PageHost:IXgPageHost
 {
  public PageHost(XingGuangController c,XgJuice j){Controller=c;Juice=j;}
  public XingGuangController Controller{get;}public XgSim Sim=>Controller.Sim;public IXgHost Host=>Controller.Host;public XgJuice Juice{get;}public int BuyAmount{get;set;}=1;public bool Visible=>true;public string Tab=>"tree";public RectTransform ComboChip=>null;public void Refresh(bool force){}public void ShowToast(string t,float s){}public void ShowTab(string id){}public bool BuyNode(XgNode n,RectTransform r)=>Sim.BuyNode(n.id,Host);
 }
}
