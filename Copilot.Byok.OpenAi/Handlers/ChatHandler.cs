using Copilot.Byok.OpenAi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace Copilot.Byok.OpenAi.Handlers
{
    /// <summary>
    /// Chat请求处理器
    /// </summary>
    sealed class ChatHandler
    {
        /// <summary>
        /// 处理chat/completions请求
        /// </summary>
        /// <param name="context">HTTP上下文</param>
        /// <param name="forwarder">HTTP转发器</param>
        /// <param name="httpClient">HTTP客户端</param>
        public static async Task HandleAsync(
            HttpContext context,
            IHttpForwarder forwarder,
            ChatHttpClient httpClient,
            ILogger<ChatHandler> logger)
        {
            var modelConfig = context.Features.Get<IModelConfigFeature>()?.ModelConfig;
            if (modelConfig == null)
            {
                context.Response.StatusCode = StatusCodes.Status502BadGateway;
                return;
            }

            logger.LogInformation($"准备转发请求到模型：{modelConfig}");
            var waitTime = await modelConfig.ConcurrentLimiter.WaitAsync(context.RequestAborted);

            try
            {
                if (waitTime > TimeSpan.FromMilliseconds(100))
                {
                    logger.LogWarning($"模型 {modelConfig} 超出并发限制，已等待 {waitTime}，现在转发请求。");
                }

                waitTime = await modelConfig.RpmLimiter.WaitAsync(context.RequestAborted);
                if (waitTime > TimeSpan.Zero)
                {
                    logger.LogWarning($"模型 {modelConfig} 超出速率限制，已等待 {waitTime}，现在转发请求。");
                }

                context.Request.Path = "/chat/completions";
                context.Request.Headers.Authorization = $"Bearer {modelConfig.ApiKey}";
                await forwarder.SendAsync(context, modelConfig.BaseUrl, httpClient);
            }
            finally
            {
                modelConfig.ConcurrentLimiter.Release();
            }
        }
    }
}
