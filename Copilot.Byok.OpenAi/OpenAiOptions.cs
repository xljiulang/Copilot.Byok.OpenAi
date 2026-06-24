using Copilot.Byok.OpenAi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Copilot.Byok.OpenAi
{
    sealed class OpenAiOptions
    {
        private readonly Lock _lock = new();
        private ModelConfig[] _modelConfigs = [];

        /// <summary>
        /// 获取或设置模型字典，键为模型名称，值为模型描述符
        /// </summary>
        public Dictionary<string, ModelDescriptor> Models { get; set; } = [];

        /// <summary>
        /// apikey 集合，允许的apikey列表，如果为空则不限制
        /// </summary>
        public HashSet<string> ApiKeys { get; set; } = [];

        /// <summary>
        /// 初始化模型配置
        /// </summary>
        public void Initialize()
        {
            this._modelConfigs = this.Models.SelectMany(kv => kv.Value.ToModelConfig(kv.Key)).ToArray();
        }

        /// <summary>
        /// 根据模型ID选择模型配置
        /// </summary>
        /// <param name="id">模型ID</param>
        /// <returns>模型配置对象，按 LastUsedTicks 排序，如果不存在则返回null</returns>
        public ModelConfig? SelectModelConfig(string? id)
        {
            lock (this._lock)
            {
                var item = this._modelConfigs
                    .Where(m => m.Id == id)
                    .OrderBy(m => m.LastUsedTicks)
                    .FirstOrDefault();

                item?.LastUsedTicks = Environment.TickCount64;
                return item;
            }
        }
    }
}
