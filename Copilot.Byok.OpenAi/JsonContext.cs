using Copilot.Byok.OpenAi.Models;
using System.Text.Json.Serialization;

namespace Copilot.Byok.OpenAi
{
    [JsonSerializable(typeof(string[]))]
    [JsonSerializable(typeof(OpenAiModel))]
    [JsonSerializable(typeof(OpenAiModelList))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    sealed partial class JsonContext : JsonSerializerContext
    {
    }
}
