
using System.Text.Json.Serialization;

namespace ArxOne.Synology;

public class SpkRepositoryPackageInformation
{
    public Dictionary<string, object> Info { get; set; }
    [JsonIgnore]
    public Dictionary<string, byte[]> Thumbnails { get; set; } = new();
    [JsonPropertyName("thumbnails")]

    public Dictionary<string, string> SerializableThumbnails
    {
        get
        {
            return Thumbnails.ToDictionary(kv => kv.Key, kv => Convert.ToBase64String(kv.Value));
        }
        set
        {
            Thumbnails = value.ToDictionary(kv => kv.Key, kv => Convert.FromBase64String(kv.Value));
        }
    }

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
