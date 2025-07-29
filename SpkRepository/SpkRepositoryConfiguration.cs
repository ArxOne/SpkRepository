using System;
using System.IO;

namespace ArxOne.Synology;

public class SpkRepositoryConfiguration
{
    public string StorageRoot { get; }

    private readonly Func<Uri> _getSiteRoot;
    public Uri SiteRoot => _getSiteRoot();

    public string? CacheName { get; set; }

    private bool _cacheDirectorySet;
    private string? _cacheDirectory;
    public string? CacheDirectory
    {
        get { return _cacheDirectorySet ? _cacheDirectory : GetDefaultCacheDirectory(); }
        set
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
