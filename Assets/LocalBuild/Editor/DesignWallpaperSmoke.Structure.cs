using System;
using System.Collections;
using System.Collections.Generic;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;

public static partial class DesignWallpaperSmoke
{
    static IEnumerator StructureTransformerScene(XingGuangController controller, ChapterOneRuntime runtime,
        XgWallpaperTraining training, XgWallpaperConcepts concepts, XgBrainWallpaperPresentation presentation)
    {
        TrainingSceneCase("Transformer: explicit stage-six idea-unlocked fixture, real purchase and rotating fully connected structure sockets");
        var sim = controller.Sim; int stage = sim.S.stage; var board = sim.S.board;
        bool reduced = controller.View.Juice.Reduced, quiet = presentation.Quiet; float scale = Time.timeScale;
        try
        {
            sim.S.stage = 6; sim.S.unlocked.Remove("transformer"); runtime.Sim.S.money = 20000;
            controller.View.Juice.Reduced = false; presentation.SetQuiet(false);
            var node = XgCatalog.Node("transformer"); double money = controller.Host.Money, cost = sim.NodeCost(node);
            Check(sim.Status(node, controller.Host) == XgSim.NodeStatus.Buyable,
                "Transformer purchase prerequisite: " + sim.Why(node, controller.Host));
            Check(sim.BuyNode("transformer", controller.Host) && Math.Abs(money - controller.Host.Money - cost) < 1e-9,
                "Real Transformer purchase charges once");
            foreach (var track in new[] { XgTrack.Vision, XgTrack.Sequence })
            {
                Check(sim.Run(track).arch == sim.BestAutoModel(sim.Run(track), controller.Host).arch, "Transformer respects actual automatic choice: " + track);
                if (sim.Run(track).arch != "transformer")
                { TrainingSceneCase("Transformer explicit active-architecture fixture for " + track); sim.Run(track).arch = "transformer"; }
            }
            Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(.9f); yield return new WaitForEndOfFrame();
            string state = TrainingSceneState(controller, runtime);
            var first = concepts.NebulaSocket(1, 4);
            Check(concepts.Architectures[0] == "transformer" && concepts.Architectures[1] == "transformer"
                && concepts.ScanWindow(0).width == 0 && concepts.RecurrentSignal(1) == Vector3.zero && concepts.AttentionIndex == -1,
                "Shared Transformer replaces grid, recurrent ring and old attention text");
            TrainingSceneCapture(training, "structure-transformer-a");
            yield return new WaitForSecondsRealtime(.8f); yield return new WaitForEndOfFrame();
            Check(Vector2.Distance(first, concepts.NebulaSocket(1, 4)) > .001f,
                "Nebula slowly rotates while simulation is paused");
            for (int region = 0; region < 2; region++) for (int i = 0; i < 10; i++)
                Check(Vector2.Distance(concepts.NebulaSocket(region, i), XgWallpaperConcepts.RegionCenter(region)) <= .06f,
                    "Nebula socket stays in its region: " + region + "/" + i);
            Check(state == TrainingSceneState(controller, runtime), "Nebula adds no learned links, resources or RNG changes");
            TrainingSceneCapture(training, "structure-transformer-b");
            controller.View.Juice.Reduced = true; presentation.SetQuiet(true);
            var still = concepts.NebulaSocket(1, 4);
            yield return new WaitForSecondsRealtime(1.05f); yield return new WaitForEndOfFrame();
            Check(still == concepts.NebulaSocket(1, 4) && concepts.canvasRenderer.GetAlpha() <= .41f,
                "Reduced stops nebula rotation and Quiet dims it");
            TrainingSceneCapture(training, "structure-transformer-reduced-quiet");
            sim.S.board = new XgBoardState(); yield return new WaitForSecondsRealtime(.2f); yield return new WaitForEndOfFrame();
            Check(concepts.Cells.Count == 0 && concepts.Links.Count == 0 && concepts.NebulaSocket(1, 4) != Vector2.zero,
                "Empty nebula shows structural sockets without inventing learned cells");
            concepts.enabled = false;
            Check(concepts.NebulaSocket(1, 4) == Vector2.zero, "Disable clears nebula binding");
            concepts.enabled = true; yield return null;
        }
        finally
        {
            concepts.enabled = true; sim.S.stage = stage; sim.S.board = board; Time.timeScale = scale;
            controller.View.Juice.Reduced = reduced; presentation.SetQuiet(quiet);
        }
        yield return new WaitForSecondsRealtime(.85f);
    }

