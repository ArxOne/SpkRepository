using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using ArxOne.Synology.Utility;

namespace ArxOne.Synology;

public class SpkRepository
{
    public string DistributionDirectory { get; }

    private readonly SpkRepositoryConfiguration _configuration;
    private readonly IReadOnlyCollection<SpkRepositorySource> _sources;
    private readonly string[] _gpgPublicKeys;

    private (IReadOnlyCollection<SpkRepositoryPackageInformations> Packages, IReadOnlyDictionary<string, byte[]> Thumbnails)? _packagesAndThumbnails;
    private (IReadOnlyCollection<SpkRepositoryPackageInformations> Packages, IReadOnlyDictionary<string, byte[]> Thumbnails) PackagesAndThumbnails => _packagesAndThumbnails ??= GetPackages(_sources);
    private IReadOnlyCollection<SpkRepositoryPackageInformations> Packages => PackagesAndThumbnails.Packages;
    private IReadOnlyDictionary<string, byte[]> Thumbnails => PackagesAndThumbnails.Thumbnails;

    public SpkRepository(SpkRepositoryConfiguration configuration, string distributionDirectory, IEnumerable<SpkRepositorySource> sources, params string[] gpgPublicKeyPaths)
    {
        DistributionDirectory = distributionDirectory;
        _configuration = configuration;
        _sources = sources.ToImmutableArray();
        _gpgPublicKeys = gpgPublicKeyPaths.Select(s => File.ReadAllText(s).Replace("\r", "")).ToArray();
    }

    public void Reload()
    {
        foreach (var source in _sources)
            source.Cache = null;
        _packagesAndThumbnails = null;
    }

    public IEnumerable<(string Path, Delegate? Handler)> GetRoutes(Func<byte[], object> getPng)
    {
        Console.WriteLine($"{Packages.Count} SPK packages");
        yield return (DistributionDirectory, delegate (string unique, string? language, string? package_update_channel, int major, string arch)
                {
                    var siteRoot = _configuration.SiteRoot;
                    var beta = string.Equals(package_update_channel, "beta", StringComparison.InvariantCultureIgnoreCase);
                    var spkRepositoryPackages = Packages.Select(p => p.Get(beta, major, arch)?.GetPackage(language, siteRoot, DistributionDirectory)).Where(p => p is not null);
                    return new Dictionary<string, object>
                    {
                        { "packages", spkRepositoryPackages },
                        { "keyrings", _gpgPublicKeys }
                    };
                }
        );
        yield return (DistributionDirectory.TrimEnd('/') + "/thumbnails/{thumbnail}", delegate (string thumbnail)
                {
                    return getPng(Thumbnails.TryGetOrDefault(thumbnail));
                }
        );
    }

    private (IReadOnlyCollection<SpkRepositoryPackageInformations> Packages, IReadOnlyDictionary<string, byte[]> Thumbnails) GetPackages(IEnumerable<SpkRepositorySource> sources)
    {
        var (packageInformations, thumbnails) = ReadPackageInformations(sources);
        var packagesByName = from p in packageInformations.Values
                             let version = p.Version
                             where version is not null
                             orderby version descending
                             let packageName = p.Package
                             where packageName is not null
                             group p by packageName
            into g
                             select g;
        return (packagesByName.Select(p => new SpkRepositoryPackageInformations(p.ToImmutableArray())).ToImmutableArray(), thumbnails);
    }

    private (IDictionary<string, SpkRepositoryPackageInformation> Packages, IReadOnlyDictionary<string, byte[]> Thumbnails) ReadPackageInformations(IEnumerable<SpkRepositorySource> sources)
    {
        var packageInformations = new Dictionary<string, SpkRepositoryPackageInformation>();
        var thumbnails = new Dictionary<string, byte[]>();
        foreach (var source in sources)
        {
            var (sourcePackageInformations, sourceThumbnails) = ReadPackageInformations(source);
            foreach (var sourcePackageInformation in sourcePackageInformations)
                packageInformations[sourcePackageInformation.LocalPath] = sourcePackageInformation;
            foreach (var sourceThumbnail in sourceThumbnails)
                thumbnails[sourceThumbnail.Key] = sourceThumbnail.Value;
        }
        return (packageInformations, thumbnails);
    }

    private string? GetCacheFilePath(SpkRepositorySource source)
    {
        var cacheDirectory = _configuration.CacheDirectory;
        if (cacheDirectory is null)
            return null;
        var root = source.SourceRelativeDirectory.Trim('/').Replace('/', '-').Replace('\\', '-');
        if (!string.IsNullOrEmpty(root))
            cacheDirectory = Path.Combine(cacheDirectory, root);
        return cacheDirectory + ".json";
    }

    private SpkRepositoryCache LoadPackageCache(SpkRepositorySource source)
    {
        if (source.Cache is not null)
            return source.Cache;
        var cacheFilePath = GetCacheFilePath(source);
        Console.WriteLine($"SPK cache is located at {cacheFilePath}");
        if (cacheFilePath is null)
            return new();
        if (!File.Exists(cacheFilePath))
            return new();
        using var cacheReader = File.OpenRead(cacheFilePath);
        try
        {
            return JsonSerializer.Deserialize<SpkRepositoryCache>(cacheReader);
        }
        catch
        {
            return new();
        }
    }

