using System;
using System.Collections.Generic;

namespace ArxOne.Synology;

public class SpkRepositoryCache
{
    public SpkRepositoryPackageInformation[] Packages { get; set; } = Array.Empty<SpkRepositoryPackageInformation>();

    public Dictionary<string, byte[]> Thumbnails { get; set; } = new();
}
