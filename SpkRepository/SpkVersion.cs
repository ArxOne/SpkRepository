namespace ArxOne.Synology;

public sealed class SpkVersion : IComparable<SpkVersion>
{
    public Version Feature { get; }
    public int? Build { get; }

    public static SpkVersion? TryParse(string literalVersion)
    {
        var split = literalVersion.Split('-', 2);
        if (!Version.TryParse(split[0], out var version))
            return null;
        if (split.Length == 1)
            return new SpkVersion(version);
        if (!int.TryParse(split[1], out var build))
            return null;
        return new SpkVersion(version, build);
    }

    public SpkVersion(Version feature, int? build = null)
    {
        Feature = feature;
        Build = build;
    }

    public int CompareTo(SpkVersion? other)
    {
        if (other is null)
            return 1;
        var r = Feature.CompareTo(other.Feature);
        if (r != 0)
            return r;
        return (Build ?? -1).CompareTo(other.Build ?? -1);
    }

    public bool Equals(SpkVersion? other) => CompareTo(other) == 0;

    public override bool Equals(object? obj) => Equals(obj as SpkVersion);

    public override int GetHashCode() => Feature.GetHashCode() ^ Build.GetHashCode();
}
