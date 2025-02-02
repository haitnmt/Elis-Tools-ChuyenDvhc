using System.Diagnostics.CodeAnalysis;
using Haihv.Elis.Tool.ChuyenDvhc.Services;
using Haihv.Elis.Tool.ChuyenDvhc.Settings;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;


namespace Haihv.Elis.Tool.ChuyenDvhc.Extensions;

public static class HybridCachingExtensions
{
    [Experimental("EXTEXP0018")]
    public static void AddHybridCaching(this IServiceCollection services)
    {
        services.AddSingleton<IDistributedCache>(sp =>
            new FileDistributedCache(
                sp.GetRequiredService<IFileService>(),
                FilePath.CacheOnDisk));
        services.AddHybridCache(options =>
        {
            options.MaximumPayloadBytes = 1024 * 1024;
            options.MaximumKeyLength = 1024;
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(60),
                LocalCacheExpiration = TimeSpan.FromDays(1)
            };
        });
    }
}