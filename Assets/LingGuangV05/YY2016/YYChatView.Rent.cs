using System.Globalization;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.YY
{
    public sealed partial class YYChatView
    {
        static readonly Color TransferOrange = new Color32(240, 151, 56, 255);
        Button rentTool;
        RectTransform rentPanel;
        TMP_InputField rentAmount;
        TMP_Text rentTitle, rentProblem;

        void RefreshRentTools()
        {
            if (rentTool == null)
            {
                var tools = FindDeep(root, "Tools");
                var conversation = FindDeep(root, "Conversation");
                if (tools == null || conversation == null) return;
                rentTool = FlatButton(tools, "", OpenRentPanel, 13, Color.white, TransferOrange);
                rentTool.name = "RentTransfer";
                Place((RectTransform)rentTool.transform, new Vector2(1, .5f), new Vector2(-108, -13), new Vector2(-12, 13));
                AddFocus(rentTool.gameObject);
                rentPanel = Rect("RentTransferPanel", conversation, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-360, 180), new Vector2(-12, 360));
                Fill(rentPanel, new Color32(255, 247, 235, 255));
                rentTitle = Text(Rect("Title", rentPanel, new Vector2(0, 1), Vector2.one, new Vector2(12, -38), new Vector2(-12, -8)), "", 14, C.Ink, TextAlignmentOptions.MidlineLeft);
                var field = Rect("Amount", rentPanel, new Vector2(0, 1), Vector2.one, new Vector2(12, -90), new Vector2(-12, -42));
                Fill(field, Color.white);
                rentAmount = BuildInput(field);
                rentAmount.lineType = TMP_InputField.LineType.SingleLine;
                rentAmount.contentType = TMP_InputField.ContentType.DecimalNumber;
                rentAmount.characterLimit = 14;
                rentAmount.onSubmit.RemoveAllListeners();
                rentAmount.onSubmit.AddListener(_ => SubmitRent());
                var pay = FlatButton(rentPanel, GameText.T("转账", "Transfer"), SubmitRent, 14, Color.white, TransferOrange);
                Place((RectTransform)pay.transform, new Vector2(1, 0), new Vector2(-112, 36), new Vector2(-12, 70));
                var cancel = FlatButton(rentPanel, GameText.T("取消", "Cancel"), () => rentPanel.gameObject.SetActive(false), 14, C.Ink, C.Side);
                Place((RectTransform)cancel.transform, new Vector2(0, 0), new Vector2(12, 36), new Vector2(112, 70));
                rentProblem = Text(Rect("Problem", rentPanel, Vector2.zero, new Vector2(1, 0), new Vector2(12, 4), new Vector2(-12, 32)), "", 12, C.Badge, TextAlignmentOptions.MidlineLeft);
                foreach (var selectable in rentPanel.GetComponentsInChildren<Selectable>(true)) AddFocus(selectable.gameObject);
                rentPanel.gameObject.SetActive(false);
            }
            bool shown = hub.S.selected == "landlord";
            rentTool.gameObject.SetActive(shown);
            rentTool.GetComponentInChildren<TMP_Text>().text = GameText.T("￥ 转账", "￥ Transfer");
            if (!shown) rentPanel.gameObject.SetActive(false);
            if (rentPanel.gameObject.activeSelf) RefreshRentTitle();
        }

        void RefreshRentTitle()
        {
            var sim = hub.runtime.Sim;
            rentTitle.text = GameText.T("应付 ¥", "Due ¥") + sim.Debt.ToString("0.00", CultureInfo.InvariantCulture)
                + GameText.T(" · 钱包 ¥", " · Wallet ¥") + sim.S.money.ToString("0.00", CultureInfo.InvariantCulture);
        }

        void OpenRentPanel()
        {
            if (rentPanel == null) RefreshRentTools();
            if (rentPanel == null || hub.S.selected != "landlord") return;
            rentPanel.gameObject.SetActive(true);
            rentPanel.SetAsLastSibling();
            rentAmount.text = hub.runtime.Sim.Debt.ToString("0.00", CultureInfo.InvariantCulture);
            rentProblem.text = "";
            RefreshRentTitle();
            rentAmount.Select();
            rentAmount.ActivateInputField();
        }

        void SubmitRent()
        {
            if (hub.S.selected != "landlord") return;
            if (!double.TryParse(rentAmount.text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
            { rentProblem.text = GameText.T("请输入金额。", "Enter an amount."); return; }
            if (hub.TransferRent(amount, out var problem)) rentPanel.gameObject.SetActive(false);
            else rentProblem.text = problem;
            dirty = true;
        }

        float RentCard(float y, YYMessage message, bool mine, float width)
        {
            float cardWidth = Mathf.Min(300, width - 88), x = mine ? width - 60 - cardWidth : 60;
            var card = Rect("RentTransferCard", scrollContent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y - 100), new Vector2(x + cardWidth, -y));
            Fill(card, TransferOrange);
            bool active = hub.CurrentRentRequest(message);
            string status = message.transferPaid ? GameText.T("已转账", "Transferred") : active ? GameText.T("房租待付", "Rent due") : GameText.T("账单记录", "Invoice history");
            double amount = active ? hub.runtime.Sim.Debt : message.transferAmount;
            if (message.rentInvoiceMonth > 0)
                status = (message.rentInvoiceMonth % 100) + GameText.T(" 月 · ", " / ") + status;
            Text(Rect("Amount", card, new Vector2(0, 1), Vector2.one, new Vector2(12, -44), new Vector2(-12, -10)), "¥" + amount.ToString("0.00", CultureInfo.InvariantCulture), 22, Color.white, TextAlignmentOptions.MidlineLeft);
            Text(Rect("Status", card, Vector2.zero, new Vector2(1, 0), new Vector2(12, 12), new Vector2(-100, 40)), status, 13, Color.white, TextAlignmentOptions.MidlineLeft);
            if (active)
            {
                var button = FlatButton(card, GameText.T("转账", "Transfer"), OpenRentPanel, 13, TransferOrange, Color.white);
                Place((RectTransform)button.transform, new Vector2(1, 0), new Vector2(-92, 10), new Vector2(-12, 42));
                AddFocus(button.gameObject);
            }
            return 100;
        }
    }
}
