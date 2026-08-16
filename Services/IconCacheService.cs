using System.Collections.Concurrent;
using SPTarkov.DI.Annotations;

namespace HideoutCraftModifier.Services;

[Injectable(InjectionType.Singleton)]
public class IconCacheService
{
    private readonly ConcurrentDictionary<string, byte[]> _cache = new();
    private readonly HttpClient _httpClient = new();

    public async Task<byte[]?> GetIconAsync(string templateId)
    {
        if (_cache.TryGetValue(templateId, out var cached))
            return cached;

        try
        {
            var bytes = await _httpClient.GetByteArrayAsync(
                $"https://assets.tarkov.dev/{templateId}-icon.webp");
            _cache.TryAdd(templateId, bytes);
            return bytes;
        }
        catch
        {
            return null;
        }
    }
}
