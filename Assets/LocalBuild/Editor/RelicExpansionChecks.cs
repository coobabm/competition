using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LingGuangV05.XingGuang;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

// Isolated state and wallet only. Does not open scenes or write player saves.
public static class RelicExpansionChecks
{
    sealed class Wallet : IXgHost
    {
        public double cash = 200000; public int payments;
        public double Compute => 1000; public double VramMB => 8192;
        public double Money => cash; public string Blocker => null;
        public bool Spend(double n) { if (n < 0 || n > cash) return false; cash -= n; payments++; return true; }
        public void Earn(double n) { cash += n; } public void Train(double seconds) { }
    }
    static int checks;
    static Func<XgState, XgState> roundTrip;
    static void Check(bool ok, string message) { checks++; if (!ok) throw new Exception("Relic expansion: " + message); }
    static void Equal(double expected, double actual, string message) => Check(Math.Abs(expected - actual) < 1e-8, message);
    static void Stage(XgSim sim, int stage)
    {
        sim.S.abilitiesEmerged = Enumerable.Range(1, stage).ToList();
        sim.S.stage = sim.S.stageVision = sim.S.stageSequence = stage;
    }
    static XgSim Copy(XgSim sim) => new XgSim(roundTrip(sim.S));
    static void SameEffects(XgRelicFx expected, XgRelicFx actual, string context)
    {
        foreach (var field in typeof(XgRelicFx).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            var a = field.GetValue(expected); var b = field.GetValue(actual);
            if (field.FieldType == typeof(double)) Equal((double)a, (double)b, context + " / " + field.Name);
            else Check(Equals(a, b), context + " / " + field.Name);
        }
    }
#if UNITY_EDITOR
    [MenuItem("Tools/Local Verification/Relic expansion purchase and persistence")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Run in isolated EditMode.");
        int count = Verify(s => JsonUtility.FromJson<XgState>(JsonUtility.ToJson(s)));
        VerifyDisk();
        Debug.Log("RelicExpansionChecks PASS: " + count + " assertions. Actual isolated purchases and JsonUtility roundtrips; no live UI or disk-save claim.");
    }
    static void VerifyDisk()
    {
        string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "LingGuang-RelicDisk-" + Guid.NewGuid().ToString("N"));
        var store = new LingGuangV05.Runtime.Persistence.AtomicSaveStore(root);
        var lab = new XgSim(); var wallet = new Wallet();
        lab.GrantRelic("redbull"); lab.EquipRelic("redbull", 3);
        var house = new LingGuangV05.Core.ChapterOneSim();
        house.S.labState = JsonUtility.ToJson(lab.S);
        Check(store.TrySave(JsonUtility.ToJson(house.S), out string message), "legacy four-slot disk save: " + message);
        var old = store.Load(); Check(old.Success && !old.Recovered, "legacy disk checksum");
        var state = JsonUtility.FromJson<LingGuangV05.Core.GameState>(old.Payload);
        lab = new XgSim(JsonUtility.FromJson<XgState>(state.labState));
        Check(lab.RelicSlotCapacity == 4 && lab.S.relicSlots[3] == "redbull", "legacy disk item position retained");
        Stage(lab, 4); lab.S.unlocked.Add("train.curve"); lab.S.handCorrect = 12;
        Check(lab.BuyNode(XgSim.RelicExpansion8Node, wallet), "disk fixture first purchase");
        Check(lab.BuyNode(XgSim.RelicExpansion12Node, wallet), "disk fixture second purchase");
        Check(lab.EquipRelic("redbull", 11), "disk fixture slot twelve");
        state.labState = JsonUtility.ToJson(lab.S); state.money = wallet.cash;
        Check(store.TrySave(JsonUtility.ToJson(state), out message), "expanded disk save: " + message);
        var loaded = new LingGuangV05.Runtime.Persistence.AtomicSaveStore(root).Load();
        Check(loaded.Success && !loaded.Recovered, "expanded primary disk checksum");
        var restoredHouse = JsonUtility.FromJson<LingGuangV05.Core.GameState>(loaded.Payload);
        var restored = new XgSim(JsonUtility.FromJson<XgState>(restoredHouse.labState));
        Check(restored.RelicSlotCapacity == 12 && restored.S.relicSlots[11] == "redbull" && restored.RelicOwned("redbull"), "expanded disk roundtrip preserves ownership and slot twelve");
        Check(restoredHouse.money == wallet.cash, "expanded disk retains post-purchase balance");
        System.IO.File.WriteAllText(store.SavePath, "deliberate isolated corrupt primary");
        var recovered = new LingGuangV05.Runtime.Persistence.AtomicSaveStore(root).Load();
        Check(recovered.Success && recovered.Recovered && recovered.Payload == old.Payload, "corrupted expanded primary recovers intact old four-slot backup");
        Debug.Log("RelicExpansion disk PASS: 11 checks, isolated files retained at " + root);
    }
