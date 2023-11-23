namespace ArxOne.Synology;

public delegate (IReadOnlyDictionary<string, object>? Info, IReadOnlyDictionary<string, byte[]> Icons) ReadPackageInfo(Stream spkStream);

public record SpkRepositorySource(string SourceRelativeDirectory, ReadPackageInfo ReadPackageInfo);
