using Copilot.Byok.OpenAi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Copilot.Byok.OpenAi.Middlewares
{
    /// <summary>
    /// 从请求体中解析出模型名称，并将其存储在 IModelFeature 中，供后续处理使用
    /// </summary>
    [Service(ServiceLifetime.Singleton)]
    sealed class ModelConfigMiddleware : IMiddleware
    {
        private readonly IOptionsMonitor<OpenAiOptions> _openAiOptions;
        private readonly RecyclableMemoryStreamManager _memoryStreamManager;

        public ModelConfigMiddleware(
            IOptionsMonitor<OpenAiOptions> openAiOptions,
            RecyclableMemoryStreamManager memoryStreamManager)
        {
            this._openAiOptions = openAiOptions;
            this._memoryStreamManager = memoryStreamManager;
        }

        /// <summary>
        /// 中间件处理方法
        /// </summary>
        /// <param name="context">HTTP上下文</param>
        /// <param name="next">下一个中间件委托</param>
        /// <returns>异步任务</returns>
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint != null && context.Request.HasJsonContentType())
            {
                var oldBody = context.Request.Body;
                using var newBody = _memoryStreamManager.GetStream();
                await oldBody.CopyToAsync(newBody);

                try
                {
                    var modelConfigFeature = this.CreateModelConfigFeature(newBody);
                    context.Features.Set<IModelConfigFeature>(modelConfigFeature);

                    newBody.Position = 0L;
                    context.Request.Body = newBody;
                    context.Request.ContentLength = newBody.Length;

                    await next(context);
                }
                finally
                {
                    context.Request.Body = oldBody;
                }
            }
            else
            {
                await next(context);
            }
        }

        /// <summary>
        /// 从请求流中解析出模型配置
        /// </summary>
        /// <param name="stream">请求流</param>
        /// <returns>模型配置对象，如果解析失败则返回null</returns>
        private ModelConfigFeature? CreateModelConfigFeature(RecyclableMemoryStream stream)
        {
            stream.Position = 0L;
            var reader = new Utf8JsonReader(stream.GetReadOnlySequence());
            if (JsonDocument.TryParseValue(ref reader, out var document))
            {
                using (document)
                {
                    var openAiRequest = document.Deserialize(JsonContext.Default.OpenAiRequest);
                    if (openAiRequest != null)
                    {
                        var id = openAiRequest.Model;
                        var modelConfig = this._openAiOptions.CurrentValue.SelectModelConfig(id);
                        if (modelConfig != null && modelConfig.Id != modelConfig.Model)
                        {
                            // 更新请求内容中的 model 值
                            openAiRequest.Model = modelConfig.Model;
                            stream.Position = 0L;
                            stream.SetLength(0L);
                            JsonSerializer.Serialize(stream, openAiRequest, JsonContext.Default.OpenAiRequest);
                        }
                        return new ModelConfigFeature(id, modelConfig);
                    }
                }
            }
            return null;
        }

        private sealed class ModelConfigFeature(string id, ModelConfig? modelConfig) : IModelConfigFeature
        {
            public string Id { get; } = id;

            public ModelConfig? ModelConfig { get; } = modelConfig;
        }
    }
}
