"""No model download needed: python -m unittest discover -s Tools/XingGuang/laya -v."""
import collections
import importlib.util
import json
from pathlib import Path
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parent


class EvaluationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        spec = importlib.util.spec_from_file_location("evaluate", ROOT / "evaluate.py")
        cls.ev = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(cls.ev)

    def row(self, truth, probability, **kwargs):
        return dict(truth=truth, p_yes=probability, elapsed_ms=10, error=None,
                    truncated=False, category="算术", level=1, **kwargs)

    def test_metrics_known_probabilities(self):
        rows = [self.row(True, .8), self.row(False, .1),
                self.row(True, .4), self.row(False, .9)]
        result = self.ev.metrics(rows)
        self.assertEqual(result["correct"], 2)
        self.assertEqual(result["accuracy"], .5)
        self.assertAlmostEqual(result["brier"], .305)
        self.assertEqual(result["confusion"], dict(tp=1, tn=1, fp=1, fn=1))
        self.assertEqual(result["majority_baseline"], .5)
        self.assertLess(result["wilson95"][0], .5)
        self.assertGreater(result["wilson95"][1], .5)

    def test_error_is_not_dropped_from_denominator(self):
        bad = self.row(True, None)
        bad["error"] = "inference failed"
        result = self.ev.metrics([self.row(False, .1), bad])
        self.assertEqual(result["accuracy"], .5)
        self.assertEqual(result["errors"], 1)
        self.assertEqual(result["n"], 2)

    def test_rejects_invalid_probability(self):
        for p in [float("nan"), float("inf"), -.01, 1.01, None, True, "0.9"]:
            with self.subTest(p=p), self.assertRaises(ValueError):
                self.ev.validate_probability(p)
        self.assertEqual(self.ev.validate_probability(0), 0)
        self.assertEqual(self.ev.validate_probability(1), 1)

    def test_confidence_for_no_and_ece(self):
        r = self.ev.metrics([self.row(False, .1)] * 10)
        self.assertAlmostEqual(r["mean_confidence"], .9)
        self.assertAlmostEqual(r["ece10"], .1)
        self.assertAlmostEqual(r["brier"], .01)

    def test_prompt_never_leaks_labels(self):
        row = dict(question="7 + 8 = 15 吗？", question_en="Is 7 + 8 = 15?",
                   truth=True, why="SECRET_ANSWER", category="SECRET_CATEGORY", seed=123)
        for language in ["zh", "en"]:
            for variant in ["primary", "direct"]:
                state, questions = self.ev.make_request(row, language, variant)
                rendered = json.dumps([state, questions], ensure_ascii=False)
                self.assertNotIn("SECRET", rendered)
                self.assertNotIn("123", rendered)
                self.assertNotIn("truth", rendered)
                self.assertIn(row["question" if language == "zh" else "question_en"], rendered)

    def test_gate_requires_all_categories_and_complete_runs(self):
        rows = []
        for cat in self.ev.CATEGORIES:
            for suite in ["balanced", "natural"]:
                for i in range(20):
                    r = self.row(i % 2 == 0, .9 if i % 2 == 0 else .1,
                                 suite=suite, language="zh", variant="primary",
                                 id=f"{suite}-{cat}-{i}", question=f"{cat}-{i}")
                    r["category"] = cat
                    rows.append(r)
        self.assertTrue(self.ev.acceptance_gate(rows, len(rows))["passed"])
        self.assertFalse(self.ev.acceptance_gate(rows, len(rows) + 1)["passed"])
        self.assertFalse(self.ev.acceptance_gate(rows[:-1], len(rows))["passed"])
        # A weak category is not hidden by a strong aggregate.
        for r in rows:
            if r["category"] == self.ev.CATEGORIES[0] and r["suite"] == "balanced":
                r["p_yes"] = 1 - r["p_yes"]
        self.assertFalse(self.ev.acceptance_gate(rows, len(rows))["passed"])

    def test_biased_or_duplicate_balanced_set_cannot_pass(self):
        rows = [self.row(True, .99, suite=suite, language="zh", variant="primary",
                         id=f"{suite}-{c}-{i}", question="the same question")
                for suite in ("balanced", "natural") for c in self.ev.CATEGORIES for i in range(20)]
        for row, c in zip(rows, [c for _ in range(2) for c in self.ev.CATEGORIES for _ in range(20)]):
            row["category"] = c
        self.assertFalse(self.ev.acceptance_gate(rows, len(rows))["passed"])
        for i, row in enumerate(rows):
            row["truth"] = i % 2 == 0
            row["p_yes"] = .99 if row["truth"] else .01
        self.assertFalse(self.ev.acceptance_gate(rows, len(rows))["passed"], "Duplicates remain invalid")

    def test_read_dataset_rejects_mislabelled_balanced_corpus(self):
        rows = []
        for cat in self.ev.CATEGORIES:
            for i in range(20):
                rows.append(dict(id=f"balanced-{cat}-{i}", suite="balanced", category=cat,
                                 question=f"{cat}-{i}", question_en=f"{cat}-{i}", truth=True, level=1))
        for i in range(500):
            rows.append(dict(id=f"natural-{i}", suite="natural", category="算术", question=str(i),
                             question_en=str(i), truth=True, level=1 + i % 5))
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / "invalid.jsonl"
            path.write_text("\n".join(json.dumps(r) for r in rows), encoding="utf-8")
            with self.assertRaises(ValueError):
                self.ev.read_dataset(path)

    def test_empty_and_truncated_results_cannot_pass(self):
        self.assertFalse(self.ev.acceptance_gate([], 0)["passed"])
        row = self.row(True, .9, suite="balanced", language="zh", variant="primary")
        row["truncated"] = True
        self.assertFalse(self.ev.acceptance_gate([row], 1)["passed"])


class ExportTests(unittest.TestCase):
    def test_export_reuses_game_generator_and_is_deterministic(self):
        self.assertTrue((ROOT / "Export.csproj").is_file(), "Missing real C# exporter")
        subprocess.run(["dotnet", "build", str(ROOT / "Export.csproj"), "-v", "quiet"], check=True,
                       capture_output=True, text=True, timeout=120)
        dll = ROOT / "bin/Debug/net8.0/Export.dll"
        with tempfile.TemporaryDirectory() as tmp:
            a, b = Path(tmp) / "a.jsonl", Path(tmp) / "b.jsonl"
            for dest in [a, b]:
                subprocess.run(["dotnet", str(dll), str(dest)], check=True,
                               capture_output=True, text=True, timeout=60)
            self.assertEqual(a.read_bytes(), b.read_bytes())
            rows = [json.loads(line) for line in a.read_text().splitlines()]
        self.assertEqual(len(rows), 680)
        self.assertEqual(len({r["id"] for r in rows}), len(rows))
        balanced = [r for r in rows if r["suite"] == "balanced"]
        self.assertEqual(len({r["question"] for r in balanced}), 180)
        counts = collections.Counter((r["category"], r["truth"]) for r in balanced)
        self.assertEqual(len(counts), 18)
        self.assertEqual(set(counts.values()), {10})
        self.assertEqual(collections.Counter(r["level"] for r in rows if r["suite"] == "natural"),
                         {i: 100 for i in range(1, 6)})
        for r in rows:
            self.assertIsInstance(r["truth"], bool)
            self.assertTrue(r["question"] and r["question_en"])


if __name__ == "__main__":
    unittest.main()
