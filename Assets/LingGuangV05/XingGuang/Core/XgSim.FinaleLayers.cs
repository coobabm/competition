using System;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// 终章 · 逐层点亮 (它在学_可视化借鉴设计 §2.5, after bbycroft/llm-viz): pre-training drawn as a stack of 12
    /// Transformer blocks that light from the bottom up, one per twelfth of <see cref="XgState.pretrain"/>, each with one
    /// plain line about what that depth of a real model tends to pick up. The rules stay in XgSim.Finale.cs; this is
    /// read-only presentation data for XgFinalePage / XgPretrainStackGraphic.
    /// </summary>
    public sealed partial class XgSim
    {
        /// <summary>Blocks in the drawn stack (a GPT-2-small sized model).</summary>
        public const int PretrainLayerCount = 12;

        // Low layers: characters, positions, word pieces. Middle: grammar and phrases. High: meaning, topic, a step of reasoning.
        // Honest in spirit: real models do not split the work this neatly (the intro line says so).
        static readonly string[] LayerLinesZh =
        {
            "它学会认字：哪个字是哪个字，常跟谁挨着。",
            "它学会看位置：同一个字，在句首和在句尾不一样。",
            "它学会把常连在一起的字拼成词：「人民」「日报」「弹幕」。",
            "它学会看标点：句子在哪儿断，引号在哪儿关。",
            "它开始分清主语和宾语：谁做了什么，对谁做。",
            "它学会词怎么搭：「吃」后面跟吃的，「很」后面跟样子。",
            "它学会把「他」「它」和前面提到的那个对上。",
            "它分得清唐诗、新闻和贴吧，各用各的说法。",
            "它学会看一段话在讲什么：天气、考试，还是游戏。",
            "它把意思相近的话放到一起：「不错」挨着「挺好」。",
            "它学会顺着前文多想一步：下了雨，地就湿了。",
            "最上面这层把下面学到的收拢起来，只为猜准下一个字。",
        };

        static readonly string[] LayerLinesEn =
        {
            "it learns to tell characters apart, and which ones tend to sit side by side.",
            "it learns position: the same character reads differently at the start and at the end.",
            "it learns to glue characters that keep appearing together into words.",
            "it learns punctuation: where a sentence breaks, where a quote closes.",
            "it starts to tell subject from object: who did what, to whom.",
            "it learns which words go together: 'eat' wants food after it, 'very' wants a description.",
            "it learns to match 'he' and 'it' back to whoever was mentioned before.",
            "it tells Tang poems, news and forum posts apart, each with its own way of talking.",
            "it learns what a passage is about: the weather, an exam, or a game.",
            "it puts sentences with similar meaning together: 'not bad' sits next to 'pretty good'.",
            "it learns to follow the text one step further: if it rained, the ground is wet.",
            "the top layer pulls together everything below, for one job: guessing the next character.",
        };

        /// <summary>Blocks lit at this pre-training progress: one per full twelfth.</summary>
        public static int PretrainLayersLitAt(double progress)
        {
            if (double.IsNaN(progress) || progress <= 0) return 0;
            return Math.Max(0, Math.Min(PretrainLayerCount, (int)Math.Floor(progress * PretrainLayerCount + 1e-9)));
        }

        /// <summary>Blocks lit now (all of them once the abilities are out).</summary>
        public int PretrainLayersLit => S.abilities ? PretrainLayerCount : PretrainLayersLitAt(S.pretrain);

        /// <summary>How far the block being trained is, 0–1 (0 when every block is lit).</summary>
        public double PretrainLayerFill
        {
            get
            {
                int lit = PretrainLayersLit;
                if (lit >= PretrainLayerCount) return 0;
                double f = S.pretrain * PretrainLayerCount - lit;
                return Math.Max(0, Math.Min(1, f));
            }
        }

        /// <summary>
        /// Pre-training is stuck right now: the breaker tripped for want of a server room, or progress sits on the
        /// effective-scale plateau. (<see cref="XgState.pretrainStalled"/> alone stays set after the plateau lifts.)
        /// </summary>
        public bool PretrainLayersStalled => !S.abilities && S.pretrainStalled && (!Has("datacenter") || S.pretrain >= PretrainCap - 1e-9);

        /// <summary>Why the next block will not light: the server room first, then <see cref="PretrainLimit"/>.</summary>
        public string PretrainStallReason()
        {
            if (!Has("datacenter"))
                return T("没有机房：3500W 的机箱一开预训练就跳闸，得去科技里租「机房」。", "No server room: a 3500 W case trips the breaker as soon as pre-training starts; rent the server room in the tree.");
            return PretrainLimit();
        }

        /// <summary>Layer <paramref name="layer"/> (1-based) and what it learns, e.g. 「第 5 层：它开始分清主语和宾语……」.</summary>
        public static string PretrainLayerLine(int layer, bool english)
        {
            int i = Math.Max(1, Math.Min(PretrainLayerCount, layer)) - 1;
            return english ? "Layer " + (i + 1) + ": " + LayerLinesEn[i] : "第 " + (i + 1) + " 层：" + LayerLinesZh[i];
        }

        public string PretrainLayerLine(int layer) => PretrainLayerLine(layer, English);

        /// <summary>The commentary line on top: the stall reason, the newest lit layer, or the intro before any.</summary>
        public string PretrainLayerHeadline()
        {
            int lit = PretrainLayersLit;
            if (PretrainLayersStalled && lit < PretrainLayerCount)
                return T("第 " + (lit + 1) + " 层点不亮：", "Layer " + (lit + 1) + " will not light: ") + PretrainStallReason();
            if (lit == 0)
                return T("还没有一层亮起来。低层学字，中层学语法，高层学意思——大致如此，真模型的分工没这么整齐。",
                         "No layer is lit yet. Low layers learn characters, middle ones grammar, high ones meaning; roughly, real models are messier.");
            return PretrainLayerLine(lit);
        }

        /// <summary>The current loss as 0 (as low as it goes) to 1 (untrained): the stack's colour.</summary>
        public double PretrainLossHeat => Math.Max(0, Math.Min(1, (PretrainLoss - .3) / 3.7));
    }
}
