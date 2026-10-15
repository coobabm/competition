$ErrorActionPreference = 'Stop'
$root = (Get-Location).Path
$candidate = Join-Path $root 'Documentation/DesignPass/Resume/sense-start-candidate'
$sourceRoot = Join-Path $candidate 'sources'
New-Item -ItemType Directory -Force -Path $sourceRoot | Out-Null
$manifest = [Collections.Generic.List[object]]::new()
function Candidate([string]$relative, [scriptblock]$edit) {
    $bytes = [IO.File]::ReadAllBytes((Join-Path $root $relative))
    $before = [Text.Encoding]::UTF8.GetString($bytes)
    $after = & $edit $before.Replace("`r`n", "`n")
    $target = Join-Path $sourceRoot ([IO.Path]::GetFileName($relative))
    [IO.File]::WriteAllText($target, $after)
    $manifest.Add([pscustomobject]@{ source=$relative; sourceSha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)); candidate=$target; candidateSha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $target).Hash })
}
function ReplaceExact([string]$text, [string]$before, [string]$after) {
    if (-not $text.Contains($before)) { throw "Missing candidate anchor: $before" }
    return $text.Replace($before, $after)
}
Candidate 'Assets/LingGuangV05/XingGuang/Core/XgSim.Progression.cs' {
    param($s)
    $s = ReplaceExact $s 'S.vision.dataset = "mnist"; S.sequence.dataset = "spam";' 'S.vision.dataset = "mnist"; S.sequence.dataset = XgMemes.SenseDesk;'
    $s = ReplaceExact $s 'S.desksOpen.Clear(); S.desksOpen.Add("arith"); S.desksOpen.Add("logic"); S.desksOpen.Add("spam"); S.desksOpen.Add(XgMemes.SenseDesk);' 'S.desksOpen.Clear(); S.desksOpen.Add("logic"); S.desksOpen.Add("spam"); S.desksOpen.Add(XgMemes.SenseDesk);'
    $s = ReplaceExact $s 'S.desk = "spam"; // start with a recognisable teaching card' 'S.desk = XgMemes.SenseDesk;'
    $s = ReplaceExact $s 'Stage one opens arithmetic, logic, common sense and SMS spam; stage two adds digits and danmaku (design v1.1 §7); the rest come with packs.' 'Stage one opens logic, common sense and SMS spam; stage two adds arithmetic, digits and danmaku. Previously opened desks remain available on load.'
    $s = ReplaceExact $s 'XgCatalog.Desk(id) != null && (id == "arith" || id == "logic" || id == "spam" || id == XgMemes.SenseDesk || (id == "mnist" || id == "danmu") && S.stage >= 2 || Has(id + ".pack") || Owns(id))' 'XgCatalog.Desk(id) != null && (DeskOpen(id) || id == "logic" || id == "spam" || id == XgMemes.SenseDesk || (id == "arith" || id == "mnist" || id == "danmu") && S.stage >= 2 || Has(id + ".pack") || Owns(id))'
    $s
}
Candidate 'Assets/LingGuangV05/XingGuang/Core/XgCatalog.cs' {
    param($s)
    ReplaceExact $s 'kind = XgDeskKind.Arith, unlock = XgDeskUnlock.Start' 'kind = XgDeskKind.Arith, unlock = XgDeskUnlock.FirstEpoch'
}
Candidate 'Assets/LingGuangV05/XingGuang/Core/XgSim.cs' {
    param($s)
    $s = ReplaceExact $s 'How a locked desk opens, under the current rules: digits and danmaku open by themselves with the second' 'How a locked desk opens, under the current rules: arithmetic, digits and danmaku open by themselves with the second'
    ReplaceExact $s 'if ((desk.id == "mnist" || desk.id == "danmu") && S.stage < 2)' 'if ((desk.id == "arith" || desk.id == "mnist" || desk.id == "danmu") && S.stage < 2)'
}
Candidate 'Assets/LingGuangV05/XingGuang/Desktop/XingGuangController.cs' {
    param($s)
    ReplaceExact $s ('            if (state == null && Sim.DeskOpen("spam")) Sim.S.desk = "spam";' + "`n") ''
}
Candidate 'Assets/LocalBuild/Editor/DesignFirstLessonChecks.cs' {
    param($s)
    $s = ReplaceExact $s 'Check(first.S.desk == "spam" && first.S.handCorrect == 0 && first.S.handWrong == 0,' 'Check(first.S.desk == XgMemes.SenseDesk && first.S.handCorrect == 0 && first.S.handWrong == 0,'
    $s = ReplaceExact $s 'Fresh game starts at the first SMS lesson' 'Fresh game starts at the first common-sense lesson'
    $s = ReplaceExact $s 'Untrained SMS prediction does not read ground truth' 'Untrained common-sense prediction does not read ground truth'
    $s = ReplaceExact $s 'ReferenceEquals(first.Card(first.S.desk), firstCard) && first.Samples("spam") == 0' 'ReferenceEquals(first.Card(first.S.desk), firstCard) && first.Samples(XgMemes.SenseDesk) == 0'
    $s = ReplaceExact $s ('        var sim = new XgSim();' + "`n" + '        card = new XgCard { id = 90001, dataset = "spam"') ('        var sim = new XgSim();' + "`n" + '        sim.SelectDesk("spam"); // This answer-path fixture deliberately visits the SMS desk.' + "`n" + '        card = new XgCard { id = 90001, dataset = "spam"')
    $s = ReplaceExact $s '        RunFirstMinuteFocus();' ('        RunSenseDeskProgression();' + "`n" + '        RunFirstMinuteFocus();')
    $helper = @'
    static void RunSenseDeskProgression()
    {
        var fresh = new XgSim(); var wallet = new Wallet();
        Check(fresh.S.desk == XgMemes.SenseDesk && fresh.S.sequence.dataset == XgMemes.SenseDesk
            && !fresh.DeskOpen("arith") && !fresh.ProgressionDeskAvailable("arith"),
            "Fresh stage one starts on common sense and keeps arithmetic locked");
        fresh.SelectDesk("arith"); fresh.Tick(.1, wallet);
        Check(fresh.S.desk == XgMemes.SenseDesk && !fresh.DeskOpen("arith"),
            "Selecting or ticking a fresh stage-one save cannot open arithmetic");
        var reloaded = new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(fresh.S)));
        Check(reloaded.S.desk == XgMemes.SenseDesk && !reloaded.DeskOpen("arith"),
            "New stage-one save reload does not re-add arithmetic through repair");
        reloaded.S.stage = 2; reloaded.Tick(.1, wallet);
        Check(reloaded.DeskOpen("arith") && reloaded.ProgressionDeskAvailable("arith")
            && reloaded.DeskConditionText(XgCatalog.Desk("arith")) == "",
            "Arithmetic opens automatically at stage two without buying its data pack");
        foreach (string desk in new[] { "spam", "arith" })
        {
            var old = new XgSim();
            if (!old.S.desksOpen.Contains("arith")) old.S.desksOpen.Add("arith");
            old.SelectDesk(desk);
            old.S.labels.Add(new XgLabelCount { dataset = "arith", count = 7 });
            long cardId = old.Card(desk).id;
            var restored = new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(old.S)));
            Check(restored.S.stage == 1 && restored.S.desk == desk && restored.DeskOpen("arith")
                && restored.ProgressionDeskAvailable("arith") && restored.Labels("arith") == 7
                && restored.Card(desk).id == cardId,
                "Existing stage-one save preserves selected desk, arithmetic access, labels and card: " + desk);
        }
    }

