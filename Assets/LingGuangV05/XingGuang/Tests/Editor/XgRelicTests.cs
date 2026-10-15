using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>稀有道具: the table, the backpack and four slots, every effect on its real number, the pairs, the sources and the save.</summary>
    public sealed class XgRelicTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1e9, vram = 4096;
            public double Money => money;
            public double Compute => 2;
            public double VramMB => vram;
            public string Blocker => null;
            public void Earn(double amount) { money += amount; }
            public bool Spend(double amount) { if (amount < 0 || money < amount) return false; money -= amount; return true; }
            public void Train(double seconds) { }
        }

        static XgSim Fresh(long seed = 12345)
        {
            var s = new XgSim { GoldChance = 0 };
            s.S.rng = seed;
            return s;
        }

        static void OpenSpam(XgSim s) { if (!s.S.desksOpen.Contains("spam")) s.S.desksOpen.Add("spam"); }

        static XgSim With(params string[] equipped)
        {
            var s = Fresh();
            foreach (var id in equipped) { Assert.IsTrue(s.GrantRelic(id), id); }
            return s;
        }

        // ───────────── the table ─────────────

        [Test] public void TheTableHasTwentyFourRelicsInThreeTiers()
        {
            Assert.AreEqual(24, XgRelics.All.Length);
            Assert.AreEqual(24, XgRelics.All.Select(r => r.id).Distinct().Count(), "ids are unique");
            Assert.AreEqual(10, XgRelics.All.Count(r => r.rarity == XgRarity.Rare));
            Assert.AreEqual(9, XgRelics.All.Count(r => r.rarity == XgRarity.Epic));
            Assert.AreEqual(5, XgRelics.All.Count(r => r.rarity == XgRarity.Legendary));
            foreach (var r in XgRelics.All)
            {
                Assert.IsNotEmpty(r.zh, r.id); Assert.IsNotEmpty(r.en, r.id);
                Assert.IsNotEmpty(r.flavorZh, r.id); Assert.IsNotEmpty(r.flavorEn, r.id);
                Assert.IsNotEmpty(r.effectZh, r.id); Assert.IsNotEmpty(r.effectEn, r.id);
                Assert.IsNotEmpty(r.glyph, r.id);
                Assert.IsNotNull(r.apply, r.id);
                Assert.IsFalse(r.zh.Contains("涌现") || r.effectZh.Contains("涌现") || r.flavorZh.Contains("涌现"), r.id + " says 涌现");
            }
        }

        [Test] public void EveryPairNamesTwoRealRelicsAndHasATextInBothLanguages()
        {
            Assert.AreEqual(6, XgRelics.Pairs.Length);
            foreach (var p in XgRelics.Pairs)
            {
                Assert.IsNotNull(XgRelics.Get(p.a), p.a); Assert.IsNotNull(XgRelics.Get(p.b), p.b);
                Assert.AreNotEqual(p.a, p.b);
                Assert.IsNotEmpty(p.zh); Assert.IsNotEmpty(p.en); Assert.IsNotEmpty(p.textZh); Assert.IsNotEmpty(p.textEn);
                Assert.AreSame(p, XgRelics.PairOf(p.a)); Assert.AreSame(p, XgRelics.PairOf(p.b));
                Assert.AreEqual(p.b, XgRelics.Partner(p, p.a));
            }
        }

        // ───────────── backpack and four slots ─────────────

        [Test] public void OwningMoreThanFourIsFineButOnlyFourAreEquipped()
        {
            var s = Fresh();
            var ids = XgRelics.All.Take(7).Select(r => r.id).ToList();
            foreach (var id in ids) Assert.IsTrue(s.GrantRelic(id));
            Assert.AreEqual(7, s.RelicsOwned.Count, "every relic goes in the backpack");
            Assert.AreEqual(4, s.RelicsEquippedCount, "four slots at most");
            for (int i = 0; i < 4; i++) Assert.AreEqual(ids[i], s.RelicInSlot(i).id, "a free slot takes it at once");
            for (int i = 4; i < 7; i++) Assert.IsFalse(s.RelicEquipped(ids[i]), ids[i]);
            Assert.IsFalse(s.EquipRelic(ids[5]), "with the slots full a plain equip says no");
            Assert.AreEqual(4, s.RelicsEquippedCount);
        }

        [Test] public void ARelicIsNeverGrantedTwice()
        {
            var s = Fresh();
            Assert.IsTrue(s.GrantRelic("tieba"));
            Assert.IsFalse(s.GrantRelic("tieba"));
            Assert.IsFalse(s.GrantRelic("not_a_relic"));
            Assert.AreEqual(1, s.RelicsOwned.Count);
        }

        [Test] public void AnUnequippedRelicDoesNothing()
        {
            var s = With("redbull");
            Assert.AreEqual(.5, s.RelicFx.cardsBonus, 1e-9);
            Assert.IsTrue(s.UnequipRelic("redbull"));
            Assert.IsTrue(s.RelicOwned("redbull"), "it stays in the backpack");
            Assert.AreEqual(0, s.RelicFx.cardsBonus, 1e-9, "and does nothing there");
            Assert.IsTrue(s.EquipRelic("redbull"));
            Assert.AreEqual(.5, s.RelicFx.cardsBonus, 1e-9);
        }

        [Test] public void SwappingPutsTheOldOneBackInTheBackpackWithoutLosingIt()
        {
            var s = Fresh();
            foreach (var id in new[] { "tieba", "redbull", "coupon", "server_key", "tablet" }) s.GrantRelic(id);
            Assert.IsFalse(s.RelicEquipped("tablet"));
            Assert.IsTrue(s.EquipRelic("tablet", 2), "replace slot 3");
            Assert.AreEqual("tablet", s.RelicInSlot(2).id);
            Assert.IsFalse(s.RelicEquipped("coupon"));
            Assert.IsTrue(s.RelicOwned("coupon"), "the replaced one is still owned");
            Assert.AreEqual(4, s.RelicsEquippedCount);
            Assert.AreEqual(0, s.RelicFx.techCut, 1e-9, "the coupon no longer works");
            Assert.AreEqual(3, s.RelicFx.handwritingMult, 1e-9, "the tablet does");
        }

        [Test] public void AnEquippedRelicMovesToAnotherSlotWithoutDoubling()
        {
            var s = Fresh();
            s.GrantRelic("tieba"); s.GrantRelic("redbull");
            Assert.IsTrue(s.EquipRelic("tieba", 3));
            Assert.AreEqual("", s.S.relicSlots[0]);
            Assert.AreEqual("redbull", s.S.relicSlots[1]);
            Assert.AreEqual("tieba", s.S.relicSlots[3]);
            Assert.AreEqual(2, s.RelicsEquippedCount);
            Assert.AreEqual(.85, s.RelicFx.paramsFactor, 1e-9, "counted once");
        }

        [Test] public void AFreedSlotTakesTheNextArrivalAtOnce()
        {
            var s = Fresh();
            foreach (var id in new[] { "tieba", "redbull", "coupon", "server_key" }) s.GrantRelic(id);
            s.UnequipSlot(1);
            bool equipped = false;
            s.RelicGained += (r, source, eq) => equipped = eq;
            s.GrantRelic("tablet", "mini");
            Assert.IsTrue(equipped);
            Assert.AreEqual("tablet", s.RelicInSlot(1).id);
        }

        [Test] public void AnArrivalWithFullSlotsGoesToTheBackpackAndMarksItNew()
        {
            var s = Fresh();
            foreach (var id in new[] { "tieba", "redbull", "coupon", "server_key" }) s.GrantRelic(id);
            s.MarkRelicsViewed();
            Assert.IsFalse(s.RelicsFresh);
            bool equipped = true; string from = null;
            s.RelicGained += (r, source, eq) => { equipped = eq; from = source; };
            s.GrantRelic("tablet", "spider");
            Assert.IsFalse(equipped); Assert.AreEqual("spider", from);
            Assert.IsTrue(s.RelicsFresh, "the red dot is on");
            Assert.IsTrue(s.RelicFresh("tablet"));
            s.MarkRelicsViewed();
            Assert.IsFalse(s.RelicsFresh, "and goes out once the backpack was seen");
            Assert.IsTrue(s.RelicSeen("tablet"));
        }

        // ───────────── every effect on its real number ─────────────

        [Test] public void DropoutRaisesTheGainOfEveryRound()
        {
            var plain = Fresh(); var with = With("dropout_paper");
            double a = plain.GainRate(plain.S.vision, 8), b = with.GainRate(with.S.vision, 8);
            Assert.AreEqual(a * 1.4, b, 1e-9);
        }

        [Test] public void ImageNetAndAttentionRaiseTheirOwnLine()
        {
            var plain = Fresh(); var img = With("imagenet_disc"); var att = With("attention_scratch");
            double v = plain.GainRate(plain.S.vision, 8), q = plain.GainRate(plain.S.sequence, 8);
            Assert.AreEqual(v * 1.6, img.GainRate(img.S.vision, 8), 1e-9);
            Assert.AreEqual(q, img.GainRate(img.S.sequence, 8), 1e-9, "vision only");
            Assert.AreEqual(q * 2, att.GainRate(att.S.sequence, 8), 1e-9);
            Assert.AreEqual(v, att.GainRate(att.S.vision, 8), 1e-9, "sequence only");
        }

        [Test] public void TheTaihuLightGivesTripleVramAndMoreGain()
        {
            var h = new Host();
            var plain = Fresh(); var with = With("taihulight");
            Assert.AreEqual(plain.Vram(h) * 3, with.Vram(h), 1e-6);
            Assert.AreEqual(plain.GainRate(plain.S.vision, 8) * 1.3, with.GainRate(with.S.vision, 8), 1e-9);
        }

        [Test] public void TheMiningCardDoublesVramAndRaisesThePowerBill()
        {
            var h = new Host();
            var plain = Fresh(); var with = With("mining_1080");
            Assert.AreEqual(plain.Vram(h) * 2, with.Vram(h), 1e-6);
            Assert.AreEqual(plain.TrainPriceFactor * 1.3, with.TrainPriceFactor, 1e-9);
        }

        [Test] public void TheMultimeterCutsThePowerBill()
        {
            var s = With("multimeter");
            Assert.AreEqual(.6, s.TrainPriceFactor, 1e-9);
        }

        [Test] public void TiebaAndKinLowerBothAbilityLines()
        {
            var plain = Fresh();
            foreach (var pair in new[] { ("tieba", .85, .85), ("advisor_page", .75, .75), ("data_broker", 1.0, .8), ("campus_vpn", .8, 1.0) })
            {
                var s = With(pair.Item1);
                Assert.AreEqual(plain.ParamsThreshold(3) * pair.Item2, s.ParamsThreshold(3), 1e-6, pair.Item1 + " parameters");
                Assert.AreEqual(plain.SamplesThreshold(3) * pair.Item3, s.SamplesThreshold(3), 1e-6, pair.Item1 + " samples");
            }
        }

        [Test] public void StackedThresholdRelicsStopAtTheFloor()
        {
            var plain = Fresh();
            var s = With("tieba", "advisor_page", "data_broker", "campus_vpn");
            Assert.That(s.RelicFx.paramsFactor, Is.LessThan(XgSim.RelicLineFloor), "four of them would cut deeper");
            Assert.AreEqual(plain.ParamsThreshold(4) * XgSim.RelicLineFloor, s.ParamsThreshold(4), 1e-6);
            Assert.AreEqual(plain.SamplesThreshold(4) * XgSim.RelicLineFloor, s.SamplesThreshold(4), 1e-6);
        }

        [Test] public void TheCouponAndTheTermSheetCutTechTreePrices()
        {
            var node = XgCatalog.Nodes.First(n => n.cost > 20 && !double.IsInfinity(n.cost) && !XgItems.IsItem(n.id));
            var plain = Fresh(); var coupon = With("coupon"); var angel = With("angel_term"); var both = With("coupon", "angel_term");
            double basePrice = plain.NodeCost(node);
            Assert.AreEqual(basePrice * .9, coupon.NodeCost(node), 1.01);
            Assert.AreEqual(basePrice * .9, angel.NodeCost(node), 1.01);
            Assert.AreEqual(basePrice * .8, both.NodeCost(node), 1.01, "they add up");
        }

        [Test] public void TheServerKeyCutsTheRent()
        {
            Assert.AreEqual(1, Fresh().RelicRentFactor, 1e-9);
            Assert.AreEqual(.75, With("server_key").RelicRentFactor, 1e-9);
        }

        [Test] public void TheWatermelonBookRaisesTheYesNoAccuracy()
        {
            var plain = Fresh(); var with = With("watermelon_book");
            Assert.AreEqual(plain.YesNoAccuracy + .15, with.YesNoAccuracy, 1e-9);
        }

        [Test] public void TheTabletTriplesTheHandwritingMemory()
        {
            Assert.AreEqual(XgMiniActionMemory.Capacity, Fresh().HandwritingCapacity);
            Assert.AreEqual(XgMiniActionMemory.Capacity * 3, With("tablet").HandwritingCapacity);
        }

        [Test] public void TheRedBullFeedsMoreCardsPerRound()
        {
            var plain = Fresh(); var with = With("redbull");
            int a = plain.CardsPerEpoch(plain.S.vision, .6, true), b = with.CardsPerEpoch(with.S.vision, .6, true);
            Assert.Greater(b, a);
            Assert.AreEqual(a * 1.5, b, 1.01);
        }

        [Test] public void AllNightKeepsTheComboAliveThroughMistakesAndTime()
        {
            var h = new Host();
            var plain = Fresh(); var with = With("allnight");
            foreach (var s in new[] { plain, with }) { OpenSpam(s); s.S.combo = 12; s.S.comboTimer = 2; }
            plain.SettleMiniAction("spam", false, h); with.SettleMiniAction("spam", false, h);
            Assert.AreEqual(0, plain.S.combo, "a mistake breaks it");
            Assert.AreEqual(12, with.S.combo, "unless the pass is equipped");
            with.Tick(30, h);
            Assert.AreEqual(12, with.S.combo, "and time does not fade it");
        }

        [Test] public void TheCrunchPairLiftsTheComboCap()
        {
            var plain = Fresh(); var one = With("allnight", "redbull"); var none = With("allnight");
            plain.S.combo = none.S.combo = one.S.combo = 100;
            Assert.AreEqual(2, plain.ComboMultiplier, 1e-9);
            Assert.AreEqual(2, none.ComboMultiplier, 1e-9);
            Assert.AreEqual(3, one.ComboMultiplier, 1e-9);
        }

        [Test] public void TheUsbStickAndKinAddSamplesToEveryHandLabel()
        {
            var h = new Host();
            foreach (var c in new[] { (new string[0], 1), (new[] { "zhou_usb" }, 2), (new[] { "alphago_book" }, 3), (new[] { "zhou_usb", "data_broker" }, 3) })
            {
                var s = With(c.Item1); OpenSpam(s);
                double before = s.Labels("spam") + s.ExtraSamples("spam");
                var r = s.SettleMiniAction("spam", true, h);
                Assert.AreEqual(before + c.Item2, s.Labels("spam") + s.ExtraSamples("spam"), 1e-9, string.Join("+", c.Item1));
                Assert.GreaterOrEqual(r.samples, c.Item2, "the answer reports them");
            }
        }

        [Test] public void TheNotebookExtrasBringSomeNoise()
        {
            var h = new Host();
            var s = With("alphago_book"); OpenSpam(s);
            for (int i = 0; i < 10; i++) s.SettleMiniAction("spam", true, h);
            Assert.Greater(s.DataNoise("spam"), 0);
            var clean = With("zhou_usb"); OpenSpam(clean);
            for (int i = 0; i < 10; i++) clean.SettleMiniAction("spam", true, h);
            Assert.AreEqual(0, clean.DataNoise("spam"), 1e-9, "the usb stick's extras are right");
        }

        [Test] public void TheArcadeTokensAddOneSamplePerRightAnswer()
        {
            var h = new Host();
            var plain = Fresh(); var with = With("arcade_tokens");
            OpenSpam(plain); OpenSpam(with);
            var a = plain.SettleMiniAction("spam", true, h); var b = with.SettleMiniAction("spam", true, h);
            Assert.AreEqual(a.samples + 1, b.samples);
            Assert.AreEqual(plain.Labels("spam") + 1, with.Labels("spam") , "one more row on the desk");
            var wrong = with.SettleMiniAction("spam", false, h);
            Assert.AreEqual(0, wrong.samples, "a wrong answer gets nothing");
        }

        [Test] public void TheForgedUserAgentTriplesWhatEachWordIsWorth()
        {
            var plain = Fresh(); var with = With("spider_ua");
            plain.ResidentRead("hello"); with.ResidentRead("hello");
            Assert.AreEqual(1, plain.ExtraSamples(XgSim.ReadDataset), 1e-9);
            Assert.AreEqual(3, with.ExtraSamples(XgSim.ReadDataset), 1e-9);
        }

        [Test] public void TheGoldBadgeAndTheTermSheetRaiseContractIncome()
        {
            var c = XgCatalog.Contracts[0];
            double Income(params string[] relics)
            {
                var s = With(relics);
                s.S.best.Add(new XgBest { dataset = c.dataset, acc = .999 });
                s.S.contracts.Add(c.id);
                return s.IncomePerSecond;
            }
            double basic = Income();
            Assert.Greater(basic, 0);
            Assert.AreEqual(basic * 1.2, Income("gold_cert"), 1e-6);
            Assert.AreEqual(basic * 1.3, Income("angel_term"), 1e-6);
            Assert.AreEqual(basic * 1.5, Income("gold_cert", "angel_term"), 1e-6);
        }

        [Test] public void BatchNormAndItsPairLowerTheChanceOfADrop()
        {
            var plain = Fresh(); var bn = With("batchnorm_paper"); var pair = With("batchnorm_paper", "dropout_paper");
            plain.S.vision.dataset = bn.S.vision.dataset = pair.S.vision.dataset = "mnist";
            double a = plain.DropRisk(plain.S.vision).chance, b = bn.DropRisk(bn.S.vision).chance, c = pair.DropRisk(pair.S.vision).chance;
            Assert.Greater(a, 0);
            Assert.AreEqual(a * .5, b, 1e-9);
            Assert.AreEqual(a * .25, c, 1e-9, "the pair takes it to −75%");
        }

        [Test] public void TheResidualStickerExcusesOneFailedRoundEveryEight()
        {
            var s = With("resnet_sticker");
            Assert.IsFalse(s.RelicGuardReady, "the sticker has to charge first");
            for (int i = 0; i < 7; i++) s.RelicRoundDone();
            Assert.IsFalse(s.RelicGuardReady);
            s.RelicRoundDone();
            Assert.IsTrue(s.RelicGuardReady, "ready after eight rounds");
            Assert.IsFalse(Fresh().RelicGuardReady);
        }

        [Test] public void EveryOtherPairChangesItsNumbers()
        {
            var fx = XgRelics.Build(new[] { "imagenet_disc", "attention_scratch" });
            Assert.AreEqual(.25, fx.gain, 1e-9); Assert.AreEqual(.6, fx.gainVision, 1e-9); Assert.AreEqual(1.0, fx.gainSequence, 1e-9);
            fx = XgRelics.Build(new[] { "mining_1080", "multimeter" });
            Assert.AreEqual(0, fx.powerRise, 1e-9, "the penalty is gone"); Assert.AreEqual(1.5, fx.vramBonus, 1e-9); Assert.AreEqual(.4, fx.powerCut, 1e-9);
            fx = XgRelics.Build(new[] { "mining_1080" });
            Assert.AreEqual(.3, fx.powerRise, 1e-9, "alone the penalty stays");
            fx = XgRelics.Build(new[] { "dropout_paper", "batchnorm_paper" });
            Assert.AreEqual(.75, fx.dropCut, 1e-9);
            fx = XgRelics.Build(new[] { "zhou_usb", "data_broker" });
            Assert.AreEqual(2, fx.extraSamples, 1e-9);
            fx = XgRelics.Build(new[] { "tieba", "advisor_page" });
            Assert.AreEqual(.85 * .75 * .95, fx.paramsFactor, 1e-9); Assert.AreEqual(.85 * .75 * .95, fx.samplesFactor, 1e-9);
            fx = XgRelics.Build(new[] { "allnight", "redbull" });
            Assert.AreEqual(1, fx.comboCapBonus, 1e-9);
        }

        [Test] public void EachRelicOnItsOwnMovesAtLeastOneChannel()
        {
            var baseline = new XgRelicFx();
            foreach (var r in XgRelics.All)
            {
                var fx = XgRelics.Build(new[] { r.id });
                bool changed = false;
                foreach (var f in typeof(XgRelicFx).GetFields())
                    if (!Equals(f.GetValue(fx), f.GetValue(baseline))) { changed = true; break; }
                Assert.IsTrue(changed, r.id + " does nothing");
            }
        }

        // ───────────── sources ─────────────

        [Test] public void NoDropComesBeforeTheFirstChoiceOfThree()
        {
            var s = Fresh(); s.RelicLimitsOn = false;
            for (int i = 0; i < 200; i++) Assert.IsFalse(s.TryRelicDrop("mini", 1));
            Assert.AreEqual(0, s.RelicsOwned.Count);
        }

        [Test] public void ALearnedAbilityOffersThreeDifferentRelicsAndPickingOneKeepsIt()
        {
            var s = Fresh();
            s.OfferRelicPick();
            Assert.IsTrue(s.RelicPickPending);
            Assert.AreEqual(3, s.RelicPick.Count);
            Assert.AreEqual(3, s.RelicPick.Distinct().Count());
            var cards = s.RelicPick.ToList();
            Assert.IsFalse(s.PickRelic("tieba") && !cards.Contains("tieba"), "a card that was not offered cannot be taken");
            var s2 = Fresh(); s2.OfferRelicPick();
            string take = s2.RelicPick[1];
            Assert.IsTrue(s2.PickRelic(take));
            Assert.IsTrue(s2.RelicOwned(take));
            Assert.AreEqual(1, s2.RelicsOwned.Count, "the other two are not given");
            Assert.IsFalse(s2.RelicPickPending);
        }

        [Test] public void ChoicesQueueUpAndComeOneAtATime()
        {
            var s = Fresh();
            s.OfferRelicPick(); s.OfferRelicPick(); s.OfferRelicPick();
            Assert.AreEqual(3, s.RelicPick.Count);
            var first = s.RelicPick.ToList();
            s.PickRelic(first[0]);
            Assert.IsTrue(s.RelicPickPending, "the next choice is dealt");
            Assert.IsFalse(s.RelicPick.Contains(first[0]), "without the one just taken");
            s.PickRelic(s.RelicPick[0]);
            Assert.IsTrue(s.RelicPickPending);
            s.PickRelic(s.RelicPick[0]);
            Assert.IsFalse(s.RelicPickPending);
            Assert.AreEqual(3, s.RelicsOwned.Count);
        }

        [Test] public void LearningAnAbilityOffersAChoice()
        {
            var s = Fresh(); var h = new Host();
            int offers = 0;
            s.RelicPickOffered += () => offers++;
            s.S.trainedParamsK = s.ParamsThreshold(2);
            s.S.labels.Add(new XgLabelCount { dataset = "logic", count = s.SamplesThreshold(2) + 10 });
            s.S.stageEpochs = 1;
            Assert.IsTrue(s.ConfirmNextStage() || s.HasAbility(2), "ability two comes");
            Assert.IsTrue(s.RelicPickPending);
            Assert.AreEqual(1, offers);
        }

        [Test] public void OnlyTheChoiceOfThreeCanShowALegendary()
        {
            int legendaryCards = 0, cards = 0;
            for (int seed = 1; seed <= 600; seed++)
            {
                var s = Fresh(seed * 7919L);
                s.OfferRelicPick();
                foreach (var id in s.RelicPick) { cards++; if (XgRelics.Get(id).rarity == XgRarity.Legendary) legendaryCards++; }
            }
            double share = legendaryCards / (double)cards;
            Assert.That(share, Is.InRange(.04, .13), "about 8% of the cards (weights 60/32/8)");

            var d = Fresh(); d.S.relicPicks = 1; d.RelicLimitsOn = false;
            int drops = 0;
            d.RelicGained += (r, source, eq) => { drops++; Assert.AreNotEqual(XgRarity.Legendary, r.rarity, "a drop is never legendary"); ResetBackpack(d); };
            for (int i = 0; i < 4000; i++) d.TryRelicDrop("mini", 1);
            Assert.AreEqual(4000, drops);
        }

        static void ResetBackpack(XgSim s)
        {
            s.S.relicOwned.Clear(); s.S.relicFresh.Clear();
            for (int i = 0; i < s.S.relicSlots.Count; i++) s.S.relicSlots[i] = "";
        }

        static int CountDrops(XgSim s, int trials, Action<int> roll)
        {
            int drops = 0;
            s.S.relicPicks = 1; s.RelicLimitsOn = false;
            s.RelicGained += (r, source, eq) => { drops++; ResetBackpack(s); };
            for (int i = 0; i < trials; i++) roll(i);
            return drops;
        }

        [Test] public void ADropRollsAtTheDocumentedChances()
        {
            var s = Fresh(777);
            Assert.AreEqual(.01, XgSim.RelicMiniChance); Assert.AreEqual(.05, XgSim.RelicPerfectChance);
            Assert.AreEqual(.005, XgSim.RelicSpiderChance); Assert.AreEqual(.10, XgSim.RelicRecordChance);
            int mini = CountDrops(s, 30000, i => s.TryRelicDrop("mini", XgSim.RelicMiniChance));
            Assert.That(mini, Is.InRange(230, 375), "1% of 30000 is 300");
            var s2 = Fresh(778);
            int record = CountDrops(s2, 6000, i => s2.TryRelicDrop("record", XgSim.RelicRecordChance));
            Assert.That(record, Is.InRange(500, 700), "10% of 6000 is 600");
        }

        [Test] public void DropsAreSeededSoTheSameGameRollsTheSame()
        {
            string Run(long seed)
            {
                var s = Fresh(seed); s.S.relicPicks = 1; s.RelicLimitsOn = false;
                var log = new List<string>();
                s.RelicGained += (r, source, eq) => { log.Add(r.id); ResetBackpack(s); };
                for (int i = 0; i < 3000; i++) s.TryRelicDrop("mini", .02);
                return string.Join(",", log);
            }
            Assert.AreEqual(Run(42), Run(42));
            Assert.AreNotEqual(Run(42), Run(43));
            Assert.IsNotEmpty(Run(42));
        }

        [Test] public void ARightAnswerInAGameRollsTheMiniDropAndACleanRunRollsThePerfectOne()
        {
            var h = new Host();
            int Tickets(bool clean, long seed, int tickets)
            {
                var s = Fresh(seed); OpenSpam(s);
                int drops = CountDrops(s, 0, i => { });
                s.RelicGained += (r, source, eq) => drops++;
                for (int t = 0; t < tickets; t++)
                {
                    long ticket = s.BeginMiniGame(XgMiniGames.ForDesk("spam"), h);
                    Assert.AreNotEqual(0, ticket);
                    int seq = 0;
                    if (!clean) s.SubmitMiniGame(ticket, ++seq, false, h);
                    for (int k = 0; k < (clean ? 10 : 9); k++) s.SubmitMiniGame(ticket, ++seq, true, h);
                    s.CancelMiniGame(ticket);
                }
                return drops;
            }
            // Ten right answers in one game: 10 × 1% plus the 5% perfect roll, about 15%; with a mistake in it only the 9 × 1%.
            int perfect = Tickets(true, 91, 1500), flawed = Tickets(false, 92, 1500);
            Assert.That(perfect, Is.InRange(170, 290), "about 225 for 1500 clean games");
            Assert.That(flawed, Is.InRange(95, 180), "about 135 when each game has a mistake");
            Assert.Greater(perfect, flawed);
        }

        [Test] public void EachWordTheResidentSpiderReadsRollsTheSmallChance()
        {
            var s = Fresh(5);
            int n = 0;
            string Word(int i) { var c = new char[5]; for (int k = 0; k < 5; k++) { c[k] = (char)('a' + i % 26); i /= 26; } return new string(c); }
            int drops = CountDrops(s, 24000, i => { if (s.ResidentRead(Word(i + 1000))) n++; });
            Assert.AreEqual(24000, n, "every word was a new one");
            Assert.That(drops, Is.InRange(80, 165), "0.5% of 24000 is 120");
        }

        [Test] public void TheCrawlerRollsOncePerBatchWithTheSameOdds()
        {
            var s = Fresh(6); s.S.relicPicks = 1; s.RelicLimitsOn = false;
            // 100 words in one grab: 1 − 0.995^100 ≈ 39%.
            int got = 0;
            s.RelicGained += (r, source, eq) => { got++; ResetBackpack(s); };
            for (int i = 0; i < 2000; i++) s.TryRelicDrop("spider", 1 - Math.Pow(1 - XgSim.RelicSpiderChance, 100));
            Assert.That(got, Is.InRange(700, 860));
        }

        [Test] public void TheCapsAndTheGapKeepDropsRare()
        {
            var s = Fresh(); s.S.relicPicks = 1;
            Assert.IsTrue(s.TryRelicDrop("mini", 1), "the first one comes");
            Assert.IsFalse(s.TryRelicDrop("mini", 1), "never twice within the gap");
            s.S.relicClock += XgSim.RelicDropGap + 1;
            Assert.IsTrue(s.TryRelicDrop("mini", 1));
            s.S.relicClock += XgSim.RelicDropGap + 1;
            Assert.IsTrue(s.TryRelicDrop("mini", 1));
            s.S.relicClock += XgSim.RelicDropGap + 1;
            Assert.IsFalse(s.TryRelicDrop("mini", 1), "three mini-game drops a game at most");
            Assert.IsTrue(s.TryRelicDrop("spider", 1), "the spider has its own count");
            s.S.relicClock += XgSim.RelicDropGap + 1;
            Assert.IsTrue(s.TryRelicDrop("record", 1));
        }

        [Test] public void ADropIsNeverOneYouAlreadyHave()
        {
            var s = Fresh(); s.S.relicPicks = 1; s.RelicLimitsOn = false;
            int rareOrEpic = XgRelics.All.Count(r => r.rarity != XgRarity.Legendary);
            for (int i = 0; i < 500; i++) s.TryRelicDrop("mini", 1);
            Assert.AreEqual(rareOrEpic, s.RelicsOwned.Count, "all of them, once each");
            Assert.AreEqual(s.RelicsOwned.Count, s.RelicsOwned.Distinct().Count());
            Assert.IsFalse(s.RelicsOwned.Any(id => XgRelics.Get(id).rarity == XgRarity.Legendary));
        }

        // ───────────── save ─────────────

        [Test] public void TheBackpackTheSlotsAndTheChoiceSurviveASave()
        {
            var s = Fresh(); s.S.relicPicks = 4;
            foreach (var id in new[] { "tieba", "redbull", "coupon", "server_key", "tablet", "multimeter" }) s.GrantRelic(id);
            s.UnequipRelic("coupon");
            s.EquipRelic("multimeter", 2);
            s.OfferRelicPick();
            var pick = s.RelicPick.ToList();
            var copy = new XgSim(UnityEngine.JsonUtility.FromJson<XgState>(UnityEngine.JsonUtility.ToJson(s.S)));
            CollectionAssert.AreEqual(s.S.relicOwned, copy.S.relicOwned);
            CollectionAssert.AreEqual(s.S.relicSlots, copy.S.relicSlots);
            CollectionAssert.AreEqual(s.S.relicSeen, copy.S.relicSeen);
            CollectionAssert.AreEqual(pick, copy.RelicPick.ToList(), "the three cards still wait");
            Assert.AreEqual(6, copy.RelicsOwned.Count);
            Assert.AreEqual(4, copy.RelicsEquippedCount);
            Assert.IsFalse(copy.RelicEquipped("coupon"));
            Assert.AreEqual(s.RelicFx.paramsFactor, copy.RelicFx.paramsFactor, 1e-9, "and the effects come back");
            Assert.AreEqual(s.S.relicRng, copy.S.relicRng);
        }

        [Test] public void AnOldSaveWithoutRelicsGetsOneChoicePerAbilityItHas()
        {
            var state = new XgState();
            state.abilitiesEmerged = new List<int> { 1, 2, 3 };
            state.relicSchema = 0;
            var s = new XgSim(state);
            Assert.AreEqual(3, s.S.relicPicks);
            Assert.IsTrue(s.RelicPickPending, "the first of them is dealt");
            Assert.AreEqual(2, s.S.relicPickQueue, "and two more wait");
            Assert.AreEqual(0, s.RelicsOwned.Count);
            var again = new XgSim(UnityEngine.JsonUtility.FromJson<XgState>(UnityEngine.JsonUtility.ToJson(s.S)));
            Assert.AreEqual(3, again.S.relicPicks, "the debt is booked once");
        }

        [Test] public void AFreshGameOwesNothing()
        {
            var s = Fresh();
            Assert.AreEqual(0, s.S.relicPicks);
            Assert.IsFalse(s.RelicPickPending);
            Assert.AreEqual(XgRelics.Slots, s.S.relicSlots.Count);
        }

        [Test] public void ABrokenSaveIsMendedWithoutLosingTheRest()
        {
            var state = new XgState();
            state.relicOwned = new List<string> { "tieba", "tieba", "ghost", "redbull" };
            state.relicSlots = new List<string> { "ghost", "redbull", "redbull", "coupon", "tablet" };
            state.relicSeen = new List<string> { "nothing" };
            state.relicFresh = new List<string> { "coupon", "tieba" };
            state.relicSchema = 1;
            var s = new XgSim(state);
            CollectionAssert.AreEqual(new[] { "tieba", "redbull" }, s.S.relicOwned);
            Assert.AreEqual(4, s.S.relicSlots.Count);
            Assert.AreEqual(1, s.S.relicSlots.Count(id => id == "redbull"), "never in two slots");
            Assert.IsFalse(s.S.relicSlots.Contains("ghost") || s.S.relicSlots.Contains("coupon"), "only owned relics are equipped");
            CollectionAssert.AreEqual(new[] { "tieba" }, s.S.relicFresh);
            Assert.IsTrue(s.RelicSeen("tieba") && s.RelicSeen("redbull"), "everything owned has been seen");
        }

        [Test] public void TheLegendariesAreOnlyInTheSourceText()
        {
            foreach (var r in XgRelics.All)
            {
                string zh = XgRelics.Source(r, false), en = XgRelics.Source(r, true);
                Assert.IsNotEmpty(zh); Assert.IsNotEmpty(en);
                if (r.rarity == XgRarity.Legendary) StringAssert.Contains("三选一", zh, r.id);
                else StringAssert.Contains("掉", zh, r.id);
            }
        }
    }
}
