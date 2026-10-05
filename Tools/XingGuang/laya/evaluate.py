"""Local-only, batch-size-one Laya evaluation. No gold labels enter model requests."""
import argparse
from collections import Counter
from datetime import datetime, timezone
import hashlib
import importlib.metadata
import json
import math
from pathlib import Path
import platform
import resource
import statistics
import time

MODEL_ID = "convaiinnovations/laya-multilingual"
REVISION = "e4e9ddf21a7b1903b7acffd8814ad4307bf63a67"
DIGESTS = {
    "model.safetensors": "9d628fd971b700382ac6f65920a86f149777b2e748e0c955fb3b19695aa8f204",
    "tokenizer/tokenizer.json": "609d8f4c067cd3950f88594c5a802616cea245823836ef5848ee4fc40aab5b6f",
}
CATEGORIES = ("算术", "数的性质", "比较", "数列", "条件推理", "三段论", "真假运算", "日历", "真话假话")


def validate_probability(value):
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        raise ValueError("P(yes) must be numeric, not a label or missing value")
    if not math.isfinite(value) or not 0 <= value <= 1:
        raise ValueError("P(yes) must be finite in [0, 1]")
    return float(value)


def make_request(row, language, variant):
    """Explicit allowlist: never copy the dataset row into inference state."""
    if language not in ("zh", "en") or variant not in ("primary", "direct"):
        raise ValueError("Unknown language or prompt variant")
    text = row["question" if language == "zh" else "question_en"]
    zh = language == "zh"
    instruction = ("题目所问的答案是‘是’吗？根据数学计算或逻辑推理作答，不假设额外条件。" if zh else
                   "Is the answer to the given question yes? Use mathematics or logical reasoning, without extra assumptions.")
    criteria = ({"true": "是，题目所问的结论成立。", "false": "否，题目所问的结论不成立或不能确定。"} if zh else
                {"true": "Yes, the conclusion in the question holds.",
                 "false": "No, the conclusion does not hold or cannot be established."})
    state = {"question": text}
    if variant == "direct":
        state = "用数学和逻辑回答是非题。" if zh else "Answer the yes/no question using mathematics and logic."
        instruction = text
    return state, {"answer": {"type": "noul", "instructions": instruction, "criteria": criteria}}


def wilson(correct, n):
    if not n:
        return None
    z = 1.959963984540054
    p, d = correct / n, 1 + z * z / n
    mid = (p + z * z / (2 * n)) / d
    half = z * math.sqrt(p * (1 - p) / n + z * z / (4 * n * n)) / d
    return [mid - half, mid + half]


def metrics(rows):
    n, correct, errors, brier = len(rows), 0, 0, 0.0
    confusion = dict(tp=0, tn=0, fp=0, fn=0)
    bins = [[] for _ in range(10)]
    confidences, latency, high = [], [], []
    for row in rows:
        if row.get("error"):
            errors += 1
            continue
        p = validate_probability(row["p_yes"])
        predicted = p >= .5
        ok = predicted == row["truth"]
        confidence = p if predicted else 1 - p
        correct += int(ok)
        confusion[("t" if ok else "f") + ("p" if predicted else "n")] += 1
        brier += (p - int(row["truth"])) ** 2
        confidences.append(confidence)
        bins[min(9, int(confidence * 10))].append((confidence, int(ok)))
        if confidence >= .8:
            high.append(ok)
        latency.append(row["elapsed_ms"])
    valid = n - errors
    ece = sum(abs(sum(c for c, _ in b) - sum(a for _, a in b)) for b in bins) / valid if valid else None
    latency.sort()
    yes = sum(r["truth"] for r in rows)
    return dict(n=n, valid=valid, errors=errors, correct=correct,
                accuracy=correct / n if n else None, wilson95=wilson(correct, n),
                yes_count=yes, majority_baseline=max(yes, n - yes) / n if n else None,
                predicted_yes=confusion["tp"] + confusion["fp"], confusion=confusion,
                brier=brier / valid if valid else None, ece10=ece,
                mean_confidence=statistics.mean(confidences) if valid else None,
                high_confidence_n=len(high), high_confidence_errors=len(high) - sum(high),
                high_confidence_accuracy=sum(high) / len(high) if high else None,
                truncated=sum(bool(r.get("truncated")) for r in rows),
                latency_median_ms=statistics.median(latency) if valid else None,
                latency_p95_ms=latency[math.ceil(.95 * len(latency)) - 1] if valid else None)


