$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../../../../..')).Path
$sources = Join-Path $PSScriptRoot 'sources'
New-Item -ItemType Directory -Force -Path $sources | Out-Null
$manifest = @()
function ReplaceExact([string]$text, [string]$before, [string]$after) {
    if (-not $text.Contains($before)) { throw "Missing source text: $before" }
    return $text.Replace($before, $after)
}
function Candidate([string]$relative, [scriptblock]$change) {
    $original = Join-Path $root $relative
    $candidate = Join-Path $sources ([IO.Path]::GetFileName($relative))
    $raw = [IO.File]::ReadAllText($original)
    $updated = & $change $raw
    [IO.File]::WriteAllText($candidate, $updated, [Text.UTF8Encoding]::new($false))
    $script:manifest += [pscustomobject]@{
        source = $relative
        candidate = $candidate
        sourceSha256 = (Get-FileHash -LiteralPath $original).Hash
        candidateSha256 = (Get-FileHash -LiteralPath $candidate).Hash
    }
}
Candidate 'Assets/LingGuangV05/XingGuang/Core/XgSim.Teaching.cs' {
    param($s)
    $s = ReplaceExact $s 'if (S.checkups.Count == 0) AddCheckup(FixedCheckup(1, "spam"), "spam", false);' 'if (!S.checkups.Exists(c => c.dataset == card.dataset && !c.taught)) AddCheckup(FixedCheckup(card.dataset), card.dataset, false);'
    $s = ReplaceExact $s 'string dataset = new[] { "spam", "mnist", "poems", "danmu", "longtext", "logic" }[Math.Max(0, Math.Min(5, S.stage - 1))];' 'string dataset = assessment.dataset;'
    $s = ReplaceExact $s 'FixedCheckup(S.stage, dataset)' 'FixedCheckup(dataset)'
    $s = ReplaceExact $s 'if (!seen.Add(c.question)) continue;' 'if (c.dataset != dataset || !seen.Add(c.question)) continue;'
    $s = ReplaceExact $s ('            while (S.checkups.Count > CheckupMemory) S.checkups.RemoveAt(0);' + "`r`n") ''
    $s = ReplaceExact $s '                answer = answer ? "是" : "否", correct = answer == card.truth, taught = taught });' ('                answer = answer ? "是" : "否", correct = answer == card.truth, taught = taught });' + "`r`n" + '            while (S.checkups.Count > CheckupMemory) S.checkups.RemoveAt(0);')
    $s = ReplaceExact $s 'static XgBoardCard FixedCheckup(int stage, string dataset)' 'static XgBoardCard FixedCheckup(string dataset)'
    $s = ReplaceExact $s 'if (stage == 1) source' 'if (dataset == "spam") source'
    $s = ReplaceExact $s 'else if (stage == 2) source' 'else if (dataset == "mnist") source'
    $s = ReplaceExact $s 'else if (stage == 3) source' 'else if (dataset == "poems") source'
    $s = ReplaceExact $s 'card.text = stage == 1 ?' 'card.text = dataset == "spam" ?'
    $s = ReplaceExact $s ': stage == 2 ?' ': dataset == "mnist" ?'
    $s = ReplaceExact $s ': stage == 3 ?' ': dataset == "poems" ?'
    $s = ReplaceExact $s ': !string.IsNullOrEmpty(card.text) ? card.text : "判断「"' ': !string.IsNullOrEmpty(card.text) ? card.text : !string.IsNullOrEmpty(card.source?.question) ? card.source.question : "判断「"'
    $s = ReplaceExact $s 'public string CheckupText(bool history = false)' 'public string CheckupText(bool history = false, string dataset = null)'
    $s = ReplaceExact $s '            if (S.checkups == null || S.checkups.Count == 0) return T(' ("            int latest = -1;`r`n            if (S.checkups != null) foreach (var c in S.checkups)`r`n                if (dataset == null || c.dataset == dataset) latest = c.assessment;`r`n            if (latest < 0) return T(")
    $s = ReplaceExact $s ('            int latest = S.checkups[S.checkups.Count - 1].assessment;' + "`r`n") ''
    $s = ReplaceExact $s '                if (!history && c.assessment != latest || history && c.taught) continue;' ("                if (dataset != null && c.dataset != dataset) continue;`r`n" + '                if (!history && c.assessment != latest || history && c.taught) continue;')
    $s
}
Candidate 'Assets/LingGuangV05/XingGuang/Desktop/XgTrainPage.Side.cs' {
    param($s)
    ReplaceExact $s 'learningText.text = Sim.CheckupText();' 'learningText.text = Sim.CheckupText(dataset: run.dataset);'
}
Candidate 'Assets/LocalBuild/Editor/DesignWallpaperSmoke.SenseStart.cs' {
    param($s)
    $lineEnd = if ($s.Contains('            yield return new WaitForEndOfFrame();' + "`r`n")) { "`r`n" } else { "`n" }
    $before = '            yield return new WaitForEndOfFrame();' + $lineEnd + '            SenseStartCapture("sense-start-training-handoff");'
    $after = @'
            var learning = SenseStartField<TMP_Text>(controller.View.Train, "learningText");
            var fixedSense = XgBoardData.Make(XgMemes.SenseDesk, 9137, 1, 20160524);
            string comparison = fixedSense.source.question;
            bool authored = false;
            foreach (var phrase in XgMemes.SenseBank)
                authored |= phrase.text == comparison && phrase.yes == fixedSense.truth;
            Check(authored && learning.gameObject.activeInHierarchy
                && learning.text == sim.CheckupText(dataset: XgMemes.SenseDesk)
                && learning.text.Contains(comparison) && !learning.text.Contains("中奖"),
                "Visible learning comparison shows the real common-sense bank question for the selected dataset");
            XgCheckup baseline = null;
            foreach (var checkup in sim.S.checkups)
                if (checkup.dataset == XgMemes.SenseDesk && !checkup.taught) baseline = checkup;
            Check(baseline != null && baseline.assessment == 0 && baseline.question == comparison
                && learning.text.Contains(" → " + baseline.answer),
                "Visible learning comparison renders the stored pre-training model answer to that same sense question");
            string comparisonState = JsonUtility.ToJson(sim.S);
            controller.View.Refresh(true);
            Check(JsonUtility.ToJson(sim.S) == comparisonState,
                "Refreshing the real training comparison preserves learning and saved state");
            yield return new WaitForEndOfFrame();
            SenseStartCapture("sense-start-training-handoff");
'@
    $after = $after.Replace("`r`n", "`n").Replace("`n", $lineEnd)
    ReplaceExact $s $before $after
}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'source-manifest.json'), ($manifest | ConvertTo-Json -Depth 5))
$production = [Collections.Generic.List[string]]::new()
$verification = [Collections.Generic.List[string]]::new()
foreach ($entry in $manifest) {
    $start = [Diagnostics.ProcessStartInfo]::new('git')
    $start.WorkingDirectory = $root
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($argument in @('diff','--no-index','--no-ext-diff','--', $entry.source, $entry.candidate)) { $start.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::Start($start)
    $diff = $process.StandardOutput.ReadToEnd()
    $errorText = $process.StandardError.ReadToEnd()
    $process.WaitForExit()
    if ($process.ExitCode -gt 1) { throw $errorText }
    $lines = $diff.Split("`n")
    $lines[0] = 'diff --git a/' + $entry.source + ' b/' + $entry.source
    for ($i = 0; $i -lt $lines.Length; $i++) {
        if ($lines[$i].StartsWith('--- ')) { $lines[$i] = '--- a/' + $entry.source }
        elseif ($lines[$i].StartsWith('+++ ')) { $lines[$i] = '+++ b/' + $entry.source }
    }
    $diff = $lines -join "`n"
    [IO.File]::WriteAllText((Join-Path $PSScriptRoot ([IO.Path]::GetFileNameWithoutExtension($entry.source) + '.patch')), $diff)
    if ($entry.source.Contains('/Editor/')) { $verification.Add($diff) } else { $production.Add($diff) }
}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'production.patch'), ($production -join "`n"))
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'verification.patch'), ($verification -join "`n"))
$manifest | ConvertTo-Json -Depth 5
