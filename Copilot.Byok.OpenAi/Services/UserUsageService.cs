using Copilot.Byok.OpenAi.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Copilot.Byok.OpenAi.Services;

/// <summary>
/// 请求统计服务，记录用户对模型的每日请求统计
/// </summary>
[Service(ServiceLifetime.Singleton, typeof(UserUsageService), typeof(IHostedService))]
sealed class UserUsageService : BackgroundService
{
    private readonly ConcurrentDictionary<StatKey, UserUsage> _userUsages = [];

    /// <summary>
    /// 记录一次请求
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="model">模型ID</param>
    /// <param name="remoteIp">客户端IP</param>
    public void Record(string userId, string model, string? remoteIp)
    {
        var date = DateTime.Now.Date;
        var statKey = new StatKey(userId, model, date);
        this._userUsages.GetOrAdd(statKey, _ => new UserUsage(userId, model)).Record(remoteIp);
    }

    public UserUsage[] GetRequestStats()
    {
        var yesterday = DateTime.Now.AddDays(-1d).Date;
        return this._userUsages
            .Where(i => i.Key.Date >= yesterday)
            .Select(i => i.Value)
            .OrderBy(i => i.LastRequestTime.Date)
            .ThenBy(i => i.RequestCount)
            .ThenBy(i => i.UserId)
            .ToArray();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested == false)
        {
            await Task.Delay(TimeSpan.FromDays(1d), stoppingToken);

            var yesterday = DateTime.Now.AddDays(-1d).Date;
            var keys = this._userUsages.Keys.Where(i => i.Date < yesterday).ToArray();
            foreach (var key in keys)
            {
                this._userUsages.TryRemove(key, out _);
            }
        }
    }

    private sealed record StatKey(string UserId, string Model, DateTime Date);
}
