using System;
using System.Collections.Generic;
using System.Text;
using LingGuangV05.XingGuang;
using static LingGuangV05.Desktop.XingGuang.XgUi;

using LingGuangV05.Core;
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
                case XgDataSource.Junk: return Lang.T("杂");
                case XgDataSource.Crowd: return T("众", "C");
                case XgDataSource.Story: return Lang.T("剧");
                case XgDataSource.Crawl: return T("爬", "W");
                default: return Lang.T("包");
            }
        }

        /// <summary>"淘货杂包 · 24 万条 · 标错 30% → 15% · 下载中 45%".</summary>
        public static string SourceLine(XgDataOffer o)
        {
            var sb = new StringBuilder();
            sb.Append(o.SourceLabel(En)).Append(" · ");
            if (o.source == XgDataSource.Crowd) sb.Append(N(o.samples, "0")).Append(Lang.T(" 条/秒 · 每条 ¥")).Append(N(o.price, "0.00")).Append(T("", " each"));
            else if (o.source == XgDataSource.Crawl) sb.Append(T("每次最多 " + N(o.samples, "0") + " 条 · ¥" + N(o.price, "0"), "up to " + N(o.samples, "0") + " rows a run · ¥" + N(o.price, "0")));
            else sb.Append(Samples(o.samples));
            sb.Append(Lang.T(" · 标错 ")).Append(NoiseText(o));
            if (o.downloading) sb.Append(Lang.T(" · 下载中 ")).Append(N(o.downloadProgress * 100, "0")).Append('%');
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
                case XgDataSource.Public: sb.Append(Lang.T("公开数据，几乎没有标错。也能在「淘货」买到。")); break;
                case XgDataSource.Junk:
                    sb.Append(Lang.T("便宜量大，但标错很多：单独用会拉低准确率。研究树的「数据清洗」能把标错减半。"));
                    break;
                case XgDataSource.Crowd: sb.Append(Lang.T("相当于雇人坐标注台：按条付钱，一直流入，少量标错。钱不够会自动下架。")); break;
                case XgDataSource.Story: sb.Append(Lang.T("要靠关系才拿得到的干净数据。")); break;
                case XgDataSource.Crawl:
                    sb.Append(T("按次付钱：爬虫在页面上踩到一个词就抓回一条，时间到或抓满为止；中途收工也留下已经抓到的。这些语料计入数据总量，少量广告和错字算标错。",
                        "Paid per run: every word the crawler steps on comes back as a row until time is up or the run is full; stopping early keeps what it grabbed. The rows count toward the data total; a few adverts and typos count as wrong labels."));
                    break;
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
                bool on = o.owned && o.included;
                sb.Append(on ? "■ " : "□ ").Append(o.Name(En)).Append("  <size=12><color=#68748C>").Append(SourceLine(o)).Append("</color></size>")
                    .Append(o.owned && !o.included ? " <color=#D63031>" + Lang.T("训练不用") + "</color>" : "").Append('\n');
            }
            sb.Append(AlwaysOnLine(sim, dataset));
            return sb.ToString();
        }

        /// <summary>"手标 1,800 条 · 众包 / 日志 300 条（总会用上）": the sources that always train, next to the pack switches.</summary>
        public static string AlwaysOnLine(XgSim sim, string dataset)
        {
            return Lang.T("手标 ") + Samples(sim.Labels(dataset)) + Lang.T(" · 众包 / 日志 ") + Samples(sim.ExtraSamples(dataset))
                + Lang.T("（总会用上）· 合计 ") + Samples(sim.Samples(dataset));
        }

        /// <summary>"噪声 3.1% · 有效样本 …" for the training page's data line.</summary>
        public static string NoiseLine(XgSim sim, string dataset)
        {
            double own = sim.Noise(dataset), outside = sim.DataNoise(dataset);
            return Lang.T("当前数据：噪声 ") + Pct0(sim.NoiseRatio(dataset))
                + Lang.T("（自己 ") + N(own, "0") + Lang.T(" · 外来 ") + Samples(outside) + Lang.T("）")
                + Lang.T(" · 有效样本 ") + Samples(sim.EffectiveLabelSamples(dataset)) + "/" + Samples(sim.Samples(dataset));
        }

        public static string NoiseTip(XgSim sim, string dataset)
        {
            var sb = new StringBuilder();
            sb.Append("<b>").Append(Lang.T("数据噪声（R5）")).Append("</b>\n");
            sb.Append(Lang.T("标错的样本把概念往反方向拉。少量会被大量对的样本冲掉；超过一成半就开始明显拖后腿。\n"));
            sb.Append(Lang.T("自动标注没被发现的错：")).Append(N(sim.Noise(dataset), "0")).Append(Lang.T(" 条（超过样本 10% 会近亲繁殖）\n"));
            sb.Append(Lang.T("数据包和众包带来的错：")).Append(Samples(sim.DataNoise(dataset))).Append('\n');
            sb.Append(Lang.T("实际翻转的训练卡：")).Append(Pct0(sim.BoardFlipRate(dataset))).Append('\n');
            sb.Append(sim.Has(XgSim.DataCleanId) ? Lang.T("「数据清洗」已买：杂包和众包的标错减半。")
                : Lang.T("研究树「数据清洗」（数据增强旁边）能把杂包和众包的标错减半。"));
            if (sim.Has("label.audit")) sb.Append('\n').Append(Lang.T("「清洗」按钮先扔掉自己的错，再扔外来的错。"));
            return sb.ToString();
        }

        /// <summary>"日志 1.2 万条（+30/秒）" or empty when the flywheel has not started for this dataset.</summary>
        public static string LogLine(XgSim sim, string dataset)
        {
            double logs = sim.Logs(dataset), rate = sim.LogsPerSecond(dataset);
            if (logs < 1 && rate <= 0) return "";
            return Lang.T("用户日志 ") + Samples(logs) + (rate > 0 ? "（+" + N(rate, rate < 10 ? "0.0" : "0") + Lang.T("/秒）") : "");
        }

        public static string LogTip(XgSim sim, string dataset)
        {
            var sb = new StringBuilder();
            sb.Append("<b>").Append(Lang.T("数据飞轮 · 用户日志")).Append("</b>\n");
            sb.Append(Lang.T("达标的订单会把用户日志传回来：没标注过的真实数据。里面有用户的聊天记录和隐私。\n"));
            sb.Append(Lang.T("现在：")).Append(Samples(sim.Logs(dataset))).Append(Lang.T("，每秒 +")).Append(N(sim.LogsPerSecond(dataset), "0.0")).Append(T("", " per second"));
            if (sim.S.stage < XgSim.FlywheelStage) sb.Append(Lang.T("（第 5 阶段前只有十分之一）"));
            sb.Append('\n');
            if (XgCatalog.Desk(dataset) != null)
                sb.Append(Lang.T("在标注台亲手标：干净，算连击，一条顶 ")).Append(N(XgSim.LogHandSamples, "0")).Append(Lang.T(" 个样本。\n"));
            sb.Append(Lang.T("让模型自己标：很快，但它错多少就标错多少（现在约 ")).Append(Pct0(sim.LogAutoNoise(dataset))).Append(Lang.T("），错的算它自己的噪声。"));
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
