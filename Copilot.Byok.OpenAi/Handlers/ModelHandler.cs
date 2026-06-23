using Copilot.Byok.OpenAi.Models;
using Microsoft.Extensions.Options;
using System.Linq;

namespace Copilot.Byok.OpenAi.Handlers
{
    /// <summary>
    /// 模型处理器
    /// </summary>
    sealed class ModelHandler
    {
        /// <summary>
        /// 获取所有模型列表
        /// </summary>
        /// <param name="options">模型选项监控器</param>
        /// <returns>OpenAI模型列表</returns>
        public static OpenAiModelList GetAll(IOptionsMonitor<ModelOptions> options)
        {
            return new OpenAiModelList
            {
                Data = options.CurrentValue.Models.Keys.Select(id => new OpenAiModel(id)).ToArray()
            };
        }

        /// <summary>
        /// 根据模型ID获取模型信息
        /// </summary>
        /// <param name="options">模型选项监控器</param>
        /// <param name="id">模型ID</param>
        /// <returns>OpenAI模型对象，如果不存在则返回null</returns>
        public static OpenAiModel? GetOne(IOptionsMonitor<ModelOptions> options, string id)
        {
            id = id.TrimStart('/');
            return options.CurrentValue.Models.ContainsKey(id) ? new OpenAiModel(id) : null;
        }
    }
}
