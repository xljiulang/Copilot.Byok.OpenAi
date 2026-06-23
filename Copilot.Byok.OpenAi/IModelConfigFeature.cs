using Copilot.Byok.OpenAi.Models;

namespace Copilot.Byok.OpenAi
{
    interface IModelConfigFeature
    {
        /// <summary>
        /// 获取当前请求的模型配置
        /// </summary>
        ModelConfig? ModelConfig { get; }
    }
}
