namespace Copilot.Byok.OpenAi.Models
{
    /// <summary>
    /// OpenAI模型信息
    /// </summary>
    sealed class OpenAiModel
    {
        /// <summary>
        /// 获取模型ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置对象类型
        /// </summary>
        public string Object { get; set; } = "model";

        /// <summary>
        /// 初始化OpenAIModel实例
        /// </summary>
        /// <param name="model">模型ID</param>
        public OpenAiModel(string model)
        {
            this.Id = model;
        } 
    }
}
