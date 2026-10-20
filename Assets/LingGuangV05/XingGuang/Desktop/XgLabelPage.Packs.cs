using System.Collections.Generic;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.Zhongbao;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;
namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XgLabelPage
    {
        readonly List<HallRow> teachingRows = new List<HallRow>();
        int teachingStage;
        int BuildTeachingPackRows(int start)
        {
            foreach (var r in teachingRows) MiniGames.XgMiniContext.DestroyObject(r.rt.gameObject);
            teachingRows.Clear(); teachingStage = Sim.S.stage;
            if (WorkContext) return 0;
            var packs = new List<XgTeachingPack>();
            foreach (var p in XgStageLessons.Desks) if (p.stage <= Sim.S.stage) packs.Add(p);
            packs.Sort((a,b) => {
                if(Sim.TeachingUncertainFirst){bool ao=Sim.TeachingPackAvailable(a.id),bo=Sim.TeachingPackAvailable(b.id);if(ao!=bo)return ao?-1:1;int unsure=TeachingSortScore(a.dataset).CompareTo(TeachingSortScore(b.dataset));if(unsure!=0)return unsure;}
                bool an = Sim.TeachingPackAvailable(a.id) && !Sim.S.teachingPacksSeen.Contains(a.id);
                bool bn = Sim.TeachingPackAvailable(b.id) && !Sim.S.teachingPacksSeen.Contains(b.id);
                return an != bn ? (an ? -1 : 1) : a.stage.CompareTo(b.stage);
            });
            foreach (var p in packs)
            {
                string id = p.id; int i = start + teachingRows.Count;
                var row = new HallRow { id = id };
                row.rt = Rect("TeachingPack " + id, hallContent, new Vector2(0,1), Vector2.one,
                    new Vector2(0,-(i+1)*HallRowH), new Vector2(0,-i*HallRowH));
                row.bg = Panel(row.rt, HallFill);
                var button = row.rt.gameObject.AddComponent<Button>(); button.targetGraphic = row.bg;
                button.onClick.AddListener(() => {
                    var owner = Sim;
                    if (owner == null || displayedSim != owner || !row.rt.gameObject.activeInHierarchy) return;
                    if (!owner.SelectTeachingPack(id)) {
                        host.ShowToast(owner.TeachingPackBlocker(p), 3);
                        // Purchased-but-uninstalled packs keep their installation guidance.
                        if (p.buy && !owner.Has(p.NodeId)) FocusTeachingPurchase(owner, p.NodeId);
                        return;
                    }
                    classicDesks.Remove(p.dataset); deskGame.Stop(); Refresh();
                });
                row.left = ui.Text(Rect("Text", row.rt, Vector2.zero, Vector2.one, new Vector2(10,2), new Vector2(-7,-2)), "",14,HallInk,TextAlignmentOptions.MidlineLeft);
                row.left.textWrappingMode = TextWrappingModes.Normal;
                row.left.overflowMode = TextOverflowModes.Truncate;
                row.pay = ui.Text(Rect("Pay",row.rt,Vector2.zero,Vector2.zero,Vector2.zero,Vector2.zero),"",12,HallMute,TextAlignmentOptions.MidlineLeft);
                UiTip.Add(row.rt, () => T(p.play,p.playEn));
                teachingRows.Add(row);
            }
            RefreshTeachingPackRows(); return teachingRows.Count;
        }
        void RefreshTeachingPackRows()
        {
            foreach (var row in teachingRows)
            {
                var p = XgTeachingPacks.Find(row.id); bool open = Sim.TeachingPackAvailable(row.id);
                bool fresh = open && !Sim.S.teachingPacksSeen.Contains(row.id);
                row.left.text = "<b>" + T(p.name,p.nameEn) + "</b>" + (fresh ? T(" <color=#CC4433>新</color>"," <color=#CC4433>NEW</color>") : "")
                    + "\n<size=11>" + (open ? T("点开这张题桌", "Open this question desk") : Sim.TeachingPackBlocker(p)) + "</size>";
                row.left.color = open ? HallInk : HallMute;
                row.bg.color = Sim.S.teachingPack == row.id ? HallSelected : HallFill;
            }
        }
    }
}

