namespace Copilot.Byok.OpenAi.Models
{
    /// <summary>
    /// OpenAI模型列表
    /// </summary>
    sealed class OpenAiModelList
    {
        /// <summary>
        /// 获取或设置对象类型
        /// </summary>
        public string @Object { get; set; } = "list";

        /// <summary>
        /// 获取或设置模型数据数组
        /// </summary>
        public OpenAiModel[] Data { get; set; } = [];
    }
}
