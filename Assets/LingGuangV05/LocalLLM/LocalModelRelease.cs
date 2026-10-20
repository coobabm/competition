namespace LingGuangV05.Desktop.LLM
{
    // The release payload is pinned to the exact bytes of the shipped GGUF: Qwen3.5-2B with the game's own LoRA
    // (晴雯 / 老周 / 灵光, Exports/model-work/lora) merged in, MTP stripped, Q5_K_S with a Q5_K token embedding.
    public static class LocalModelRelease
    {
        public const string Name = "Qwen3.5-2B";
        public const string FileName = "Qwen3.5-2B-Q5_K_S-emb5.gguf";
        public const long Bytes = 1306533984L;
        public const string Sha256 = "4b154ece943d21ebdf8179935a335cd35897babd212cb144a86a8605bf1782b1";
    }
}