    static IEnumerator StructureAttentionScene(XingGuangController controller, ChapterOneRuntime runtime,
        XgWallpaperTraining training, XgWallpaperConcepts concepts, XgBrainWallpaperPresentation presentation)
    {
        TrainingSceneCase("Attention: actual purchase and actual correctly taught sentence, illustrative beam not model weights");
        var sim = controller.Sim; int stage = sim.S.stage;
        bool reduced = controller.View.Juice.Reduced, quiet = presentation.Quiet;
        try
        {
            sim.S.stage = 4; sim.S.unlocked.Remove("attention"); controller.View.Juice.Reduced = false;
            // Later architecture checks exhausted the original finite wallet fixture.
            runtime.Sim.S.money = 10000;
            presentation.SetQuiet(false);
            var node = XgCatalog.Node("attention"); double money = controller.Host.Money, cost = sim.NodeCost(node);
            Check(sim.Status(node, controller.Host) == XgSim.NodeStatus.Buyable,
                "Attention purchase prerequisites: " + sim.Why(node, controller.Host));
            Check(sim.BuyNode("attention", controller.Host), "Real Attention purchase succeeds");
            Check(Math.Abs(money - controller.Host.Money - cost) < 1e-9
                && sim.Run(XgTrack.Sequence).arch == sim.BestAutoModel(sim.Run(XgTrack.Sequence), controller.Host).arch,
                "Attention charges actual cost and respects automatic selection");
            if (sim.Run(XgTrack.Sequence).arch != "attention")
            { TrainingSceneCase("Attention explicit active-architecture fixture; automatic rig preference differs"); sim.Run(XgTrack.Sequence).arch = "attention"; }
            yield return new WaitForSecondsRealtime(.2f);
            Check(concepts.AttentionIndex == -1, "No beam or invented sentence appears before teaching");
            sim.SelectDesk("sense"); var card = sim.Card("sense");
            Check(sim.TeachingGuess(card, out bool guess) && sim.TryEvaluateTeachingGuess("sense", card.id, guess,
                guess == card.truth, controller.Host, out var answer) && answer.correct, "Actual common-sense sentence is taught through the normal evaluation path");
            yield return null; yield return new WaitForEndOfFrame();
            Check(concepts.AttentionCount > 1 && concepts.AttentionCount <= 6 && concepts.AttentionIndex >= 0,
                "Attention displays a bounded excerpt from the real learned sentence");
            var expected = System.Globalization.StringInfo.GetTextElementEnumerator(XgSim.LessonText(card)); int index = 0;
            while (expected.MoveNext() && index < concepts.AttentionCount)
            {
                string value = expected.GetTextElement(); if (string.IsNullOrWhiteSpace(value)) continue;
                var label = concepts.transform.Find("Source character " + index).GetComponent<TMPro.TMP_Text>();
                Check(label.text == value && !label.richText, "Displayed source character matches taught text: " + index);
                index++;
            }
            int first = concepts.AttentionIndex; string state = TrainingSceneState(controller, runtime);
            TrainingSceneCapture(training, "structure-attention-source-a");
            yield return new WaitForSecondsRealtime(.7f); yield return new WaitForEndOfFrame();
            Check(concepts.AttentionIndex != first && concepts.AttentionTarget(concepts.AttentionIndex).x < XgWallpaperConcepts.RegionCenter(1).x,
                "Beam revisits another real source position on the encoder side");
            Check(state == TrainingSceneState(controller, runtime), "Beam display changes no game state after teaching");
            TrainingSceneCapture(training, "structure-attention-source-b");
            controller.View.Juice.Reduced = true; presentation.SetQuiet(true);
            yield return new WaitForSecondsRealtime(1.05f); yield return new WaitForEndOfFrame();
            Check(concepts.AttentionIndex == 0 && concepts.canvasRenderer.GetAlpha() <= .41f,
                "Reduced freezes the source beam and Quiet dims the structure");
            TrainingSceneCapture(training, "structure-attention-reduced-quiet");
            concepts.enabled = false;
            Check(concepts.AttentionCount == 0 && concepts.AttentionIndex == -1, "Disable forgets transient sentence and beam");
            concepts.enabled = true; yield return null;
        }
        finally
        {
            concepts.enabled = true; sim.S.stage = stage; controller.View.Juice.Reduced = reduced; presentation.SetQuiet(quiet);
        }
        yield return new WaitForSecondsRealtime(.85f);
    }

