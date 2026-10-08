using System;
using LingGuangV05.XingGuang;
using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The training page's data-economy parts (design v1.1 §11.8.3–11.8.4): the noise line with its breakdown, the
    /// user-log button of the data flywheel (let the deployed model label the logs) and the pack tips. They live in the
    /// 数据集 view of the 装备 card (XgTrainPage.Side.cs), together with the dataset list.
    /// </summary>
    public sealed partial class XgTrainPage
    {
        XgBtn logAuto;

        void BuildDataEconomy(RectTransform pane)
        {
            logAuto = ui.Button(pane, "", ToggleLogAuto, 11);
            logAuto.rt.gameObject.name = "UserLogs";
            logAuto.label.enableAutoSizing = true; logAuto.label.fontSizeMin = 8; logAuto.label.fontSizeMax = 11;
            PlaceTopRight(logAuto, 162, 2, 150, 22);
            UiTip.Add(logAuto.rt, () => XgDataUi.LogTip(Sim, Sim.Run(Track).dataset));
        }

        void RefreshDataEconomy(XgRun run)
        {
            string dataset = run.dataset;
            noiseText.text = XgDataUi.NoiseLine(Sim, dataset);
            double flips = Sim.BoardFlipRate(dataset);
            noiseText.color = flips >= .01 ? XgDark.Bad : Sim.NoiseRatio(dataset) > 0 ? XgDark.Ink : XgDark.Muted;

            string logs = XgDataUi.LogLine(Sim, dataset);
            bool show = logs.Length > 0 || Sim.LogAutoOn(dataset);
            logAuto.Show(show);
            noiseText.rectTransform.offsetMax = new Vector2(show ? -170 : -12, noiseText.rectTransform.offsetMax.y);
            if (!show) return;
            bool on = Sim.LogAutoOn(dataset);
            bool can = on || Sim.LogAutoBlocker(dataset) == null;
            logAuto.Set((on ? "■ " : "□ ") + Lang.T("模型标日志 ") + "<size=9>" + XgUi.Samples(Sim.Logs(dataset)) + "</size>", can,
                on ? XgDark.AccentSoft : XgDark.Button, on ? XgDark.Accent : XgDark.Ink);
        }

        void ToggleLogAuto()
        {
            string dataset = Sim.Run(Track).dataset;
            bool on = !Sim.LogAutoOn(dataset);
            if (Sim.SetLogAuto(dataset, on)) Fx.Play(on ? XgJuice.Sfx.Id.Unlock : XgJuice.Sfx.Id.Click, 1, .5f);
            else Fx.Play(XgJuice.Sfx.Id.Thud, 1, .4f);
            view.Refresh(true);
        }

        /// <summary>The 数据集 view of the 装备 card: noise and cleaning, the datasets with their packs, the GPU to drop a pack on.</summary>
        void RefreshDataPane(XgTrack track, XgRun run)
        {
            // Own mistakes plus outside noise from packs and crowd tasks, and the user-log button (XgTrainPage.Data.cs).
            double noise = Sim.Noise(run.dataset) + Sim.DataNoise(run.dataset);
            RefreshDataEconomy(run);
            bool audit = Sim.Has("label.audit");
            cleanNoise.Show(Sim.Has("label.audit"));
            int cleanable = audit ? Sim.CleanableNoise(run.dataset, Host, XgSim.NoiseCleanBatchLimit) : 0;
            string cleaningState = cleanable > 0 ? Lang.T("本次 ") + cleanable + " · ¥" + Money(cleanable * XgSim.NoiseCleanMoneyPerItem)
                : noise < 1 ? Lang.T("无噪声") : Sim.ProjectActive ? Lang.T("研发占用 GPU")
                : Host.Blocker != null || Host.Compute <= 0 ? Lang.T("无可用算力") : Lang.T("经费不足或暂不可清洗");
            cleanNoise.Set(Lang.T("清洗最多 ") + XgSim.NoiseCleanBatchLimit + T(" 条", " labels") + "\n<size=10>" + cleaningState + "</size>", cleanable > 0, cleanable > 0 ? XgDark.AccentSoft : XgDark.Button, cleanable > 0 ? XgDark.Accent : XgDark.Muted);
            noiseHint.text = audit
                ? "¥" + Money(XgSim.NoiseCleanMoneyPerItem) + " + " + N(XgSim.NoiseCleanGpuSecondsPerItem, "0.0") + Lang.T(" GPU秒 / 条") + "\n" + Lang.T("模拟数据清洗；不训练 GGUF")
                : Lang.T("模拟数据质量统计（非 GGUF 训练）");
            noiseHint.rectTransform.offsetMax = new Vector2(audit ? -180 : -12, noiseHint.rectTransform.offsetMax.y);

            dataTitle.text = Lang.T("数据包 · 拖到 GPU 安装");
            var data = XgCatalog.DatasetsFor(track).FindAll(x => Sim.DatasetAvailable(x.id) || Sim.NodeVisible(XgCatalog.Node(x.id + ".pack")));
            string dkey = string.Join(",", data.ConvertAll(x => x.id)) + track;
            if (dkey != shownDataKey)
            {
                shownDataKey = dkey;
                foreach (var b in dataBtns) UnityEngine.Object.Destroy(b.rt.gameObject);
                foreach (var b in packBtns) UnityEngine.Object.Destroy(b.rt.gameObject);
                dataBtns.Clear(); dataIds.Clear();
                packBtns.Clear();
                for (int i = 0; i < data.Count; i++)
                {
                    string id = data[i].id;
                    var b = ui.Button(dataBox, "", () => { if (Sim.SetDataset(Track, id)) { Fx.Play(XgJuice.Sfx.Id.Swoosh); Fx.Knock(dataBtns[dataIds.IndexOf(id)].rt, .12f); } Refresh(); }, 13);
                    b.rt.anchorMin = new Vector2(0, 1); b.rt.anchorMax = new Vector2(1, 1); b.rt.pivot = new Vector2(.5f, 1);
                    b.rt.offsetMin = new Vector2(0, -i * 36 - 32); b.rt.offsetMax = new Vector2(-98, -i * 36);
                    dataBtns.Add(b); dataIds.Add(id);
                    UiTip.Add(b.rt, () => { var d = XgCatalog.Dataset(id); return d == null ? "" : "<b>" + T(d.name, d.nameEn) + "</b>\n" + (string.IsNullOrEmpty(d.note) ? "" : T(d.note, d.noteEn) + "\n") + Lang.T("指标：") + T(d.metric, d.metricEn) + Lang.T("\n样本：") + Sim.Samples(id).ToString("0") + Lang.T("（不够就去标注台标，或买数据包）") + "\n" + XgDataUi.SourcesSummary(Sim, id); });
                    var install = ui.Button(dataBox, "", () => InstallPack(id), 12);
                    PlaceTopRight(install, 94, i * 36, 92, 32);
                    var drag = install.rt.gameObject.AddComponent<XgDataPackDrag>(); drag.Page = this; drag.Dataset = id;
                    UiTip.Add(install.rt, () => PackTip(id));
                    packBtns.Add(install);
                }
                dataBox.sizeDelta = new Vector2(0, data.Count * 36);
            }
            for (int i = 0; i < dataBtns.Count; i++)
            {
                var x = XgCatalog.Dataset(dataIds[i]);
                bool on = x.id == run.dataset;
                double sc = Sim.BestScore(x.id);
                string grade = Sim.BestAcc(x.id) > 0 ? "  " + Hex(on ? Color.white : XgDark.Grades[XgSim.Grade(sc)]) + XgCatalog.GradeNames[XgSim.Grade(sc)] + " " + N(sc, "0") + "</color>" : "";
                bool enough = Sim.Samples(x.id) >= XgCatalog.SamplesToTrain;
                dataBtns[i].Set(T(x.name, x.nameEn) + grade, Sim.DatasetAvailable(x.id) && (enough || on) && !run.epochActive, on ? XgDark.Accent : XgDark.Button, on ? Color.white : XgDark.Ink);
                var node = XgCatalog.Node(x.id + ".pack");
                // The public pack itself; a junk pack also makes Owns() true but leaves the public pack for sale.
                bool owned = Sim.S.owned.Contains(x.id) || node != null && Sim.Has(node.id);
                bool canBuy = node != null && Sim.Status(node, Host) == XgSim.NodeStatus.Buyable;
                packBtns[i].Show(node != null);
                // Any pack of this dataset still coming down (public, junk or story; XgSim.DataSources.cs).
                var download = XgDataUi.ActiveDownload(Sim, x.id);
                if (download != null)
                {
                    // 摆渡云 at 100KB/s: shows the time left; clicking pays for acceleration.
                    packBtns[i].Show(true);
                    double speed = Sim.AccelerateOfferCost(download.id);
                    bool rich = Host.Money >= speed;
                    packBtns[i].Set(N(download.downloadProgress * 100, "0") + Lang.T("% · 加速 ¥") + Money(speed), rich, rich ? XgDark.AccentSoft : XgDark.Button, rich ? XgDark.Accent : XgDark.Muted);
                }
                else packBtns[i].Set(owned ? Lang.T("已安装") : node != null ? "↓ ¥" + Money(Sim.NodeCost(node)) : "", !owned && canBuy, canBuy ? XgDark.AccentSoft : XgDark.Button, canBuy ? XgDark.Accent : XgDark.Muted);
            }
            // Pack switches of the current dataset below the list; also sizes the scroll content (XgTrainPage.Sources.cs).
            RefreshSources(run, dataBtns.Count);
            double watts = Host is XingGuangHost home ? home.TrainingWatts : 0;
            gpuLabel.text = "▣ GPU  · " + Lang.T("松手安装数据包") + "\n" +
                Lang.T("训练负载 ") + N(watts, "0") + " W  · " + Lang.T("累计 GPU 时间 ") + N(Sim.S.trainedSeconds, "0.0") + " s";
        }

        /// <summary>The install button's note: the public pack, then every other source of this dataset.</summary>
        string PackTip(string dataset)
        {
            var download = XgDataUi.ActiveDownload(Sim, dataset);
            if (download != null) return XgDataUi.OfferTip(Sim, download);
            return Lang.T("装上完整数据包：一次补齐整套样本。") + "\n" + XgDataUi.SourcesSummary(Sim, dataset)
                + "\n<size=12><color=#6F95A5>" + Lang.T("杂包在「淘货」买，众包在标注台发。") + "</color></size>";
        }
    }
}
