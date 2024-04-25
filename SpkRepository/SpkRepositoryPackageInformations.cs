using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ArxOne.Synology.Utility;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ArxOne.Synology;

public class SpkRepositoryPackageInformations
{
    private const string NoArchitecture = "noarch";
    private readonly Dictionary<SpkRepositoryPackageInformationKey, SpkRepositoryPackageInformation?> _informations = [];

    private readonly IReadOnlyCollection<string> _architectures;

    public SpkRepositoryPackageInformations(IReadOnlyCollection<SpkRepositoryPackageInformation> packageInformations)
    {
        // VERY suboptimal, TODO rewrite
        var packageInformationsArray = packageInformations.OrderByDescending(p => p.Version).ToImmutableArray();
        var osMajors = packageInformationsArray.Select(i => i.OsMinimumVersion.Feature.Major).Distinct();
        _architectures = packageInformations.SelectMany(x => x.Architectures).Distinct().ToImmutableArray();

        foreach (var architecture in _architectures)
            foreach (var beta in new[] { false, true })
                foreach (var osMajor in osMajors)
                    _informations[new SpkRepositoryPackageInformationKey(beta, osMajor, architecture)] = packageInformationsArray.FirstOrDefault(p => (beta || !p.Beta) && p.OsMinimumVersion.Feature.Major == osMajor);
    }

    public SpkRepositoryPackageInformation? Get(bool beta, int majorVersion, string architecture)
    {
        if (_architectures.Count == 1 && _architectures.Contains(NoArchitecture, StringComparer.CurrentCultureIgnoreCase))
            architecture = NoArchitecture;
        Console.WriteLine($" Get SpkRepositoryPackageInformation : beta={beta}; majorVersion={majorVersion}; arch={architecture}");
        var key = new SpkRepositoryPackageInformationKey(beta, Math.Min(7, majorVersion), architecture);
        var information = _informations.TryGetOrDefault(key);
        var join = information?.Architectures is not null ? string.Join(" - ", information.Architectures) : "";
        Console.WriteLine($"Return SpkRepositoryPackageInformation : beta={information?.Beta}; majorVersion={information?.OsMinVer}; arch={join}");
        return information;
    }
}
