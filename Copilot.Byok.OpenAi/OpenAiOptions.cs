using Copilot.Byok.OpenAi.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace Copilot.Byok.OpenAi
{
    sealed class OpenAiOptions
    {
        private readonly Lock _lock = new();
        private Dictionary<string, ModelConfigRing> _modelConfigRings = [];

        /// <summary>
        /// apikey 集合，允许的apikey列表，如果为空则不限制
        /// </summary>
        public HashSet<string> ApiKeys { get; set; } = [];

        /// <summary>
        /// 获取或设置模型字典，键为模型名称，值为模型描述符
        /// </summary>
        public Dictionary<string, ModelDescriptor> Models { get; set; } = [];

        /// <summary>
        /// 初始化模型配置
        /// </summary>
        internal void Initialize()
        {
            lock (this._lock)
            {
                this._modelConfigRings = this.Models.ToDictionary(kv => kv.Key, kv => new ModelConfigRing(kv.Value.ToModelConfigs(kv.Key)));
            }
        }

        /// <summary>
        /// 根据模型ID选择模型配置
        /// </summary>
        /// <param name="id">模型ID</param>
        /// <returns>模型配置对象，如果不存在则返回null</returns>
        internal ModelConfig? SelectModelConfig(string? id)
        {
            if (id == null)
            {
                return null;
            }

            lock (this._lock)
            {
                return this._modelConfigRings.TryGetValue(id, out var ring) ? ring.Next() : null;
            }
        }

        private sealed class ModelConfigRing
        {
            private int _index = 0;
            private readonly ModelConfig[] _items;

            public ModelConfigRing(IEnumerable<ModelConfig> items)
            {
                _items = items.ToArray();
            }

            public ModelConfig? Next()
            {
                if (_items.Length == 0)
                {
                    return null;
                }

                var item = _items[_index];
                _index = (_index + 1) % _items.Length;
                return item;
            }
        }
    }
}
