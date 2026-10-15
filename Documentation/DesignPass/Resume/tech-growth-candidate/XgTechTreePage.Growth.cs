using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    // Presentation for 4:004 / the explicitly referenced merge plan §3.2.
    // Every purchase, threshold and stock quantity stays owned by the existing simulation.
    public sealed partial class XgTechTreePage
    {
        sealed class GrowthAbility
        {
            public RectTransform root, paramsFill, samplesFill;
            public Image rim;
            public TMP_Text name, status, description, parameters, samples;
        }
        sealed class GrowthPack
        {
            public XgConsumableDef definition;
            public RectTransform root;
            public TMP_Text name, effect, stock;
            public XgBtn buy;
        }
        readonly GrowthAbility[] growthAbilities = new GrowthAbility[XgSim.AbilityCount];
        readonly List<GrowthPack> growthPacks = new List<GrowthPack>();
        RectTransform growthShelf, growthShelfContent;
        TMP_Text growthShelfTitle;
        XgBtn growthShelfToggle, growthShelfClose;
        bool growthShelfExpanded = true, growthShelfForced;
        float growthHeaderHeight = 154;

        // The catalog stage is the time a thing becomes visible, not necessarily the ability
        // whose threshold it lowers. Keep that real target for discount items.
        public static int GrowthColumn(XgTechEntry entry)
        {
            int target = XgSim.AbilityCount;
            var discounts = XgSim.DiscountsOf(entry.First.id);
            foreach (var discount in discounts) target = Math.Min(target, discount.ability);
            return discounts.Count > 0 ? Math.Max(0, target - 1) : Math.Min(XgSim.AbilityCount - 1, Math.Max(0, entry.Stage));
        }

        TMP_Text BuildGrowthAbility(int column)
        {
            var rt = TopLeft("GrowthAbility " + (column + 1), content, column * StageWidth + 24, 12, StageWidth - 48, 154);
            var row = new GrowthAbility { root = rt, rim = Panel(rt, XgDark.Line) };
            row.rim.raycastTarget = false;
            Panel(Rect("Inner", rt, Vector2.zero, Vector2.one, Vector2.one, -Vector2.one), XgDark.Card).raycastTarget = false;
            row.name = ui.Text(Strip("Name", rt, 10, 28, 12, 150), "", 20, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            row.status = ui.Text(Strip("Status", rt, 10, 28, StageWidth - 210, 12), "", 13, XgDark.Muted, TextAlignmentOptions.MidlineRight);
            row.description = ui.Text(Strip("Description", rt, 43, 38, 12, 12), "", 13, XgDark.Muted, TextAlignmentOptions.TopLeft);
            row.parameters = ui.Text(Strip("Parameters", rt, 83, 23, 12, 12), "", 14, XgDark.Params, TextAlignmentOptions.MidlineLeft);
            row.samples = ui.Text(Strip("Samples", rt, 116, 23, 12, 12), "", 14, XgDark.Data, TextAlignmentOptions.MidlineLeft);
            row.paramsFill = Bar(Strip("ParameterBar", rt, 107, 4, 12, 12), "Fill", XgDark.Track, XgDark.Params);
            row.samplesFill = Bar(Strip("SampleBar", rt, 140, 4, 12, 12), "Fill", XgDark.Track, XgDark.Data);
            growthAbilities[column] = row;
            if (column + 1 < XgSim.AbilityCount)
                ui.Text(TopLeft("GrowthArrow " + column, content, (column + 1) * StageWidth - 22, 65, 44, 32), "→", 22, XgDark.Muted, TextAlignmentOptions.Center).raycastTarget = false;
            return row.name;
        }

        void BuildGrowthShelf()
        {
            growthShelfToggle = ui.Button(root, "", () => SetGrowthShelf(!GrowthShelfVisible), 13);
            growthShelfToggle.rt.gameObject.name = "GrowthShelfToggle";
            growthShelf = Rect("GrowthConsumables", root, new Vector2(1, 0), Vector2.one, new Vector2(-234, 1), new Vector2(-1, -78));
            Panel(growthShelf, XgDark.Card);
            growthShelfTitle = ui.Text(Strip("Title", growthShelf, 8, 30, 10, 44), "", 15, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            growthShelfClose = ui.Button(growthShelf, "×", () => SetGrowthShelf(false), 18);
            growthShelfClose.rt.gameObject.name = "GrowthShelfClose";
            PlaceTopRight(growthShelfClose, 38, 8, 28, 28);
            var list = Rect("List", growthShelf, Vector2.zero, Vector2.one, new Vector2(5, 5), new Vector2(-5, -44));
            Panel(list, Color.clear); list.gameObject.AddComponent<RectMask2D>();
            growthShelfContent = Rect("Content", list, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
            growthShelfContent.pivot = new Vector2(.5f, 1);
            var scroll = list.gameObject.AddComponent<ScrollRect>();
            scroll.content = growthShelfContent; scroll.viewport = list; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30; scroll.inertia = false;
            float y = 0;
            foreach (var definition in XgConsumables.All)
            {
                var pack = new GrowthPack { definition = definition };
                pack.root = Strip("GrowthPack " + definition.id, growthShelfContent, y, 170, 3, 3); y += 178;
                Panel(pack.root, XgDark.Panel).raycastTarget = false;
                pack.name = ui.Text(Strip("Name", pack.root, 8, 42, 8, 8), "", 15, XgDark.Ink, TextAlignmentOptions.TopLeft);
                pack.effect = ui.Text(Strip("Effect", pack.root, 52, 52, 8, 8), "", 13, XgDark.Muted, TextAlignmentOptions.TopLeft);
                pack.stock = ui.Text(Strip("Stock", pack.root, 108, 23, 8, 8), "", 13, XgDark.Good, TextAlignmentOptions.MidlineLeft);
                pack.buy = ui.Button(pack.root, "", () => BuyGrowthPack(pack), 13);
                pack.buy.rt.gameObject.name = "GrowthBuyPack " + definition.id;
                pack.buy.label.textWrappingMode = TextWrappingModes.Normal;
                PlaceBottom(pack.buy, 8, 28, 8, 8);
                growthPacks.Add(pack);
            }
            growthShelfContent.sizeDelta = new Vector2(0, y);
            LayoutGrowthShelf();
        }

        public bool GrowthShelfVisible => growthShelf != null && growthShelf.gameObject.activeSelf;
        public RectTransform GrowthAbilityTarget(int ability) => ability >= 1 && ability <= growthAbilities.Length ? growthAbilities[ability - 1]?.root : null;
        public RectTransform GrowthPackTarget(string id) => growthPacks.Find(pack => pack.definition.id == id)?.buy.rt;
        public int GrowthBuyableCount()
        {
            int count = 0;
            foreach (var entry in XgTechTree.Entries)
                if (XgTechTree.Visible(entry, Sim) && XgTechTree.Status(entry, Sim, Host) == XgSim.NodeStatus.Buyable) count++;
            return count;
        }

        public void SetGrowthShelf(bool open)
        {
            growthShelfExpanded = open; growthShelfForced = open;
            hovered = null; HideTooltip(); RefreshGrowth();
            Pan.Clamp();
        }

        void LayoutGrowthShelf()
        {
            if (growthShelf == null) return;
            float width = root.rect.width;
            bool open = growthShelfExpanded && (width >= 720 || growthShelfForced);
            bool docked = width >= 720;
            float shelfWidth = Mathf.Min(233, Mathf.Max(1, width - 8));
            growthShelf.offsetMin = new Vector2(-shelfWidth - 1, 1);
            growthShelf.gameObject.SetActive(open);
            viewport.offsetMax = new Vector2(open && docked ? -shelfWidth - 8 : -1, -78);
            growthShelfToggle.rt.anchorMin = growthShelfToggle.rt.anchorMax = new Vector2(0, 1);
            growthShelfToggle.rt.offsetMin = new Vector2(width >= 600 ? 232 : 12, -73);
            growthShelfToggle.rt.offsetMax = new Vector2(width >= 600 ? 344 : Mathf.Min(124, width - 8), -45);
            var legend = root.Find("Legend"); if (legend != null) legend.gameObject.SetActive(width >= 850);
            miniMap.gameObject.SetActive(width >= 600);
            if (open) growthShelf.SetAsLastSibling();
            tooltip?.SetAsLastSibling(); eraCard?.SetAsLastSibling();
        }

        void RefreshGrowth()
        {
            LayoutGrowthShelf();
            for (int i = 0; i < growthAbilities.Length; i++)
            {
                int ability = i + 1;
                var row = growthAbilities[i];
                bool learned = Sim.HasAbility(ability), next = Sim.NextAbility == ability;
                row.name.text = ability + " · " + XgSim.AbilityName(ability, Sim.English);
                row.rim.color = learned ? XgDark.Good : next ? XgDark.Params : XgDark.Line;
                row.status.text = learned ? T("已学会", "Learned") : next ? T("下一项", "Next") : T("尚未学会", "Not yet");
                row.description.text = T(XgSim.AbilityUnlocks[ability], XgSim.AbilityUnlocksEn[ability]);
                row.parameters.text = ability == 1 ? T("参数：起始即有", "Parameters: available from the start")
                    : T("参数 ", "Parameters ") + XgSim.ParamsText(Sim.TrainedParamsK) + " / " + XgSim.ParamsText(Sim.ParamsThreshold(ability))
                    + GrowthFormer(XgSim.ParamsText(XgSim.AbilityParamsK[ability]), Sim.ParamsDiscount(ability) < 1);
                row.samples.text = ability == 1 ? T("样本：起始即有", "Samples: available from the start")
                    : T("有效样本 ", "Effective samples ") + XgSim.SamplesText(Sim.TrainedSamples) + " / " + XgSim.SamplesText(Sim.SamplesThreshold(ability))
                    + GrowthFormer(XgSim.SamplesText(XgSim.AbilitySamples[ability]), Sim.SamplesDiscount(ability) < 1);
                SetBar(row.paramsFill, ability == 1 ? 1 : (float)Sim.ParamsProgress(ability));
                SetBar(row.samplesFill, ability == 1 ? 1 : (float)Sim.SamplesProgress(ability));
            }
            growthShelfTitle.text = T("消耗品", "Consumables");
            growthShelfToggle.Set(T("消耗品", "Consumables") + (GrowthShelfVisible ? " −" : " +"), true);
            foreach (var pack in growthPacks)
            {
                var definition = pack.definition;
                int stock = Sim.ConsumableCount(definition.id);
                bool full = stock + definition.pack > XgSim.MaxStock;
                pack.name.text = T(definition.name, definition.nameEn);
                pack.effect.text = T(definition.note, definition.noteEn);
                pack.stock.text = T("库存 ", "Stock ") + stock + " / " + XgSim.MaxStock;
                pack.buy.Set(full ? T("已满", "Full") : "¥" + N(definition.price, "0") + " / " + definition.pack,
                    !full && Host.Money + 1e-9 >= definition.price, null, XgDark.Money);
            }
            LayoutGrowthAbilities();
            LayoutGrowthPacks();
        }

        static string GrowthFormer(string before, bool changed) => changed ? "\n<size=12><color=#6F95A5><s>" + T("原 ", "was ") + before + "</s></color></size>" : "";

        static void GrowthStrip(RectTransform rect, float top, float height, float left, float right)
        {
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, -top - height); rect.offsetMax = new Vector2(-right, -top);
        }

        static float GrowthTextHeight(TMP_Text text, float width, float minimum)
            => Mathf.Max(minimum, Mathf.Ceil(text.GetPreferredValues(text.text, Mathf.Max(1, width), Mathf.Infinity).y) + 2);

        void LayoutGrowthAbilities()
        {
            float maximum = 154, width = StageWidth - 48;
            foreach (var row in growthAbilities)
            {
                float title = Mathf.Max(GrowthTextHeight(row.name, width - 162, 28), GrowthTextHeight(row.status, 138, 28));
                GrowthStrip(row.name.rectTransform, 10, title, 12, 150);
                GrowthStrip(row.status.rectTransform, 10, title, width - 150, 12);
                float y = 10 + title + 5;
                foreach (var text in new[] { row.description, row.parameters, row.samples })
                {
                    float height = GrowthTextHeight(text, width - 24, text == row.description ? 20 : 23);
                    GrowthStrip(text.rectTransform, y, height, 12, 12); y += height + 5;
                    if (text == row.parameters || text == row.samples)
                    {
                        var bar = text == row.parameters ? row.paramsFill : row.samplesFill;
                        GrowthStrip((RectTransform)bar.parent, y, 4, 12, 12); y += 10;
                    }
                }
                maximum = Mathf.Max(maximum, y + 5);
            }
            foreach (var row in growthAbilities)
            {
                row.root.offsetMin = new Vector2(row.root.offsetMin.x, -12 - maximum);
                row.root.offsetMax = new Vector2(row.root.offsetMax.x, -12);
            }
            // Keep all node coordinates, focus targets and connectors in the same content space.
            // Changing language or gaining a discount can add a line without covering the first lane.
            float delta = maximum - growthHeaderHeight;
            if (Mathf.Abs(delta) > .1f)
            {
                growthHeaderHeight = maximum; treeHeight += delta;
                foreach (var lane in Bands) if (bandTop.ContainsKey(lane)) bandTop[lane] += delta;
                foreach (RectTransform stripe in bandLayer) stripe.anchoredPosition += Vector2.down * delta;
                foreach (var title in laneTitles) title.anchoredPosition += Vector2.down * delta;
                foreach (var view in views)
                {
                    view.home += Vector2.down * delta; position[view.entry] = view.home;
                    ((RectTransform)view.transform).anchoredPosition = view.home;
                }
                foreach (var link in links) link.image.rectTransform.anchoredPosition += Vector2.down * delta;
            }
            content.sizeDelta = new Vector2(content.sizeDelta.x, treeHeight);
        }

        void LayoutGrowthPacks()
        {
            float width = growthShelf.rect.width;
            float titleHeight = GrowthTextHeight(growthShelfTitle, width - 54, 30);
            GrowthStrip(growthShelfTitle.rectTransform, 8, titleHeight, 10, 44);
            var list = (RectTransform)growthShelfContent.parent;
            list.offsetMax = new Vector2(-5, -titleHeight - 16);
            float cardWidth = Mathf.Max(1, width - 16), top = 0;
            foreach (var pack in growthPacks)
            {
                float y = 8;
                foreach (var text in new[] { pack.name, pack.effect, pack.stock })
                {
                    float height = GrowthTextHeight(text, cardWidth - 16, text == pack.stock ? 23 : 20);
                    GrowthStrip(text.rectTransform, y, height, 8, 8); y += height + 6;
                }
                float buyHeight = GrowthTextHeight(pack.buy.label, cardWidth - 28, 28);
                GrowthStrip(pack.buy.rt, y, buyHeight, 8, 8);
                float cardHeight = y + buyHeight + 8;
                GrowthStrip(pack.root, top, cardHeight, 3, 3); top += cardHeight + 8;
            }
            growthShelfContent.sizeDelta = new Vector2(0, top);
        }

        void BuyGrowthPack(GrowthPack pack)
        {
            if (Sim.BuyConsumable(pack.definition.id, Host)) { Fx.Play(XgJuice.Sfx.Id.Coin); host.Refresh(true); }
            else Fx.Play(XgJuice.Sfx.Id.Thud);
        }

        public void FocusGrowthAbility(int ability)
        {
            if (!built) return;
            int column = Math.Max(0, Math.Min(XgSim.AbilityCount - 1, ability - 1));
            Canvas.ForceUpdateCanvases();
            Pan.FitColumn(column * StageWidth + StageWidth * .5f);
        }

        bool GrowthLaneVisible(string lane, int column)
        {
            foreach (var entry in XgTechTree.Entries)
                if (entry.First.kind != XgNodeKind.Ability && GrowthColumn(entry) == column && XgCatalog.Band(entry.Lane) == lane && XgTechTree.Visible(entry, Sim)) return true;
            return false;
        }
    }
}
