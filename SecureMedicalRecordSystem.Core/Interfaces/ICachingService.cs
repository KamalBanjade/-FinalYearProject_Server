using System;
using System.Threading.Tasks;

namespace SecureMedicalRecordSystem.Core.Interfaces;

public interface ICachingService
{
    /// <summary>
    /// Gets a value from cache or sets it if it doesn't exist.
    /// </summary>
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);

    /// <summary>
    /// Removes a value from cache.
    /// </summary>
    Task InvalidateAsync(string key);

    /// <summary>
    /// Removes values from cache by pattern (only for Redis).
    /// </summary>
    Task InvalidateByPatternAsync(string pattern);
}
