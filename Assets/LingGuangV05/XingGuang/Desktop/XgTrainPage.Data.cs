using LingGuangV05.XingGuang;
using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The training page's data-economy parts (design v1.1 §11.8.3–11.8.4): the noise line with its breakdown, the
    /// user-log button of the data flywheel (let the deployed model label the logs) and the pack tips.
    /// </summary>
    public sealed partial class XgTrainPage
    {
        XgBtn logAuto;

        void BuildDataEconomy(RectTransform right)
        {
            logAuto = ui.Button(right, "", ToggleLogAuto, 11);
            logAuto.rt.gameObject.name = "UserLogs";
            logAuto.label.enableAutoSizing = true; logAuto.label.fontSizeMin = 8; logAuto.label.fontSizeMax = 11;
            PlaceTopRight(logAuto, 14, 406, 150, 24);
            UiTip.Add(logAuto.rt, () => XgDataUi.LogTip(Sim, Sim.Run(Track).dataset));
            UiTip.Add(noiseText, () => XgDataUi.NoiseTip(Sim, Sim.Run(Track).dataset));
        }

        void RefreshDataEconomy(XgRun run)
        {
            string dataset = run.dataset;
            noiseText.text = XgDataUi.NoiseLine(Sim, dataset);
            double flips = Sim.BoardFlipRate(dataset);
            noiseText.color = flips >= .01 ? XgPalette.Bad : Sim.NoiseRatio(dataset) > 0 ? XgPalette.Ink : XgPalette.Muted;

            string logs = XgDataUi.LogLine(Sim, dataset);
            bool show = logs.Length > 0 || Sim.LogAutoOn(dataset);
            logAuto.Show(show);
            noiseText.rectTransform.offsetMax = new Vector2(show ? -170 : -14, noiseText.rectTransform.offsetMax.y);
            if (!show) return;
            bool on = Sim.LogAutoOn(dataset);
            bool can = on || Sim.LogAutoBlocker(dataset) == null;
            logAuto.Set((on ? "■ " : "□ ") + Lang.T("模型标日志 ") + "<size=9>" + XgUi.Samples(Sim.Logs(dataset)) + "</size>", can,
                on ? XgPalette.AccentSoft : XgPalette.Button, on ? XgPalette.Accent : XgPalette.Ink);
        }

        void ToggleLogAuto()
        {
            string dataset = Sim.Run(Track).dataset;
            bool on = !Sim.LogAutoOn(dataset);
            if (Sim.SetLogAuto(dataset, on)) Fx.Play(on ? XgJuice.Sfx.Id.Unlock : XgJuice.Sfx.Id.Click, 1, .5f);
            else Fx.Play(XgJuice.Sfx.Id.Thud, 1, .4f);
            view.Refresh(true);
        }

        /// <summary>The install button's note: the public pack, then every other source of this dataset.</summary>
        string PackTip(string dataset)
        {
            var download = XgDataUi.ActiveDownload(Sim, dataset);
            if (download != null) return XgDataUi.OfferTip(Sim, download);
            return Lang.T("装上完整数据包：一次补齐整套样本。") + "\n" + XgDataUi.SourcesSummary(Sim, dataset)
                + "\n<size=12><color=#68748C>" + Lang.T("杂包在「淘货」买，众包在标注台发。") + "</color></size>";
        }
    }
}
