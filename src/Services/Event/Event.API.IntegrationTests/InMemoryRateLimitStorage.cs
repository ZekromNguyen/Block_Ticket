using Event.Infrastructure.Security.RateLimiting.Interfaces;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System;

namespace Event.API.IntegrationTests
{
    public class InMemoryRateLimitStorage : IRateLimitStorage
    {
        private readonly ConcurrentDictionary<string, (long count, DateTime expiration)> _storage = new();

        public Task<(long count, DateTime expiration)> IncrementAsync(string key, int windowSizeSeconds, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var expiration = now.AddSeconds(windowSizeSeconds);

            var entry = _storage.AddOrUpdate(key, (1, expiration), (k, v) =>
            {
                if (v.expiration < now)
                {
                    return (1, expiration);
                }
                return (v.count + 1, v.expiration);
            });

            return Task.FromResult(entry);
        }

        public Task<(long count, bool isExceeded)> SlidingWindowIncrementAsync(string key, int windowSizeSeconds, int limit, CancellationToken cancellationToken = default)
        {
            var (count, expiration) = IncrementAsync(key, windowSizeSeconds, cancellationToken).Result;
            return Task.FromResult((count, count > limit));
        }

        // The following methods are not critical for the tests, so they can have simple mock implementations.
        public Task<(long count, DateTime expiration)> GetCountAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult((0L, DateTime.UtcNow));
        public Task<bool> ResetAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<System.Collections.Generic.Dictionary<string, object>> GetStatisticsAsync(CancellationToken cancellationToken = default) => Task.FromResult(new System.Collections.Generic.Dictionary<string, object>());
        public Task CleanupExpiredKeysAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
