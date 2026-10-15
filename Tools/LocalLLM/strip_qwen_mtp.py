"""Copy a Qwen3.5 GGUF without its MTP (multi-token-prediction / nextn) block.

The game runs the model through LLM for Unity (LlamaLib v2.0.5, llama.cpp of 2026-03), which predates MTP support
and refuses newer GGUFs with "missing tensor 'blk.32.ssm_conv1d.weight'". MTP is only used for speculative decoding,
so dropping it changes nothing the game uses. Every other tensor and metadata value is copied byte for byte;
block_count drops by the MTP layers and nextn_predict_layers is removed.

    python3 -m venv /tmp/ggufenv && /tmp/ggufenv/bin/pip install gguf
    /tmp/ggufenv/bin/python Tools/LocalLLM/strip_qwen_mtp.py <with-mtp.gguf> Assets/StreamingAssets/LingGuang/Models/Qwen_Qwen3.5-4B-Q4_K_M.gguf
"""
import sys
import gguf

src, dst = sys.argv[1], sys.argv[2]
reader = gguf.GGUFReader(src)
arch = reader.fields['general.architecture'].contents()
mtp = int(reader.fields[f'{arch}.nextn_predict_layers'].contents())
blocks = int(reader.fields[f'{arch}.block_count'].contents())
keep_blocks = blocks - mtp
dropped = tuple(f'blk.{i}.' for i in range(keep_blocks, blocks))
writer = gguf.GGUFWriter(dst, arch=arch, endianess=reader.endianess)
alignment = reader.fields.get('general.alignment')
if alignment is not None:
    writer.data_alignment = int(alignment.contents())
for field in reader.fields.values():
    if field.name == gguf.Keys.General.ARCHITECTURE or field.name.startswith('GGUF.'):
        continue
    if field.name == f'{arch}.nextn_predict_layers':
        continue
    val_type = field.types[0]
    sub_type = field.types[-1] if val_type == gguf.GGUFValueType.ARRAY else None
    value = keep_blocks if field.name == f'{arch}.block_count' else field.contents()
    writer.add_key_value(field.name, value, val_type, sub_type=sub_type)
kept = [t for t in reader.tensors if not t.name.startswith(dropped)]
for t in kept:
    writer.add_tensor_info(t.name, t.data.shape, t.data.dtype, t.data.nbytes, t.tensor_type)
writer.write_header_to_file()
writer.write_kv_data_to_file()
writer.write_ti_data_to_file()
for t in kept:
    writer.write_tensor_data(t.data, tensor_endianess=reader.endianess)
writer.close()
print(f'{arch}: {blocks} -> {keep_blocks} blocks, {len(reader.tensors)} -> {len(kept)} tensors')
