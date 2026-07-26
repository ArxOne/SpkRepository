namespace ArxOne.Synology;

using System.Collections.Generic;

public class SpkRepositoryCache
{
    public SpkRepositoryPackageInformation[] Packages { get; set; } = [];

    public Dictionary<string, byte[]> Thumbnails { get; set; } = [];
}
