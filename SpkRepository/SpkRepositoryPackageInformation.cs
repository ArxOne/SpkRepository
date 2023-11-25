
using System.Text.Json.Serialization;

namespace ArxOne.Synology;

public class SpkRepositoryPackageInformation
{
    public Dictionary<string, object> Info { get; set; }
  
    public Dictionary<string, string> Thumbnails { get; set; } = new();

    public string LocalPath { get; set; }
    public string DownloadPath { get; set; }

    public string? Package
    {
        get
        {
            Info.TryGetValue("package", out var package);
            return package as string;
        }
    }

    public SpkVersion? Version
    {
        get
        {
            if (!Info.TryGetValue("version", out var literalVersion))
                return null;
            if (literalVersion is not string stringVersion)
                return null;
            return SpkVersion.TryParse(stringVersion);
        }
    }

    public bool Beta
    {
        get
        {
            if (!Info.TryGetValue("beta", out var literalBeta))
                return false;
            if (literalBeta is int intBeta)
                return intBeta != 0;
            if (literalBeta is not string stringBeta)
                return false;
            return new SpkBool(stringBeta);
        }
    }
}
