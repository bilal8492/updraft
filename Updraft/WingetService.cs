using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Updraft;

public static class WingetService
{
    public static async Task<string> RunAsync(params string[] args)
    {
        var psi = new ProcessStartInfo("winget")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Could not start winget");
        var stdout = p.StandardOutput.ReadToEndAsync();
        _ = p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();
        return await stdout;
    }

    public static string Verb(string mode) => mode switch
    {
        "upgrade" => "upgrade",
        "installed" => "uninstall",
        _ => "install",
    };

    /// <summary>
    /// Package ids end up in a PowerShell script, so only allow the characters real winget ids use.
    /// This also rejects truncated ids ending in an ellipsis and anything with shell metacharacters.
    /// </summary>
    public static bool IsSafeId(string id) => Regex.IsMatch(id, @"^\w[\w.+\-]*$");

    public static string Command(Pkg p, string mode)
    {
        var v = Verb(mode);
        var flags = v == "uninstall" ? "" : " --accept-package-agreements --accept-source-agreements";
        return $"winget {v} --id {p.Id} -e{flags}";
    }

    /// <summary>Parses winget's fixed-width table. Column positions come from the header line, so it works in any UI language.</summary>
    public static List<Pkg> Parse(string text, string mode)
    {
        var result = new List<Pkg>();

        // Progress spinners use \r to overwrite themselves: keep the last non-empty segment of each line.
        var lines = text.Split('\n')
            .Select(l => l.Split('\r').LastOrDefault(s => s.Trim().Length > 0) ?? "")
            .ToList();

        int sep = -1;
        for (int i = 1; i < lines.Count; i++)
            if (Regex.IsMatch(lines[i], @"^-{5,}\s*$")) { sep = i; break; }
        if (sep < 1) return result;

        var cols = Regex.Matches(lines[sep - 1], @"\S+(?: \S+)*");
        if (cols.Count < 3) return result;
        var starts = cols.Select(m => m.Index).ToArray();

        string Cut(string line, int n)
        {
            int s = starts[n];
            if (s >= line.Length) return "";
            int e = n + 1 < starts.Length ? Math.Min(starts[n + 1], line.Length) : line.Length;
            return line[s..e].Trim();
        }

        for (int i = sep + 1; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.Trim().Length == 0) break;          // end of table
            if (line.Length <= starts[1]) continue;      // footer such as "3 upgrades available."

            var p = new Pkg { Name = Cut(line, 0), Id = Cut(line, 1), Version = Cut(line, 2) };
            if (mode != "discover" && starts.Length >= 5) p.Available = Cut(line, 3);
            if (starts.Length >= 4) p.Source = Cut(line, starts.Length - 1);
            if (p.Id.Length > 0) result.Add(p);
        }
        return result;
    }

    public static async Task<PkgDetail> ShowAsync(string id)
    {
        var text = await RunAsync("show", "--id", id, "-e", "--accept-source-agreements");

        string Field(string key)
        {
            var m = Regex.Match(text, $@"^\s*{key}:\s*(.+?)\s*$", RegexOptions.Multiline);
            return m.Success ? m.Groups[1].Value : "";
        }

        var d = Regex.Match(text, @"^Description:\s*(.*?)(?=^\S|\z)", RegexOptions.Multiline | RegexOptions.Singleline);
        var desc = d.Success ? Regex.Replace(d.Groups[1].Value, @"\s*\r?\n\s*", " ").Trim() : "";
        return new PkgDetail(desc, Field("Publisher"), Field("License"), Field("Homepage"));
    }
}