    static IEnumerator StructureSequenceScene(XingGuangController controller, ChapterOneRuntime runtime,
        XgWallpaperTraining training, XgWallpaperConcepts concepts, XgBrainWallpaperPresentation presentation)
    {
        TrainingSceneCase("Seq2Seq: actual purchase, divided reading region and tightening bottleneck");
        var sim = controller.Sim; int stage = sim.S.stage;
        bool reduced = controller.View.Juice.Reduced, quiet = presentation.Quiet; float scale = Time.timeScale;
        try
        {
            sim.S.stage = 4; sim.S.unlocked.Remove("seq2seq"); controller.View.Juice.Reduced = false;
            presentation.SetQuiet(false); sim.Run(XgTrack.Sequence).arch = "lstm";
            yield return new WaitForSecondsRealtime(.9f);
            var node = XgCatalog.Node("seq2seq"); double money = controller.Host.Money, cost = sim.NodeCost(node);
            Check(sim.Status(node, controller.Host) == XgSim.NodeStatus.Buyable && sim.BuyNode("seq2seq", controller.Host),
                "Real Seq2Seq purchase succeeds with finite Host");
            Check(Math.Abs(money - controller.Host.Money - cost) < 1e-9
                && sim.Run(XgTrack.Sequence).arch == sim.BestAutoModel(sim.Run(XgTrack.Sequence), controller.Host).arch,
                "Seq2Seq charges actual cost and respects automatic selection");
            if (sim.Run(XgTrack.Sequence).arch != "seq2seq")
            { TrainingSceneCase("Seq2Seq explicit active-architecture fixture; automatic rig preference differs"); sim.Run(XgTrack.Sequence).arch = "seq2seq"; }
            Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(.18f); yield return new WaitForEndOfFrame();
            float early = concepts.SequenceNeck(1);
            Check(early > .0061f && early < .03f, "Bottleneck has an intermediate width on unscaled time");
            TrainingSceneCapture(training, "structure-seq2seq-moving");
            string state = TrainingSceneState(controller, runtime);
            yield return new WaitForSecondsRealtime(.7f); yield return new WaitForEndOfFrame();
            Check(Mathf.Abs(concepts.SequenceNeck(1) - .006f) < .00001f && concepts.SequenceNeck(1) < early,
                "The channel finishes tightening between the two reading halves");
            Check(state == TrainingSceneState(controller, runtime), "Sequence morph preserves complete learning and wallet state");
            TrainingSceneCapture(training, "structure-seq2seq-final");
            controller.View.Juice.Reduced = true; presentation.SetQuiet(true);
            sim.Run(XgTrack.Sequence).arch = "lstm"; yield return new WaitForSecondsRealtime(.15f);
            sim.Run(XgTrack.Sequence).arch = "seq2seq"; yield return new WaitForSecondsRealtime(1.05f); yield return new WaitForEndOfFrame();
            Check(Mathf.Abs(concepts.SequenceNeck(1) - .006f) < .00001f && concepts.canvasRenderer.GetAlpha() <= .41f,
                "Reduced adopts final bottleneck immediately and Quiet dims it");
            TrainingSceneCapture(training, "structure-seq2seq-reduced-quiet");
            concepts.enabled = false;
            Check(concepts.SequenceNeck(1) == 0, "Disable clears sequence structure");
            concepts.enabled = true; yield return null; yield return new WaitForEndOfFrame();
            Check(concepts.SequenceNeck(1) > 0 && concepts.SequenceNeck(0) == 0, "Re-enable restores only the reading bottleneck");
        }
        finally
        {
            concepts.enabled = true; sim.S.stage = stage; Time.timeScale = scale;
            controller.View.Juice.Reduced = reduced; presentation.SetQuiet(quiet);
        }
        yield return new WaitForSecondsRealtime(.85f);
    }

