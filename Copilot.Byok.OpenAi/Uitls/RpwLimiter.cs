using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Copilot.Byok.OpenAi.Uitls
{
    /// <summary>
    /// 提供基于滑动时间窗口的请求速率限制（Rate Per Window, RPW）的抽象基类。
    /// </summary>
    /// <remarks>
    /// 线程安全：<see cref="WaitAsync"/> 可在多个线程上并发调用，
    /// 内部通过 <see cref="SemaphoreSlim"/> 保证原子操作。
    /// </remarks>
    abstract class RpwLimiter : IDisposable
    {
        /// <summary>
        /// 等待直到允许下一次请求，并返回实际等待的时间。
        /// </summary>
        /// <param name="cancellationToken">用于取消等待操作的 <see cref="CancellationToken"/></param>
        /// <returns>
        /// 表示异步操作的任务，其结果为本次等待的 <see cref="TimeSpan"/>。
        /// 若无需等待则返回 <see cref="TimeSpan.Zero"/>。
        /// </returns>
        /// <exception cref="OperationCanceledException">
        /// 当 <paramref name="cancellationToken"/> 触发取消时引发。
        /// </exception>
        public abstract Task<TimeSpan> WaitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 释放 <see cref="RpwLimiter"/> 使用的所有资源。
        /// </summary>
        /// <remarks>
        /// 调用 <see cref="Dispose()"/> 后不应再调用 <see cref="WaitAsync"/>。
        /// </remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放 <see cref="RpwLimiter"/> 使用的托管和非托管资源。
        /// </summary>
        /// <param name="disposing">
        /// 若为 <see langword="true"/>，同时释放托管和非托管资源；
        /// 若为 <see langword="false"/>，仅释放非托管资源。
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// 创建一个请求速率限制器实例。
        /// </summary>
        /// <param name="maxRequestsPerWindow">时间窗口内允许的最大请求数</param>
        /// <param name="window">滑动时间窗口，必须大于或等于 1 秒</param>
        /// <returns>
        /// 若 <paramref name="maxRequestsPerWindow"/> 大于 0，返回 <see cref="DefaultRpwLimiter"/> 实例；
        /// 否则返回 <see cref="NoOpRpwLimiter"/> 单例，不执行实际限制。
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// 当 <paramref name="window"/> 小于 1 秒时引发。
        /// </exception>
        public static RpwLimiter Create(int maxRequestsPerWindow, TimeSpan window)
        {
            if (window < TimeSpan.FromSeconds(1))
            {
                throw new ArgumentOutOfRangeException(nameof(window), "窗口时间必须大于等于1秒。");
            }

            return maxRequestsPerWindow <= 0
                ? NoOpRpwLimiter.Instance
                : new DefaultRpwLimiter(maxRequestsPerWindow, window);
        }

        /// <summary>
        /// 空操作限流器，始终立即放行，不执行任何速率限制。
        /// 当 <c>maxRequestsPerWindow &lt;= 0</c> 时由 <see cref="RpwLimiter.Create"/> 返回。
        /// </summary>
        private sealed class NoOpRpwLimiter : RpwLimiter
        {
            public static readonly RpwLimiter Instance = new NoOpRpwLimiter();

            /// <summary>
            /// 不执行任何等待，立即返回 <see cref="TimeSpan.Zero"/>。
            /// </summary>
            public override Task<TimeSpan> WaitAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(TimeSpan.Zero);
            }
        }

        /// <summary>
        /// 基于滑动时间窗口的默认速率限制器实现。
        /// </summary>
        /// <remarks>
        /// 使用 <see cref="Stopwatch"/> 高精度计时和 <see cref="ConcurrentQueue{T}"/> 记录请求时间戳，
        /// 在 <see cref="SemaphoreSlim"/> 的保护下进行原子化的窗口滑动检查。
        /// </remarks>
        private sealed class DefaultRpwLimiter : RpwLimiter
        {
            private readonly int _maxRequestsPerWindow;
            private readonly long _windowInTicks;
            private readonly ConcurrentQueue<long> _requestTimestamps = new();
            private readonly SemaphoreSlim _semaphore = new(1, 1);
            private static readonly Stopwatch _stopwatch = Stopwatch.StartNew();

            /// <summary>
            /// 初始化 <see cref="DefaultRpwLimiter"/> 的新实例。
            /// </summary>
            /// <param name="maxRequestsPerWindow">时间窗口内允许的最大请求数</param>
            /// <param name="window">滑动时间窗口</param>
            public DefaultRpwLimiter(int maxRequestsPerWindow, TimeSpan window)
            {
                _maxRequestsPerWindow = maxRequestsPerWindow; 
                _windowInTicks = (long)(window.TotalSeconds * Stopwatch.Frequency);
            }

            /// <summary>
            /// 等待直到允许下一次请求，并返回实际等待的时间。
            /// </summary>
            /// <inheritdoc cref="RpwLimiter.WaitAsync(CancellationToken)"/>
            public override async Task<TimeSpan> WaitAsync(CancellationToken cancellationToken = default)
            {
                await _semaphore.WaitAsync(cancellationToken);
                try
                {
                    var now = _stopwatch.ElapsedTicks;
                    var waitTime = TimeSpan.Zero;

                    // 清理过期的请求记录
                    while (_requestTimestamps.TryPeek(out var oldest) && now - oldest > _windowInTicks)
                    {
                        _requestTimestamps.TryDequeue(out _);
                    }

                    // 达到请求上限，计算等待时长
                    if (_requestTimestamps.Count >= _maxRequestsPerWindow)
                    {
                        if (_requestTimestamps.TryPeek(out var oldestTimestamp))
                        {
                            var waitTicks = _windowInTicks - (now - oldestTimestamp);
                            if (waitTicks > 0)
                            {
                                // 避免 Stopwatch ticks 与 .NET ticks (100ns) 单位不一致的移植性问题
                                waitTime = TimeSpan.FromSeconds(waitTicks / (double)Stopwatch.Frequency);
                                await Task.Delay(waitTime, cancellationToken);
                            }
                        }
                    }

                    _requestTimestamps.Enqueue(_stopwatch.ElapsedTicks);
                    return waitTime;
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            /// <summary>
            /// 释放 <see cref="DefaultRpwLimiter"/> 使用的托管资源。
            /// </summary>
            /// <param name="disposing">
            /// 若为 <see langword="true"/>，则释放 <see cref="_semaphore"/>。
            /// </param>
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _semaphore.Dispose();
                }
            }
        }
    }
}
