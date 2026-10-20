using System;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Core.Girlfriend;
using LingGuangV05.Runtime;
using LingGuangV05.Desktop.YY;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using LingGuangV05.Core.Forum;
using LingGuangV05.Desktop.Bodu;
public static class WorldYyGiftRoutingChecks
{
    static void Require(bool value, string name) { if (!value) throw new Exception(name); }
    static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(target, value);
    [MenuItem("LingGuang/Checks/World all interactions and photos")]
    public static void Run() { RunInteractions(); RunPhotos(); }
    public static void RunMonthlyRent()
    {
        var go = new GameObject("MonthlyRentDeliveryChecks"); go.SetActive(false);
        try
        {
            var sim = new ChapterOneSim(); sim.S.prologue = 2; sim.S.appInstallState = 2;
            GameCalendar.Advance(sim.S, GameCalendar.DayIndex(new DateTime(2016, 9, 1))); sim.SettleCalendar();
            Require(sim.S.rentInvoiceOutbox.Count == 1, "first-frame bill is durable before YY exists");
            sim = new ChapterOneSim(JsonUtility.FromJson<GameState>(JsonUtility.ToJson(sim.S)));
            var runtime = go.AddComponent<ChapterOneRuntime>(); runtime.SetSimulationForTests(sim);
            var hub = go.AddComponent<YYChatHub>(); Set(hub, "runtime", runtime);
            var state = new YYState(); state.conversations.Add(new YYConversation { id = "landlord" });
            typeof(YYChatHub).GetProperty("S").SetValue(hub, state);
            hub.FlushPendingRentInvoices();
            Require(sim.S.rentInvoiceOutbox.Count == 1 && state.conversations[0].messages.Count == 0, "cannot deliver into an unbound or previous save");
            Set(hub, "linked", sim); hub.FlushPendingRentInvoices();
            var conv = state.conversations[0];
            Require(conv.messages.Count == 2 && conv.unread == 1 && sim.S.rentInvoiceOutbox.Count == 0, "late YY initialization delivers text and card once");
            Require(conv.messages[1].rentInvoiceMonth == 201609 && conv.messages[1].transferAmount == 2100, "card identifies the actual billing month");
            hub.FlushPendingRentInvoices(); Require(conv.messages.Count == 2, "repeat Update does not duplicate");
            var replay = new RentInvoice { month = 201609, amount = 2100, total = 2100 };
            sim.S.rentInvoiceOutbox.Add(replay); hub.FlushPendingRentInvoices();
            Require(conv.messages.Count == 2 && conv.unread == 1 && sim.S.rentInvoiceOutbox.Count == 0, "persisted receipt deduplicates a recovered outbox");
            GameCalendar.Advance(sim.S, GameCalendar.DayIndex(new DateTime(2016, 12, 1))); sim.SettleCalendar();
            Require(sim.S.rentInvoiceOutbox.Count == 3, "October through December queued separately");
            hub.FlushPendingRentInvoices();
            Require(conv.messages.Count == 8 && conv.unread == 4, "each month delivers exactly one bill pair");
            Require(conv.messages[3].rentInvoiceMonth == 201610 && conv.messages[5].rentInvoiceMonth == 201611 && conv.messages[7].rentInvoiceMonth == 201612, "months delivered in order");
            Require(hub.CurrentRentRequest(conv.messages[7]) && !hub.CurrentRentRequest(conv.messages[1]), "only latest outstanding-total card is actionable");
            sim.S.money = 20000; Require(hub.TransferRent(100, out _), "partial transfer accepts current invoice");
            Require(sim.Debt == 9350 && hub.CurrentRentRequest(conv.messages[7]), "partial payment leaves the latest bill active");
            Require(hub.TransferRent(sim.Debt, out _), "remaining balance settles");
            Require(!hub.CurrentRentRequest(conv.messages[7]), "settled bill cannot be paid twice");
            var next = new ChapterOneSim(); runtime.SetSimulationForTests(next);
            next.S.prologue = 2; next.S.appInstallState = 2;
            GameCalendar.Advance(next.S, GameCalendar.DayIndex(new DateTime(2016, 6, 1))); next.SettleCalendar();
            int oldCount = conv.messages.Count; hub.FlushPendingRentInvoices();
            Require(conv.messages.Count == oldCount && next.S.rentInvoiceOutbox.Count == 1, "cross-save pending bill waits for matching chat state");
            Debug.Log("MonthlyRentDeliveryChecks PASS: durable first-frame invoice, JSON reload, matching owner, receipt deduplication, calendar catch-up, unread, partial/full payment, cross-save guard.");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
    [MenuItem("LingGuang/Checks/World interactions")]
    public static void RunInteractions()
    {
        // Inactive object prevents Awake from loading a real save. TestMode prevents disk writes.
        var go = new GameObject("WorldYyGiftRoutingChecks"); go.SetActive(false);
        try
        {
            var runtime = go.AddComponent<ChapterOneRuntime>();
            runtime.SetSimulationForTests(new ChapterOneSim());
            var hub = go.AddComponent<YYChatHub>(); Set(hub, "runtime", runtime);
            var state = new YYState();
            state.conversations.Add(new YYConversation { id = YYChatHub.GirlfriendId });
            typeof(YYChatHub).GetProperty("S").SetValue(hub, state);
            var gf = go.AddComponent<YYGirlfriend>(); Set(gf, "runtime", runtime); gf.hub = hub; hub.Girlfriend = gf;
            var now = GfNow.Of(runtime.Sim.S); GirlfriendRules.Begin(state.girlfriend, now, 12345);
            var g = state.girlfriend; var beat = QingwenArc.Beat("hello"); QingwenArc.Opened(g, beat, now.game);
            g.orders.Add(new GfGiftOrder { gift = GirlfriendRules.GiftIphone, state = GirlfriendRules.GiftHeld });
            Require(gf.GiveFromPanel(GirlfriendRules.GiftIphone), "panel accepts held gift");
            Require(g.orders[0].state == GirlfriendRules.GiftToHer, "panel -> hub.Send -> actual OnPlayerLine sends");
            Require(g.arcOpen == "hello" && !QingwenArc.Done(g, beat), "gift does not consume the pending arc");
            Require(!gf.GiveFromPanel(GirlfriendRules.GiftIphone), "cannot resend the same parcel");
            hub.Send(YYChatHub.GirlfriendId, QingwenArc.Choices(beat, false)[0]);
            Require(QingwenArc.Done(g, beat) && string.IsNullOrEmpty(g.arcOpen), "next real answer continues and completes the arc");
            CheckRentButtons(go, runtime, hub, state);
            CheckGroupPresetsAndPhotos(runtime, hub);
            CheckNewsDates();
            CheckForumVisibility();
            Debug.Log("WorldYyGiftRoutingChecks PASS: panel delivery, duplicate prevention, pending arc preserved and then answerable.");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    static object Get(object obj, string field) => obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
    static object Call(object obj, string method, params object[] args) => obj.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, args);
    static RectTransform Child(Transform parent, string name)
    {
        var child = new GameObject(name, typeof(RectTransform)); child.transform.SetParent(parent, false); return (RectTransform)child.transform;
    }
    static void CheckRentButtons(GameObject go, ChapterOneRuntime runtime, YYChatHub hub, YYState state)
    {
        var sim = runtime.Sim; sim.S.prologue = 2; sim.S.appInstallState = 2;
        GameCalendar.Advance(sim.S, GameCalendar.DayIndex(new DateTime(2016,6,2))); sim.SettleCalendar(); sim.S.money = 5000;
        sim.Tick(sim.Config.rentDaySeconds);
        Require(sim.Debt == 2100 && sim.S.bills.rent == 0, "invoice is not an automatic debit");
        state.selected = "landlord"; state.conversations.Add(new YYConversation { id = "landlord" }); hub.ReceiveRentRequest();
        var request = hub.Conversation("landlord").messages[0]; Require(hub.CurrentRentRequest(request), "unpaid request card active");
        var view = go.AddComponent<YYChatView>(); Set(view, "hub", hub); Set(view, "font", TMP_Settings.defaultFontAsset);
        var root = Child(go.transform, "RentTestRoot"); Child(root, "Tools"); Child(root, "Conversation"); Set(view,"root",root);
        Call(view,"RefreshRentTools");
        ((Button)Get(view,"rentTool")).onClick.Invoke();
        var panel = (RectTransform)Get(view,"rentPanel"); Require(panel.gameObject.activeSelf,"real toolbar button opens amount panel");
        var field = (TMP_InputField)Get(view,"rentAmount"); field.text = "100";
        Button pay = null; foreach(var button in panel.GetComponentsInChildren<Button>(true))
        { var text = button.GetComponentInChildren<TMP_Text>(true); if(text != null && (text.text == "转账" || text.text == "Transfer")) pay = button; }
        Require(pay != null,"real payment button exists"); double money = sim.S.money; pay.onClick.Invoke();
        Require(sim.Debt == 2000 && sim.S.money == money-100 && sim.S.bills.rent == 100,"real payment button submits partial amount");
        Require(hub.CurrentRentRequest(request),"partial payment leaves request active");
        hub.Send("landlord","转你 2000"); Require(sim.Debt == 0 && !hub.CurrentRentRequest(request),"typed remaining payment settles request");
        money = sim.S.money; hub.Send("landlord","转你 2000"); Require(sim.S.money == money,"repeat settled invoice cannot charge again");
    }
    static void CheckNewsDates()
    {
        var method = typeof(BoduView).GetMethod("AvailableNews", BindingFlags.NonPublic | BindingFlags.Static);
        var before = (IList)method.Invoke(null,new object[]{new DateTime(2015,12,31)}); Require(before.Count == 0,"no future 2016 headlines in 2015");
        int previous = 0;
        foreach(var today in new[]{new DateTime(2016,6,1),new DateTime(2016,9,1),new DateTime(2016,12,31)})
        {
            var rows=(IList)method.Invoke(null,new object[]{today}); Require(rows.Count >= previous,"news unlocks monotonically"); previous=rows.Count;
            DateTime last=DateTime.MaxValue;
            foreach(var row in rows){var date=DateTime.ParseExact((string)row.GetType().GetField("date").GetValue(row),"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture);Require(date.Year==2016 && date<=today && date<=last,"dated news only, newest first");last=date;}
        }
        Require(previous > 0,"real browser news resource loaded");
    }
    static void CheckForumVisibility()
    {
        var asset=Resources.Load<TextAsset>("LingGuangV05/Forum/forum"); Require(asset != null,"real forum resource exists");
        var lib=ForumLibrary.Parse(asset.text); int checkedThreads=0;
        foreach(var thread in lib.Threads) if(thread.id.StartsWith("ask_",StringComparison.Ordinal))
        {
            checkedThreads++; var flags=new System.Collections.Generic.HashSet<string>();
            var ctx=new ForumContext{stage=thread.minStage,today=new DateTime(2016,12,31),flag=flags.Contains};
            Require(thread.mine && thread.requires.Length>0,"data thread belongs to player and requires a post");
            Require(!ForumLibrary.Visible(thread,ctx),"data thread invisible before player posts");
            flags.Add(thread.requires); Require(ForumLibrary.Visible(thread,ctx),"post becomes visible after posted flag");
            foreach(var floor in thread.floors) if(floor.requires.Length>0)
            { Require(ForumLibrary.View(floor,ctx)==FloorView.Hidden,"reply absent before answer flag");flags.Add(floor.requires);Require(ForumLibrary.View(floor,ctx)!=FloorView.Hidden,"reply available after authored answer"); }
        }
        Require(checkedThreads>=2,"ImageNet and UN request threads checked");
    }

    static void CheckGroupPresetsAndPhotos(ChapterOneRuntime runtime, YYChatHub hub)
    {
        // Content checks first, on the full date-eligible pool of this fresh save (nothing sent yet).
        var presets=(IList)Call(hub,"AvailableGroupPresets");
        foreach(var preset in presets)
        {
            var type=preset.GetType(); string zh=(string)type.GetField("text").GetValue(preset), en=(string)type.GetField("textEn").GetValue(preset);
            Require(!string.IsNullOrWhiteSpace(zh)&&!string.IsNullOrWhiteSpace(en),"preset bilingual text");
            var replies=(string[])type.GetField("replies").GetValue(preset); var english=(string[])type.GetField("repliesEn").GetValue(preset);
            Require(replies!=null&&english!=null&&replies.Length==english.Length,"every preset reply translated");
            foreach(var text in replies)foreach(var banned in new[]{"摆渡众包","房租","训练包"})Require(!text.Contains(banned),"group private-topic exclusion");
            foreach(var banned in new[]{"摆渡众包","房租","训练包"})Require(!zh.Contains(banned),"preset private-topic exclusion");
        }
        CheckGroupPresetLifecycle(runtime, hub, presets);
    }
    static string PresetId(object preset) => (string)preset.GetType().GetField("id").GetValue(preset);
    static string PresetIdForText(IList presets, string shown)
    {
        foreach(var preset in presets)
        {
            var type=preset.GetType();
            if((string)type.GetField("text").GetValue(preset)==shown||(string)type.GetField("textEn").GetValue(preset)==shown) return PresetId(preset);
        }
        return null;
    }
    // Lifecycle semantics: at most three bubbles, frozen for the game day, never a sent id, no same-day refill.
    static void CheckGroupPresetLifecycle(ChapterOneRuntime runtime, YYChatHub hub, IList presets)
    {
        var life=hub.S.groupPresets; Require(life!=null,"YYState carries a group preset lifecycle");
        Require(presets.Count>=3,"real resource offers at least three eligible presets on this date");
        int today=GameCalendar.CurrentDay(runtime.Sim.S);
        var choices=(string[])Call(hub,"GroupPresetChoices");
        Require(choices.Length<=3,"never more than three preset bubbles");
        Require(choices.Length==Math.Min(3,presets.Count),"fresh day with nothing sent shows a full set of bubbles");
        var ids=new string[choices.Length];
        for(int i=0;i<choices.Length;i++)
        {
            ids[i]=PresetIdForText(presets,choices[i]); Require(ids[i]!=null,"every bubble is a real eligible preset");
            Require(!life.sentIds.Contains(ids[i]),"no bubble repeats a sent preset");
            for(int j=0;j<i;j++)Require(ids[j]!=ids[i],"bubbles are distinct");
        }
        // The getter is a pure read: calling it again changes neither the answer nor the save.
        bool hadSelection=life.hasDailySelection; int selectionDay=life.selectionDay, dailyCount=life.dailyIds.Count;
        var again=(string[])Call(hub,"GroupPresetChoices");
        Require(again.Length==choices.Length&&Array.TrueForAll(again,x=>Array.IndexOf(choices,x)>=0),"getter is stable within the day");
        Require(life.hasDailySelection==hadSelection&&life.selectionDay==selectionDay&&life.dailyIds.Count==dailyCount,"getter does not mutate the frozen selection");
        // One explicit freeze stores exactly the previewed ids for today; a second one is a no-op.
        bool frozeNow=(bool)Call(hub,"FreezeGroupPresetDay");
        Require(frozeNow==!(hadSelection&&selectionDay==today),"freeze reports a change only when today was not yet frozen");
        Require(life.hasDailySelection&&life.selectionDay==today,"selection is frozen for the current game day");
        Require(!(bool)Call(hub,"FreezeGroupPresetDay"),"second freeze on the same day is a no-op");
        var frozen=(string[])Call(hub,"GroupPresetChoices");
        Require(frozen.Length==choices.Length&&Array.TrueForAll(frozen,x=>Array.IndexOf(choices,x)>=0),"frozen ids equal the pure preview");
        // Sending one removes it for good and does not refill today, although the pool still has more presets.
        string sentText=choices[0], sentId=ids[0];
        Require((bool)Call(hub,"SendGroupPreset",sentText),"real preset send path accepts a shown bubble");
        Require(life.sentIds.Contains(sentId),"sent id persisted in the lifecycle state");
        var after=(string[])Call(hub,"GroupPresetChoices");
        Require(after.Length==choices.Length-1&&Array.IndexOf(after,sentText)<0,"sent bubble disappears without same-day refill");
        Require(Array.TrueForAll(after,x=>Array.IndexOf(choices,x)>=0),"remaining bubbles are today's frozen ones");
        foreach(var preset in (IList)Call(hub,"AvailableGroupPresets"))Require(PresetId(preset)!=sentId,"sent preset leaves the eligible pool");
        Require(!(bool)Call(hub,"SendGroupPreset",sentText),"a sent preset text is no longer a preset send");
        // Next game day: a fresh frozen set, still at most three and never the sent id.
        GameCalendar.Advance(runtime.Sim.S,today+1);
        var tomorrow=(string[])Call(hub,"GroupPresetChoices");
        Require(tomorrow.Length<=3&&Array.IndexOf(tomorrow,sentText)<0,"next day never cycles a sent preset back");
        Require((bool)Call(hub,"FreezeGroupPresetDay")&&life.selectionDay==today+1,"next day freezes once");
        Call(hub,"StopPresetReplies",true); // The inactive fixture never plays the queued replies.
    }
    [MenuItem("LingGuang/Checks/World group photos")]
    public static void RunPhotos()
    {
        var photos=(System.Collections.Generic.Dictionary<string,string>)typeof(YYChatView).GetField("GroupPhotos",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        var unique=new System.Collections.Generic.HashSet<string>(photos.Values); Require(unique.Count>=13,"all authored group photo mappings");
        foreach(var key in unique)Require(LingGuangV05.Desktop.Media.DesktopMedia.Picture(key)!=null,"group photo resource resolves: "+key);
        Debug.Log("WorldYyGiftRoutingChecks.RunPhotos PASS: all group photos resolve.");
    }
}
