using System;
using System.IO;

namespace ArxOne.Synology;

public class SpkRepositoryConfiguration
{
    public string StorageRoot { get; }

    private readonly Func<Uri> _getSiteRoot;
    public Uri SiteRoot => _getSiteRoot();

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
        return Path.Combine(Path.GetTempPath(), "spk-repository");
    }
}
