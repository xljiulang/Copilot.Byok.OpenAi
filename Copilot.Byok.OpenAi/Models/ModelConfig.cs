using Copilot.Byok.OpenAi.Uitls;

namespace Copilot.Byok.OpenAi.Models
{
    /// <summary>
    /// 模型配置，包含模型的基本信息和限流控制
    /// </summary>
    sealed class ModelConfig
    {
        /// <summary>
        /// 模型Id
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// 获取或设置模型名称
        /// </summary>
        public required string Model { get; init; }

        /// <summary>
        /// 获取或设置API密钥
        /// </summary>
        public required string ApiKey { get; init; }

        /// <summary>
        /// 获取或设置基础URL地址
        /// </summary>
        public required string BaseUrl { get; init; }

        /// <summary>
        /// 索引，用于标识模型在配置中的位置
        /// </summary>
        public required int Index { get; init; }

        /// <summary>
        /// 获取用于控制并发访问的信号量
        /// </summary>
        public required RpwLimiter RpmLimiter { get; init; }

        /// <summary>
        /// 获取用于控制并发访问的信号量
        /// </summary>
        public required ConcurrentLimiter ConcurrentLimiter { get; init; }

        /// <summary>
        /// 最后一次使用的时间戳
        /// </summary>
        public long LastUsedTicks { get; set; }

        /// <summary>
        /// 返回模型的字符串表示
        /// </summary>
        /// <returns>模型名称</returns>
        public override string ToString()
        {
            var modelName = $"{this.Id}/{this.Model}";
            return this.Index < 0 ? modelName : $"{modelName}[{this.Index}]";
        }
    }
}
