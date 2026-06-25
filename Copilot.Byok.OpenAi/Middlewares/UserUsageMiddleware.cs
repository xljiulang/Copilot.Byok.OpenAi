using Copilot.Byok.OpenAi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Copilot.Byok.OpenAi.Middlewares;

/// <summary>
/// 请求统计中间件——记录用户对模型的每日请求统计
/// </summary>
[Service(ServiceLifetime.Singleton)]
sealed class UserUsageMiddleware : IMiddleware
{
    private readonly UserUsageService _statService;

    public UserUsageMiddleware(UserUsageService statService)
    {
        _statService = statService;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var modelId = context.Features.Get<IModelConfigFeature>()?.Id;

            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(modelId))
            {
                var remoteIp = context.Connection.RemoteIpAddress;
                if (remoteIp != null && remoteIp.IsIPv4MappedToIPv6)
                {
                    remoteIp = remoteIp.MapToIPv4();
                }
                this._statService.Record(userId, modelId, remoteIp?.ToString());
            }
        }

        await next(context);
    }
}
