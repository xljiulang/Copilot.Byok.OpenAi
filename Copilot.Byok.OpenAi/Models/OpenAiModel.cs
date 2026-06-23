using System;

namespace Copilot.Byok.OpenAi.Models
{
    /// <summary>
    /// OpenAI模型信息
    /// </summary>
    sealed class OpenAiModel : IEquatable<OpenAiModel>
    {
        /// <summary>
        /// 获取模型ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置对象类型
        /// </summary>
        public string @Object { get; set; } = "model";

        /// <summary>
        /// 初始化OpenAIModel实例
        /// </summary>
        /// <param name="model">模型ID</param>
        public OpenAiModel(string model)
        {
            this.Id = model;
        }

        /// <summary>
        /// 比较两个OpenAIModel对象是否相等
        /// </summary>
        /// <param name="other">要比较的另一个OpenAIModel对象</param>
        /// <returns>如果相等则返回true，否则返回false</returns>
        public bool Equals(OpenAiModel? other)
        {
            return other != null && other.Id == this.Id && other.Object == this.Object;
        }

        /// <summary>
        /// 比较当前对象与指定对象是否相等
        /// </summary>
        /// <param name="obj">要比较的对象</param>
        /// <returns>如果相等则返回true，否则返回false</returns>
        public override bool Equals(object? obj)
        {
            return obj is OpenAiModel other && this.Equals(other);
        }

        /// <summary>
        /// 返回对象的哈希码
        /// </summary>
        /// <returns>对象的哈希码</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(this.Id, this.Object);
        }
    }
}
