using System;
using LingGuangV05.XingGuang;

// Prints how the concept board behaves on the synthetic tasks (tuning aid for design v1.1 §4).
static class Probe
{
    static void Row(string name, XgKnobs k, Func<int, XgBoardCard> make, int poolSize, int cards)
    {
        var brain = new XgBoard();
        var pool = XgBoardTasks.Set(make, 1, poolSize);
        var test = XgBoardTasks.Set(make, 99, 200);
        string line = name.PadRight(34);
        int done = 0;
        foreach (int checkpoint in new[] { cards / 8, cards / 4, cards / 2, cards })
        {
            XgBoardTasks.Run(brain, k, pool, checkpoint - done, test, out int nan); done = checkpoint;
            line += $" {brain.Accuracy(pool, k):0.00}/{brain.Accuracy(test, k):0.00}{(nan > 0 ? "!" + nan : "")}";
        }
        line += $"  cells {brain.S.concepts.Count} layer {brain.MaxLayer(make(0).region)} sup {brain.S.superposed} ev {brain.S.evicted}";
        Console.WriteLine(line);
    }

    static void Bench(string name, XgKnobs k, Func<int, XgBoardCard> make, int poolSize, int cards)
    {
        var brain = new XgBoard(); var pool = XgBoardTasks.Set(make, 3, poolSize);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < cards; i++) brain.Train(pool[i % pool.Count], k);
        sw.Stop();
        Console.WriteLine($"{name.PadRight(30)} {cards} cards {sw.Elapsed.TotalMilliseconds:0} ms = {sw.Elapsed.TotalMilliseconds * 1000 / cards:0.0} µs/card, cells {brain.S.concepts.Count}, links {brain.S.links.Count}, evicted {brain.S.evicted}");
    }

    static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "data")
        {
            Console.WriteLine("dataset: config → train/test at 1/8 1/4 1/2 1");
            void D(string ds, string label, XgKnobs k, int pool = 600, int cards = 6000, int level = 1)
            {
                Row(ds + (level > 1 ? " L" + level : "") + " " + label, k, s => XgBoardData.Make(ds, s, level, 20160801), pool, cards);
            }
            var perceptron = new XgKnobs { depth = 1, width = 8, lr = .3, bias = true };
            var mlp = new XgKnobs { depth = 3, width = 32, activation = XgActivation.Sigmoid, lr = .3, bias = true };
            var cnn = new XgKnobs { depth = 3, width = 64, activation = XgActivation.Relu, wiring = XgWiring.LocalShared, lr = .1, bias = true };
            var rnn = new XgKnobs { depth = 3, width = 64, activation = XgActivation.Relu, wiring = XgWiring.Recurrent, lr = .1, bias = true };
            var lstm = new XgKnobs { depth = 3, width = 64, activation = XgActivation.Relu, wiring = XgWiring.GatedRecurrent, lr = .01, clip = true, bias = true };
            var s2s = new XgKnobs { depth = 3, width = 256, activation = XgActivation.Relu, wiring = XgWiring.EncoderDecoder, lr = .01, clip = true, bias = true };
            var att = new XgKnobs { depth = 3, width = 256, activation = XgActivation.Relu, wiring = XgWiring.Attention, lr = .01, clip = true, bias = true };
            D("logic", "perceptron", perceptron); D("logic", "mlp", mlp); D("logic", "mlp", mlp, level: 4); D("logic", "perceptron", perceptron, level: 4);
            D("spam", "perceptron", perceptron); D("spam", "perceptron w32", new XgKnobs { depth = 1, width = 32, lr = .3 });
            D("mnist", "mlp", mlp); D("mnist", "cnn", cnn);
            D("danmu", "mlp", mlp); D("danmu", "rnn", rnn);
            D("cifar", "mlp", mlp); D("cifar", "cnn", cnn);
            D("meme", "cnn", cnn); D("go", "cnn", cnn);
            D("poems", "rnn", rnn); D("poems", "lstm", lstm);
            D("headline", "perceptron w32", new XgKnobs { depth = 1, width = 32, lr = .3 }); D("review", "mlp", mlp);
            D("longtext", "rnn", rnn); D("longtext", "lstm", lstm);
            D("translate", "lstm", lstm); D("translate", "s2s", s2s); D("translate", "attention", att); D("translate", "s2s", s2s, level: 4); D("translate", "attention", att, level: 4); D("translate", "full", mlp);
            var s2sFast = new XgKnobs { depth = 2, width = 256, activation = XgActivation.Relu, wiring = XgWiring.EncoderDecoder, lr = .1, clip = true, bias = true };
            var attFast = new XgKnobs { depth = 2, width = 256, activation = XgActivation.Relu, wiring = XgWiring.Attention, lr = .1, clip = true, bias = true };
            var lstmFast = new XgKnobs { depth = 2, width = 256, activation = XgActivation.Relu, wiring = XgWiring.GatedRecurrent, lr = .1, clip = true, bias = true };
            D("translate", "lstm lr.1 d2", lstmFast); D("translate", "s2s lr.1 d2", s2sFast); D("translate", "s2s lr.1 d2", s2sFast, level: 4); D("translate", "att lr.1 d2", attFast, level: 4);
            return;
        }
        if (args.Length > 0 && args[0] == "walls")
        {
            void W(string ds, string label, XgKnobs k, int cards = 6000) => Row(ds + " " + label, k, s => XgBoardData.Make(ds, s, 1, 20160801), 600, cards);
            W("xor", "d2 sigmoid lr.1", new XgKnobs { depth = 2, width = 8, activation = XgActivation.Sigmoid, lr = .1, bias = true }, 2000);
            W("xor", "d1", new XgKnobs { depth = 1, width = 8, lr = .1, bias = true }, 2000);
            W("parallel", "any+pos", new XgKnobs { depth = 3, width = 256, activation = XgActivation.Relu, wiring = XgWiring.AnyToAny, position = true, skip = true, lr = .1, clip = true, bias = true });
            W("parallel", "any", new XgKnobs { depth = 3, width = 256, activation = XgActivation.Relu, wiring = XgWiring.AnyToAny, skip = true, lr = .1, clip = true, bias = true });
            W("parallel", "attention", new XgKnobs { depth = 3, width = 256, activation = XgActivation.Relu, wiring = XgWiring.Attention, lr = .1, clip = true, bias = true });
            W("parallel", "gated", new XgKnobs { depth = 3, width = 256, activation = XgActivation.Relu, wiring = XgWiring.GatedRecurrent, lr = .01, clip = true, bias = true });
            W("longtext", "gated lr.01", new XgKnobs { depth = 3, width = 64, activation = XgActivation.Relu, wiring = XgWiring.GatedRecurrent, lr = .01, clip = true, bias = true });
            W("translate", "s2s w256", new XgKnobs { depth = 2, width = 128, activation = XgActivation.Relu, wiring = XgWiring.EncoderDecoder, lr = .1, clip = true, bias = true });
            W("mnist", "cnn d3 w32", new XgKnobs { depth = 3, width = 32, activation = XgActivation.Relu, wiring = XgWiring.LocalShared, lr = .1, bias = true });
            W("mnist", "cnn d3 w32 sig", new XgKnobs { depth = 3, width = 32, activation = XgActivation.Sigmoid, wiring = XgWiring.LocalShared, lr = .3, bias = true });
            W("danmu", "rnn d3 w32", new XgKnobs { depth = 3, width = 32, activation = XgActivation.Relu, wiring = XgWiring.Recurrent, lr = .1, bias = true });
            W("danmu", "rnn d3 w32 sig", new XgKnobs { depth = 3, width = 32, activation = XgActivation.Sigmoid, wiring = XgWiring.Recurrent, lr = .3, bias = true });
            W("mnist", "mlp d3 w32", new XgKnobs { depth = 3, width = 32, activation = XgActivation.Relu, lr = .1, bias = true });
            W("danmu", "mlp d3 w32", new XgKnobs { depth = 3, width = 32, activation = XgActivation.Relu, lr = .1, bias = true });
            W("mnist", "cnn", new XgKnobs { depth = 2, width = 16, activation = XgActivation.Relu, wiring = XgWiring.LocalShared, lr = .1, bias = true });
            W("danmu", "rnn", new XgKnobs { depth = 2, width = 16, activation = XgActivation.Relu, wiring = XgWiring.Recurrent, lr = .1, bias = true });
            return;
        }
        if (args.Length > 0 && args[0] == "stability")
        {
            string ds = args.Length > 1 ? args[1] : "longtext";
            double target = args.Length > 2 ? double.Parse(args[2]) : .75;
            int minDistance = ds == "longtext" ? 10 : int.MinValue;
            var configs = ds == "translate" ? new (string name, XgKnobs k)[]
            {
                ("ref s2s d2 w128 lr.1", new XgKnobs { depth = 2, width = 128, activation = XgActivation.Relu, wiring = XgWiring.EncoderDecoder, lr = .1, clip = true, bias = true }),
                ("s2s d3 w64 lr.1", new XgKnobs { depth = 3, width = 64, activation = XgActivation.Relu, wiring = XgWiring.EncoderDecoder, lr = .1, clip = true, bias = true }),
                ("s2s d2 w128 lr.01", new XgKnobs { depth = 2, width = 128, activation = XgActivation.Relu, wiring = XgWiring.EncoderDecoder, lr = .01, clip = true, bias = true }),
                ("attention d2 w128", new XgKnobs { depth = 2, width = 128, activation = XgActivation.Relu, wiring = XgWiring.Attention, lr = .1, clip = true, bias = true }),
                ("WRONG gated", new XgKnobs { depth = 2, width = 128, activation = XgActivation.Relu, wiring = XgWiring.GatedRecurrent, lr = .1, clip = true, bias = true }),
                ("WRONG full", new XgKnobs { depth = 2, width = 128, activation = XgActivation.Relu, wiring = XgWiring.Full, lr = .1, clip = true, bias = true }),
            } : new (string name, XgKnobs k)[]
            {
                ("ref gated lr.01 w64 d3", new XgKnobs { depth = 3, width = 64, activation = XgActivation.Relu, wiring = XgWiring.GatedRecurrent, lr = .01, clip = true, bias = true }),
                ("gated lr.03", new XgKnobs { depth = 3, width = 64, activation = XgActivation.Relu, wiring = XgWiring.GatedRecurrent, lr = .03, clip = true, bias = true }),
                ("gated lr.1 clip", new XgKnobs { depth = 3, width = 64, activation = XgActivation.Relu, wiring = XgWiring.GatedRecurrent, lr = .1, clip = true, bias = true }),
                ("gated lr.01 w32", new XgKnobs { depth = 3, width = 32, activation = XgActivation.Relu, wiring = XgWiring.GatedRecurrent, lr = .01, clip = true, bias = true }),
                ("gated lr.01 d2", new XgKnobs { depth = 2, width = 64, activation = XgActivation.Relu, wiring = XgWiring.GatedRecurrent, lr = .01, clip = true, bias = true }),
                ("gated sigmoid", new XgKnobs { depth = 3, width = 64, activation = XgActivation.Sigmoid, wiring = XgWiring.GatedRecurrent, lr = .03, clip = true, bias = true }),
                ("WRONG rnn lr.01", new XgKnobs { depth = 3, width = 64, activation = XgActivation.Relu, wiring = XgWiring.Recurrent, lr = .01, clip = true, bias = true }),
                ("WRONG rnn lr.1", new XgKnobs { depth = 3, width = 64, activation = XgActivation.Relu, wiring = XgWiring.Recurrent, lr = .1, clip = true, bias = true }),
                ("WRONG full", new XgKnobs { depth = 3, width = 64, activation = XgActivation.Relu, wiring = XgWiring.Full, lr = .1, clip = true, bias = true }),
            };
            var prior = new XgKnobs { depth = 3, width = 64, activation = XgActivation.Relu, wiring = XgWiring.Recurrent, lr = .1, bias = true };
            foreach (var cfg in configs)
            {
                var line = cfg.name.PadRight(26);
                foreach (bool prefilled in new[] { false, true })
                {
                    var results = new System.Collections.Generic.List<string>();
                    for (int salt = 1; salt <= 8; salt++)
                    {
                        var board = new XgBoard();
                        if (prefilled)
                            for (int i = 0; i < 4000; i++)
                            {
                                string other = new[] { "poems", "spam", "danmu", "news" }[i % 4];
                                board.Train(XgBoardData.Make(other, XgBoardData.Seed(other, i % 600, XgBoardData.Use.Train, salt), 1, 20160801), prior);
                            }
                        var exam = new System.Collections.Generic.List<XgBoardCard>();
                        for (int i = 0; exam.Count < 80 && i < 2000; i++)
                        {
                            var c = XgBoardData.Make(ds, XgBoardData.Seed(ds, i, XgBoardData.Use.Exam, salt), 1, 20160801);
                            if (c.distance >= minDistance) exam.Add(c);
                        }
                        int passedAt = -1;
                        for (int n = 0; n < 30000; n += 250)
                        {
                            for (int i = 0; i < 250; i++) board.Train(XgBoardData.Make(ds, XgBoardData.Seed(ds, (n + i) % 600, XgBoardData.Use.Train, salt), 1, 20160801), cfg.k);
                            if (board.Accuracy(exam, cfg.k) >= target) { passedAt = n + 250; break; }
                        }
                        results.Add(passedAt < 0 ? "  ✗" : (passedAt / 1000.0).ToString("0.0").PadLeft(4) + "k");
                    }
                    line += (prefilled ? " | prefilled:" : " | fresh:") + string.Join("", results);
                }
                Console.WriteLine(line);
            }
            return;
        }
        if (args.Length > 0 && args[0] == "bench")
        {
            Bench("digit full w128 d4", new XgKnobs { depth = 4, width = 128, activation = XgActivation.Relu, lr = .3 }, s => XgBoardTasks.Digit(s), 2000, 20000);
            Bench("digit local w128 d4", new XgKnobs { depth = 4, width = 128, activation = XgActivation.Relu, wiring = XgWiring.LocalShared, lr = .3 }, s => XgBoardTasks.Digit(s), 2000, 20000);
            Bench("first20 gated w128 d4", new XgKnobs { depth = 4, width = 128, activation = XgActivation.Relu, wiring = XgWiring.GatedRecurrent, lr = .1 }, s => XgBoardTasks.FirstChar(s, 20), 2000, 20000);
            Bench("first20 any w256 d6", new XgKnobs { depth = 6, width = 256, activation = XgActivation.Relu, wiring = XgWiring.AnyToAny, skip = true, lr = .1 }, s => XgBoardTasks.FirstChar(s, 20), 2000, 20000);
            return;
        }
        Console.WriteLine("train/test accuracy at 1/8, 1/4, 1/2, 1 of the cards");
        Row("linear perceptron", new XgKnobs { depth = 1, width = 8, lr = .1 }, XgBoardTasks.Linear, 200, 800);
        Row("xor perceptron", new XgKnobs { depth = 1, width = 8, lr = .3 }, XgBoardTasks.Xor, 200, 800);
        Row("xor d2 step", new XgKnobs { depth = 2, width = 8, lr = .3 }, XgBoardTasks.Xor, 200, 800);
        Row("xor d2 sigmoid lr.3", new XgKnobs { depth = 2, width = 8, activation = XgActivation.Sigmoid, lr = .3 }, XgBoardTasks.Xor, 200, 800);
        Row("xor d2 sigmoid lr.01", new XgKnobs { depth = 2, width = 8, activation = XgActivation.Sigmoid, lr = .01 }, XgBoardTasks.Xor, 200, 800);
        Row("xor d6 sigmoid lr.3", new XgKnobs { depth = 6, width = 8, activation = XgActivation.Sigmoid, lr = .3 }, XgBoardTasks.Xor, 200, 800);
        Row("xor d2 sigmoid lr1", new XgKnobs { depth = 2, width = 8, activation = XgActivation.Sigmoid, lr = 1 }, XgBoardTasks.Xor, 200, 800);
        Row("digit full d3 w32", new XgKnobs { depth = 3, width = 32, activation = XgActivation.Sigmoid, lr = .3 }, s => XgBoardTasks.Digit(s), 60, 3000);
        Row("digit local d3 w32", new XgKnobs { depth = 3, width = 32, activation = XgActivation.Sigmoid, wiring = XgWiring.LocalShared, lr = .3 }, s => XgBoardTasks.Digit(s), 60, 3000);
        Row("digit local relu d3 w32", new XgKnobs { depth = 3, width = 32, activation = XgActivation.Relu, wiring = XgWiring.LocalShared, lr = .3 }, s => XgBoardTasks.Digit(s), 60, 3000);
        Row("first4 rnn d3", new XgKnobs { depth = 3, width = 32, activation = XgActivation.Relu, wiring = XgWiring.Recurrent, lr = .1 }, s => XgBoardTasks.FirstChar(s, 4), 120, 3000);
        Row("first12 rnn d3", new XgKnobs { depth = 3, width = 32, activation = XgActivation.Relu, wiring = XgWiring.Recurrent, lr = .1 }, s => XgBoardTasks.FirstChar(s, 12), 120, 3000);
        Row("first12 gated d3", new XgKnobs { depth = 3, width = 32, activation = XgActivation.Relu, wiring = XgWiring.GatedRecurrent, lr = .1 }, s => XgBoardTasks.FirstChar(s, 12), 120, 3000);
        Row("first12 gated d3 w128", new XgKnobs { depth = 3, width = 128, activation = XgActivation.Relu, wiring = XgWiring.GatedRecurrent, lr = .1 }, s => XgBoardTasks.FirstChar(s, 12), 120, 3000);
        Row("first12 gated d3 lr.03", new XgKnobs { depth = 3, width = 32, activation = XgActivation.Relu, wiring = XgWiring.GatedRecurrent, lr = .03 }, s => XgBoardTasks.FirstChar(s, 12), 120, 3000);
        Row("first12 full d3", new XgKnobs { depth = 3, width = 32, activation = XgActivation.Relu, wiring = XgWiring.Full, lr = .1 }, s => XgBoardTasks.FirstChar(s, 12), 120, 3000);
    }
}
