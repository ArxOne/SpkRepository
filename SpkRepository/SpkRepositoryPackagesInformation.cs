
namespace ArxOne.Synology;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Utility;

public class SpkRepositoryPackagesInformation
{
    private const string NoArchitecture = "noarch";
    private readonly Dictionary<SpkRepositoryPackageInformationKey, SpkRepositoryPackageInformation?> _information = [];

    private readonly IReadOnlyCollection<string> _architectures;

    public SpkRepositoryPackagesInformation(IReadOnlyCollection<SpkRepositoryPackageInformation> packagesInformation)
    {
        // VERY suboptimal, TODO rewrite
        var packagesInformationArray = packagesInformation.OrderByDescending(p => p.Version).ToImmutableArray();
        var osMajors = packagesInformationArray.Select(i => i.OsMinimumVersion.Feature.Major).Distinct().ToImmutableArray();
        _architectures = [..packagesInformation.SelectMany(x => x.Architectures).Distinct()];

        foreach (var architecture in _architectures)
            foreach (var beta in new[] { false, true })
                foreach (var osMajor in osMajors)
                    _information[new SpkRepositoryPackageInformationKey(beta, osMajor, architecture)] = packagesInformationArray.FirstOrDefault(p => (beta || !p.Beta) && p.OsMinimumVersion.Feature.Major == osMajor && p.Architectures.Contains(architecture));
    }

    public SpkRepositoryPackageInformation? Get(bool beta, int majorVersion, string architecture)
    {
        if (_architectures.Count == 1 && _architectures.Contains(NoArchitecture, StringComparer.CurrentCultureIgnoreCase))
            architecture = NoArchitecture;
        var key = new SpkRepositoryPackageInformationKey(beta, Math.Min(7, majorVersion), architecture);
        return _information.TryGetOrDefault(key);
    }
}