    static IEnumerator StructureResidualScene(XingGuangController controller, ChapterOneRuntime runtime,
        XgWallpaperTraining training, XgWallpaperConcepts concepts, XgBrainWallpaperPresentation presentation)
    {
        TrainingSceneCase("ResNet: real stage-four purchase and two curved structural bypasses, without adding learned links");
        var sim = controller.Sim; int stage = sim.S.stage;
        bool reduced = controller.View.Juice.Reduced, quiet = presentation.Quiet;
        try
        {
            sim.S.stage = 4; sim.S.unlocked.Remove("resnet"); controller.View.Juice.Reduced = false;
            yield return new WaitForSecondsRealtime(.9f);
            var node = XgCatalog.Node("resnet"); double price = sim.NodeCost(node), money = controller.Host.Money;
            Check(sim.Status(node, controller.Host) == XgSim.NodeStatus.Buyable && sim.BuyNode("resnet", controller.Host),
                "Actual ResNet purchase succeeds with the finite Host");
            Check(Math.Abs(money - controller.Host.Money - price) < 1e-9
                && sim.Run(XgTrack.Vision).arch == sim.BestAutoModel(sim.Run(XgTrack.Vision), controller.Host).arch,
                "ResNet charges once and follows the real automatic configuration");
            if (sim.Run(XgTrack.Vision).arch != "resnet")
            { TrainingSceneCase("ResNet active-architecture visual fixture; automatic choice on this rig differs"); sim.Run(XgTrack.Vision).arch = "resnet"; }
            yield return new WaitForSecondsRealtime(.9f); yield return new WaitForEndOfFrame();
            Check(concepts.Architectures[0] == "resnet", "The wallpaper is bound to the real active ResNet architecture");
            string state = TrainingSceneState(controller, runtime);
            var center = XgWallpaperConcepts.RegionCenter(0);
            for (int route = 0; route < 2; route++)
            {
                Vector2 start = concepts.ResidualPoint(0, route, 0), end = concepts.ResidualPoint(0, route, 1), middle = concepts.ResidualPoint(0, route, .5f);
                Check(start.x < center.x - .059f && end.x > center.x + .059f
                    && Mathf.Abs(middle.y - center.y) > .05f && Mathf.Abs(middle.x - center.x) < .00001f,
                    "Residual arc connects outer layers and bends clear of the middle: " + route);
                for (int i = 0; i <= 24; i++)
                {
                    Vector2 p = concepts.ResidualPoint(0, route, i / 24f) - center;
                    if (Mathf.Abs(p.x) > .06001f || Mathf.Abs(p.y) > .06001f) throw new Exception("Residual path escaped its region");
                }
            }
            TrainingSceneCapture(training, "structure-resnet-bypass");
            controller.View.Juice.Reduced = true; presentation.SetQuiet(true);
            yield return new WaitForSecondsRealtime(1.05f); yield return new WaitForEndOfFrame();
            Check(concepts.canvasRenderer.GetAlpha() <= .41f && concepts.ResidualPoint(0, 0, .5f).y > center.y,
                "Residual structural arcs remain legible in Reduced/Quiet");
            Check(state == TrainingSceneState(controller, runtime), "Residual rendering adds no learned links and changes no complete game state");
            TrainingSceneCapture(training, "structure-resnet-reduced-quiet");
            concepts.enabled = false;
            Check(concepts.ResidualPoint(0, 0, .5f) == Vector2.zero, "Disabling clears residual structure binding");
            concepts.enabled = true; yield return null;
        }
        finally
        {
            concepts.enabled = true; sim.S.stage = stage; controller.View.Juice.Reduced = reduced; presentation.SetQuiet(quiet);
        }
        yield return new WaitForSecondsRealtime(.85f);
    }

