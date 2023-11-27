using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ArxOne.Synology.Utility;

namespace ArxOne.Synology;

public class SpkRepositoryPackageInformations
{
    private readonly Dictionary<(bool Beta, int Major), SpkRepositoryPackageInformation?> _informations = new();

    public SpkRepositoryPackageInformations(IEnumerable<SpkRepositoryPackageInformation> packageInformations)
    {
        // VERY suboptimal, TODO rewrite
        var packageInformationsArray = packageInformations.OrderByDescending(p => p.Version).ToImmutableArray();
        var osMajors = packageInformationsArray.Select(i => i.OsMinimumVersion.Feature.Major).Distinct();
        foreach (var beta in new[] { false, true })
            foreach (var osMajor in osMajors)
                _informations[(beta, osMajor)] = packageInformationsArray.FirstOrDefault(p => p.Beta == beta && p.OsMinimumVersion.Feature.Major == osMajor);
    }

    public SpkRepositoryPackageInformation? Get(bool beta, int majorVersion)
    {
        var information = _informations.TryGetOrDefault((beta, Math.Min(7, majorVersion)));
        return information;
    }
}
