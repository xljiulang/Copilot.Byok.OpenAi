using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Copilot.Byok.OpenAi.Models
{
    /// <summary>
    /// OpenAi 的请求
    /// </summary>
    sealed class OpenAiRequest
    {
        /// <summary>
        /// 获取或设置模型名称
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置扩展数据，用于存储额外的请求参数
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; } = [];
    }
}
