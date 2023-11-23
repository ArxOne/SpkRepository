using System.Text.Json.Serialization;

namespace ArxOne.Synology;

public class SpkRepositoryPackage
{
    [JsonPropertyName("link")] public Uri Link { get; set; }
    [JsonPropertyName("thumbnail")] public Uri[] Thumbnails { get; set; }
    [JsonPropertyName("package")] public string Package { get; set; }
    [JsonPropertyName("version")] public string Version { get; set; }
    [JsonPropertyName("dname")] public string DisplayName { get; set; }
    [JsonPropertyName("desc")] public string Description { get; set; }
    [JsonPropertyName("maintainer")] public string Maintainer { get; set; }
    [JsonPropertyName("changelog")] public string ChangeLogHtml { get; set; }
    [JsonPropertyName("deppkgs")] public string? DependenciesPackages { get; set; }
    [JsonPropertyName("conflictpkgs")] public string? ConflictingPackages { get; set; }
    [JsonPropertyName("qinst")] public bool QInstall { get; set; } = true;
    [JsonPropertyName("qstart")] public bool QStart { get; set; } = true;
    [JsonPropertyName("qupgrade")] public bool QUpgrade { get; set; } = true;
    [JsonPropertyName("thirdparty")] public bool ThirdParty { get; set; } = true;

    [JsonPropertyName("snapshot")] public Uri[] Snapshots { get; set; } = Array.Empty<Uri>();
    [JsonPropertyName("maintainer_url")] public Uri? MaintainerUrl { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("price")] public int? Price { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("download_count")] public int DownloadCount { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("recent_download_count")] public int? RecentDownloadCount { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("size")] public long? Size { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("md5")] public string? Md5Hex { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("depsers")] public string? StartDepServices { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("start")] public bool Start { get; set; } = true;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("distributor")] public string? Distributor { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("distributor_url")] public Uri? DistributorUrl { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("support_url")] public Uri? SupportUrl { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("category")] public int Category { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("subcategory")] public int SubCategory { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("type")] public int Type { get; set; }
    [JsonPropertyName("silent_install")] public bool SilentInstall { get; set; }
    [JsonPropertyName("silent_uninstall")] public bool SilentStart { get; set; }
    [JsonPropertyName("silent_upgrade")] public bool SilentUpgrade { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("auto_upgrade_from")] public string? AutoUpgradeFrom { get; set; }

    public SpkRepositoryPackage()
    {
    }

    public SpkRepositoryPackage(IReadOnlyDictionary<string, object> info)
    {
        throw new NotImplementedException();
    }
}
