from pathlib import Path
import difflib, hashlib, json

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
TARGET = 'Assets/LingGuangV05/XingGuang/Desktop/XgTechTreePage.cs'
source = ROOT / TARGET
original = source.read_text(encoding='utf-8-sig')
code = original

def replace(old, new):
    global code
    assert code.count(old) == 1, old[:100]
    code = code.replace(old, new)

replace('        const float TooltipWidth = 312;', '''        const float TooltipWidth = 312;
        RectTransform tipContent;
        ScrollRect tipScroll;
        LayoutElement tipHeadLayout, tipCostLayout;
        float tipExitAt = -1;
'''.rstrip())
replace('var bg = Panel(tooltip, new Color32(5, 9, 14, 250)); bg.sprite = skin.fill; bg.raycastTarget = false;',
'''var bg = Panel(tooltip, new Color32(5, 9, 14, 250)); bg.sprite = skin.fill; bg.raycastTarget = true;''')
replace('            var column = tooltip.gameObject.AddComponent<VerticalLayoutGroup>();', '''            var clip = Rect("Clip", tooltip, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-12, 0));
            clip.gameObject.AddComponent<RectMask2D>();
            tipContent = Rect("TipContent", clip, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
            tipContent.pivot = new Vector2(.5f, 1);
            tipScroll = tooltip.gameObject.AddComponent<ScrollRect>();
            tipScroll.viewport = clip; tipScroll.content = tipContent;
            tipScroll.horizontal = false; tipScroll.vertical = true;
            tipScroll.movementType = ScrollRect.MovementType.Clamped;
            tipScroll.inertia = false; tipScroll.scrollSensitivity = 24;
            var track = Rect("ScrollTrack", tooltip, new Vector2(1, 0), Vector2.one, new Vector2(-10, 4), new Vector2(-2, -4));
            Panel(track, XgDark.Card);
            var handle = Rect("Handle", track, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var handleImage = Panel(handle, XgDark.Muted);
            var scrollbar = track.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handle; scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            tipScroll.verticalScrollbar = scrollbar;
            // Reserve the gutter even when hidden, avoiding a wrap/scrollbar feedback loop.
            tipScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            var events = tooltip.gameObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => tipExitAt = -1); events.triggers.Add(enter);
            var leave = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            leave.callback.AddListener(_ => tipExitAt = Time.unscaledTime + .35f); events.triggers.Add(leave);
            var column = tipContent.gameObject.AddComponent<VerticalLayoutGroup>();''')
replace('            tooltip.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;',
'''            tipContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;''')
start, end = code.index('        void BuildTooltip()'), code.index('        void BuildEraCard()')
part = code[start:end]
for name in ['Header', 'Level', 'Description', 'CostBG', 'Line']:
    part = part.replace(f'Rect("{name}", tooltip,', f'Rect("{name}", tipContent,')
code = code[:start] + part + code[end:]
replace('            tipName.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;', '''            var nameLayout = tipName.gameObject.AddComponent<LayoutElement>();
            nameLayout.minWidth = 0; nameLayout.flexibleWidth = 1;
            tipHeadLayout = head.gameObject.AddComponent<LayoutElement>();''')
replace('            bar.gameObject.AddComponent<LayoutElement>().preferredHeight = 28;',
'''            tipCostLayout = bar.gameObject.AddComponent<LayoutElement>();
            tipCostLayout.preferredHeight = 28;''')
replace('            tipCost.fontStyle = FontStyles.Bold; tipCost.textWrappingMode = TextWrappingModes.NoWrap;',
'''            tipCost.fontStyle = FontStyles.Bold; tipCost.textWrappingMode = TextWrappingModes.Normal;''')
replace('''                if (hovered == v || !v.gameObject.activeInHierarchy) return;
                hovered = v;''', '''                tipExitAt = -1;
                if (hovered == v || !v.gameObject.activeInHierarchy) return;
                if (hovered != null)
                {
                    hovered.square.DOKill(); hovered.square.localScale = Vector3.one;
                    hovered.square.localRotation = Quaternion.identity;
                }
                hovered = v;''')
replace('''                hovered = null;
                v.square.DOKill(); v.square.localScale = Vector3.one; v.square.localRotation = Quaternion.identity;
                HideTooltip();''', '''                // Keep the card alive while the pointer crosses the gap to its scroll area.
                tipExitAt = Time.unscaledTime + .35f;
                v.square.DOKill(); v.square.localScale = Vector3.one; v.square.localRotation = Quaternion.identity;''')
