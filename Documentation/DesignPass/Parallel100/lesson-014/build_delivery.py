"""Build this isolated delivery; never writes production files."""
import difflib
import hashlib
import json
from pathlib import Path

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[3]
TARGET = "Assets/LingGuangV05/XingGuang/Desktop/XgWallpaperConcepts.cs"
ADDED = "Assets/LingGuangV05/XingGuang/Desktop/XgWallpaperConcepts.Lesson.cs"
original = (ROOT / TARGET).read_text(encoding="utf-8-sig")
candidate = original

def replace_once(before, after):
    global candidate
    assert candidate.count(before) == 1, before
    candidate = candidate.replace(before, after)

replace_once("            graphic.BuildRegionLabels();", "            graphic.BuildRegionLabels();\n            graphic.ObserveLessonSource();")
replace_once("        void Update()\n        {", "        void Update()\n        {\n            ObserveLessonSource();\n            TickLessonPulse();")
replace_once("            float alpha = (presentation != null ? presentation.LightLevel : 1) * reveal;",
             "            // A completed lesson may glow immediately during the initial region fade.\n"
             "            float alpha = (presentation != null ? presentation.LightLevel : 1)\n"
             "                * (wallpaper.Reveal.Visible ? Mathf.Max(reveal, LessonPulse) : 0);")
replace_once("            DrawRegions(vh, area, scale);", "            DrawRegions(vh, area, scale);\n            DrawLessonHalo(vh, area, scale);")
replace_once("                var halo = cell.tint; halo.a = .09f;", "                var tint = LessonTint(cell);\n                var halo = tint; halo.a = .09f;")
replace_once("                XgDraw.Disc(vh, point, radius, cell.tint, 12);", "                XgDraw.Disc(vh, point, radius, tint, 12);")

(OUT / "XgWallpaperConcepts.candidate.cs").write_text(candidate, encoding="utf-8", newline="\n")
added = (OUT / "XgWallpaperConcepts.Lesson.cs").read_text(encoding="utf-8")
patch = "".join(difflib.unified_diff(original.splitlines(True), candidate.splitlines(True),
                                    fromfile="a/" + TARGET, tofile="b/" + TARGET))
patch += "".join(difflib.unified_diff([], added.splitlines(True), fromfile="/dev/null", tofile="b/" + ADDED))
(OUT / "changes.patch").write_text(patch, encoding="utf-8", newline="\n")
dependencies = [TARGET,
    "Assets/LingGuangV05/XingGuang/Desktop/XgWallpaperConcepts.Regions.cs",
    "Assets/LingGuangV05/XingGuang/Desktop/XgBrainWallpaper.cs",
    "Assets/LingGuangV05/XingGuang/Desktop/XgBrainWallpaperPresentation.cs",
    "Assets/LingGuangV05/XingGuang/Desktop/XingGuangController.cs",
    "Assets/LingGuangV05/XingGuang/Desktop/XgShapes.cs",
    "Assets/LingGuangV05/XingGuang/Core/XgSim.Teaching.cs",
    "Assets/LingGuangV05/XingGuang/Core/XgBoardData.cs",
    "Assets/LingGuangV05/Runtime/ChapterOneRuntime.cs",
    "Assets/LocalBuild/Editor/DesignFirstLessonChecks.cs",
    "Documentation/DesignPass/Agents/issues-task-001.md"]
hashes = {p: hashlib.sha256((ROOT / p).read_bytes()).hexdigest() for p in dependencies}
(OUT / "source-sha256.json").write_text(json.dumps(hashes, indent=2) + "\n", encoding="utf-8")
print("Isolated patch generated. Target SHA256: " + hashes[TARGET])
