using Copilot.Byok.OpenAi.Models;
using System.Text.Json.Serialization;

namespace Copilot.Byok.OpenAi
{
    [JsonSerializable(typeof(OpenAiModel))]
    [JsonSerializable(typeof(OpenAiModelList))]
    [JsonSerializable(typeof(OpenAiRequest))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    sealed partial class JsonContext : JsonSerializerContext
    {
    }
}
