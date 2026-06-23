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
    sealed class ModelMiddleware : IMiddleware
    {
        private static readonly RecyclableMemoryStreamManager _memoryStreamManager = new();
        private readonly IOptionsMonitor<ModelOptions> modelOptions;

        public ModelMiddleware(IOptionsMonitor<ModelOptions> modelOptions)
        {
            this.modelOptions = modelOptions;
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
                    var modelConfig = this.GetModelConfig(newBody);
                    context.Features.Set<IModelConfigFeature>(new ModelFeature(modelConfig));

                    newBody.Position = 0L;
                    context.Request.Body = newBody;

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
        private ModelConfig? GetModelConfig(RecyclableMemoryStream stream)
        {
            stream.Position = 0L;
            var reader = new Utf8JsonReader(stream.GetReadOnlySequence());
            if (JsonDocument.TryParseValue(ref reader, out var document))
            {
                if (document.RootElement.TryGetProperty("model", out var modelProperty))
                {
                    var model = modelProperty.GetString();
                    return this.modelOptions.CurrentValue.Select(model);
                }
            }
            return null;
        }

        private sealed class ModelFeature(ModelConfig? modelConfig) : IModelConfigFeature
        {
            public ModelConfig? ModelConfig { get; } = modelConfig;
        }
    }
}
