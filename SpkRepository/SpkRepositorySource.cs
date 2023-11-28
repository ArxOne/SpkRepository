using System.Collections.Generic;
using System.IO;

namespace ArxOne.Synology;

public delegate (IReadOnlyDictionary<string, object>? Info, IReadOnlyDictionary<string, byte[]> Icons) ReadPackageInfo(Stream spkStream);

public record SpkRepositorySource(string SourceRelativeDirectory, ReadPackageInfo ReadPackageInfo)
{
    internal SpkRepositoryCache? Cache { get; set; }
}
