using System.Linq;
using LingGuang.Core;
using LingGuang.Game;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuang.Tests
{
    public sealed class IsaacHudTests
    {
        GameObject root;
        Hud hud;
        TMP_FontAsset font;

        [OneTimeSetUp]
        public void LoadFont()
        {
            font = TMP_FontAsset.CreateFontAsset(Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular"),
                64, 6, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
        }

        [OneTimeTearDown]
        public void ReleaseFont()
        {
            if (font == null) return;
            foreach (var texture in font.atlasTextures) if (texture != null) Object.DestroyImmediate(texture);
            Object.DestroyImmediate(font.material);
            Object.DestroyImmediate(font);
        }

        [SetUp]
        public void Build()
        {
            root = new GameObject("HUD test");
            hud = root.AddComponent<Hud>();
            hud.Build(font);
        }

        [TearDown] public void Cleanup() { Object.DestroyImmediate(root); }

        static RunState Populated()
        {
            var s = RunState.NewRun(new GameConfig(), 17);
            s.stage = 3; s.coins = 40; s.insight = 4;
            s.items.Clear(); s.items.AddRange(new[] { "earphones", "computer", "bicycle", "oldphoto", "diary" });
            s.drugs.Clear(); s.drugs.AddRange(new[] { "coffee", "feed" });
            s.shop.Clear();
            s.shop.Add(new ShopOffer { id = "bicycle", price = 5 });
            s.shop.Add(new ShopOffer { id = "oldphoto", price = 5, sold = true });
            s.shop.Add(new ShopOffer { id = "feed", price = 3 });
            s.candidates.Clear(); s.candidates.AddRange(new[] { Shape.Conduct, Shape.Cone, Shape.Converge });
            return s;
        }

        Button ButtonWith(string label) => hud.GetComponentsInChildren<Button>(true)
            .First(b => b.GetComponentInChildren<TextMeshProUGUI>().text.StartsWith(label));

        [Test]
        public void Refresh_PreservesRulesAndOriginalArt_UsesTransparentContainers()
        {
            var s = Populated();
            int coins = s.coins, count = s.sparks.Count, round = s.RoundIndex;
            var random = s.rng.Clone();
            hud.Refresh(s, false);
            Assert.That(s.coins, Is.EqualTo(coins));
            Assert.That(s.sparks.Count, Is.EqualTo(count));
            Assert.That(s.RoundIndex, Is.EqualTo(round));
            Assert.That(s.rng.Next(10000), Is.EqualTo(random.Next(10000)));
            Assert.That(hud.canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(hud.canvas.GetComponent<GraphicRaycaster>(), Is.Not.Null);
            foreach (var rt in new[] { hud.itemsPanel, hud.drugsPanel, hud.shopPanel, hud.candidateBar })
            { Assert.That(rt.GetComponent<Image>().raycastTarget, Is.False); Assert.That(rt.GetComponent<Image>().color.a, Is.Zero); }
            var icon = hud.candidateBar.GetComponentsInChildren<Image>().First(i => i.name == "NeuronIcon");
            Assert.That(icon.sprite, Is.SameAs(NeuronArt.Get(Shape.Conduct)));
            Assert.That(icon.raycastTarget, Is.False);
            Assert.That(hud.lightAnchor, Is.SameAs(hud.lightText.rectTransform));
        }

        [Test]
        public void Buttons_KeepAllControllerCallbacksAndCorrectIndices()
        {
            hud.Refresh(Populated(), false);
            int candidate = -1, buy = -1, drug = -1, confirms = 0, ends = 0, undo = 0, trials = 0, skip = 0, refresh = 0;
            hud.onCandidate = i => candidate = i; hud.onBuy = i => buy = i; hud.onUseDrug = i => drug = i;
            hud.onConfirm = () => confirms++; hud.onEndRound = () => ends++; hud.onUndo = () => undo++;
            hud.onTrial = () => trials++; hud.onSkipCandidates = () => skip++; hud.onRefreshShop = () => refresh++;
            ButtonWith("锥体").onClick.Invoke(); ButtonWith("自行车").onClick.Invoke(); ButtonWith("咖啡").onClick.Invoke();
            ButtonWith("发动").onClick.Invoke(); ButtonWith("结束本轮").onClick.Invoke(); ButtonWith("撤销").onClick.Invoke();
            ButtonWith("试运行").onClick.Invoke(); ButtonWith("放弃").onClick.Invoke(); ButtonWith("刷新").onClick.Invoke();
            Assert.That(new[] { candidate, buy, drug, confirms, ends, undo, trials, skip, refresh },
                Is.EqualTo(new[] { 1, 0, 0, 1, 1, 1, 1, 1, 1 }));
            Assert.That(ButtonWith("旧照片").interactable, Is.False);
        }

        [Test]
        public void Tooltip_HoverAndFocusSurviveNullBoardTooltip_ClearOnExitDisableAndModal()
        {
            hud.Refresh(Populated(), false);
            var h = hud.itemsPanel.GetComponentsInChildren<HudUiHint>().First();
            Assert.That(h.Description, Does.Contain(EffectDef.Get("earphones").desc));
            Assert.That(h.GetComponent<Selectable>(), Is.Not.Null);
            h.OnPointerEnter(null); hud.ShowTooltip(null, new Vector2(Screen.width, 0));
            Assert.That(hud.tooltipPanel.gameObject.activeSelf, Is.True);
            Assert.That(hud.tooltip.text, Does.Contain("跨区域"));
            Assert.That(hud.tooltipPanel.GetComponentsInChildren<Graphic>().All(g => !g.raycastTarget), Is.True);
            h.OnPointerExit(null); hud.ShowTooltip(null, Vector2.zero);
            Assert.That(hud.tooltipPanel.gameObject.activeSelf, Is.False);
            h.OnSelect(null); hud.ShowTooltip(null, Vector2.zero);
            Assert.That(hud.tooltipPanel.gameObject.activeSelf, Is.True);
            h.gameObject.SetActive(false); Assert.That(hud.ActiveUiHint, Is.Null);
            hud.ShowModal("标题", "<color=#ffd9a0>强调</color>", "继续", null);
            hud.ShowTooltip("must not cover modal", Vector2.zero);
            Assert.That(hud.tooltipPanel.gameObject.activeSelf, Is.False);
            Assert.That(hud.modalBody.text, Does.Contain("#713e19"));
        }

        [Test]
        public void RepeatedRefresh_ReplacesHintsWithoutDuplicateControls()
        {
            var s = Populated(); hud.Refresh(s, false);
            int buttons = hud.GetComponentsInChildren<Button>(true).Length;
            hud.itemsPanel.GetComponentInChildren<HudUiHint>().OnPointerEnter(null);
            hud.Refresh(s, false);
            Assert.That(hud.ActiveUiHint, Is.Null);
            Assert.That(hud.GetComponentsInChildren<Button>(true).Length, Is.EqualTo(buttons));
            Assert.That(hud.itemsPanel.GetComponentsInChildren<HudUiHint>().Length, Is.EqualTo(5));
        }

        [Test]
        public void ResourceAndInsightStates_AreRealNotInventedHealth()
        {
            var s = Populated(); s.startsLeft = 2; s.movesLeft = 1; s.insight = 0;
            hud.Refresh(s, false);
            Assert.That(hud.startsText.text, Is.EqualTo("02  起点次数"));
            Assert.That(hud.coinsText.text, Does.Contain("40  余光"));
            Assert.That(ButtonWith("复盘").interactable, Is.False);
            Assert.That(ButtonWith("再来一次").interactable, Is.False);
            s.insight = 4; hud.Refresh(s, false);
            Assert.That(ButtonWith("复盘").interactable, Is.True);
            Assert.That(ButtonWith("再来一次").interactable, Is.True);
            s.items.Remove("computer"); hud.Refresh(s, false);
            Assert.That(ButtonWith("试运行").gameObject.activeSelf, Is.False);
        }

        [TestCase(1920, 1080)]
        [TestCase(1821, 1138)]
        public void PopulatedLayout_StaysInsideReferenceCanvas(float width, float height)
        {
            hud.Refresh(Populated(), false);
            hud.canvas.GetComponent<CanvasScaler>().enabled = false;
            hud.canvas.renderMode = RenderMode.WorldSpace;
            var canvas = (RectTransform)hud.canvas.transform;
            canvas.sizeDelta = new Vector2(width, height);
            Canvas.ForceUpdateCanvases();
            var corners = new Vector3[4];
            foreach (var panel in new[] { hud.itemsPanel, hud.drugsPanel, hud.shopPanel, hud.candidateBar })
            {
                panel.GetWorldCorners(corners);
                foreach (var p in corners)
                {
                    var local = canvas.InverseTransformPoint(p);
                    Assert.That(local.x, Is.InRange(canvas.rect.xMin, canvas.rect.xMax), panel.name);
                    Assert.That(local.y, Is.InRange(canvas.rect.yMin, canvas.rect.yMax), panel.name);
                }
            }
            // Bottom cards must not overlap the right-hand action strip at either aspect ratio.
            Assert.That(hud.candidateBar.anchoredPosition.x + 712 + width / 2, Is.LessThan(width - 330));
        }

        [Test]
        public void PaperAndDoodles_AreCachedPixelArt_WithoutConsumingRandom()
        {
            var random = Random.state;
            Assert.That(IsaacHudSkin.PaperSprite, Is.SameAs(IsaacHudSkin.PaperSprite));
            Assert.That(IsaacHudSkin.Icon("coin"), Is.SameAs(IsaacHudSkin.Icon("coin")));
            Assert.That(IsaacHudSkin.PaperSprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(IsaacHudSkin.ItemGlyph("oldphoto"), Is.EqualTo("photo"));
            Assert.That(IsaacHudSkin.ItemGlyph("loveletter"), Is.EqualTo("book"));
            Assert.That(Random.state, Is.EqualTo(random));
        }
    }
}
