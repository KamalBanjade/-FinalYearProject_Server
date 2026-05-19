using Microsoft.Extensions.Caching.Memory;
using SecureMedicalRecordSystem.Core.Interfaces;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class CachingService : ICachingService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IWebHostEnvironment _env;

    public CachingService(IMemoryCache memoryCache, IWebHostEnvironment env)
    {
        _memoryCache = memoryCache;
        _env = env;
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        // Try In-Memory Cache first (Fastest layer)
        if (_memoryCache.TryGetValue(key, out T? localValue))
        {
            return localValue;
        }

        // Cache Miss - Execute factory
        var freshValue = await factory();

        if (freshValue != null)
        {
            var ttl = expiration ?? TimeSpan.FromMinutes(5);
            _memoryCache.Set(key, freshValue, ttl);
        }

        return freshValue;
    }

    public Task InvalidateAsync(string key)
    {
        _memoryCache.Remove(key);
        return Task.CompletedTask;
    }

    public Task InvalidateByPatternAsync(string pattern)
    {
        // In-memory cache doesn't support pattern invalidation easily.
        // We rely on TTL or explicit key invalidation.
        return Task.CompletedTask;
    }
}
