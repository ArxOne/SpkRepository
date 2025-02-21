using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
// ReSharper disable StringLiteralTypo

namespace ArxOne.Synology;

public class SpkRepositoryPackage
{
    // -- required
    [JsonPropertyName("link")] public Uri Link { get; set; }
    [JsonPropertyName("thumbnail")] public Uri[] Thumbnails { get; set; }

    [JsonPropertyName("package")] public string Package { get; set; }
    [JsonPropertyName("version")] public string Version { get; set; }
    [JsonPropertyName("dname")] public string DisplayName { get; set; }
    [JsonPropertyName("desc")] public string Description { get; set; }
    [JsonPropertyName("maintainer")] public string Maintainer { get; set; }
    [JsonPropertyName("changelog")] public string? ChangeLogHtml { get; set; }
    [JsonPropertyName("deppkgs")] public string? DependenciesPackages { get; set; }
    [JsonPropertyName("conflictpkgs")] public string? ConflictingPackages { get; set; }
    [JsonPropertyName("qinst")] public bool QInstall { get; set; } = true;
    [JsonPropertyName("qstart")] public bool QStart { get; set; } = true;
    [JsonPropertyName("qupgrade")] public bool QUpgrade { get; set; } = true;
    [JsonPropertyName("thirdparty")] public bool ThirdParty { get; set; } = true;

    // -- optional
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("md5")] public string? Md5Hex { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("size")] public long? Size { get; set; }

    [JsonPropertyName("snapshot")] public Uri[] Snapshots { get; set; } = [];
    [JsonPropertyName("maintainer_url")] public string? MaintainerUrl { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("price")] public int? Price { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("download_count")] public int DownloadCount { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("recent_download_count")] public int? RecentDownloadCount { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("depsers")] public string? StartDepServices { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("start")] public bool Start { get; set; } = true;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("distributor")] public string? Distributor { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("distributor_url")] public string? DistributorUrl { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("support_url")] public string? SupportUrl { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("category")] public int Category { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("subcategory")] public int SubCategory { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("type")] public int Type { get; set; }
    [JsonPropertyName("silent_install")] public bool SilentInstall { get; set; }
    [JsonPropertyName("silent_uninstall")] public bool SilentUninstall { get; set; }
    [JsonPropertyName("silent_upgrade")] public bool SilentUpgrade { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("auto_upgrade_from")] public string? AutoUpgradeFrom { get; set; }

    public SpkRepositoryPackage()
    {
    }

    public SpkRepositoryPackage(IReadOnlyDictionary<string, object?> info, string? language = null)
    {
        Package = TryGet<string>(info, "package")!;
        Version = TryGet<string>(info, "version")!;
        DisplayName = TryGetLanguage<string>(info, "displayname", language) ?? Package;
        Description = TryGetLanguage<string>(info, "description", language) ?? "";
        QStart = TryGet<bool?>(info, "qstart") ?? true;
        QInstall = TryGet<bool?>(info, "qinst") ?? true;
        QUpgrade = TryGet<bool?>(info, "qupgrade") ?? true;
        Maintainer = TryGet<string>(info, "maintainer") ?? "";
        MaintainerUrl = TryGet<string>(info, "maintainer_url");
        Distributor = TryGet<string>(info, "distributor");
        DistributorUrl = TryGet<string>(info, "distributor_url");
        SupportUrl = TryGet<string>(info, "support_url") ?? "";
        ChangeLogHtml = TryGet<string>(info, "changelog");
        DependenciesPackages = TryGet<string>(info, "install_dep_packages");
        ConflictingPackages = TryGet<string>(info, "install_conflict_packages");
        SilentInstall = TryGet<bool?>(info, "silent_install") ?? false;
        SilentUpgrade = TryGet<bool?>(info, "silent_upgrade") ?? false;
        SilentUninstall = TryGet<bool?>(info, "silent_uninstall") ?? false;
        //          'price' => 0,
        //'download_count' => 0, // Will only display values over 1000, do not display it by default
        //'recent_download_count' => 0,
        //'link' => $pkg->spk_url,
        //'size' => filesize($pkg->spk),
        //'md5' => md5_file($pkg->spk),
        //'thumbnail' => $pkg->thumbnail_url,
        //'snapshot' => $pkg->snapshot_url,
        //'start' => true,
        //'auto_upgrade_from' => $this->ifEmpty($pkg, 'auto_upgrade_from'),
    }

    private static TValue? TryGet<TValue>(IReadOnlyDictionary<string, object?> info, string key)
    {
        if (info.TryGetValue(key, out var value) && value is not null)
            return (TValue?)Convert(value, typeof(TValue));
        return default;
    }

    private static object? Convert(object value, Type targetType)
    {
        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            return Convert(value, targetType.GenericTypeArguments[0]);
        if (targetType == typeof(string))
            return value.ToString();
        if (targetType == typeof(int))
            return System.Convert.ToInt32(value);
        if (targetType == typeof(bool))
        {
            if (value is bool b)
                return b;
            if (value is string s)
                return (bool)new SpkBool(s);
            throw new NotSupportedException($"Can’t convert {value.GetType()} to bool");
        }
        throw new NotSupportedException($"Can’t convert {value.GetType()} to {targetType}");
    }

    private static TValue? TryGetLanguage<TValue>(IReadOnlyDictionary<string, object?> info, string key, string? language = null)
    {
        return TryGet<TValue>(info, $"{key}_{language ?? "eng"}") ?? TryGet<TValue>(info, key);
    }
}
