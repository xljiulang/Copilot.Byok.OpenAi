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
        /// 获取所有模型ID
        /// </summary>
        /// <param name="options">模型选项监控器</param>
        /// <returns>模型ID数组</returns>
        public static string[] Get(IOptionsMonitor<ModelOptions> options)
        {
            return options.CurrentValue.Models.Keys.ToArray();
        }

        /// <summary>
        /// 获取所有模型列表
        /// </summary>
        /// <param name="options">模型选项监控器</param>
        /// <returns>OpenAI模型列表</returns>
        public static OpenAIModelList GetAll(IOptionsMonitor<ModelOptions> options)
        {
            return new OpenAIModelList
            {
                Data = options.CurrentValue.Models.Keys.Select(i => new OpenAiModel(i)).ToArray()
            };
        }

        /// <summary>
        /// 根据模型ID获取模型信息
        /// </summary>
        /// <param name="options">模型选项监控器</param>
        /// <param name="model">模型ID</param>
        /// <returns>OpenAI模型对象，如果不存在则返回null</returns>
        public static OpenAiModel? GetOne(IOptionsMonitor<ModelOptions> options, string model)
        {
            model = model.TrimStart('/');
            return options.CurrentValue.Models.ContainsKey(model) ? new OpenAiModel(model) : null;
        }
    }
}
