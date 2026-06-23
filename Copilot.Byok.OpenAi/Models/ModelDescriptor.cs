using Copilot.Byok.OpenAi.Uitls;
using System;
using System.Collections.Generic;

namespace Copilot.Byok.OpenAi.Models
{
    /// <summary>
    /// 模型描述符，用于配置模型的各种参数
    /// </summary>
    sealed class ModelDescriptor
    {
        /// <summary>
        /// 获取或设置每分钟最大请求数，0表示不限制
        /// </summary>
        public int RequestsPerMinute { get; set; }

        /// <summary>
        /// 获取或设置最大并发请求数，0表示不限制
        /// </summary>
        public int MaxConcurrentRequests { get; set; }

        /// <summary>
        /// 获取或设置基础URL地址
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置API密钥数组
        /// </summary>
        public string[] ApiKeys { get; set; } = [];

        /// <summary>
        /// 将模型描述符转换为模型配置集合
        /// </summary>
        /// <param name="model">模型名称</param>
        /// <returns>模型配置枚举</returns>
        public IEnumerable<ModelConfig> ToModelConfig(string model)
        {
            var index = this.ApiKeys.Length == 1 ? -1 : 0;
            foreach (var apiKey in this.ApiKeys)
            {
                yield return new ModelConfig
                {
                    Model = model,
                    BaseUrl = this.BaseUrl,
                    ApiKey = apiKey,
                    Index = index++,
                    RpmLimiter = RpwLimiter.Create(this.RequestsPerMinute, TimeSpan.FromMinutes(1)),
                    ConcurrentLimiter = ConcurrentLimiter.Create(this.MaxConcurrentRequests)
                };
            }
        }
    }
}
