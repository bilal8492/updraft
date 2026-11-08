namespace UpInstall;

public enum UpdateKind { None, Patch, Minor, Major }

/// <summary>Loose version parsing for winget's free-form version strings ("1.2.3", "&lt; 3.14.7", "2024.11.05", "v2.0-beta").</summary>
public static class Versions
{
    /// <summary>The leading numeric parts, or null when the string has none ("Unknown", "latest").</summary>
    public static int[]? Parse(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return null;
        v = v.Trim().TrimStart('<', '>', '=', ' ', 'v', 'V');
        var core = v.Split('-', '+', ' ', '_')[0];
        var nums = new List<int>();
        foreach (var part in core.Split('.'))
        {
            int i = 0;
            while (i < part.Length && char.IsAsciiDigit(part[i])) i++;
            if (i == 0 || !int.TryParse(part[..i], out var n)) break;
            nums.Add(n);
            if (i < part.Length) break;   // "5a": keep the 5, stop there
        }
        return nums.Count > 0 ? nums.ToArray() : null;
    }

    /// <summary>Negative when a &lt; b, positive when a &gt; b, null when either can't be read.</summary>
    public static int? Compare(string? a, string? b)
    {
        var x = Parse(a);
        var y = Parse(b);
        if (x is null || y is null) return null;
        for (int i = 0; i < Math.Max(x.Length, y.Length); i++)
        {
            int p = i < x.Length ? x[i] : 0, q = i < y.Length ? y[i] : 0;
            if (p != q) return p.CompareTo(q);
        }
        return 0;
    }

    /// <summary>Which part of the version an update changes: the first differing number decides.</summary>
    public static UpdateKind Classify(string? from, string? to)
    {
        var x = Parse(from);
        var y = Parse(to);
        if (x is null || y is null) return UpdateKind.None;
        for (int i = 0; i < Math.Max(x.Length, y.Length); i++)
        {
            int p = i < x.Length ? x[i] : 0, q = i < y.Length ? y[i] : 0;
            if (p == q) continue;
            if (q < p) return UpdateKind.None;
            return i switch { 0 => UpdateKind.Major, 1 => UpdateKind.Minor, _ => UpdateKind.Patch };
        }
        return UpdateKind.None;
    }

    /// <summary>A version string that is safe to pass to winget and to put in a generated script.</summary>
    public static bool IsSafe(string v) => v.Length is > 0 and <= 64 && System.Text.RegularExpressions.Regex.IsMatch(v, @"^[0-9A-Za-z][0-9A-Za-z.+\-_]*$");
}
