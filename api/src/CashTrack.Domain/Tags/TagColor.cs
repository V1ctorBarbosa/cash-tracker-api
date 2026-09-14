using System.Text.RegularExpressions;

namespace CashTrack.Domain.Tags;

/// <summary>
/// A hex color (e.g. "#3B82F6") used consistently for one tag across every chart it appears in
/// (Section 20 of the implementation document).
/// </summary>
public readonly partial record struct TagColor
{
    public string Value { get; }

    public TagColor(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !HexColorPattern().IsMatch(value))
        {
            throw new ArgumentException("Color must be a hex value like #3B82F6.", nameof(value));
        }

        Value = value.ToUpperInvariant();
    }

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColorPattern();
}
