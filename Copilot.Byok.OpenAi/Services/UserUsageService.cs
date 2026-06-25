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

    /// <summary>
    /// 获取从昨天到今天的所有用量统计数据
    /// </summary>
    /// <returns>用量统计数组，按日期、请求次数、用户ID排序</returns>
    public UserUsage[] GetRequestStats()
    {
        var yesterday = DateTime.Now.AddDays(-1d).Date;
        return this._userUsages
            .Where(i => i.Key.Date >= yesterday)
            .Select(i => i.Value)
            .OrderByDescending(i => i.LastRequestTime.Date)
            .ThenByDescending(i => i.RequestCount)
            .ThenBy(i => i.UserId)
            .ToArray();
    }

    /// <summary>
    /// 后台任务，每天清理超过一天的过期统计记录，防止内存泄漏
    /// </summary>
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

    /// <summary>
    /// 统计记录的唯一键
    /// </summary>
    private sealed record StatKey(string UserId, string Model, DateTime Date);
}