#endif
    public static int Verify(Func<XgState, XgState> serializeAndRead)
    {
        roundTrip = serializeAndRead ?? throw new ArgumentNullException(nameof(serializeAndRead));
        checks = 0;
        var sim = new XgSim(); var wallet = new Wallet();
        var first = XgCatalog.Node(XgSim.RelicExpansion8Node);
        var second = XgCatalog.Node(XgSim.RelicExpansion12Node);
        Check(first != null && second != null, "catalog contains both real nodes");
        Check(first.stage == 3 && second.stage == 4 && second.parent == first.id, "approved stages and prerequisite");
        Equal(12000, first.cost, "first catalog ordinary price, no StagePrice");
        Equal(48000, second.cost, "second catalog ordinary price, no StagePrice");
        Equal(12000, sim.NodeCost(first), "first quote, no legacy quarter multiplier");
        Equal(48000, sim.NodeCost(second), "second quote, no legacy quarter multiplier");
        Check(sim.RelicSlotCapacity == 4 && sim.S.relicSlots.Count == 4, "new game has four slots");
        for (int stage = 1; stage <= 2; stage++)
        {
            Stage(sim, stage); double before = wallet.cash;
            Check(sim.NodeVisible(first) && sim.Status(first, wallet) == XgSim.NodeStatus.Locked, "early visible but locked " + stage);
            Check(!sim.BuyNode(first.id, wallet) && wallet.cash == before && wallet.payments == 0, "early purchase rejected " + stage);
        }
        Stage(sim, 4);
        Check(!sim.BuyNode(second.id, wallet) && wallet.payments == 0, "cannot skip first expansion");
        Stage(sim, 3); wallet.cash = 11999;
        Check(!sim.BuyNode(first.id, wallet) && wallet.payments == 0 && wallet.cash == 11999, "insufficient funds unchanged");
        // Real grants/equip before purchase verify expansion retains all original slots and effects.
        string[] initial = { "redbull", "allnight", "tablet", "tieba" };
        foreach (string id in initial) sim.GrantRelic(id);
        var beforeFx = sim.RelicFx;
        wallet.cash = 200000;
        Check(sim.BuyNode(first.id, wallet), "first real purchase");
        Equal(188000, wallet.cash, "first actual debit");
        Check(wallet.payments == 1 && sim.RelicSlotCapacity == 8 && sim.S.relicSlots.Count == 8, "eight slots available immediately");
        Check(initial.SequenceEqual(sim.S.relicSlots.Take(4)), "original equipment stays in original positions");
        SameEffects(beforeFx, sim.RelicFx, "empty expansion keeps effects");
        Check(!sim.BuyNode(first.id, wallet) && wallet.payments == 1 && wallet.cash == 188000, "first duplicate cannot debit");
        Check(!sim.BuyNode(second.id, wallet) && wallet.payments == 1, "second blocked at stage three");
        var eight = Copy(sim);
        Check(eight.RelicSlotCapacity == 8 && eight.S.relicSlots.Count == 8 && initial.SequenceEqual(eight.S.relicSlots.Take(4)), "eight-slot JSON roundtrip");
        Stage(sim, 4);
        Check(sim.BuyNode(second.id, wallet), "second real purchase");
        Equal(140000, wallet.cash, "second actual debit");
        Check(wallet.payments == 2 && sim.RelicSlotCapacity == 12 && sim.S.relicSlots.Count == 12, "twelve slots available immediately");
        Check(!sim.BuyNode(second.id, wallet) && wallet.payments == 2, "second duplicate cannot debit");
        Check(!sim.EquipRelic(initial[0], 12), "slot thirteen rejected");

        // Exercise every new slot through equip/unequip; compare all actual effect channels.
        foreach (string id in initial) sim.UnequipRelic(id);
        foreach (string id in new[] { "redbull", "allnight", "coupon" }) sim.GrantRelic(id);
        foreach (string id in sim.S.relicSlots.ToArray()) if (!string.IsNullOrEmpty(id)) sim.UnequipRelic(id);
        for (int slot = 4; slot < 12; slot++)
        {
            var empty = sim.RelicFx;
            Check(sim.EquipRelic("redbull", slot), "equip slot " + (slot + 1));
            SameEffects(XgRelics.Build(new[] { "redbull" }), sim.RelicFx, "high slot effect " + (slot + 1));
            Check(!ReferenceEquals(empty, sim.RelicFx), "high slot invalidates cached effect " + (slot + 1));
            Check(sim.UnequipSlot(slot), "unequip slot " + (slot + 1));
            SameEffects(XgRelics.Build(new string[0]), sim.RelicFx, "high slot removal " + (slot + 1));
        }
        sim.EquipRelic("allnight", 4); sim.EquipRelic("redbull", 11);
        SameEffects(XgRelics.Build(new[] { "allnight", "redbull" }), sim.RelicFx, "pair spanning slots five and twelve");
        sim.EquipRelic("coupon", 10);
        var twelve = Copy(sim);
        Check(twelve.RelicSlotCapacity == 12 && twelve.S.relicSlots.SequenceEqual(sim.S.relicSlots), "all twelve positions survive JSON");
        SameEffects(sim.RelicFx, twelve.RelicFx, "JSON retains effect and pair channels");
        // A fifth item in an old four-slot save stays owned, without granting free capacity.
        var legacy = new XgSim(); foreach (string id in initial) legacy.GrantRelic(id);
        legacy.GrantRelic("coupon"); legacy.S.relicSlots.Add("coupon");
        var migrated = Copy(legacy);
        Check(migrated.RelicSlotCapacity == 4 && migrated.S.relicSlots.Count == 4, "legacy save receives no free expansion");
        Check(initial.SequenceEqual(migrated.S.relicSlots) && migrated.RelicOwned("coupon") && !migrated.RelicEquipped("coupon"), "legacy four preserved, extra stays in backpack");
        Check(Copy(migrated).S.relicSlots.SequenceEqual(migrated.S.relicSlots), "legacy repair idempotent");

        var discount = new XgSim(); Stage(discount, 3); discount.GrantRelic("coupon");
        var discountedWallet = new Wallet();
        Equal(10800, discount.NodeCost(first), "legitimate ten-percent relic discount");
        Check(discount.BuyNode(first.id, discountedWallet), "discounted real purchase");
        Equal(189200, discountedWallet.cash, "discounted actual debit");
        Stage(discount, 4);
        Equal(43200, discount.NodeCost(second), "second discount");
        Check(discount.BuyNode(second.id, discountedWallet), "second discounted purchase");
        Equal(146000, discountedWallet.cash, "second discounted actual debit");
        Check(XgSim.RelicExpansionForSlot(4) == first.id && XgSim.RelicExpansionForSlot(7) == first.id
            && XgSim.RelicExpansionForSlot(8) == second.id && XgSim.RelicExpansionForSlot(11) == second.id, "locked slot target mapping");
        foreach (bool english in new[] { false, true })
        {
            sim.English = english;
            Check(XgTechTreeCopy.Covered(first) && XgTechTreeCopy.Covered(second), "three-line tech copies covered");
            Check(XgTechTreeCopy.Name(first, sim).Contains(english ? "Relic" : "道具"), "actual localized first name");
            Check(XgTechTree.EntryOf(first.id) != null && XgTechTree.EntryOf(second.id) != null, "real tech entries addressable");
        }
        // Full capacity replacement/movement must preserve inventory, uniqueness and effects.
        var full = new XgSim(); Stage(full, 4); var fullWallet = new Wallet();
        Check(full.BuyNode(first.id, fullWallet) && full.BuyNode(second.id, fullWallet), "full fixture buys both expansions");
        foreach (var relic in XgRelics.All) Check(full.GrantRelic(relic.id), "grant full collection " + relic.id);
        Check(full.RelicFreeSlot == -1 && full.RelicsEquippedCount == 12, "auto-equip fills exactly twelve slots");
        string replaced = full.S.relicSlots[11], incoming = XgRelics.All[12].id;
        Check(!full.EquipRelic(incoming), "full automatic equip rejected");
        Check(full.EquipRelic(incoming, 11), "replace twelfth slot");
        Check(full.RelicOwned(replaced) && !full.RelicEquipped(replaced) && full.RelicsOwned.Count == XgRelics.All.Length, "replacement never loses ownership");
        Check(full.EquipRelic(incoming, 4) && full.S.relicSlots[11] == "" && full.S.relicSlots.Count(id => id == incoming) == 1, "moving high-slot relic cannot duplicate");
        Check(full.EquipRelic(replaced), "released high slot reusable");
        SameEffects(XgRelics.Build(full.S.relicSlots), full.RelicFx, "full equipment effects");
        var fullCopy = Copy(full);
        Check(fullCopy.S.relicOwned.SequenceEqual(full.S.relicOwned) && fullCopy.S.relicSlots.SequenceEqual(full.S.relicSlots), "full inventory and replacement survive serialization");
        foreach (var id in full.S.relicSlots.ToArray()) if (!string.IsNullOrEmpty(id)) full.UnequipRelic(id);
        foreach (var relic in XgRelics.All)
        {
            Check(full.EquipRelic(relic.id, 11), "every relic works in twelfth slot " + relic.id);
            SameEffects(XgRelics.Build(new[] { relic.id }), full.RelicFx, "twelfth slot all channels " + relic.id);
            Check(full.UnequipSlot(11), "every relic removed from twelfth slot " + relic.id);
        }
        foreach (var pair in XgRelics.Pairs)
        {
            full.EquipRelic(pair.a, 4); full.EquipRelic(pair.b, 11);
            SameEffects(XgRelics.Build(new[] { pair.a, pair.b }), full.RelicFx, "every cross-page pair " + pair.zh);
            full.UnequipSlot(4); full.UnequipSlot(11);
        }
        // Expansion is research: existing diagnosis coupons remain reserved for their legal node kinds.
        var coupons = new XgSim(); Stage(coupons, 3); coupons.S.diagnosisCoupons.AddRange(new[] { "params", "structure" });
        Equal(12000, coupons.NodeCost(first), "unrelated diagnosis coupons do not discount expansion");
        Check(coupons.BuyNode(first.id, new Wallet()) && coupons.S.diagnosisCoupons.Count == 2, "unrelated diagnosis coupons are not consumed");
        var stack = new XgSim(); Stage(stack, 3); Check(stack.GrantRelic("coupon") && stack.GrantRelic("angel_term"), "grant stacked discount fixture");
        double expectedStack = Math.Ceiling(12000 * (1 - Math.Min(.7, stack.RelicFx.techCut)));
        Equal(expectedStack, stack.NodeCost(first), "stacked legal relic discounts");
        var stackWallet = new Wallet(); double stackBefore = stackWallet.cash;
        Check(stack.BuyNode(first.id, stackWallet), "stacked discount purchase");
        Equal(expectedStack, stackBefore - stackWallet.cash, "stacked actual debit");
        Check(Copy(Copy(fullCopy)).S.relicSlots.SequenceEqual(fullCopy.S.relicSlots), "twelve-slot repeated repair is idempotent");
        return checks;
    }
}
