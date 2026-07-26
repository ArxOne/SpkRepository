
namespace ArxOne.Synology;

using System.IO;
using System.Collections.Immutable;

public delegate (ImmutableDictionary<string, object>? Info, ImmutableDictionary<string, byte[]> Icons) ReadPackageInfo(Stream spkStream);

public record SpkRepositorySource(string SourceRelativeDirectory, ReadPackageInfo ReadPackageInfo)
{
    internal SpkRepositoryCache? Cache { get; set; }
}
