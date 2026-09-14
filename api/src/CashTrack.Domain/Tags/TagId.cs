namespace CashTrack.Domain.Tags;

public readonly record struct TagId
{
    public string Value { get; }

    public TagId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Tag id cannot be empty.", nameof(value));
        }

        Value = value;
    }
}
