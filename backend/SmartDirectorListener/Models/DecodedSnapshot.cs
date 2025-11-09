using System.Collections.Generic;
using System.Linq;

namespace Backend.SmartDirectorListener.Models;

public class DecodedSnapshot
{
    public string Raw { get; init; } = string.Empty;

    public string PlayerA1 { get; init; } = string.Empty;
    public string PlayerA2 { get; init; } = string.Empty;
    public string PlayerB1 { get; init; } = string.Empty;
    public string PlayerB2 { get; init; } = string.Empty;

    public string TeamA => string.IsNullOrWhiteSpace(PlayerA2)
        ? PlayerA1
        : $"{PlayerA1} / {PlayerA2}";

    public string TeamB => string.IsNullOrWhiteSpace(PlayerB2)
        ? PlayerB1
        : $"{PlayerB1} / {PlayerB2}";

    public string PointsA { get; init; } = string.Empty;
    public string PointsB { get; init; } = string.Empty;

    public string Set1A { get; init; } = string.Empty;
    public string Set1B { get; init; } = string.Empty;
    public string Set2A { get; init; } = string.Empty;
    public string Set2B { get; init; } = string.Empty;
    public string Set3A { get; init; } = string.Empty;
    public string Set3B { get; init; } = string.Empty;

    public string Server { get; init; } = string.Empty;
    public string BestOf { get; init; } = string.Empty;
    public string MatchClock { get; init; } = string.Empty;

    private IReadOnlyList<string>? _sets;

    public IReadOnlyList<string> Sets => _sets ??= BuildSets();

    public string PointsDisplay
    {
        get
        {
            var left = (PointsA ?? string.Empty).Trim();
            var right = (PointsB ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(left) && string.IsNullOrEmpty(right))
            {
                return string.Empty;
            }

            return $"{left}-{right}";
        }
    }

    private IReadOnlyList<string> BuildSets()
    {
        static string FormatSet(string left, string right)
        {
            var cleanLeft = (left ?? string.Empty).Trim();
            var cleanRight = (right ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(cleanLeft) && string.IsNullOrEmpty(cleanRight))
            {
                return string.Empty;
            }

            return $"{cleanLeft}-{cleanRight}".Trim();
        }

        var values = new List<string>(3)
        {
            FormatSet(Set1A, Set1B),
            FormatSet(Set2A, Set2B),
            FormatSet(Set3A, Set3B)
        };

        return values.Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();
    }
}