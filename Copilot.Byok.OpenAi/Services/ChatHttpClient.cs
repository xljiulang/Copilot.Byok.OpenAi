using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using Yarp.ReverseProxy.Forwarder;

namespace Copilot.Byok.OpenAi.Services
{
    /// <summary>
    /// HTTP客户端服务，用于与OpenAI API进行通信
    /// </summary>
    [Service(ServiceLifetime.Singleton)]
    sealed class ChatHttpClient : HttpMessageInvoker
    {
        /// <summary>
        /// 初始化ChatHttpClient实例
        /// </summary>
        public ChatHttpClient()
            : base(CreateHttpHandler())
        {
        }

        /// <summary>
        /// 创建HTTP消息处理器
        /// </summary>
        /// <returns>配置好的SocketsHttpHandler实例</returns>
        private static HttpMessageHandler CreateHttpHandler()
        {
            return new SocketsHttpHandler
            {
                UseProxy = false,
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.None,
                UseCookies = false,
                EnableMultipleHttp2Connections = true,
                ActivityHeadersPropagator = new ReverseProxyPropagator(DistributedContextPropagator.Current),
                ConnectTimeout = TimeSpan.FromSeconds(15),
            };
        }
    }
}
