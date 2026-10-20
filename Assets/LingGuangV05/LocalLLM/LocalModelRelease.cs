namespace LingGuangV05.Desktop.LLM
{
    // The release payload is pinned to the exact bytes of the shipped GGUF: Qwen3.5-2B with the game's own LoRA
    // (晴雯 / 老周 / 灵光, Exports/model-work/lora) merged in, MTP stripped, Q5_K_S with a Q5_K token embedding.
    public static class LocalModelRelease
    {
        public const string EmbeddingFileName = "bge-small-zh-v1.5-q8_0.gguf";
        public const long EmbeddingBytes = 26472640L;
        public const string EmbeddingSha256 = "5a88d266870fbd27c6f329df60de80e2d4cf3bbd5e6f080bd5c1b2e5abb12039";
        public const int EmbeddingDimensions = 512;
        public const string Name = "Qwen3.5-2B";
        public const string FileName = "Qwen3.5-2B-Q5_K_S-emb5.gguf";
        public const long Bytes = 1306533984L;
        public const string Sha256 = "4b154ece943d21ebdf8179935a335cd35897babd212cb144a86a8605bf1782b1";
    }
}