'@
    ReplaceExact $s '    static void RunFirstMinuteFocus()' ($helper + '    static void RunFirstMinuteFocus()')
}
Candidate 'Assets/LocalBuild/Editor/PlayabilityChecks.cs' {
    param($s)
    $s = ReplaceExact $s 'Check(sim.S.desk == "spam", "Fresh game starts with a recognisable SMS lesson");' 'Check(sim.S.desk == XgMemes.SenseDesk, "Fresh game starts with a common-sense lesson");'
    $s = ReplaceExact $s 'Check(sim.GhostGuess("spam", out _, out _), "First card has a prediction without a checkpoint");' ('Check(sim.GhostGuess(XgMemes.SenseDesk, out _, out _), "Default common-sense card has a prediction without a checkpoint");' + "`n" + '            sim.SelectDesk("spam"); // The following reward and assessment regression is specifically an SMS lesson.')
    ReplaceExact $s 'Check(reader.DeskOpen(XgMemes.SenseDesk) && reader.S.desk == "spam", "New sense desk is preserved alongside the SMS teaching start");' 'Check(reader.DeskOpen(XgMemes.SenseDesk) && reader.S.desk == XgMemes.SenseDesk, "Common sense remains the default desk after resident reading");'
}
Candidate 'Assets/LocalBuild/Editor/DesignWallpaperSmoke.FirstMinute.cs' {
    param($s)
    $s = ReplaceExact $s ('            if (deterministicCorrection)' + "`n" + '            {') ('            Check(controller.Sim.S.desk == XgMemes.SenseDesk && controller.Sim.Card(controller.Sim.S.desk).dataset == XgMemes.SenseDesk' + "`n" + '                && !controller.Sim.DeskOpen("arith"), "Real setup enters the default common-sense desk with arithmetic still locked");' + "`n" + '            if (deterministicCorrection)' + "`n" + '            {')
    $before = @'
                var fixture = new XgCard { id = 91370001, dataset = "spam", seed = 9137, truth = true, level = 1,
                    question = "恭喜您中奖，请点击领取", questionEn = "Congratulations! Click to claim your prize." };
                controller.Sim.S.cards.RemoveAll(c => c.dataset == "spam");
'@
    $after = @'
                var phrase = XgMemes.Pick(XgMemes.SenseDesk, new System.Random(9137), 1, controller.Sim.Today, controller.Sim.Topic, false);
                Check(phrase != null && phrase.yes && phrase.textEn == "Beijing is the capital of China.",
                    "Correction input is a real, authored common-sense phrase with unchanged truth");
                var fixture = new XgCard { id = 91370001, dataset = XgMemes.SenseDesk, track = (int)XgTrack.Sequence,
                    seed = 9137, truth = phrase.yes, level = 1, question = phrase.text, questionEn = phrase.textEn,
                    why = phrase.why, whyEn = phrase.whyEn, source = phrase.source,
                    category = XgCatalog.Desk(XgMemes.SenseDesk).name, categoryEn = XgCatalog.Desk(XgMemes.SenseDesk).nameEn };
                var features = XgBoardData.FromCard(fixture);
                bool realWords = features.region == "sequence" && features.features.Count > 0;
                foreach (var feature in features.features) realWords &= fixture.question.Contains(feature.name);
                Check(realWords, "Deterministic common-sense card uses its real production word features");
                controller.Sim.S.cards.RemoveAll(c => c.dataset == XgMemes.SenseDesk);
'@
    $s = ReplaceExact $s $before $after
    $s = ReplaceExact $s 'Scenario=deterministic correction; only card input replaced before any lesson.' 'Scenario=deterministic common-sense correction; only card input replaced with its authored bank phrase before any lesson.'
    $s = ReplaceExact $s 'question.text == "Congratulations! Click to claim your prize."' 'question.text == "Beijing is the capital of China."'
    $s
}
[IO.File]::WriteAllText((Join-Path $candidate 'source-manifest.json'), ($manifest | ConvertTo-Json -Depth 5))
$production = [Collections.Generic.List[string]]::new()
$verification = [Collections.Generic.List[string]]::new()
foreach ($item in $manifest) {
    $patch = Join-Path $candidate ([IO.Path]::GetFileNameWithoutExtension($item.source) + '.patch')
    & git diff --no-index --ignore-space-at-eol -- $item.source $item.candidate > $patch
    if ($LASTEXITCODE -gt 1) { throw "Could not produce patch for $($item.source)" }
    $lines = [IO.File]::ReadAllLines($patch)
    $lines[0] = 'diff --git a/' + $item.source + ' b/' + $item.source
    $crlf = [IO.File]::ReadAllText((Join-Path $root $item.source)).Contains("`r`n")
    for ($i = 0; $i -lt $lines.Length; $i++) {
        if ($lines[$i].StartsWith('--- ')) { $lines[$i] = '--- a/' + $item.source }
        elseif ($lines[$i].StartsWith('+++ ')) { $lines[$i] = '+++ b/' + $item.source }
        elseif ($crlf -and ($lines[$i].StartsWith(' ') -or $lines[$i].StartsWith('+') -or $lines[$i].StartsWith('-'))) { $lines[$i] += "`r" }
    }
    $text = ($lines -join "`n") + "`n"
    [IO.File]::WriteAllText($patch, $text)
    if ($item.source.Contains('/Editor/')) { $verification.Add($text) } else { $production.Add($text) }
}
[IO.File]::WriteAllText((Join-Path $candidate 'production.patch'), ($production -join "`n"))
[IO.File]::WriteAllText((Join-Path $candidate 'verification.patch'), ($verification -join "`n"))
$manifest | Select-Object source, sourceSha256, candidateSha256 | Format-List
