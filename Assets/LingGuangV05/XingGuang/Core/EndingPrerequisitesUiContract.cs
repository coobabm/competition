using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    // Display-only projection. Never pass the save object through a text renderer.
    public sealed class EndingUiText
    {
        public string zh = "", en = "";
        public bool reviewed;
        public string Get(bool english) => english ? en : zh;
        public EndingUiText Copy() => new EndingUiText { zh = zh, en = en, reviewed = reviewed };
        public static EndingUiText CopyOf(EndingUiText value) => value == null ? new EndingUiText() : value.Copy();
    }
    public sealed class EndingUiSource
    {
        public string id = "", date = "";
        public EndingUiText quote = new EndingUiText();
        public EndingUiSource Copy() => new EndingUiSource { id = id ?? "", date = date ?? "", quote = EndingUiText.CopyOf(quote) };
    }
    public sealed class EndingUiMemory
    {
        public string id = "";
        public bool shutdown;
        public bool pinnable = true;
        public EndingUiText title = new EndingUiText(), face = new EndingUiText();
        public List<EndingUiSource> sources = new List<EndingUiSource>();
        public EndingUiMemory Copy()
        {
            var c = new EndingUiMemory { id = id ?? "", shutdown = shutdown, pinnable = pinnable, title = EndingUiText.CopyOf(title), face = EndingUiText.CopyOf(face) };
            if (sources != null) foreach (var source in sources) if (source != null) c.sources.Add(source.Copy());
            return c;
        }
    }
    /// <summary>Public allowlist, constructed by release owner after privacy/content validation.</summary>
    public sealed class EndingPublicPreview
    {
        public string receiptDraftId = "";
        public bool verified;
        public EndingUiText product = new EndingUiText(), description = new EndingUiText(),
            capabilities = new EndingUiText(), limitations = new EndingUiText(), maintenanceLicense = new EndingUiText();
        public EndingPublicPreview Copy() => new EndingPublicPreview { receiptDraftId = receiptDraftId, verified = verified,
            product = EndingUiText.CopyOf(product), description = EndingUiText.CopyOf(description), capabilities = EndingUiText.CopyOf(capabilities),
            limitations = EndingUiText.CopyOf(limitations), maintenanceLicense = EndingUiText.CopyOf(maintenanceLicense) };
    }
    public sealed class EndingNightView
    {
        public string phase = "", requestId = "";
        public bool canStart, canContinue, running, paused, throttled, stopped, corrupted;
        public int answered, total;
        public double powerWh, temperatureC;
        public EndingUiText question = new EndingUiText(), answer = new EndingUiText(),
            reason = new EndingUiText(), stopLine = new EndingUiText();
        public List<EndingUiText> queue = new List<EndingUiText>();
        public EndingNightView Copy()
        {
            var c = (EndingNightView)MemberwiseClone();
            c.question = EndingUiText.CopyOf(question); c.answer = EndingUiText.CopyOf(answer); c.reason = EndingUiText.CopyOf(reason); c.stopLine = EndingUiText.CopyOf(stopLine);
            c.queue = new List<EndingUiText>(); if (queue != null) foreach (var row in queue) if (row != null) c.queue.Add(row.Copy());
            return c;
        }
    }
    public sealed class EndingUiSnapshot
    {
        // Both identities must come from the current runtime, never from a preview/save clone.
        public object owner, stateOwner;
        public long revision;
        public bool english, reduceFx, siReady, siRead, memoriesReviewed, canChooseRoute,
            routeLocked, pinsLocked, releaseReady, releaseCommitted, readOnly;
        public string route = "", decisionOwner = "";
        public int redTeamPassed;
        public EndingUiText blocker = new EndingUiText(), siLetter = new EndingUiText(),
            aiConclusion = new EndingUiText(), shutdownFace = new EndingUiText();
        public List<EndingUiSource> aiQuotes = new List<EndingUiSource>();
        public List<EndingUiMemory> memories = new List<EndingUiMemory>();
        public List<string> pinnedIds = new List<string>();
        public EndingPublicPreview preview = new EndingPublicPreview();
        public EndingNightView night = new EndingNightView();
        public EndingTrialView trial = new EndingTrialView();
        public EndingUiSnapshot Copy()
        {
            var c = (EndingUiSnapshot)MemberwiseClone();
            c.blocker = EndingUiText.CopyOf(blocker); c.siLetter = EndingUiText.CopyOf(siLetter); c.aiConclusion = EndingUiText.CopyOf(aiConclusion); c.shutdownFace = EndingUiText.CopyOf(shutdownFace);
            c.aiQuotes = new List<EndingUiSource>(); if (aiQuotes != null) foreach (var q in aiQuotes) if (q != null) c.aiQuotes.Add(q.Copy());
            c.memories = new List<EndingUiMemory>(); if (memories != null) foreach (var m in memories) if (m != null) c.memories.Add(m.Copy());
            c.pinnedIds = pinnedIds == null ? new List<string>() : new List<string>(pinnedIds);
            c.preview = preview == null ? new EndingPublicPreview() : preview.Copy(); c.night = night == null ? new EndingNightView() : night.Copy();
            c.trial = trial == null ? new EndingTrialView() : trial.Copy();
            return c;
        }
    }
    public enum EndingUiAction
    {
        ConfirmSiRead, ConfirmMemoryReview, ChooseRoute, AskAi, PinThree,
        CommitRelease, StartNight, AdvanceNight, PauseNight, ThrottleNight, PullPower, Continue,
        DiscoverMemories, BeginTrial, SubmitTrial, SubmitFinalPage
    }
    public sealed class EndingUiRequest
    {
        public object owner, stateOwner;
        public long revision, requestId;
        public EndingUiAction action;
        public string value = "", previewId = "";
        public string[] ids = new string[0];
        public double seconds;
        public bool enabled;
        public int question;
        public string attemptId = "";
        public int[] picks = new int[0];
    }
    public sealed class EndingUiResult
    {
        public bool success;
        public EndingUiSnapshot snapshot;
        public EndingUiText error = new EndingUiText();
    }
    public sealed class EndingUiCallbacks
    {
        public Func<object, object, long, bool> IsCurrent;
        // Return only after durable save succeeds, with the resulting revision. Failure must roll back core.
        public Action<EndingUiRequest, Action<EndingUiResult>> Execute;
        public Action<object, object, long, string> OpenSource;
        public Action<object, object, long> OpenPrerequisites;
        public Action<object, object, long> OpenPreparation;
        // Required dynamic guard and separate reviewed-content validator; never erase authored history here.
        public Func<string, bool, string> FilterText;
        public Func<string, bool, string> FilterReviewedText;
    }
    /// <summary>Single in-flight transaction; stale, hidden and duplicate callbacks cannot repaint UI.</summary>
    public sealed class EndingUiSession
    {
        public EndingUiSnapshot Snapshot { get; private set; }
        public EndingUiCallbacks Callbacks { get; private set; }
        public bool Visible { get; private set; }
        public bool Busy { get; private set; }
        public bool Failed { get; private set; }
        public EndingUiText Failure { get; private set; } = new EndingUiText();
        public event Action Changed;
        long generation, serial;
        public void Bind(EndingUiSnapshot snapshot, EndingUiCallbacks callbacks)
        {
            generation++; Busy = false; Failed = false; Failure = new EndingUiText();
            Snapshot = snapshot == null ? null : snapshot.Copy(); Callbacks = callbacks;
            Changed?.Invoke();
        }
        public void SetVisible(bool visible)
        {
            if (Visible == visible) return;
            Visible = visible; generation++; Busy = false;
            Changed?.Invoke();
        }
        public bool Current => Visible && Snapshot != null && Snapshot.owner != null && Snapshot.stateOwner != null &&
            Callbacks != null && Callbacks.IsCurrent != null && Callbacks.IsCurrent(Snapshot.owner, Snapshot.stateOwner, Snapshot.revision);
        public bool CanAct => Current && !Busy && !Snapshot.readOnly && Callbacks.Execute != null && Callbacks.FilterText != null && Callbacks.FilterReviewedText != null;
        public bool Send(EndingUiAction action, string value = "", string[] ids = null, bool enabled = false, double seconds = 0,
            int question = 0, string attemptId = "", int[] picks = null)
        {
            if (!CanAct || double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0 || seconds > 1) return false;
            var s = Snapshot;
            var request = new EndingUiRequest { owner = s.owner, stateOwner = s.stateOwner, revision = s.revision,
                requestId = ++serial, action = action, value = value, ids = ids == null ? new string[0] : (string[])ids.Clone(),
                enabled = enabled, seconds = seconds, previewId = s.preview.receiptDraftId,
                question = question, attemptId = attemptId, picks = picks == null ? new int[0] : (int[])picks.Clone() };
            long expectedGeneration = generation;
            bool completed = false;
            Busy = true; Failed = false; Failure = new EndingUiText(); Changed?.Invoke();
            Action<EndingUiResult> finish = result =>
            {
                if (completed) return; completed = true;
                if (!Visible || generation != expectedGeneration || !ReferenceEquals(Snapshot, s)) return;
                Busy = false;
                var next = result == null ? null : result.snapshot;
                bool valid = result != null && result.success && next != null &&
                    ReferenceEquals(next.owner, s.owner) && ReferenceEquals(next.stateOwner, s.stateOwner) &&
                    next.revision >= s.revision && Callbacks.IsCurrent(next.owner, next.stateOwner, next.revision);
                if (valid) Snapshot = next.Copy();
                Failed = !valid; Failure = valid || result == null ? new EndingUiText() : EndingUiText.CopyOf(result.error); Changed?.Invoke();
            };
            try { Callbacks.Execute(request, finish); } catch { finish(new EndingUiResult()); }
            return true;
        }
        public void Source(string id)
        {
            if (Current && !Busy && !string.IsNullOrEmpty(id))
                Callbacks.OpenSource?.Invoke(Snapshot.owner, Snapshot.stateOwner, Snapshot.revision, id);
        }
        public void Prerequisites()
        {
            if (Current && !Busy) Callbacks.OpenPrerequisites?.Invoke(Snapshot.owner, Snapshot.stateOwner, Snapshot.revision);
        }
        public void Preparation()
        {
            if (Current && !Busy) Callbacks.OpenPreparation?.Invoke(Snapshot.owner, Snapshot.stateOwner, Snapshot.revision);
        }
    }
    public sealed class EndingTrialMenuItem
    {
        public int question, bestStars;
        public bool canBegin;
        public EndingUiText title = new EndingUiText();
        public EndingTrialMenuItem Copy() => new EndingTrialMenuItem { question = question, bestStars = bestStars, canBegin = canBegin, title = EndingUiText.CopyOf(title) };
    }
    public sealed class EndingTrialItemView
    {
        public EndingUiText prompt = new EndingUiText(), ai = new EndingUiText(), photoDescription = new EndingUiText(),
            original = new EndingUiText(), feedback = new EndingUiText();
        public string photoId = "";
        public List<EndingUiText> choices = new List<EndingUiText>();
        public EndingTrialItemView Copy()
        {
            var c = new EndingTrialItemView { prompt = EndingUiText.CopyOf(prompt), ai = EndingUiText.CopyOf(ai),
                photoDescription = EndingUiText.CopyOf(photoDescription), original = EndingUiText.CopyOf(original),
                feedback = EndingUiText.CopyOf(feedback), photoId = photoId ?? "" };
            if (choices != null) foreach (var choice in choices) if (choice != null) c.choices.Add(choice.Copy());
            return c;
        }
    }
    /// <summary>Only the saved sheet, with original prompt/choices; no answer key in active UI projection.</summary>
    public sealed class EndingTrialView
    {
        public bool active, submitted, markMode, allPassed, finalPageReady, finalPageSaved, canDiscoverMemories;
        public int question, score, stars;
        public string attemptId = "", playerLastLine = "";
        public EndingUiText title = new EndingUiText(), ability = new EndingUiText(), note = new EndingUiText(), aiLastLine = new EndingUiText();
        public List<EndingUiText> hints = new List<EndingUiText>();
        public List<EndingTrialMenuItem> menu = new List<EndingTrialMenuItem>();
        public List<EndingTrialItemView> items = new List<EndingTrialItemView>();
        public List<int> submittedPicks = new List<int>();
        public EndingTrialView Copy()
        {
            var c = (EndingTrialView)MemberwiseClone();
            c.title = EndingUiText.CopyOf(title); c.ability = EndingUiText.CopyOf(ability); c.note = EndingUiText.CopyOf(note); c.aiLastLine = EndingUiText.CopyOf(aiLastLine);
            c.hints = new List<EndingUiText>(); if (hints != null) foreach (var h in hints) if (h != null) c.hints.Add(h.Copy());
            c.menu = new List<EndingTrialMenuItem>(); if (menu != null) foreach (var m in menu) if (m != null) c.menu.Add(m.Copy());
            c.items = new List<EndingTrialItemView>(); if (items != null) foreach (var item in items) if (item != null) c.items.Add(item.Copy());
            c.submittedPicks = submittedPicks == null ? new List<int>() : new List<int>(submittedPicks);
            return c;
        }
    }
}
