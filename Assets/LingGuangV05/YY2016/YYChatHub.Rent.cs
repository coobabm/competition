using System;
using System.Globalization;
using System.Text.RegularExpressions;
using LingGuangV05.Runtime;

namespace LingGuangV05.Desktop.YY
{
    public sealed partial class YYChatHub
    {
        static readonly Regex RentTransferLine = new Regex(@"^\s*(?:转给你|转你|transfer(?:\s+you)?|pay(?:\s+you)?)\s*[¥￥]?\s*(\d+(?:\.\d{1,2})?)\s*(?:块|元)?\s*[。.!！]?\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        bool HandleLandlordLine(string text)
        {
            var match = RentTransferLine.Match(text);
            if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
            {
                TransferRent(amount, out _);
                return true;
            }
            if (text == "确认卖电脑" || text == "Confirm selling the computer") return false;
            var sim = runtime?.Sim;
            Receive("landlord", sim != null && sim.Debt > 0
                ? GameText.T("还差 ¥" + sim.Debt.ToString("0.00", CultureInfo.InvariantCulture) + "。可以先转一部分，在上面的转账卡里操作。", "¥" + sim.Debt.ToString("0.00", CultureInfo.InvariantCulture) + " remains. A partial payment is fine; use the transfer card above.")
                : GameText.T("现在没有待交的房租，到期我会在这里告诉你。", "No rent is due right now. I'll send the next bill here."));
            return true;
        }

        public bool TransferRent(double amount, out string problem)
        {
            var conv = Conversation("landlord");
            if (runtime?.Sim == null || conv == null) { problem = GameText.T("房东会话还没准备好。", "The landlord conversation is not ready."); return false; }
            int at = conv.messages.Count;
            if (!runtime.Sim.TransferRent(amount, out problem)) { Receive("landlord", problem); return false; }
            conv.messages.Insert(at, new YYMessage { from = Me, kind = YYKind.Transfer, transferAmount = amount, transferPaid = true,
                text = "¥" + amount.ToString("0.00", CultureInfo.InvariantCulture) + GameText.T(" 已转账", " transferred"), gameSeconds = Now });
            Touch();
            return true;
        }

        public void ReceiveRentRequest()
        {
            var conv = Conversation("landlord");
            var sim = runtime?.Sim;
            if (conv == null || sim == null || sim.Debt <= 0) return;
            conv.messages.Add(new YYMessage { from = "landlord", kind = YYKind.Transfer, transferAmount = sim.Debt, gameSeconds = Now,
                text = GameText.T("房租 · 待转账", "Rent · payment requested") });
            Touch();
        }

        public bool CurrentRentRequest(YYMessage message)
        {
            var conv = Conversation("landlord");
            if (conv == null || runtime?.Sim == null || runtime.Sim.Debt <= 0) return false;
            for (int i = conv.messages.Count - 1; i >= 0; i--)
                if (conv.messages[i].kind == YYKind.Transfer && !conv.messages[i].transferPaid) return ReferenceEquals(message, conv.messages[i]);
            return false;
        }
    }
}
