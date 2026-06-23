using Copilot.Byok.OpenAi.Models;
using System.Text.Json.Serialization;

namespace Copilot.Byok.OpenAi
{
    [JsonSerializable(typeof(string[]))]
    [JsonSerializable(typeof(ModelConfig[]))] 
    [JsonSerializable(typeof(OpenAiModel))]
    [JsonSerializable(typeof(OpenAIModelList))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    sealed partial class JsonContext : JsonSerializerContext
    {
    }
}
