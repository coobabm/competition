#!/bin/zsh
# Candidate quantization only: this repository does NOT contain the team's LoRA adapter,
# training data/configuration, merge pipeline, or pinned bf16/imatrix/tool revisions.
# It cannot recreate the shipped LoRA r2 weights from the public base model alone.
# For the exact release use the two Git LFS objects and verify Models/SHA256SUMS.txt.
# Supply the original LoRA-merged bf16 GGUF (with MTP), original imatrix, and matching
# llama.cpp toolchain explicitly; this script never downloads a base model as a substitute.
set -eu
: ${LORA_MERGED_BF16:?Original LoRA-merged bf16 GGUF required; not supplied in this repository}
: ${IMATRIX:?Original imatrix required; its source revision is not recorded}
: ${LLAMA:?Directory containing the compatible llama-quantize executable required}
WORK=${WORK:-Exports/model-work/rebuild-candidate}
mkdir -p "$WORK"
python3 Tools/LocalLLM/strip_qwen_mtp.py "$LORA_MERGED_BF16" "$WORK/merged-bf16-nomtp.gguf"
"$LLAMA/llama-quantize" --imatrix "$IMATRIX" --token-embedding-type q5_K \
  "$WORK/merged-bf16-nomtp.gguf" "$WORK/Qwen3.5-2B-Q5_K_S-emb5.gguf" Q5_K_S
# Leave candidate outside Assets even on success; never overwrite a verified release payload.
(cd "$WORK" && printf '%s  %s\n' \
  '4b154ece943d21ebdf8179935a335cd35897babd212cb144a86a8605bf1782b1' \
  'Qwen3.5-2B-Q5_K_S-emb5.gguf' | shasum -a 256 -c -)
