using System.Runtime.Serialization;

namespace LLMAPI.Enums
{
    public enum ModelType
    {
        [EnumMember(Value = "openai/gpt-4o")]
        ChatGpt4o,

        [EnumMember(Value = "openai/gpt-4o-mini")]
        ChatGpt4oMini,

        [EnumMember(Value = "google/gemini-2.0-flash-001")]
        Gemini2_5Flash,

        [EnumMember(Value = "google/gemini-2.0-flash-lite-001")]
        Gemini2_5FlashLite,

        [EnumMember(Value = "anthropic/claude-3.5-sonnet")]
        Claude3_5Sonnet,

        [EnumMember(Value = "anthropic/claude-3-haiku")]
        Claude3Haiku,

        [EnumMember(Value = "meta-llama/llama-3.2-90b-vision-instruct")]
        Llama3_2_90bVisionInstruct,

        [EnumMember(Value = "meta-llama/llama-3.2-11b-vision-instruct")]
        Llama3_2_11bVisionInstruct,

        [EnumMember(Value = "deepseek/deepseek-r1:free")]
        DeepSeekR1,

        [EnumMember(Value = "mistralai/pixtral-large-2411")]
        MistralPixtralLarge,

        [EnumMember(Value = "mistralai/pixtral-12b")]
        MistralPixtral12b,

        [EnumMember(Value = "qwen/qwen-vl-max")]
        QwenVlMax,

        [EnumMember(Value = "qwen/qwen-2.5-vl-7b-instruct")]
        Qwen2_5Vl7bInstruct,

        [EnumMember(Value = "amazon/nova-lite-v1")]
        AmazonNovaLiteV1,

        [EnumMember(Value = "fireworks/firellava-13b")]
        FireworksFireLlava13b
    }

}