start, end = code.index('        void ShowTooltip('), code.index('        void FillTooltip(')
code = code[:start] + '''        void ShowTooltip(XgTechNodeView v)
        {
            FillTooltip(v);
            tooltip.gameObject.SetActive(true); tooltip.SetAsLastSibling(); eraCard.SetAsLastSibling();
            // A size/rotation shake can cross the screen edge after placement has clamped the card.
            tooltip.DOKill(); tooltip.localRotation = Quaternion.identity; tooltip.localScale = Vector3.one;
            tipScroll.StopMovement(); tipContent.anchoredPosition = Vector2.zero;
            PlaceTooltip(v);
            tipScroll.verticalNormalizedPosition = 1;
        }

        void HideTooltip()
        {
            tipExitAt = -1;
            if (tooltip == null) return;
            tooltip.DOKill(); tooltip.localRotation = Quaternion.identity; tooltip.localScale = Vector3.one;
            tooltip.gameObject.SetActive(false);
        }

        /// <summary>Intersect the page with the visible screen, in page-local canvas units.</summary>
        Rect TooltipBounds()
        {
            var bounds = root.rect;
            var canvas = root.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var safe = Screen.safeArea;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(root, safe.min, camera, out var a)
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(root, safe.max, camera, out var b))
                bounds = Rect.MinMaxRect(Mathf.Max(bounds.xMin, Mathf.Min(a.x, b.x)), Mathf.Max(bounds.yMin, Mathf.Min(a.y, b.y)),
                    Mathf.Min(bounds.xMax, Mathf.Max(a.x, b.x)), Mathf.Min(bounds.yMax, Mathf.Max(a.y, b.y)));
            return bounds;
        }

        /// <summary>Wrap at the actual width, then cap height and expose the remainder by scrolling.</summary>
        void PlaceTooltip(XgTechNodeView v)
        {
            var bounds = TooltipBounds();
            if (bounds.width <= 8 || bounds.height <= 8) { HideTooltip(); return; }
            float w = XgTechTooltipGeometry.Width(bounds.width, TooltipWidth);
            tooltip.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
            // 12 gutter + 24 padding; keep the icon only when the title has usable space.
            float inner = Mathf.Max(1, w - 36);
            bool showIcon = inner >= 100;
            tipIcon.transform.parent.gameObject.SetActive(showIcon);
            tipHeadLayout.preferredHeight = Mathf.Max(showIcon ? 36 : 0,
                tipName.GetPreferredValues(tipName.text, Mathf.Max(1, inner - (showIcon ? 44 : 0)), Mathf.Infinity).y);
            tipCostLayout.preferredHeight = Mathf.Max(28, tipCost.GetPreferredValues(tipCost.text, Mathf.Max(1, inner - 16), Mathf.Infinity).y + 8);
            LayoutRebuilder.ForceRebuildLayoutImmediate(tipContent);
            float h = Mathf.Min(tipContent.rect.height, bounds.height - 8);
            tooltip.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
            // Preserve the current scroll position on live copy/size updates, clamped to the new extent.
            tipContent.anchoredPosition = new Vector2(0, Mathf.Clamp(tipContent.anchoredPosition.y, 0, Mathf.Max(0, tipContent.rect.height - h)));
            Vector2 top = root.InverseTransformPoint(v.square.TransformPoint(new Vector3(0, SquareSize * .5f, 0)));
            Vector2 bottom = root.InverseTransformPoint(v.transform.TransformPoint(new Vector3(0, -SquareSize * .5f - 62, 0)));
            XgTechTooltipGeometry.Place(bounds.xMin, bounds.yMin, bounds.xMax, bounds.yMax, w, h, top.x, top.y, bottom.y, out float x, out float y);
            tooltip.pivot = new Vector2(.5f, 1);
            // anchoredPosition uses the centre anchor; local bounds need not have a centred pivot.
            tooltip.anchoredPosition = new Vector2(x, y) - root.rect.center;
        }

''' + code[end:]
replace('''            if (!built) return;
            if (eraRemaining > 0)''', '''            if (!built) return;
            if (tipExitAt >= 0 && Time.unscaledTime >= tipExitAt)
            {
                hovered = null; HideTooltip(); PaintLinks();
                foreach (var v in views) if (v.gameObject.activeSelf)
                    v.ring.color = v == pinned ? new Color(1, 1, 1, .85f) : Color.clear;
            }
            if (eraRemaining > 0)''')
insert = '''    // Pure geometry kept separate so edge cases can run without Unity or player state.
    internal static class XgTechTooltipGeometry
    {
        internal static float Width(float available, float preferred) => Math.Max(1, Math.Min(preferred, available - 8));

        // x is the centre, y is the top edge. Callers cap w/h before placement.
        internal static void Place(float left, float bottom, float right, float top,
            float w, float h, float nodeX, float nodeTop, float nodeBottom, out float x, out float y)
        {
            float desiredTop = nodeTop + 10 + h <= top - 4 ? nodeTop + 10 + h : nodeBottom - 4;
            x = Math.Max(left + w * .5f + 4, Math.Min(nodeX, right - w * .5f - 4));
            y = Math.Max(bottom + h + 4, Math.Min(desiredTop, top - 4));
        }
    }

'''
replace('    /// <summary>A label that keeps its Chinese and English text and shows the one for the current language.</summary>',
insert + '    /// <summary>A label that keeps its Chinese and English text and shows the one for the current language.</summary>')

deps = [TARGET, 'Assets/LingGuangV05/XingGuang/Desktop/XgUi.cs', 'Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs', 'Assets/LingGuangV05/XingGuang/Core/XgTechTree.cs']
manifest = {p: hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in deps}
assert manifest[TARGET] == 'f27cc730c4871776fc3e29fb95d765c099c02007889648334c42e3491ca8a523', 'Source changed; review before rebuilding patch'
(HERE/'source-sha256.json').write_text(json.dumps(manifest, indent=2)+'\n', encoding='utf-8')
patch = ''.join(difflib.unified_diff(original.splitlines(True), code.splitlines(True), fromfile='a/'+TARGET, tofile='b/'+TARGET, n=5))
patch = '# Base-SHA256: ' + manifest[TARGET] + '\n# Target: ' + TARGET + '\n' + patch
(HERE/'changes.patch').write_text(patch, encoding='utf-8', newline='')
(HERE/'proposed-XgTechTreePage.cs').write_text(code, encoding='utf-8')
print('Created patch and proposed source in task directory only.')
