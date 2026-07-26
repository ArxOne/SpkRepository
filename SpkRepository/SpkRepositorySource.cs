
namespace ArxOne.Synology;

using System.IO;
using System.Collections.Immutable;

public delegate (ImmutableDictionary<string, object>? Info, ImmutableDictionary<string, byte[]> Icons) ReadPackageInfo(Stream spkStream);

public record SpkRepositorySource
{
    internal SpkRepositoryCache? Cache { get; set; }
    public string SourceRelativeDirectory { get; init; }
    public ReadPackageInfo ReadPackageInfo { get; init; }
    public string? SourceID { get; init; }

    public SpkRepositorySource(string sourceRelativeDirectory, ReadPackageInfo readPackageInfo, string? sourceID = null)
    {
        SourceRelativeDirectory = sourceRelativeDirectory;
        ReadPackageInfo = readPackageInfo;
        SourceID = sourceID;
    }
}
