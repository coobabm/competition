using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XgTrainPage
    {
        const float DataInstallHintHeight = 84;
        RectTransform dataInstallHint;
        TMP_Text dataInstallHeading, dataInstallInstructions;
        Outline dataInstallFocus;
        float dataInstallFocusUntil;

        public void FocusDataInstallArea()
        {
            RefreshDataInstallHint();
            dataInstallFocusUntil = Time.unscaledTime + 4;
            TickDataInstallFocus();
        }

        void TickDataInstallFocus()
        {
            if (dataInstallFocus == null) return;
            bool focused = Time.unscaledTime < dataInstallFocusUntil;
            dataInstallFocus.effectColor = new Color(.25f, .9f, .72f, focused ? .7f + .2f * Mathf.Sin(Time.unscaledTime * 5) : .28f);
            dataInstallFocus.effectDistance = focused ? new Vector2(2, -2) : new Vector2(1, -1);
        }

        void BuildDataInstallHint()
        {
            // This banner is inside the real root drop target, not a second installation action.
            dataInstallHint = ui.Card(root, "DataInstallHint", new Vector2(0, 1), Vector2.one,
                new Vector2(0, -DataInstallHintHeight), Vector2.zero);
            dataInstallFocus = dataInstallHint.gameObject.AddComponent<Outline>();
            TickDataInstallFocus();
            dataInstallHeading = ui.Text(Rect("Status", dataInstallHint, new Vector2(0, 1), Vector2.one,
                new Vector2(12, -28), new Vector2(-12, -6)), "", 14, XgDark.Data, TextAlignmentOptions.MidlineLeft);
            dataInstallInstructions = ui.Text(Rect("Instructions", dataInstallHint, Vector2.zero, Vector2.one,
                new Vector2(12, 6), new Vector2(-12, -30)), "", 13, XgDark.Muted, TextAlignmentOptions.TopLeft);
            dataInstallInstructions.richText = false;
            dataInstallInstructions.textWrappingMode = TextWrappingModes.Normal;
            dataInstallHint.gameObject.SetActive(false);
        }

        void RefreshDataInstallHint()
        {
            if (dataInstallHint == null) return;
            bool available = Sim.CanInstallData;
            bool show = available || Sim.S.dataFiles.Count > 0 || Sim.S.downloadQueue.Count > 0;
            dataInstallHint.gameObject.SetActive(show);
            float inset = show ? DataInstallHintHeight + RowGap : 0;
            left.offsetMax = new Vector2(left.offsetMax.x, -inset);
            right.offsetMax = new Vector2(right.offsetMax.x, -inset);
            if (!show) return;
            if (!available)
            {
                dataInstallHeading.text = T("数据包安装区 · 尚未解锁", "Data Pack Installation · Locked");
                dataInstallInstructions.text = Sim.DataInstallBlocker();
                return;
            }

            int ready = 0, installed = 0;
            foreach (var file in Sim.S.dataFiles)
            {
                if (file == null) continue;
                if (file.installed) installed++;
                else if (Sim.DataFileReady(file.fileId)) ready++;
            }
            dataInstallHeading.text = T("数据包安装区 · ", "Data Pack Installation · ") + (ready > 0
                ? T(ready + " 个待安装", ready + " ready to install")
                : installed > 0 ? T("已安装 " + installed + " 个，暂无待安装包", installed + " installed; none waiting")
                : T("暂无可安装包", "No packs ready yet"));
            string folder = T("资料文件夹 → 训练数据", "Training files → Training data");
            dataInstallInstructions.text = (ready > 0
                ? T("拖到这里安装数据包：打开「" + folder + "」，拖入待安装包。", "Drop data packs here to install: open " + folder + " and drag in ready packs.")
                : T("下载完成后，在「" + folder + "」找到数据包，拖到这里安装。", "After downloading, find packs in " + folder + " and drag them here to install."))
                + "\n" + T("按住 Ctrl 多选后，可一起拖入。", "Hold Ctrl to select several packs, then drag them together.");
        }
    }
}
