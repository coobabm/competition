namespace LingGuangV05.Runtime
{
    // Disclosure for the player; never part of character dialogue or model prompts.
    public static class AiContentNotice
    {
        public static string Title(bool english) => english ? "AI-generated content" : "AI 合成内容";
        public static string Short(bool english) => english
            ? "AI-generated replies · For reference only"
            : "AI 合成内容 · 生成回复仅供参考";
        public static string Description(bool english) => english
            ? "Includes a local AI model. Some conversations and answers are AI-generated and are for reference only."
            : "本游戏内置 AI 模型，部分对话和回答为 AI 合成内容，仅供参考。";
    }
}
