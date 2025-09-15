using System;
using System.IO;

namespace ArxOne.Synology;

public record SpkRepositoryConfiguration
{
    public string StorageRoot { get; init; }

    private readonly Func<Uri> _getSiteRoot;
    public Uri SiteRoot => _getSiteRoot();

    public string? CacheName { get; init; }

    private readonly bool _cacheDirectorySet;
    private readonly string? _cacheDirectory;
    public string? CacheDirectory
    {
        get { return _cacheDirectorySet ? _cacheDirectory : GetDefaultCacheDirectory(); }
        init
        {
            _cacheDirectory = value;
            _cacheDirectorySet = true;
        }
    }

    public SpkRepositoryConfiguration(Func<Uri> getSiteRoot, string storageRoot)
    {
        StorageRoot = storageRoot;
        _getSiteRoot = getSiteRoot;
    }

    private string GetDefaultCacheDirectory()
    {
        var defaultCacheDirectory = Path.Combine(Path.GetTempPath(), "spk-repository");
        if (CacheName is not null)
            defaultCacheDirectory = Path.Combine(defaultCacheDirectory, CacheName);
        return defaultCacheDirectory;
    }
}