def acceptance_gate(rows, expected_primary_zh):
    primary = [r for r in rows if r["language"] == "zh" and r["variant"] == "primary"]
    reasons = []
    if not primary or len(primary) != expected_primary_zh:
        reasons.append("Missing or incomplete Chinese primary evaluation")
    if len({r.get("id") for r in primary}) != len(primary):
        reasons.append("Missing or duplicate prediction IDs")
    if any(r.get("error") or r.get("truncated") for r in primary):
        reasons.append("Inference errors or truncated input")
    for suite in ("balanced", "natural"):
        part = [r for r in primary if r["suite"] == suite]
        if not part or metrics(part)["accuracy"] < .75:
            reasons.append(suite + " Chinese accuracy below 75%")
    for category in CATEGORIES:
        part = [r for r in primary if r["suite"] == "balanced" and r["category"] == category]
        if (len(part) != 20 or Counter(r["truth"] for r in part) != {True: 10, False: 10}
                or len({r.get("question") for r in part}) != 20):
            reasons.append(category + ": expected 20 unique questions, 10 yes and 10 no")
        if not part or metrics(part)["accuracy"] < .60:
            reasons.append(category + ": accuracy below 60%")
    return dict(passed=not reasons, reasons=reasons, overall_threshold=.75, category_threshold=.60)


def sha256(path):
    with Path(path).open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def read_dataset(path):
    rows = [json.loads(line) for line in Path(path).read_text(encoding="utf-8").splitlines() if line.strip()]
    ids = set()
    for row in rows:
        if row["id"] in ids:
            raise ValueError("Duplicate row ID: " + row["id"])
        ids.add(row["id"])
        if type(row["truth"]) is not bool or row["category"] not in CATEGORIES:
            raise ValueError("Invalid ground truth or category")
        if row["suite"] not in ("balanced", "natural") or not 1 <= row["level"] <= 5:
            raise ValueError("Invalid suite or level")
        if not row["question"].strip() or not row["question_en"].strip():
            raise ValueError("Empty question")
    if not rows:
        raise ValueError("Empty corpus")
    balanced = [r for r in rows if r["suite"] == "balanced"]
    natural = [r for r in rows if r["suite"] == "natural"]
    expected_buckets = {(c, truth): 10 for c in CATEGORIES for truth in (True, False)}
    if Counter((r["category"], r["truth"]) for r in balanced) != expected_buckets:
        raise ValueError("Balanced suite requires exactly 10 yes and 10 no in each of nine categories")
    if len({r["question"] for r in balanced}) != 180:
        raise ValueError("Balanced suite must contain 180 distinct questions")
    if Counter(r["level"] for r in natural) != {level: 100 for level in range(1, 6)}:
        raise ValueError("Natural suite requires exactly 100 draws per level")
    return rows


def grouped_metrics(rows, field):
    return {str(key): metrics([r for r in rows if r[field] == key]) for key in sorted({r[field] for r in rows})}


