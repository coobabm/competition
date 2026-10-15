$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
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
    [IO.File]::WriteAllText($candidate, (& $change ([IO.File]::ReadAllText($original))), [Text.UTF8Encoding]::new($false))
    $script:manifest += [pscustomobject]@{
        source = $relative; candidate = $candidate
        sourceSha256 = (Get-FileHash -LiteralPath $original).Hash
        candidateSha256 = (Get-FileHash -LiteralPath $candidate).Hash
    }
}
Candidate 'Assets/LingGuangV05/XingGuang/Desktop/XgLabelPage.cs' {
    param($s)
    $s = ReplaceExact $s 'TMP_Text wkTitle, wkRule, qno;' 'TMP_Text wkTitle, wkRule, qno, lessonProgress;'
    $anchor = '            qno = ui.Text(Rect("QuestionNo", body, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-94, -58), new Vector2(-14, -36)), "", 13, ZhongbaoSkin.Mute, TextAlignmentOptions.MidlineRight);'
    $lineEnd = if ($s.Contains($anchor + "`r`n")) { "`r`n" } else { "`n" }
    $s = ReplaceExact $s $anchor ($anchor + $lineEnd + '            lessonProgress = ui.Text(Rect("LessonProgress", body, new Vector2(0, 1), new Vector2(1, 1), new Vector2(14, -58), new Vector2(-14, -36)), "", 16, ZhongbaoSkin.Ink, TextAlignmentOptions.Center);' + $lineEnd + '            lessonProgress.gameObject.SetActive(false);')
    $anchor = '            if (teachingCompanion != null) teachingCompanion.gameObject.SetActive(!special && Sim.TeachMode && Sim.ReviewCard(Sim.S.desk) == null);'
    $lineEnd = if ($s.Contains($anchor + "`r`n")) { "`r`n" } else { "`n" }
    $goal = @'
            bool lessonGoal = !special && Sim.TeachMode && Sim.S.stage == 1 && Sim.S.epochs == 0 && Sim.ReviewCard(Sim.S.desk) == null;
            lessonProgress.gameObject.SetActive(lessonGoal);
            meta.gameObject.SetActive(!lessonGoal); qno.gameObject.SetActive(!lessonGoal);
            if (lessonGoal)
                lessonProgress.text = (int)Math.Max(0, Math.Min(XgCatalog.SamplesToTrain, Sim.Samples(Sim.S.desk))) + "/" + XgCatalog.SamplesToTrain;
'@
    ReplaceExact $s $anchor ($goal.Replace("`r`n", "`n").Replace("`n", $lineEnd) + $lineEnd + $anchor)
}
Candidate 'Assets/LingGuangV05/XingGuang/Desktop/XgTeachCompanion.cs' {
    param($s)
    $anchor = '            PlaceBottom(self.train, 106, 48, 20, 20);'
    $lineEnd = if ($s.Contains($anchor + "`r`n")) { "`r`n" } else { "`n" }
    $s = ReplaceExact $s $anchor ($anchor + $lineEnd + '            self.train.rt.gameObject.SetActive(false);')
    $anchor = '            train.Set(firstLesson ? T("训练 →", "Train →") : ready ? T("带它去训练 →", "Train together →") : T("再教 ", "Teach ") + (XgCatalog.SamplesToTrain - count) + T(" 张就能训练", " more cards"), ready, ready && !firstLesson ? XgDark.Accent : XgDark.Track, XgDark.Ink);'
    ReplaceExact $s $anchor ($anchor + $lineEnd + '            train.rt.gameObject.SetActive(ready);')
}
Candidate 'Assets/LocalBuild/Editor/DesignWallpaperSmoke.FirstMinute.cs' {
    param($s)
    $anchor = '            int answers = 0, sample = -1;'
    $lineEnd = if ($s.Contains($anchor + "`r`n")) { "`r`n" } else { "`n" }
    $s = ReplaceExact $s $anchor ('            CheckLessonGoalState(controller, 0);' + $lineEnd + $anchor)
    $anchor = '                    Check(train.button.IsInteractable() && chat.button.IsInteractable() && board.button.IsInteractable(),'
    $s = ReplaceExact $s $anchor ('                    CheckLessonGoalState(controller, XgCatalog.SamplesToTrain);' + $lineEnd + $anchor)
    $anchor = '        CheckFirstMinuteEnglishTextFits(train.label, "English companion train button");'
    $lineEnd = if ($s.Contains($anchor + "`r`n")) { "`r`n" } else { "`n" }
    $replacement = @'
        CheckLessonGoalState(controller, answered ? 1 : 0);
        if (train.rt.gameObject.activeInHierarchy)
            CheckFirstMinuteEnglishTextFits(train.label, "English companion train button");
        else
            Check(!train.button.IsInteractable(), "English training action stays hidden and unavailable before four examples");
'@
    ReplaceExact $s $anchor ($replacement.Replace("`r`n", "`n").Replace("`n", $lineEnd))
}
Candidate 'Assets/LocalBuild/Editor/DesignWallpaperSmoke.cs' {
    param($s)
    $anchor = '            if (SessionState.GetString("DesignWallpaperSmoke.Mode", "") == "sense-start" && step >= 240)'
    $lineEnd = if ($s.Contains($anchor + "`r`n")) { "`r`n" } else { "`n" }
    $route = @'
            if (SessionState.GetString("DesignWallpaperSmoke.Mode", "") == "lesson-goal" && step >= 250)
            {
                TickLessonGoal(controller, runtime); return;
            }
'@
    $s = ReplaceExact $s $anchor ($route.Replace("`r`n", "`n").Replace("`n", $lineEnd) + $lineEnd + $anchor)
    $anchor = '                    if (mode == "sense-start") step = 240;'
    $lineEnd = if ($s.Contains($anchor + "`r`n")) { "`r`n" } else { "`n" }
    ReplaceExact $s $anchor ($anchor + $lineEnd + '                    if (mode == "lesson-goal") step = 250;')
}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'source-manifest.json'), ($manifest | ConvertTo-Json -Depth 5))
$production = [Collections.Generic.List[string]]::new()
$verification = [Collections.Generic.List[string]]::new()
foreach ($entry in $manifest) {
    $start = [Diagnostics.ProcessStartInfo]::new('git')
    $start.WorkingDirectory = $root; $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true; $start.RedirectStandardError = $true
    foreach ($argument in @('diff','--no-index','--no-ext-diff','--', $entry.source, $entry.candidate)) { $start.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::Start($start)
    $diff = $process.StandardOutput.ReadToEnd(); $errorText = $process.StandardError.ReadToEnd(); $process.WaitForExit()
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
