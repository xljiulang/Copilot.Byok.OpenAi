using System;
using System.Threading;
using System.Threading.Tasks;

namespace Copilot.Byok.OpenAi.Utils
{
    abstract class ConcurrentLimiter : IDisposable
    {
        /// <summary>
        /// 等待直到允许下一次请求
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>异步任务</returns>
        public abstract Task<TimeSpan> WaitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 释放锁
        /// </summary>
        public abstract void Release();

        /// <summary>
        /// 释放托管资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing">是否正在释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
        }

        public static ConcurrentLimiter Create(int maxConcurrentRequests)
        {
            return maxConcurrentRequests <= 0
                ? NoOpConcurrentLimiter.Instance
                : new DefaultConcurrentLimiter(maxConcurrentRequests);
        }

        private sealed class NoOpConcurrentLimiter : ConcurrentLimiter
        {
            public static readonly ConcurrentLimiter Instance = new NoOpConcurrentLimiter();

            public override Task<TimeSpan> WaitAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(TimeSpan.Zero);
            }

            public override void Release()
            {
            }
        }

        private sealed class DefaultConcurrentLimiter : ConcurrentLimiter
        {
            private readonly SemaphoreSlim _semaphore;

            public DefaultConcurrentLimiter(int maxConcurrentRequests)
            {
                _semaphore = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
            }

            public override async Task<TimeSpan> WaitAsync(CancellationToken cancellationToken = default)
            {
                var ticks = Environment.TickCount64;
                await _semaphore.WaitAsync(cancellationToken);
                return TimeSpan.FromMilliseconds(Environment.TickCount64 - ticks);
            }

            public override void Release()
            {
                _semaphore.Release();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _semaphore.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}
