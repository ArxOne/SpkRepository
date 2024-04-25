using System;

namespace ArxOne.Synology;

public class SpkRepositoryPackageInformationKey(bool beta, int major, string architecture)
{
    private bool Beta { get; } = beta;

    private int Major { get; } = major;

    private string Architecture { get; } = architecture;

    public override bool Equals(object? obj)
    {
        return obj is SpkRepositoryPackageInformationKey informationKey && Equals(informationKey);
    }

    private bool Equals(SpkRepositoryPackageInformationKey obj)
    {
        return Beta == obj.Beta && Major == obj.Major && Architecture.Equals(obj.Architecture, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Beta, Major, Architecture);
    }
}