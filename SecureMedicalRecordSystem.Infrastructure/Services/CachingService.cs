using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using SecureMedicalRecordSystem.Core.Interfaces;
using System.Text.Json;
using Serilog;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class CachingService : ICachingService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IMemoryCache _memoryCache;
    private readonly IWebHostEnvironment _env;
    private static bool _redisWarningLogged = false;

    public CachingService(IDistributedCache distributedCache, IMemoryCache memoryCache, IWebHostEnvironment env)
    {
        _distributedCache = distributedCache;
        _memoryCache = memoryCache;
        _env = env;
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        // 1. Try In-Memory Cache first (Fastest layer)
        if (_memoryCache.TryGetValue(key, out T? localValue))
        {
            return localValue;
        }

        // 2. Try Distributed Cache (Redis)
        try
        {
            var cachedJson = await _distributedCache.GetStringAsync(key);
            if (!string.IsNullOrEmpty(cachedJson))
            {
                var redisValue = JsonSerializer.Deserialize<T>(cachedJson);
                if (redisValue != null)
                {
                    // Backfill local memory cache for subsequent fast hits
                    _memoryCache.Set(key, redisValue, expiration ?? TimeSpan.FromMinutes(5));
                    return redisValue;
                }
            }
        }
        catch (Exception ex)
        {
            if (!_redisWarningLogged)
            {
                Log.Warning("Redis is not available (localhost:6379). Falling back to In-Memory cache for this session. Error: {Message}", ex.Message);
                _redisWarningLogged = true;
            }
            else
            {
                Log.Debug("Redis cache error for key {Key}: {Message}. (Suppressed additional warnings)", key, ex.Message);
            }
        }

        // 3. Cache Miss - Execute factory
        var freshValue = await factory();

        if (freshValue != null)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5)
            };

            // Set both
            _memoryCache.Set(key, freshValue, options.AbsoluteExpirationRelativeToNow.Value);
            
            try
            {
                await _distributedCache.SetStringAsync(key, JsonSerializer.Serialize(freshValue), options);
            }
            catch (Exception ex)
            {
                if (!_redisWarningLogged)
                {
                    Log.Warning("Failed to set Redis cache (localhost:6379). Error: {Message}", ex.Message);
                    _redisWarningLogged = true;
                }
                else
                {
                    Log.Debug("Failed to set Redis cache for key {Key}: {Message}", key, ex.Message);
                }
            }
        }

        return freshValue;
    }

    public async Task InvalidateAsync(string key)
    {
        _memoryCache.Remove(key);
        try
        {
            await _distributedCache.RemoveAsync(key);
        }
        catch (Exception ex)
        {
            if (!_redisWarningLogged)
            {
                Log.Warning("Failed to invalidate Redis key {Key}. Error: {Message}", key, ex.Message);
                _redisWarningLogged = true;
            }
            else
            {
                Log.Debug("Failed to invalidate Redis key {Key}: {Message}", key, ex.Message);
            }
        }
    }

    public Task InvalidateByPatternAsync(string pattern)
    {
        // Redis pattern invalidation is complex with IDistributedCache
        // For now, we manually handle specific keys or rely on TTL
        // In a real Redis implementation, we'd use IConnectionMultiplexer
        return Task.CompletedTask;
    }
}
