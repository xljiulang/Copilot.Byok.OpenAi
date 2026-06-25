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
            var feature = context.Features.Get<IModelConfigFeature>();
            if (feature == null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var modelConfig = feature.ModelConfig;
            if (modelConfig == null)
            {
                context.Response.StatusCode = StatusCodes.Status502BadGateway;
                logger.LogError($"找不到匹配的模型配置: {feature.Id}");
                return;
            }
            
            logger.LogInformation($"准备转发请求到模型：{modelConfig}");
            // RPM 限流
            var rpmWaitTime = await modelConfig.RpmLimiter.WaitAsync(context.RequestAborted);
            if (rpmWaitTime > TimeSpan.Zero)
            {
                logger.LogWarning($"模型 {modelConfig} 超出速率限制，已等待 {rpmWaitTime}，现在转发请求。");
            }

            // 在获取并发槽位前检查请求是否已取消，避免浪费并发槽位
            if (context.RequestAborted.IsCancellationRequested)
            {
                return;
            }

            var concurrentWaitTime = await modelConfig.ConcurrentLimiter.WaitAsync(context.RequestAborted);
            try
            {
                if (concurrentWaitTime > TimeSpan.FromMilliseconds(100))
                {
                    logger.LogWarning($"模型 {modelConfig} 超出并发限制，已等待 {concurrentWaitTime}，现在转发请求。");
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
