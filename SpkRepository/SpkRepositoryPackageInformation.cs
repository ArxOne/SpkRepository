
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArxOne.Synology;

public class SpkRepositoryPackageInformation
{
    [JsonIgnore]
    public Dictionary<string, object?> Info { get; set; }

    #region Serialization garbage (or failure)

    [JsonPropertyName("Info")]
    public Dictionary<string, object> SerializableInfo
    {
        get { return Info; }
        set
        {
            Info = value.ToDictionary(kv => kv.Key, kv => GetJsonValue((JsonElement)kv.Value));
        }
    }

    private static object? GetJsonValue(JsonElement jsonElement)
    {
        return jsonElement.ValueKind switch
        {
            //JsonValueKind.Undefined => expr,
            //JsonValueKind.Object => jsonElement.GetValue<IDictionary<string, object>>(),
            JsonValueKind.Array => jsonElement.EnumerateArray().Select(GetJsonValue).ToArray(),
            JsonValueKind.String => jsonElement.GetString(),
            JsonValueKind.Number => jsonElement.GetInt32(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    #endregion

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

    public SpkRepositoryPackage GetPackage(string? language)
    {
        return new SpkRepositoryPackage(Info, language);
    }
}
