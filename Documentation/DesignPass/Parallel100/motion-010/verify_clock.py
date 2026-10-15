"""Source-bound static/model checks only. Does not run Unity or touch production.
Run: python verify_clock.py [--write-patch]
The optional flag writes only the candidate patch beside this script.
"""
from pathlib import Path
import difflib
import hashlib
import json
import struct
import sys

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
BUTTON = "Assets/LingGuangV05/XingGuang/Desktop/XgButtonPressFeedback.cs"
JUICE = "Assets/LingGuangV05/XingGuang/Desktop/XgJuice.cs"
HASHES = {
    BUTTON: "1F284BD333A1049AA0A8463BF9B3A3913D6C359AF0FEAA22794A66BCFAAABE78",
    JUICE: "2ABCA8BD6A93D03CF679A786F1A8B6EB2B50A2505A68CE2AA5F890844CF567EC",
    "Assets/LingGuangV05/XingGuang/Desktop/XgUi.cs": "A81D4AA3591C28EFA023FB2CAB4DCC976CC581A1B132CFE910FD181CEDC50F14",
    "Assets/LocalBuild/Editor/MotionButtonChecks.cs": "22E10F5BD0C8DC49168D401F727214E47B9782B7B26C6D801C48FB3D8D59530E",
    "Documentation/DesignPass/Agents/motion-task-001.md": "755DBC9C34002157AE71792FA8484A02F0776311C36CF586518DC97D9C99B9EE",
}


def replace_once(source, old, new):
    assert source.count(old) == 1, old
    return source.replace(old, new, 1)


def f32(x):
    return struct.unpack("f", struct.pack("f", x))[0]


def simulate(life, deltas, skip_first_update, old_timer, time_scale):
    # Model only the observed Update/LateUpdate ordering and capped unscaled clock.
    # time_scale intentionally does not multiply unscaled frame deltas.
    assert time_scale >= 0
    age = elapsed = f32(0)
    life = f32(life)
    pending = True
    for frame in range(512):
        dt = min(f32(deltas[frame % len(deltas)]), f32(.05))
        if not (frame == 0 and skip_first_update):
            age = f32(age + dt)
            if f32(age / life) >= 1:
                pending = False  # Production restores both transforms before removal.
        elapsed = f32(elapsed + dt)
        cleanup = elapsed >= life if old_timer else not pending
        if cleanup:
            return {"frames": frame + 1, "engine_age": age,
                    "premature": pending}
    raise AssertionError("Positive frame deltas did not complete")


def main():
    sources = {}
    for path, expected in HASHES.items():
        data = (ROOT / path).read_bytes()
        actual = hashlib.sha256(data).hexdigest().upper()
        assert actual == expected, f"Dependency changed; re-review before using patch: {path}: {actual}"
        sources[path] = data.decode("utf-8-sig").replace("\r\n", "\n")

    b, j = sources[BUTTON], sources[JUICE]
    assert "float raw = Time.unscaledDeltaTime;" in j
    assert "float dt = Mathf.Min(raw, .05f);" in j
    assert "if (k >= 1) { p.rt.localScale = p.scale; p.rt.anchoredPosition = p.pos; done.Add(p.rt); continue; }" in j
    assert "foreach (var rt in done) punches.Remove(rt);" in j
    assert "pressFx.Clear();\n            pressFx.enabled = false;" in b

    nb = replace_once(b, "        float elapsed, duration;\n", "")
    nb = replace_once(nb, "            duration = reduced ? .12f : .18f;\n            elapsed = 0;", "            float duration = reduced ? .12f : .18f;")
    nb = replace_once(nb, "            elapsed += Mathf.Min(Time.unscaledDeltaTime, .05f);\n", "")
    nb = replace_once(nb, "Label == null || elapsed >= duration ||", "Label == null || !pressFx.HasActiveKnock(Label) ||")
    nj = replace_once(j, "        public void Float(Vector2 at, string text, Color color, float size = 22, float rise = 60, float life = .75f, float scale = 1.3f)",
        "        /// <summary>True until Knock restores its target or is explicitly cleared.</summary>\n"
        "        public bool HasActiveKnock(RectTransform target) => target != null && punches.ContainsKey(target);\n\n"
        "        public void Float(Vector2 at, string text, Color color, float size = 22, float rise = 60, float life = .75f, float scale = 1.3f)")
    patch = "# motion-010 candidate only; source SHA256 preconditions:\n"
    patch += "".join(f"# {p}: {HASHES[p]}\n" for p in (BUTTON, JUICE))
    for path, old, new in ((BUTTON, b, nb), (JUICE, j, nj)):
        patch += "".join(difflib.unified_diff(old.splitlines(True), new.splitlines(True),
                                            fromfile="a/" + path, tofile="b/" + path))
    if "--write-patch" in sys.argv:
        (HERE / "changes.patch").write_text(patch, encoding="utf-8", newline="\n")
    assert (HERE / "changes.patch").read_text(encoding="utf-8") == patch

    profiles = {"60fps": [1/60], "20fps": [.05], "10fps": [.1],
                "2fps": [.5], "long_gap": [1/60, 2, 1/60],
                "zero_then_progress": [0, 0, .05], "mixed": [.001, .049, .3, .016]}
    cases = 0
    for life in (.18, .12):
        for deltas in profiles.values():
            for skip in (False, True):
                reference = None
                for scale in (0, .1, 1):
                    result = simulate(life, deltas, skip, False, scale)
                    assert not result["premature"]
                    assert result["engine_age"] >= f32(life)
                    if reference is not None:
                        assert result == reference
                    reference = result
                    cases += 1

    # The failure depends on legitimate callback order, not a different delta formula.
    old_normal = simulate(.18, [.1], True, True, 0)
    old_reduced = simulate(.12, [.1], True, True, 0)
    assert old_normal["premature"] and old_normal["frames"] == 4
    assert old_reduced["premature"] and old_reduced["frames"] == 3
    assert simulate(.18, [.1], True, False, 0)["frames"] == 5
    assert simulate(.12, [.1], True, False, 0)["frames"] == 4
    # Already updated on the trigger frame must not add an arbitrary extra frame.
    assert simulate(.18, [.1], False, False, 0)["frames"] == 4
    assert simulate(.12, [.1], False, False, 0)["frames"] == 3
    print(json.dumps({"kind": "static-source-and-clock-model-only", "source_hashes_match": True,
                      "candidate_model_cases": cases, "old_normal_counterexample": old_normal,
                      "old_reduced_counterexample": old_reduced,
                      "unity_run": False, "visual_acceptance": False}, indent=2))


if __name__ == "__main__":
    main()
