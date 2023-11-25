namespace ArxOne.Synology;

internal class ByteArrayComparer : IEqualityComparer<byte[]>
{
    public static readonly ByteArrayComparer Default = new();

    public bool Equals(byte[]? x, byte[]? y)
    {
        if (x is null)
            return y is null;
        if (y is null)
            return false;
        ReadOnlySpan<byte> xs = x;
        return xs.SequenceEqual(y);
    }

    public int GetHashCode(byte[] obj)
    {
        var h = 0;
        for (int i = Math.Min(7, obj.Length - 1); i >= 0; i--)
            h = (h << 4) ^ obj[i];
        return h;
    }
}