    static IEnumerator StructureGatedScene(XingGuangController controller, ChapterOneRuntime runtime,
        XgWallpaperTraining training, XgWallpaperConcepts concepts, XgBrainWallpaperPresentation presentation)
    {
        TrainingSceneCase("LSTM/GRU: real stage-three purchases; selected gates retain signal, the travelling signal fades much less than RNN");
        var sim = controller.Sim; int stage = sim.S.stage;
        bool reduced = controller.View.Juice.Reduced, quiet = presentation.Quiet; float scale = Time.timeScale;
        try
        {
            sim.S.stage = 3; controller.View.Juice.Reduced = false; presentation.SetQuiet(false);
            foreach (string id in new[] { "lstm", "gru" })
            {
                sim.S.unlocked.Remove(id); yield return new WaitForSecondsRealtime(.9f);
                var node = XgCatalog.Node(id); double price = sim.NodeCost(node), money = controller.Host.Money;
                Check(sim.Status(node, controller.Host) == XgSim.NodeStatus.Buyable && sim.BuyNode(id, controller.Host),
                    "Actual gated-memory purchase succeeds: " + id);
                Check(sim.Has(id) && sim.Run(XgTrack.Sequence).arch == sim.BestAutoModel(sim.Run(XgTrack.Sequence), controller.Host).arch
                    && Math.Abs(money - controller.Host.Money - price) < 1e-9,
                    "Gated purchase charges once and respects the real automatic model choice: " + id);
                if (sim.Run(XgTrack.Sequence).arch != id)
                {
                    TrainingSceneCase(id + ": explicit active-architecture visual fixture; this hardware/data prefers another owned architecture after purchase");
                    sim.Run(XgTrack.Sequence).arch = id;
                }
                Time.timeScale = 0; yield return new WaitForSecondsRealtime(.45f); yield return new WaitForEndOfFrame();
                Check(concepts.Architectures[1] == id && concepts.GateRetention(1, 0) == 0,
                    "First memory gate has no retained trace before the real-time signal arrives: " + id);
                Vector3 early = concepts.RecurrentSignal(1); string state = TrainingSceneState(controller, runtime);
                yield return new WaitForSecondsRealtime(1.8f); yield return new WaitForEndOfFrame();
                Vector3 late = concepts.RecurrentSignal(1);
                Check(late.z > early.z * .8f && late.z > .7f,
                    "Gated signal remains bright over the same route where RNN strongly faded: " + id);
                Check(concepts.GateRetention(1, 0) > .3f && concepts.GateRetention(1, 1) == 0,
                    "Selected gate keeps a bounded trace while another gate passes without retaining: " + id);
                Check(state == TrainingSceneState(controller, runtime), "Memory illustration changes no learned state or resources: " + id);
                TrainingSceneCapture(training, "structure-" + id + "-retained");
                controller.View.Juice.Reduced = true; presentation.SetQuiet(true); yield return null;
                Vector3 still = concepts.RecurrentSignal(1); float retained = concepts.GateRetention(1, 0);
                yield return new WaitForSecondsRealtime(1.05f); yield return new WaitForEndOfFrame();
                Check(still == concepts.RecurrentSignal(1) && retained == concepts.GateRetention(1, 0)
                    && concepts.canvasRenderer.GetAlpha() <= .41f, "Reduced gated memory stays static and obeys Quiet: " + id);
                TrainingSceneCapture(training, "structure-" + id + "-reduced-quiet");
                controller.View.Juice.Reduced = false; presentation.SetQuiet(false);
                concepts.enabled = false;
                Check(concepts.GateRetention(1, 0) == 0 && concepts.RecurrentSignal(1) == Vector3.zero,
                    "Disabling clears both retained and circulating signal: " + id);
                concepts.enabled = true; yield return null;
            }
        }
        finally
        {
            concepts.enabled = true; sim.S.stage = stage; controller.View.Juice.Reduced = reduced;
            presentation.SetQuiet(quiet); Time.timeScale = scale;
        }
        yield return new WaitForSecondsRealtime(.85f);
    }