    private void SavePackageCache(SpkRepositorySource source, SpkRepositoryCache repositoryCache)
    {
        source.Cache = repositoryCache;
        var cacheFilePath = GetCacheFilePath(source);
        if (cacheFilePath is null)
            return;
        var cacheDirectory = Path.GetDirectoryName(cacheFilePath);
        if (!Directory.Exists(cacheDirectory))
            Directory.CreateDirectory(cacheDirectory);
        using var cacheWriter = File.Create(cacheFilePath);
        JsonSerializer.Serialize(cacheWriter, repositoryCache);
    }

    private (IEnumerable<SpkRepositoryPackageInformation> Packages, IReadOnlyDictionary<string, byte[]> Thumbnails) ReadPackageInformations(SpkRepositorySource source)
    {
        var repositoryCache = LoadPackageCache(source);
        var packageInformations = repositoryCache.Packages.ToDictionary(p => p.LocalPath);
        var thumbnailsReferencesCount = repositoryCache.Thumbnails.ToDictionary(kv => kv.Key, _ => 0);
        var removedPackagesInformation = packageInformations.Keys.ToHashSet();
        var spkFiles = Directory.Exists(source.SourceRelativeDirectory) ? Directory.GetFiles(source.SourceRelativeDirectory, "*.spk") : [];
        var hasNew = FetchSpkInformations(source, spkFiles, packageInformations, removedPackagesInformation, repositoryCache, thumbnailsReferencesCount);

        SaveOrRemoveInformations(source, hasNew, removedPackagesInformation, thumbnailsReferencesCount, repositoryCache, packageInformations);

        return (repositoryCache.Packages, repositoryCache.Thumbnails);
    }

    private bool FetchSpkInformations(SpkRepositorySource source, IEnumerable<string> spkFiles, IDictionary<string, SpkRepositoryPackageInformation> packageInformations,
        ICollection<string> removedPackagesInformation, SpkRepositoryCache repositoryCache, Dictionary<string, int> thumbnailsReferencesCount)
    {
        var hasNew = false;
        foreach (var spkFile in spkFiles)
        {
            if (packageInformations.TryGetValue(spkFile, out var packageInformation))
                removedPackagesInformation.Remove(spkFile);
            else
            {
                if (ReadSpkFile(source, spkFile, repositoryCache, packageInformations, ref packageInformation, ref hasNew))
                    continue;
            }

            if (packageInformation is null)
                continue;
            foreach (var thumbnailKey in packageInformation.Thumbnails.Keys)
                thumbnailsReferencesCount[thumbnailKey] = thumbnailsReferencesCount.TryGetOrDefault(thumbnailKey) + 1;
        }
        return hasNew;
    }

    private void SaveOrRemoveInformations(SpkRepositorySource source, bool hasNew, HashSet<string> removedPackagesInformation, Dictionary<string, int> thumbnailsReferencesCount,
        SpkRepositoryCache repositoryCache, Dictionary<string, SpkRepositoryPackageInformation> packageInformations)
    {
        if (!hasNew && removedPackagesInformation.Count <= 0)
            return;
        var unusedThumbnails = thumbnailsReferencesCount.Where(kv => kv.Value == 0).Select(kv => kv.Key);
        foreach (var unusedThumbnail in unusedThumbnails)
            repositoryCache.Thumbnails.Remove(unusedThumbnail);
        foreach (var removedPackageInformation in removedPackagesInformation)
            packageInformations.Remove(removedPackageInformation);
        repositoryCache.Packages = packageInformations.Values.ToArray();
        SavePackageCache(source, repositoryCache);
    }

    private bool ReadSpkFile(SpkRepositorySource source, string spkFile, SpkRepositoryCache repositoryCache, IDictionary<string,
            SpkRepositoryPackageInformation> packageInformations, ref SpkRepositoryPackageInformation? packageInformation, ref bool hasNew)
    {
        try
        {
            using var spkStream = File.OpenRead(spkFile);
            var (info, icons) = source.ReadPackageInfo(spkStream);
            if (info is null)
                return true;
            var osMinVer = info.TryGetOrDefault("os_min_ver") as string ?? info.TryGetOrDefault("firmware") as string;
            if (osMinVer is null)
                return true;

            var thumbnailsId = icons.ToDictionary(
                i => Convert.ToHexString(MD5.HashData(i.Value)).ToLower() + ".png",
                kv => (Name: kv.Key, Data: kv.Value));
            foreach (var thumbnail in thumbnailsId)
                repositoryCache.Thumbnails[thumbnail.Key] = thumbnail.Value.Data;
            packageInformation = new SpkRepositoryPackageInformation(
                spkFile,
                "/" + GetPath(GetPathParts(spkFile).Skip(GetPathParts(_configuration.StorageRoot).Length), '/'),
                osMinVer
            )
            {
                Info = info.ToDictionary(),
                Thumbnails = thumbnailsId.ToDictionary(kv => kv.Value.Name, kv => kv.Key)
            };
            packageInformations[spkFile] = packageInformation;
            hasNew = true;
        }
        catch (FormatException)
        {
        }

        return false;
    }

    private static string[] GetPathParts(string s) => s.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
    private static string GetPath(IEnumerable<string> s, char separator) => string.Join(separator, s);
}