def run(args):
    import torch
    from laya import Agent

    dataset = read_dataset(args.dataset)
    # New directory: never mix a rerun with an old report or accidentally overwrite it.
    args.output.mkdir(parents=True, exist_ok=False)
    torch.set_num_threads(args.threads)
    torch.manual_seed(20261003)
    started = time.perf_counter()
    print("Loading pinned multilingual checkpoint; verifying SHA-256...", flush=True)
    agent = Agent(MODEL_ID, device=args.device, revision=REVISION, expected_sha256=DIGESTS)
    if str(agent.device) != args.device:
        raise RuntimeError(f"Requested {args.device}, got {agent.device}; refuse misleading device metadata")
    load_s = time.perf_counter() - started
    source = Path(__file__).resolve().parents[3] / "Assets/LingGuangV05/XingGuang/Core/XgLogic.cs"
    jobs = [(row, lang, variant) for variant in ("primary", "direct") for lang in ("zh", "en")
            for row in dataset if variant == "primary" or row["suite"] == "balanced"]
    metadata = dict(model=MODEL_ID, revision=REVISION, expected_sha256=DIGESTS,
                    versions={p: importlib.metadata.version(p) for p in
                              ("laya", "torch", "transformers", "tokenizers", "huggingface-hub")},
                    platform=platform.platform(), machine=platform.machine(), device=str(agent.device),
                    torch_threads=args.threads, batch_size=1, started_utc=datetime.now(timezone.utc).isoformat(),
                    model_load_seconds=load_s, corpus_sha256=sha256(args.dataset), generator_sha256=sha256(source),
                    evaluator_sha256=sha256(__file__), corpus_rows=len(dataset), expected_predictions=len(jobs),
                    suite_sizes=dict(Counter(r["suite"] for r in dataset)),
                    unique_questions={s: len({r["question"] for r in dataset if r["suite"] == s})
                                      for s in ("balanced", "natural")},
                    gate_policy="Chinese primary both suites >=0.75; balanced each category >=0.60, n>=20; no errors/truncation",
                    prompt_policy="Both prompts fixed before inference; direct is diagnostic only; no tuning or training",
                    probability_note="Uncalibrated on game data; confidence is NOT empirical accuracy")
    # Preserve the exact evaluator used; later code edits cannot invalidate provenance.
    (args.output / "evaluate.snapshot.py").write_bytes(Path(__file__).read_bytes())
    (args.output / "metadata.json").write_text(json.dumps(metadata, ensure_ascii=False, indent=2), encoding="utf-8")

    def infer(row, language, variant):
        state, questions = make_request(row, language, variant)
        t = time.perf_counter()
        result = agent.predict(state, questions, lang=language)
        if args.device == "mps":
            torch.mps.synchronize()
        elapsed = (time.perf_counter() - t) * 1000
        p = validate_probability(result["answers"]["answer"]["noul"])
        usage = result.get("usage", {})
        return p, elapsed, usage

    # Explicit warmup, excluded from steady-state latency; never fit on these questions.
    warmups = []
    for language in ("zh", "en"):
        p, elapsed, usage = infer(dataset[0], language, "primary")
        warmups.append(dict(language=language, elapsed_ms=elapsed, p_yes=p, usage=usage))
    (args.output / "warmup.json").write_text(json.dumps(warmups, ensure_ascii=False, indent=2), encoding="utf-8")
    rows = []
    failures = 0
    with (args.output / "predictions.jsonl").open("x", encoding="utf-8") as stream:
        for index, (row, language, variant) in enumerate(jobs):
            item = dict(row, language=language, variant=variant, error=None, p_yes=None,
                        elapsed_ms=None, truncated=False)
            try:
                p, elapsed, usage = infer(row, language, variant)
                item.update(p_yes=p, prediction=p >= .5, confidence=max(p, 1-p),
                            correct=(p >= .5) == row["truth"], elapsed_ms=elapsed,
                            truncated=bool(usage.get("truncated", False)), usage=usage)
            except Exception as exc:
                item["error"] = f"{type(exc).__name__}: {exc}"
                failures += 1
            rows.append(item)
            stream.write(json.dumps(item, ensure_ascii=False, allow_nan=False) + "\n")
            stream.flush()
            if (index + 1) % 50 == 0:
                print(f"{index + 1}/{len(jobs)} predictions; errors={failures}; last={item['elapsed_ms']} ms", flush=True)
            if failures >= 10:
                print("Stopping after 10 errors; output will be marked incomplete", flush=True)
                break
    summary = dict(metadata=metadata, complete=len(rows) == len(jobs), actual_predictions=len(rows),
                   gate=acceptance_gate(rows, len(dataset)), results={})
    for variant in ("primary", "direct"):
        for lang in ("zh", "en"):
            for suite in ("balanced", "natural"):
                part = [r for r in rows if r["variant"] == variant and r["language"] == lang and r["suite"] == suite]
                if part:
                    summary["results"][f"{variant}/{lang}/{suite}"] = dict(
                        overall=metrics(part), by_category=grouped_metrics(part, "category"), by_level=grouped_metrics(part, "level"))
    summary["peak_process_rss_bytes"] = resource.getrusage(resource.RUSAGE_SELF).ru_maxrss * (1 if platform.system() == "Darwin" else 1024)
    summary["total_seconds"] = time.perf_counter() - started
    (args.output / "summary.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2, allow_nan=False), encoding="utf-8")
    print(json.dumps({k: v["overall"]["accuracy"] for k, v in summary["results"].items()}, ensure_ascii=False, indent=2))
    print("GATE:", json.dumps(summary["gate"], ensure_ascii=False), flush=True)
    return 0 if summary["complete"] and not failures else 2


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dataset", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True, help="A new run directory")
    parser.add_argument("--device", choices=("cpu", "mps"), default="cpu")
    parser.add_argument("--threads", type=int, default=4)
    args = parser.parse_args()
    if not 1 <= args.threads <= 32:
        parser.error("--threads must be between 1 and 32")
    raise SystemExit(run(args))
