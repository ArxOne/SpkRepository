using System.IO;

namespace ArxOne.Synology;

public class SpkRepositoryConfiguration
{
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

    public SpkRepositoryConfiguration()
    {
    }

    private string GetDefaultCacheDirectory()
    {
        return Path.Combine(Path.GetTempPath(), "spk-repository");
    }
}
