#!/bin/zsh
# Rebuilds the shipped chat model (see Assets/StreamingAssets/LingGuang/Models/MODEL_CARD.md).
# Needs a llama.cpp built from source (Homebrew's 2026-10 build asserts on Qwen3.5: GGML_ASSERT(state->ne[0] == S_v)).
#   LLAMA=~/llama.cpp/build/bin WORK=Exports/model-work Tools/LocalLLM/quantize_qwen35_2b.sh
set -e
WORK=${WORK:-Exports/model-work}; LLAMA=${LLAMA:-$WORK/src-llama/build/bin}
B=https://huggingface.co/bartowski/Qwen_Qwen3.5-2B-GGUF/resolve/main
mkdir -p $WORK
[ -f $WORK/Qwen3.5-2B-bf16.gguf ] || curl -L -o $WORK/Qwen3.5-2B-bf16.gguf $B/Qwen_Qwen3.5-2B-bf16.gguf
[ -f $WORK/imatrix.gguf ] || curl -L -o $WORK/imatrix.gguf $B/Qwen_Qwen3.5-2B-imatrix.gguf
python3 Tools/LocalLLM/strip_qwen_mtp.py $WORK/Qwen3.5-2B-bf16.gguf $WORK/Qwen3.5-2B-bf16-nomtp.gguf
$LLAMA/llama-quantize --imatrix $WORK/imatrix.gguf --token-embedding-type q5_K $WORK/Qwen3.5-2B-bf16-nomtp.gguf \
  Assets/StreamingAssets/LingGuang/Models/Qwen3.5-2B-Q5_K_S-emb5.gguf Q5_K_S
curl -L -o Assets/StreamingAssets/LingGuang/Embedding/bge-small-zh-v1.5-q8_0.gguf \
  https://huggingface.co/CompendiumLabs/bge-small-zh-v1.5-gguf/resolve/main/bge-small-zh-v1.5-q8_0.gguf
(cd Assets/StreamingAssets/LingGuang/Models && shasum -a 256 -c SHA256SUMS.txt)
