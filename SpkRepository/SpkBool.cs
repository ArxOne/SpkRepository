namespace ArxOne.Synology;

public class SpkBool
{
    public bool Value { get; }

    public SpkBool(string? literal)
    {
        Value = literal is not null && string.Equals(literal, "yes", StringComparison.InvariantCultureIgnoreCase);
    }

    public SpkBool(bool value)
    {
        Value = value;
    }

    public static implicit operator string(SpkBool b) => b.Value ? "yes" : "no";
    public static implicit operator bool(SpkBool b) => b.Value;
}