    static IEnumerator StructureRecurrentScene(XingGuangController controller, ChapterOneRuntime runtime,
        XgWallpaperTraining training, XgWallpaperConcepts concepts, XgBrainWallpaperPresentation presentation)
    {
        TrainingSceneCase("RNN: real purchase at stage two; signal follows the ring and loses brightness without changing learned state");
        var sim = controller.Sim; int oldStage = sim.S.stage;
        bool reduced = controller.View.Juice.Reduced, quiet = presentation.Quiet; float scale = Time.timeScale;
        try
        {
            sim.S.stage = 2; sim.S.unlocked.Remove("rnn");
            controller.View.Juice.Reduced = false; presentation.SetQuiet(false);
            yield return new WaitForSecondsRealtime(.9f);
            var node = XgCatalog.Node("rnn"); double money = controller.Host.Money, price = sim.NodeCost(node);
            Check(sim.Status(node, controller.Host) == XgSim.NodeStatus.Buyable && sim.BuyNode("rnn", controller.Host),
                "Actual RNN purchase succeeds through the finite Host");
            Check(sim.Run(XgTrack.Sequence).arch == "rnn" && Math.Abs(money - controller.Host.Money - price) < 1e-9,
                "RNN purchase charges once and configures the true reading structure");
            Time.timeScale = 0; yield return new WaitForSecondsRealtime(.9f); yield return new WaitForEndOfFrame();
            string state = TrainingSceneState(controller, runtime);
            Vector3 first = concepts.RecurrentSignal(1); var center = XgWallpaperConcepts.RegionCenter(1);
            Check(concepts.Architectures[1] == "rnn" && first.z > .2f
                && Mathf.Abs(Vector2.Distance(new Vector2(first.x, first.y), center) - .06f) < .00001f,
                "Real RNN signal starts on its reading ring with visible brightness");
            TrainingSceneCapture(training, "structure-rnn-early");
            yield return new WaitForSecondsRealtime(1.3f); yield return new WaitForEndOfFrame();
            Vector3 later = concepts.RecurrentSignal(1);
            Check(Vector2.Distance(new Vector2(first.x, first.y), new Vector2(later.x, later.y)) > .01f
                && later.z < first.z * .6f && later.z > 0,
                "The moving signal naturally dims as it travels around the ring, even at timeScale zero");
            TrainingSceneCapture(training, "structure-rnn-late");
            Check(state == TrainingSceneState(controller, runtime), "Recurrent presentation changes no learned state, RNG or money");
            controller.View.Juice.Reduced = true; presentation.SetQuiet(true);
            yield return null; Vector3 still = concepts.RecurrentSignal(1);
            yield return new WaitForSecondsRealtime(1.05f); yield return new WaitForEndOfFrame();
            Check(still == concepts.RecurrentSignal(1) && concepts.canvasRenderer.GetAlpha() <= .41f,
                "Reduced holds a static RNN signal and Quiet dims it");
            TrainingSceneCapture(training, "structure-rnn-reduced-quiet");
            concepts.enabled = false;
            Check(concepts.RecurrentSignal(1) == Vector3.zero, "Disabling clears recurrent activity");
            concepts.enabled = true; yield return null;
            Check(concepts.RecurrentSignal(1).z > 0, "Re-enable binds the current RNN without stale activity");
        }
        finally
        {
            concepts.enabled = true; sim.S.stage = oldStage; controller.View.Juice.Reduced = reduced;
            presentation.SetQuiet(quiet); Time.timeScale = scale;
        }
        yield return new WaitForSecondsRealtime(.85f);
    }

