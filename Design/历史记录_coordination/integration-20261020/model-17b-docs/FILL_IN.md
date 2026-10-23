# Fill-in checklist — what still needs the real file

Two values cannot be written from upstream metadata, because they must describe **the bytes you
actually ship**. Both are `<PENDING>` in this draft. Nothing else is unresolved.

> **Status update — both values are now measured, not unknown.** Late in this task the GGUF appeared
> on disk at `Assets/StreamingAssets/LingGuang/Models/Qwen3-1.7B-Q4_K_M.gguf` (1,107,409,472 bytes) —
> a parallel process was swapping it in. I hashed the file myself:
>
> ```
> b139949c5bd74937ad8ed8c8cf3d9ffb1e99c866c823204dc42c0d91fa181897  Qwen3-1.7B-Q4_K_M.gguf
> sha256sum, 4.6 s, 2026-10-20 04:40 +08:00
> ```
>
> That matches the size HF reports **and** the upstream `X-Linked-Etag`. The placeholder form is kept
> here as requested so the draft stays honest about provenance, but if you copy these files in, paste
> the three values below rather than the literal `<PENDING>` — the repo's own copy already carries the
> correct hash and a placeholder would regress it.

## 1. `MODEL_SOURCE.json` — `bytes` and `sha256`

```json
"bytes": "<PENDING>",
"sha256": "<PENDING>",
```

Resolved values: **`"bytes": 1107409472`** and
**`"sha256": "b139949c5bd74937ad8ed8c8cf3d9ffb1e99c866c823204dc42c0d91fa181897"`**.

**Type trap.** The 4B file has `"bytes": 2740937888` — a JSON *number*. The placeholder is a
*string*, so any consumer that does `(long)entry["bytes"]` will throw while the placeholder is in
place. Replace the whole token, quotes included:

```json
"bytes": 1107409472,
"sha256": "b139949c5bd74937ad8ed8c8cf3d9ffb1e99c866c823204dc42c0d91fa181897",
```

(`LocalModelRelease.Bytes` is a `long`; keep the JSON numeric so the two agree.)

Expected size: **1,107,409,472** bytes. That is what HF reports for `Qwen3-1.7B-Q4_K_M.gguf` at
revision `d7f544ee…`, and it is what the file on disk measures. If your copy differs, stop and find
out why before filling anything in.

## 2. `SHA256SUMS.txt`

```
<PENDING>  Qwen3-1.7B-Q4_K_M.gguf
```

Resolved line: `b139949c5bd74937ad8ed8c8cf3d9ffb1e99c866c823204dc42c0d91fa181897  Qwen3-1.7B-Q4_K_M.gguf`.
Two spaces between hash and filename (sha256sum format), file is CRLF, single trailing newline.

## 3. `THIRD_PARTY.txt`

`Recorded model SHA256: <PENDING>.` on the model bullet — same hash as above.

## 4. Code constants (yours, listed for completeness)

`Assets/LingGuangV05/LocalLLM/LocalModelRelease.cs`

```csharp
public const string Name = "Qwen3.5-4B";              // -> "Qwen3-1.7B"
public const string FileName = "Qwen3.5-4B-Q4_K_M.gguf"; // -> "Qwen3-1.7B-Q4_K_M.gguf"
public const long   Bytes = 2740937888L;              // -> 1107409472L
public const string Sha256 = "00fe7986…";             // -> real hash
```

`Bytes` / `Sha256` must match `MODEL_SOURCE.json` exactly; a release-side check
(`SerialNativeQa.cs`, and `IntegratedReleaseBuild.cs`'s canonical-GGUF assertion) compares them.

## How to produce the hash

```bash
# git-bash / macOS
sha256sum Qwen3-1.7B-Q4_K_M.gguf

# Windows cmd, if you prefer the OS tool
certutil -hashfile Qwen3-1.7B-Q4_K_M.gguf SHA256

# Hugging Face CLI (also verifies against the repo's own record)
hf download unsloth/Qwen3-1.7B-GGUF Qwen3-1.7B-Q4_K_M.gguf \
  --revision d7f544eead698dbd1f15126ef60b45a1e1933222
```

If you download through the CLI, its integrity check already pins the hash — copy that value
straight into the three places above.

## The Xet caveat, and why it no longer applies

HF's range response for this file carried
`X-Linked-Etag: b139949c5bd74937ad8ed8c8cf3d9ffb1e99c866c823204dc42c0d91fa181897`. This repo is on
HF's Xet storage, where the exposed `oid` / `xetHash` (`b79e3a4b…`, `de942b08…`) are *not* the file
hash, so the ETag could not be trusted on its own. It has now been confirmed by hashing the file on
disk — same 64 hex characters. Recorded here so the next person does not have to redo the doubt.
