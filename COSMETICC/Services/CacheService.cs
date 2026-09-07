using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace COSMETICC.Services
{
    public class CacheService
    {
        private readonly IMemoryCache _memoryCache;

        public CacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan absoluteExpirationRelativeToNow)
        {
            if (_memoryCache.TryGetValue(key, out T? cachedItem) && cachedItem != null)
            {
                return cachedItem;
            }

            var newItem = await factory();
            if (newItem != null)
            {
                _memoryCache.Set(key, newItem, absoluteExpirationRelativeToNow);
            }

            return newItem;
        }

        public void Remove(string key)
        {
            _memoryCache.Remove(key);
        }
    }
}