    static IEnumerator StructureConvolutionScene(XingGuangController controller, ChapterOneRuntime runtime,
        XgWallpaperTraining training, XgWallpaperConcepts concepts, XgBrainWallpaperPresentation presentation)
    {
        TrainingSceneCase("Convolution: stage-2/16 pinned concepts fixture, real LeNet purchase, bounded grid scan on unscaled time");
        var sim = controller.Sim; var previousBoard = sim.S.board; int stage = sim.S.stage;
        bool reduced = controller.View.Juice.Reduced, quiet = presentation.Quiet; float scale = Time.timeScale;
        var board = new XgBoardState { nextId = 17 };
        for (int i = 1; i <= 16; i++) board.concepts.Add(new XgConcept
            { id = i, key = "conv-" + i, region = "vision", layer = 1, w = .5, s = 3, pinned = true });
        try
        {
            sim.S.stage = 2; sim.S.board = board; sim.S.unlocked.Remove("lenet");
            sim.Run(XgTrack.Vision).arch = "mlp"; controller.View.Juice.Reduced = false;
            yield return new WaitForSecondsRealtime(.9f);
            var node = XgCatalog.Node("lenet"); double money = controller.Host.Money, cost = sim.NodeCost(node);
            Check(sim.Status(node, controller.Host) == XgSim.NodeStatus.Buyable && sim.BuyNode("lenet", controller.Host),
                "Actual stage-two LeNet purchase succeeds with the finite Host");
            Check(sim.Run(XgTrack.Vision).arch == "lenet" && Math.Abs(money - controller.Host.Money - cost) < 1e-9,
                "LeNet purchase charges its true cost once and configures the actual vision model");
            Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(.9f); yield return new WaitForEndOfFrame();
            Check(concepts.Architectures[0] == "lenet" && concepts.Cells.Count == 16,
                "Convolution grid preserves the 16 protected real concept IDs");
            string state = TrainingSceneState(controller, runtime);
            Rect first = concepts.ScanWindow(0); int frames = 0, changes = 0;
            var previous = first; float deadline = Time.unscaledTime + 3;
            TrainingSceneCapture(training, "structure-conv-scan-a");
            while (Time.unscaledTime < deadline)
            {
                yield return null;
                Rect current = concepts.ScanWindow(0);
                var center = XgWallpaperConcepts.RegionCenter(0);
                if (current.width <= 0 || current.xMin < center.x - .06001f || current.xMax > center.x + .06001f
                    || current.yMin < center.y - .06001f || current.yMax > center.y + .06001f)
                    throw new Exception("Convolution scan escaped its visual region");
                if ((current.position - previous.position).sqrMagnitude > 1e-10f) changes++;
                previous = current; frames++;
            }
            yield return new WaitForEndOfFrame(); TrainingSceneCapture(training, "structure-conv-scan-b");
            Check(frames > 8 && changes > 5, "Scanner moves naturally across frames while paused and stays within the grid");
            Check(state == TrainingSceneState(controller, runtime), "Scanning makes no learning, RNG, wallet or progression changes");
            controller.View.Juice.Reduced = true; presentation.SetQuiet(true);
            yield return null; Rect still = concepts.ScanWindow(0);
            yield return new WaitForSecondsRealtime(1.05f); yield return new WaitForEndOfFrame();
            Check(still == concepts.ScanWindow(0) && concepts.canvasRenderer.GetAlpha() <= .41f,
                "Reduced keeps a static scan window and Quiet dims the whole structure");
            TrainingSceneCapture(training, "structure-conv-reduced-quiet");
            controller.View.Juice.Reduced = false; presentation.SetQuiet(false);
            board.concepts.Clear(); yield return new WaitForSecondsRealtime(.2f); yield return new WaitForEndOfFrame();
            Check(concepts.Cells.Count == 0 && concepts.ScanWindow(0).width > 0 && concepts.Links.Count == 0,
                "Empty convolution grid still scans without creating learned concepts or links");
            TrainingSceneCapture(training, "structure-conv-empty");
            concepts.enabled = false;
            Check(concepts.ScanWindow(0).width == 0, "Disabling the wallpaper clears scanner state");
            concepts.enabled = true; yield return null;
            Check(concepts.ScanWindow(0).width > 0, "Re-enable restores the current convolution structure");
        }
        finally
        {
            concepts.enabled = true; sim.S.stage = stage; sim.S.board = previousBoard;
            controller.View.Juice.Reduced = reduced; presentation.SetQuiet(quiet); Time.timeScale = scale;
        }
        yield return new WaitForSecondsRealtime(.85f);
    }

