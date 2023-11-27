﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
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

    public SpkRepository(SpkRepositoryConfiguration configuration, string distributionDirectory, IEnumerable<SpkRepositorySource> sources, params string[] gpgPublicKeyPaths)
    {
        DistributionDirectory = distributionDirectory;
        _configuration = configuration;
        _sources = sources.ToImmutableArray();
        _gpgPublicKeys = gpgPublicKeyPaths.Select(s => File.ReadAllText(s).Replace("\r", "")).ToArray();
    }

    public IEnumerable<(string Path, Delegate? Handler)> GetRoutes()
    {
        var packages = GetPackages(_sources);
        yield return (DistributionDirectory, delegate(string unique, string? language, string? package_update_channel, int major)
                {
                    var siteRoot = _configuration.SiteRoot;
                    var beta = string.Equals(package_update_channel, "beta", StringComparison.InvariantCultureIgnoreCase);
                    return new Dictionary<string, object>
                    {
                        { "packages", packages.Select(p => p.Get(beta, major)?.GetPackage(language, siteRoot)).Where(p => p is not null) },
                        { "keyrings", _gpgPublicKeys }
                    };
                }
            );
    }

    private IEnumerable<SpkRepositoryPackageInformations> GetPackages(IEnumerable<SpkRepositorySource> sources)
    {
        var packageInformations = ReadPackageInformations(sources).Values;
        var packagesByName = from p in packageInformations
                             let version = p.Version
                             where version is not null
                             orderby version descending
                             let packageName = p.Package
                             where packageName is not null
                             group p by packageName
            into g
                             select g;
        return packagesByName.Select(p => new SpkRepositoryPackageInformations(p));
    }

    private IDictionary<string, SpkRepositoryPackageInformation> ReadPackageInformations(IEnumerable<SpkRepositorySource> sources)
    {
        var packageInformations = new Dictionary<string, SpkRepositoryPackageInformation>();
        foreach (var source in sources)
        {
            var sourcePackageInformations = ReadPackageInformations(source);
            foreach (var sourcePackageInformation in sourcePackageInformations)
                packageInformations[sourcePackageInformation.LocalPath] = sourcePackageInformation;
        }
        return packageInformations;
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
        var cacheFilePath = GetCacheFilePath(source);
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

    private void SavePackageInformations(SpkRepositorySource source, SpkRepositoryCache repositoryCache)
    {
        var cacheFilePath = GetCacheFilePath(source);
        if (cacheFilePath is null)
            return;
        var cacheDirectory = Path.GetDirectoryName(cacheFilePath);
        if (!Directory.Exists(cacheDirectory))
            Directory.CreateDirectory(cacheDirectory);
        using var cacheWriter = File.Create(cacheFilePath);
        JsonSerializer.Serialize(cacheWriter, repositoryCache);
    }

    private IEnumerable<SpkRepositoryPackageInformation> ReadPackageInformations(SpkRepositorySource source)
    {
        var repositoryCache = LoadPackageCache(source);
        var packageInformations = repositoryCache.Packages.ToDictionary(p => p.LocalPath);
        var thumbnailsReferencesCount = repositoryCache.Thumbnails.ToDictionary(kv => kv.Key, kv => 0);
        var removedPackagesInformation = packageInformations.Keys.ToHashSet();
        var spkFiles = Directory.Exists(source.SourceRelativeDirectory) ? Directory.GetFiles(source.SourceRelativeDirectory, "*.spk") : Array.Empty<string>();
        bool hasNew = false;
        foreach (var spkFile in spkFiles)
        {
            if (packageInformations.TryGetValue(spkFile, out var packageInformation))
            {
                removedPackagesInformation.Remove(spkFile);
            }
            else
            {
                try
                {
                    using var spkStream = File.OpenRead(spkFile);
                    var (info, icons) = source.ReadPackageInfo(spkStream);
                    if (info is null)
                        continue;
                    var osMinVer = info.TryGetOrDefault("os_min_ver") as string ?? info.TryGetOrDefault("firmware") as string;
                    if (osMinVer is null)
                        continue;

                    var thumbnailsId = icons.ToDictionary(
                        i => Convert.ToHexString(MD5.HashData(i.Value)) + ".png",
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
            }

            if (packageInformation is not null)
            {
                foreach (var thumbnailKey in packageInformation.Thumbnails.Keys)
                    thumbnailsReferencesCount[thumbnailKey] = thumbnailsReferencesCount.TryGetOrDefault(thumbnailKey) + 1;
            }
        }

        if (hasNew || removedPackagesInformation.Count > 0)
        {
            var unusedThumbnails = thumbnailsReferencesCount.Where(kv => kv.Value == 0).Select(kv => kv.Key);
            foreach (var unusedThumbnail in unusedThumbnails)
                repositoryCache.Thumbnails.Remove(unusedThumbnail);
            foreach (var removedPackageInformation in removedPackagesInformation)
                packageInformations.Remove(removedPackageInformation);
            repositoryCache.Packages = packageInformations.Values.ToArray();
            SavePackageInformations(source, repositoryCache);
        }

        return packageInformations.Values;
    }

    private static string[] GetPathParts(string s) => s.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
    private static string GetPath(IEnumerable<string> s, char separator) => string.Join(separator, s);
}
