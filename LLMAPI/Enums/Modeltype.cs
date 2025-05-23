﻿using System.Runtime.Serialization;

namespace LLMAPI.Enums
{
    /// <summary>
    /// Enumerates the types of Language Learning Models (LLMs) supported by the API.
    /// Each enum member is associated with an OpenRouter model identifier via the <see cref="EnumMemberAttribute"/>.
    /// </summary>
    public enum ModelType
    {
        /// <summary>
        /// OpenAI's GPT-4.1 model.
        /// </summary>
        [EnumMember(Value = "openai/gpt-4.1")]
        ChatGpt4_1,

        /// <summary>
        /// OpenAI's GPT-4o-mini model.
        /// </summary>
        [EnumMember(Value = "openai/gpt-4.1-mini")]
        ChatGpt4_1Mini,

        /// <summary>
        /// OpenAI's GPT-4o-mini model.
        /// </summary>
        [EnumMember(Value = "openai/gpt-4.1-nano")]
        ChatGpt4_1Nano,

        /// <summary>
        /// Google's Gemini 2.0 Flash model.
        /// </summary>
        [EnumMember(Value = "google/gemini-2.0-flash-001")]
        Gemini2_0Flash,

        /// <summary>
        /// Google's Gemini 2.0 Flash Lite model.
        /// </summary>
        [EnumMember(Value = "google/gemini-2.0-flash-lite-001")]
        Gemini2_0FlashLite,

        /// <summary>
        /// Google's Gemini 2.0 Flash Lite model.
        /// </summary>
        [EnumMember(Value = "google/gemma-3-4b-it")]
        Gemma3_4B,

        /// <summary>
        /// Google's Gemini 2.5 Flash Preview) model.
        /// </summary>
        [EnumMember(Value = "google/gemini-2.5-flash-preview")]
        Gemini2_5FlashPreview,

        /// <summary>
        /// Anthropic's Claude 3 Opus model.
        /// </summary>
        [EnumMember(Value = "anthropic/claude-3-opus")]
        Claude3Opus,

        /// <summary>
        /// Anthropic's Claude 3.7 Sonnet model.
        /// </summary>
        [EnumMember(Value = "anthropic/claude-3.7-sonnet")]
        Claude3_7Sonnet,

        /// <summary>
        /// Anthropic's Claude 3.5 Haiku model.
        /// </summary>
        [EnumMember(Value = "anthropic/claude-3.5-haiku")]
        Claude3_5Haiku,

        /// <summary>
        /// Meta Llama 3.2 90B Vision Instruct model.
        /// </summary>
        [EnumMember(Value = "meta-llama/llama-3.2-90b-vision-instruct")]
        Llama3_2_90bVisionInstruct,

        /// <summary>
        /// Meta Llama 3.2 11B Vision Instruct model.
        /// </summary>
        [EnumMember(Value = "meta-llama/llama-3.2-11b-vision-instruct")]
        Llama3_2_11bVisionInstruct,

        ///// <summary>
        ///// DeepSeek R1 Free model.
        ///// </summary>
        //[EnumMember(Value = "deepseek/deepseek-r1:free")]
        //DeepSeekR1,

        /// <summary>
        /// Mistral Pixtral Large 2411 model.
        /// </summary>
        [EnumMember(Value = "mistralai/pixtral-large-2411")]
        MistralPixtralLarge,

        /// <summary>
        /// Mistral Pixtral 24B model.
        /// </summary>
        [EnumMember(Value = "mistralai/mistral-small-3.1-24b-instruct")]
        MistralPixtral24b,

        /// <summary>
        /// Mistral Pixtral 12B model.
        /// </summary>
        [EnumMember(Value = "mistralai/pixtral-12b")]
        MistralPixtral12b,

        /// <summary>
        /// Qwen 2.5 VL 72B Instruct Free model.
        /// </summary>
        [EnumMember(Value = "qwen/qwen2.5-vl-72b-instruct:free")]
        Qwen2_5Vl72bInstruct,

        /// <summary>
        /// Qwen 2.5 VL 7B Instruct model.
        /// </summary>
        [EnumMember(Value = "qwen/qwen-2.5-vl-7b-instruct")]
        Qwen2_5Vl7bInstruct,

        /// <summary>
        /// Amazon Nova Lite V1 model.
        /// </summary>
        [EnumMember(Value = "amazon/nova-lite-v1")]
        AmazonNovaLiteV1,

        ///// <summary>
        ///// Fireworks FireLlava 13B model.
        ///// </summary>
        //[EnumMember(Value = "fireworks/firellava-13b")]
        //FireworksFireLlava13b,

        ///// <summary>
        ///// Liuhaotian Llava 34B model.
        ///// </summary>
        //[EnumMember(Value = "liuhaotian/llava-yi-34b")]
        //liuhaotianLlava34b,

        /// <summary>
        /// Grok 2 Vision 1212 model by x-AI.
        /// </summary>
        [EnumMember(Value = "x-ai/grok-2-vision-1212")]
        Grok2Vision1212,

        /// <summary>
        /// Microsoft's Phi-4 Multimodal Instruct model.
        /// </summary>
        [EnumMember(Value = "microsoft/phi-4-multimodal-instruct")]
        MicrosoftPhi4MultimodalInstruct,

    }
}