    static IEnumerator StructureMlpScene(XingGuangController controller, ChapterOneRuntime runtime,
        XgWallpaperTraining training, XgWallpaperConcepts concepts)
    {
        TrainingSceneCase("MLP: explicit six-pinned-concept visual fixture (normal purchases reset unpinned concepts), actual purchase/price/automatic architecture, natural-time morph");
        var sim = controller.Sim; var previousBoard = sim.S.board;
        bool reduced = controller.View.Juice.Reduced;
        float oldScale = Time.timeScale;
        var fixture = new XgBoardState { nextId = 7 };
        for (int i = 1; i <= 6; i++) fixture.concepts.Add(new XgConcept
            { id = i, key = "structure-" + i, region = "vision", layer = 1, w = .5, s = 3, pinned = true });
        try
        {
            sim.S.board = fixture;
            sim.S.unlocked.Remove("mlp");
            sim.Run(XgTrack.Vision).arch = "perceptron";
            runtime.Sim.S.money = 10000; // Purchase fixture only; never the player's save.
            controller.View.Juice.Reduced = false;
            yield return new WaitForSecondsRealtime(.9f); yield return new WaitForEndOfFrame();
            var starts = new Dictionary<int, Vector2>();
            foreach (var cell in concepts.Cells) starts[cell.id] = cell.uv;
            Check(starts.Count == 6 && concepts.Architectures[0] == "perceptron", "Six real fixture concepts begin on one perceptron column");
            TrainingSceneCapture(training, "structure-mlp-before");
            var node = XgCatalog.Node("mlp"); double cost = sim.NodeCost(node), money = controller.Host.Money;
            Check(sim.Status(node, controller.Host) == XgSim.NodeStatus.Buyable, "MLP can be bought through the real finite Host: " + sim.Why(node, controller.Host));
            Check(sim.BuyNode("mlp", controller.Host) && sim.Run(XgTrack.Vision).arch == "mlp"
                && Math.Abs(money - controller.Host.Money - cost) < 1e-9,
                "Actual MLP purchase charges once and automatically rewires the visual region");
            string state = TrainingSceneState(controller, runtime);
            Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(.23f); yield return new WaitForEndOfFrame();
            Check(concepts.Architectures[0] == "mlp", "Wallpaper follows the purchased architecture without another training round");
            int moved = 0;
            foreach (var cell in concepts.Cells)
            {
                Vector2 target = XgWallpaperConcepts.RegionCenter(0) + Vector2.Scale(new Vector2(.06f,.06f), XgWallpaperConcepts.LayoutPosition("mlp", cell.id - 1, 6));
                if (Vector2.Distance(cell.uv, starts[cell.id]) > .0001f && Vector2.Distance(cell.uv, target) > .0001f) moved++;
            }
            Check(moved >= 4, "Existing concept IDs move through intermediate positions on unscaled time");
            TrainingSceneCapture(training, "structure-mlp-moving");
            yield return new WaitForSecondsRealtime(.65f); yield return new WaitForEndOfFrame();
            var columns = new HashSet<int>();
            foreach (var cell in concepts.Cells)
            {
                columns.Add(Mathf.RoundToInt(cell.uv.x * 1000));
                var target = XgWallpaperConcepts.RegionCenter(0) + Vector2.Scale(new Vector2(.06f,.06f), XgWallpaperConcepts.LayoutPosition("mlp", cell.id - 1, 6));
                Check(Vector2.Distance(cell.uv, target) < .00001f && starts.ContainsKey(cell.id), "Concept keeps its ID and reaches the exact MLP layer: " + cell.id);
            }
            Check(columns.Count == 3 && concepts.Cells.Count == 6 && concepts.Links.Count == 0 && fixture.links.Count == 0,
                "Three structural layers do not invent learned concepts or Core links");
            Check(TrainingSceneState(controller, runtime) == state, "Structure morph alone preserves complete game state after purchase");
            TrainingSceneCapture(training, "structure-mlp-final");
            controller.View.Juice.Reduced = true;
            sim.Run(XgTrack.Vision).arch = "perceptron"; yield return new WaitForSecondsRealtime(.15f);
            sim.Run(XgTrack.Vision).arch = "mlp"; yield return null; yield return new WaitForEndOfFrame();
            foreach (var cell in concepts.Cells)
            {
                var target = XgWallpaperConcepts.RegionCenter(0) + Vector2.Scale(new Vector2(.06f,.06f), XgWallpaperConcepts.LayoutPosition("mlp", cell.id - 1, 6));
                Check(Vector2.Distance(cell.uv, target) < .00001f, "Reduced mode immediately adopts final structure: " + cell.id);
            }
            concepts.enabled = false; concepts.enabled = true;
            yield return null; yield return new WaitForEndOfFrame();
            Check(concepts.Cells.Count == 6, "Re-enabling an existing structure restores its concepts without replaying purchase");
            fixture.concepts.Clear();
            yield return new WaitForSecondsRealtime(.15f); yield return new WaitForEndOfFrame();
            Check(concepts.Cells.Count == 0 && concepts.Architectures[0] == "mlp" && concepts.canvasRenderer.GetMesh().vertexCount > 0,
                "An empty MLP region still shows structural sockets without fabricated learned concepts");
            TrainingSceneCapture(training, "structure-mlp-empty");
        }
        finally
        {
            Time.timeScale = oldScale; controller.View.Juice.Reduced = reduced;
            sim.S.board = previousBoard;
        }
        yield return new WaitForSecondsRealtime(.85f);
    }
}


