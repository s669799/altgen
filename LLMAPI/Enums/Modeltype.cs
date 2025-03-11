using System.Runtime.Serialization;

namespace LLMAPI.Enums
{
    public enum ModelType
    {
        [EnumMember(Value = "openai/gpt-4o")]
        ChatGPT_4o,

        [EnumMember(Value = "openai/gpt-4o-mini")]
        ChatGPT_4o_Mini,

        [EnumMember(Value = "google/gemini-2.0-flash-001")]
        Gemini_25_Flash,

        [EnumMember(Value = "google/gemini-2.0-flash-lite-001")]
        Gemini_25_Flash_Lite,

        [EnumMember(Value = "anthropic/claude-3.5-sonnet")]
        Claude_35_Sonnet,

        [EnumMember(Value = "anthropic/claude-3-haiku")]
        Claude_3_Haiku,

        [EnumMember(Value = "meta-llama/llama-3.2-90b-vision-instruct")]
        Llama_32_90b_Vision_Instruct,

        [EnumMember(Value = "meta-llama/llama-3.2-11b-vision-instruct")]
        Llama_32_11b_Vision_Instruct,

        [EnumMember(Value = "deepseek/deepseek-r1:free")]
        DeepSeek_R1,

        [EnumMember(Value = "mistralai/pixtral-large-2411")]
        Mistral_Pixtral_Large,

        [EnumMember(Value = "mistralai/pixtral-12b")]
        Mistral_Pixtral_12b,

        [EnumMember(Value = "qwen/qwen-vl-max")]
        Qwen_VL_Max,

        [EnumMember(Value = "qwen/qwen-2.5-vl-7b-instruct")]
        Qwen_VL_7B_Instruct,

        [EnumMember(Value = "amazon/nova-lite-v1")]
        Amazon_Nova_Lite_V1,

        [EnumMember(Value = "fireworks/firellava-13b")]
        Fireworks_FireLlava_13B
    }
}
