using System;
using System.Collections.Generic;
using System.Threading;

namespace Copilot.Byok.OpenAi.Models;

/// <summary>
/// 用户对模型的每日请求统计
/// </summary>
sealed record UserUsage
{
    private readonly Lock _lock = new();

    /// <summary>
    /// 用户id
    /// </summary>
    public string UserId { get; }

    /// <summary>
    /// 模型
    /// </summary>
    public string Model { get; }

    /// <summary>
    /// 自然日请求总次数
    /// </summary>
    public int RequestCount { get; private set; }

    /// <summary>
    /// 最后一次请求时间
    /// </summary>
    public DateTimeOffset LastRequestTime { get; private set; }

    /// <summary>
    /// 去重的客户端IP记录
    /// </summary>
    public HashSet<string> IPAddresses { get; } = [];

    public UserUsage(string userId, string model)
    {
        this.UserId = userId;
        this.Model = model;
    }

    public void Record(string? remoteIp)
    {
        lock (_lock)
        {
            this.RequestCount += 1;
            this.LastRequestTime = DateTimeOffset.Now;
            if (string.IsNullOrEmpty(remoteIp) == false)
            {
                this.IPAddresses.Add(remoteIp);
            }
        }
    }
}
