﻿using System.Collections.Immutable;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;

namespace ArxOne.Synology;

public class SpkRepository
{
    public string WebRoot { get; }

    private readonly SpkRepositoryConfiguration _configuration;
    private readonly IReadOnlyCollection<SpkRepositorySource> _sources;

    public SpkRepository(SpkRepositoryConfiguration configuration, string webRoot, IEnumerable<SpkRepositorySource> sources)
    {
        WebRoot = webRoot;
        _configuration = configuration;
        _sources = sources.ToImmutableArray();
    }

    public IEnumerable<(string Path, Delegate? Handler)> GetRoutes()
    {
        var packages = GetPackages(_sources, false);
        yield return (WebRoot, delegate ()
        {
            return new Dictionary<string, object>
            {
                { "packages", packages.Select(p=>p.Info) }
            };
        }
        );
    }

    private IEnumerable<SpkRepositoryPackageInformation> GetPackages(IEnumerable<SpkRepositorySource> sources, bool includeBeta)
    {
        var packagesByName =
            from information in ReadPackageInformations(sources).Values
            let package = information.Package
            where package is not null
            where includeBeta || !information.Beta
            group information by package
            into packages
            let latestPackageVersion =
                (from packageVersions in packages
                 let version = packageVersions.Version
                 where version is not null
                 orderby version descending
                 select packageVersions).First()
            select latestPackageVersion;
        return packagesByName.ToImmutableArray();
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

    private DataContractJsonSerializer GetSerializer(Type type)
    {
        return new DataContractJsonSerializer(type, new DataContractJsonSerializerSettings { });
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
        return (SpkRepositoryCache)GetSerializer(typeof(SpkRepositoryCache)).ReadObject(cacheReader);
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
        GetSerializer(typeof(SpkRepositoryCache)).WriteObject(cacheWriter, repositoryCache);
    }

    private IEnumerable<SpkRepositoryPackageInformation> ReadPackageInformations(SpkRepositorySource source)
    {
        var repositoryCache = LoadPackageCache(source);
        var packageInformations = repositoryCache.Packages.ToDictionary(p => p.LocalPath);
        var thumbnailsReferencesCount = repositoryCache.Thumbnails.ToDictionary(kv => kv.Key, kv => 0);
        var removedPackageInformation = packageInformations.Keys.ToHashSet();
        var spkFiles = Directory.Exists(source.SourceRelativeDirectory) ? Directory.GetFiles(source.SourceRelativeDirectory, "*.spk") : Array.Empty<string>();
        bool hasNew = false;
        foreach (var spkFile in spkFiles)
        {
            if (packageInformations.TryGetValue(spkFile, out var packageInformation))
            {
                removedPackageInformation.Remove(spkFile);
            }
            else
            {
                try
                {
                    using var spkStream = File.OpenRead(spkFile);
                    var (info, icons) = source.ReadPackageInfo(spkStream);
                    if (info is null)
                        continue;

                    var thumbnailsId = icons.ToDictionary(
                        i => Convert.ToBase64String(SHA1.HashData(i.Value)).TrimEnd('=') + ".png",
                        kv => (Name: kv.Key, Data: kv.Value));
                    foreach (var thumbnail in thumbnailsId)
                        repositoryCache.Thumbnails[thumbnail.Key] = thumbnail.Value.Data;
                    packageInformation = new SpkRepositoryPackageInformation
                    {
                        LocalPath = spkFile,
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
                {
                    thumbnailsReferencesCount.TryGetValue(thumbnailKey, out var count);
                    thumbnailsReferencesCount[thumbnailKey] = count + 1;
                }
            }
        }

        if (hasNew || removedPackageInformation.Count > 0)
        {
            var unusedThumbnails = thumbnailsReferencesCount.Where(kv => kv.Value == 0).Select(kv => kv.Key);
            foreach (var unusedThumbnail in unusedThumbnails)
                repositoryCache.Thumbnails.Remove(unusedThumbnail);
            repositoryCache.Packages = packageInformations.Values.ToArray();
            SavePackageInformations(source, repositoryCache);
        }

        return packageInformations.Values;
    }
}