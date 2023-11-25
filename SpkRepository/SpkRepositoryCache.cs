using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace ArxOne.Synology;

public class SpkRepositoryCache
{
    public SpkRepositoryPackageInformation[] Packages { get; set; } = Array.Empty<SpkRepositoryPackageInformation>();

    [JsonIgnore]
    public Dictionary<string, byte[]> Thumbnails { get; set; } = new();

    [JsonPropertyName("thumbnails")]
    public Dictionary<string, object> SerializableThumbnails
    {
        get { return Thumbnails.ToDictionary(kv => kv.Key, kv => (object) Convert.ToBase64String(kv.Value)); }
        set { Thumbnails = value.ToDictionary(kv => kv.Key, kv => Convert.FromBase64String((string) kv.Value)); }
    }
}
