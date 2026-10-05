using System;
using System.Collections.Generic;
using System.Text;
using LingGuangV05.XingGuang;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// Text for the data economy (design v1.1 §11.8.3–11.8.4) shared by the label and training pages: where each
    /// dataset's data comes from, how dirty it is, downloads, and the user-log pile of the data flywheel.
    /// </summary>
    public static class XgDataUi
    {
        public static string Pct0(double v) => N(v * 100, v > 0 && v < .01 ? "0.0" : "0") + "%";

        /// <summary>Glyph of a source for the shop row's icon.</summary>
        public static string Glyph(XgDataSource s)
        {
            switch (s)
            {
                case XgDataSource.Junk: return T("杂", "J");
                case XgDataSource.Crowd: return T("众", "C");
                case XgDataSource.Story: return T("剧", "S");
                default: return T("包", "P");
            }
        }

        /// <summary>"淘货杂包 · 24 万条 · 标错 30% → 15% · 下载中 45%".</summary>
        public static string SourceLine(XgDataOffer o)
        {
            var sb = new StringBuilder();
            sb.Append(o.SourceLabel(En)).Append(" · ");
            if (o.source == XgDataSource.Crowd) sb.Append(N(o.samples, "0")).Append(T(" 条/秒 · 每条 ¥", " labels/s · ¥")).Append(N(o.price, "0.00")).Append(T("", " each"));
            else sb.Append(Samples(o.samples));
            sb.Append(T(" · 标错 ", " · wrong ")).Append(NoiseText(o));
            if (o.downloading) sb.Append(T(" · 下载中 ", " · downloading ")).Append(N(o.downloadProgress * 100, "0")).Append('%');
            return sb.ToString();
        }

        static string NoiseText(XgDataOffer o)
        {
            string raw = Pct0(o.noise);
            if (Math.Abs(o.effectiveNoise - o.noise) < 1e-9) return o.noise >= .15 ? "<color=#D63031>" + raw + "</color>" : raw;
            return "<s>" + raw + "</s> → <color=#1E9E5A>" + Pct0(o.effectiveNoise) + "</color>";
        }

        /// <summary>Hover note for one offer: what it is, its numbers, where to get it and why it may be locked.</summary>
        public static string OfferTip(XgSim sim, XgDataOffer o)
        {
            if (o == null) return "";
            var sb = new StringBuilder();
            sb.Append("<b>").Append(o.Name(En)).Append("</b>\n");
            if (!string.IsNullOrEmpty(o.Description(En))) sb.Append(o.Description(En)).Append('\n');
            sb.Append(SourceLine(o)).Append('\n');
            switch (o.source)
            {
                case XgDataSource.Public: sb.Append(T("公开数据，几乎没有标错。也能在「淘货」买到。", "Public data, hardly any wrong labels. Also sold on Taohuo.")); break;
                case XgDataSource.Junk:
                    sb.Append(T("便宜量大，但标错很多：单独用会拉低准确率。研究树的「数据清洗」能把标错减半。", "Cheap and big, but full of wrong labels: alone it drags accuracy down. 数据清洗 in the research tree halves them."));
                    break;
                case XgDataSource.Crowd: sb.Append(T("相当于雇人坐标注台：按条付钱，一直流入，少量标错。钱不够会自动下架。", "Like hiring people for your desk: paid per label, a steady trickle, a few mistakes. Taken down when the money runs out.")); break;
                case XgDataSource.Story: sb.Append(T("要靠关系才拿得到的干净数据。", "Clean data you only get through someone you know.")); break;
            }
            if (o.downloading) sb.Append('\n').Append(T("还剩 " + N(o.downloadLeft, "0") + " 秒；点一下开摆渡云超级会员加速 ¥", N(o.downloadLeft, "0") + " s left; click for a Bodu Cloud super-member speed-up ¥")).Append(Money(sim.AccelerateOfferCost(o.id)));
            else if (!o.owned && !o.available && o.lockedReason.Length > 0) sb.Append("\n<color=#D63031>").Append(T(o.lockedReason, o.lockedReasonEn)).Append("</color>");
            return sb.ToString();
        }

        /// <summary>Every source of a dataset in a few lines (training-page tips).</summary>
        public static string SourcesSummary(XgSim sim, string dataset)
        {
            var sb = new StringBuilder();
            foreach (var o in sim.OffersFor(dataset))
            {
                if (!o.owned && !o.available && o.source != XgDataSource.Public) continue;
                sb.Append(o.owned ? "■ " : "□ ").Append(o.Name(En)).Append("  <size=12><color=#68748C>").Append(SourceLine(o)).Append("</color></size>\n");
            }
            return sb.ToString().TrimEnd('\n');
        }

        /// <summary>"噪声 3.1% · 有效样本 …" for the training page's data line.</summary>
        public static string NoiseLine(XgSim sim, string dataset)
        {
            double own = sim.Noise(dataset), outside = sim.DataNoise(dataset);
            return T("当前数据：噪声 ", "Current data: noise ") + Pct0(sim.NoiseRatio(dataset))
                + T("（自己 ", " (own ") + N(own, "0") + T(" · 外来 ", " · outside ") + Samples(outside) + T("）", ")")
                + T(" · 有效样本 ", " · effective ") + Samples(sim.EffectiveLabelSamples(dataset)) + "/" + Samples(sim.Samples(dataset));
        }

        public static string NoiseTip(XgSim sim, string dataset)
        {
            var sb = new StringBuilder();
            sb.Append("<b>").Append(T("数据噪声（R5）", "Data noise (R5)")).Append("</b>\n");
            sb.Append(T("标错的样本把概念往反方向拉。少量会被大量对的样本冲掉；超过一成半就开始明显拖后腿。\n", "Wrong labels pull concepts the wrong way. A little is drowned out by the many right rows; past about 15% it starts to hurt.\n"));
            sb.Append(T("自动标注没被发现的错：", "Own uncaught automatic mistakes: ")).Append(N(sim.Noise(dataset), "0")).Append(T(" 条（超过样本 10% 会近亲繁殖）\n", " (over 10% of the samples means inbreeding)\n"));
            sb.Append(T("数据包和众包带来的错：", "Wrong rows from packs and crowd tasks: ")).Append(Samples(sim.DataNoise(dataset))).Append('\n');
            sb.Append(T("实际翻转的训练卡：", "Training cards actually flipped: ")).Append(Pct0(sim.BoardFlipRate(dataset))).Append('\n');
            sb.Append(sim.Has(XgSim.DataCleanId) ? T("「数据清洗」已买：杂包和众包的标错减半。", "数据清洗 owned: junk-pack and crowd mistakes are halved.")
                : T("研究树「数据清洗」（数据增强旁边）能把杂包和众包的标错减半。", "数据清洗 in the research tree (next to augmentation) halves junk-pack and crowd mistakes."));
            if (sim.Has("label.audit")) sb.Append('\n').Append(T("「清洗」按钮先扔掉自己的错，再扔外来的错。", "The clean button discards your own mistakes first, then outside ones."));
            return sb.ToString();
        }

        /// <summary>"日志 1.2 万条（+30/秒）" or empty when the flywheel has not started for this dataset.</summary>
        public static string LogLine(XgSim sim, string dataset)
        {
            double logs = sim.Logs(dataset), rate = sim.LogsPerSecond(dataset);
            if (logs < 1 && rate <= 0) return "";
            return T("用户日志 ", "User logs ") + Samples(logs) + (rate > 0 ? "（+" + N(rate, rate < 10 ? "0.0" : "0") + T("/秒）", "/s)") : "");
        }

        public static string LogTip(XgSim sim, string dataset)
        {
            var sb = new StringBuilder();
            sb.Append("<b>").Append(T("数据飞轮 · 用户日志", "Data flywheel · user logs")).Append("</b>\n");
            sb.Append(T("达标的订单会把用户日志传回来：没标注过的真实数据。里面有用户的聊天记录和隐私。\n", "Contracts that meet their threshold send back user logs: real, unlabelled data, with users' chats and private lives in it.\n"));
            sb.Append(T("现在：", "Now: ")).Append(Samples(sim.Logs(dataset))).Append(T("，每秒 +", ", +")).Append(N(sim.LogsPerSecond(dataset), "0.0")).Append(T("", " per second"));
            if (sim.S.stage < XgSim.FlywheelStage) sb.Append(T("（第 5 阶段前只有十分之一）", " (a tenth of it before stage 5)"));
            sb.Append('\n');
            if (XgCatalog.Desk(dataset) != null)
                sb.Append(T("在标注台亲手标：干净，算连击，一条顶 ", "Label them by hand on the desk: clean, keeps the combo, each worth ")).Append(N(XgSim.LogHandSamples, "0")).Append(T(" 个样本。\n", " samples.\n"));
            sb.Append(T("让模型自己标：很快，但它错多少就标错多少（现在约 ", "Let the model label them: fast, but it gets as many wrong as it does on tests (now about ")).Append(Pct0(sim.LogAutoNoise(dataset))).Append(T("），错的算它自己的噪声。", "), and those count as its own noise."));
            return sb.ToString();
        }

        /// <summary>The live download for a dataset (public, junk or story pack), or null.</summary>
        public static XgDataOffer ActiveDownload(XgSim sim, string dataset)
        {
            foreach (var o in sim.OffersFor(dataset)) if (o.downloading) return o;
            return null;
        }
    }
}
