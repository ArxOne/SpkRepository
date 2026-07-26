
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ArxOne.Synology;

using System.Collections.Immutable;

public partial record SpkRepositoryPackageInformation
{
    [GeneratedRegex(@"\[([^\[\]]+)\]")]
    private static partial Regex ArchitecturesRegex();

    [JsonIgnore] public ImmutableDictionary<string, object?> Info { get; init; } = ImmutableDictionary<string, object?>.Empty;

    #region Serialization garbage (or failure)

    [JsonPropertyName("Info")]
    public ImmutableDictionary<string, object> SerializableInfo
    {
        get { return Info; }
        init
        {
            Info = value.ToImmutableDictionary(kv => kv.Key, kv => GetJsonValue((JsonElement)kv.Value));
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

    public ImmutableDictionary<string, string> Thumbnails { get; init; } = ImmutableDictionary<string, string>.Empty;

    public string LocalPath { get; init; } = "";
    public string DownloadPath { get; init; } = "";
    public string OsMinVer { get; init; } = "";

    public SpkVersion? OsMinimumVersion => SpkVersion.TryParse(OsMinVer);

    public ImmutableArray<string> Architectures { get; init; }

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

    protected SpkRepositoryPackageInformation() { }

    public SpkRepositoryPackageInformation(string localPath, string downloadPath, string osMinVer)
    {
        LocalPath = localPath;
        DownloadPath = downloadPath;
        OsMinVer = osMinVer;
        Architectures = GetArchitectures();
    }

    private ImmutableArray<string> GetArchitectures()
    {
        var filename = Path.GetFileName(LocalPath);
        if (filename.Contains("noarch"))
        {
            return ["noarch"];
        }
        var match = ArchitecturesRegex().Match(filename);

        if (!match.Success)
            return [];

        var platformsString = match.Groups[1].Value;
        return [.. platformsString.Split('-')];
    }

    public SpkRepositoryPackage GetPackage(string? language, Uri siteRoot, string distributionDirectory)
    {
        distributionDirectory = distributionDirectory.TrimEnd('/');
        return new SpkRepositoryPackage(Info, language)
        {
            Link = new Uri(siteRoot, DownloadPath),
            Thumbnails = Thumbnails.Values.Select(t => new Uri(siteRoot, $"{distributionDirectory}/thumbnails/{t}")).ToArray(),
        };
    }
}
