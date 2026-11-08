using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Updraft;

public static class WingetService
{
    /// <summary>Sample data and simulated runs instead of real winget calls (started with --demo).</summary>
    public static bool Demo { get; set; }

    public static async Task<string> RunAsync(params string[] args)
    {
        if (Demo) return await DemoData.RunAsync(args);
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

    /// <summary>True when the failure is "winget.exe isn't on this PC" (file or path not found).</summary>
    public static bool IsMissing(Exception ex) => ex is System.ComponentModel.Win32Exception { NativeErrorCode: 2 or 3 };

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

    /// <summary>Runs winget to completion and returns its exit code plus stdout and stderr combined.</summary>
    public static async Task<(int Code, string Output)> RunCaptureAsync(params string[] args)
    {
        if (Demo) return await DemoData.RunCaptureAsync(args);
        var psi = new ProcessStartInfo("winget")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Could not start winget");
        p.StandardInput.Close();
        var so = p.StandardOutput.ReadToEndAsync();
        var se = p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();
        return (p.ExitCode, (await so) + (await se));
    }

    /// <summary>Ids of packages that have a pin, from `winget pin list`. The Id column is the second one in that table.</summary>
    public static HashSet<string> ParsePinIds(string text) =>
        Parse(text, "installed").Select(p => p.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>The last meaningful line of winget output, for showing why something failed.</summary>
    public static string LastLine(string output)
    {
        var lines = output.Split('\n').Select(l => l.Split('\r').LastOrDefault(s => s.Trim().Length > 0) ?? "")
            .Select(l => l.Trim()).Where(l => l.Length > 0 && !l.All(c => c is '-' or '\\' or '|' or '/'));
        return lines.LastOrDefault() ?? "";
    }

    /// <summary>The winget arguments for one package, shared by the copied script and the in-app runner.</summary>
    public static List<string> Args(Pkg p, string mode)
    {
        var v = Verb(mode);
        var s = AppSettings.Current;
        var args = new List<string> { v, "--id", p.Id, "-e" };
        if (v != "uninstall" && p.TargetVersion.Length > 0 && Versions.IsSafe(p.TargetVersion))
        {
            args.Add("--version");
            args.Add(p.TargetVersion);
        }
        if (v != "uninstall" && s.AcceptAgreements)
        {
            args.Add("--accept-package-agreements");
            args.Add("--accept-source-agreements");
        }
        if (s.InstallMode == "silent") args.Add("--silent");
        else if (s.InstallMode == "interactive") args.Add("--interactive");
        return args;
    }

    public static string Command(Pkg p, string mode) => "winget " + string.Join(' ', Args(p, mode));

    // winget exit codes that mean "nothing left to do" rather than a failure.
    private static readonly HashSet<uint> BenignExitCodes = new() { 0x8A15002B /* no applicable update */, 0x8A150061 /* already installed */ };

    public static bool IsSuccess(int exitCode) => exitCode == 0 || BenignExitCodes.Contains(unchecked((uint)exitCode));

    /// <summary>
    /// Runs winget for one package with no window, streaming cleaned-up output into the package's log and status.
    /// Must be called from the UI thread: the output pumps resume there, so Pkg updates are safe.
    /// </summary>
    public static async Task<int> RunPackageAsync(Pkg p, string mode, CancellationToken ct)
    {
        if (Demo) return await DemoData.RunPackageAsync(p, mode, ct, Handle);
        var psi = new ProcessStartInfo("winget")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var a in Args(p, mode)) psi.ArgumentList.Add(a);   // ArgumentList: no shell, no quoting issues

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Could not start winget");
        proc.StandardInput.Close();   // never wait for a prompt nobody can answer
        using var reg = ct.Register(() => { try { proc.Kill(entireProcessTree: true); } catch { /* already gone */ } });

        var pumps = Task.WhenAll(PumpAsync(proc.StandardOutput, p), PumpAsync(proc.StandardError, p));
        await proc.WaitForExitAsync();
        await pumps;
        return proc.ExitCode;
    }

    private static readonly Regex Percent = new(@"(\d{1,3})\s*%", RegexOptions.Compiled);
    private static readonly Regex Sizes = new(@"^[\d.,]+\s*[KMGT]?i?B\s*/\s*[\d.,]+\s*[KMGT]?i?B$", RegexOptions.Compiled);

    private static async Task PumpAsync(StreamReader reader, Pkg p)
    {
        var buf = new char[1024];
        var line = new StringBuilder();
        int n;
        while ((n = await reader.ReadAsync(buf, 0, buf.Length)) > 0)
        {
            for (int i = 0; i < n; i++)
            {
                if (buf[i] is '\r' or '\n') Handle(line, p);   // winget redraws progress with \r, so treat it as a line end
                else line.Append(buf[i]);
            }
        }
        Handle(line, p);
    }

    private static void Handle(StringBuilder sb, Pkg p)
    {
        var text = sb.ToString().Trim();
        sb.Clear();
        Handle(text, p);
    }

    private static void Handle(string text, Pkg p)
    {
        if (text.Length == 0) return;
        if (text.All(c => c is '-' or '\\' or '|' or '/')) return;   // spinner frames

        bool bar = text.Contains('█') || text.Contains('▒');
        var m = Percent.Match(text);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var pct) && pct <= 100) p.SetProgress(pct);

        if (bar || Sizes.IsMatch(text))
        {
            // Progress redraws: show the latest one as status, but keep them out of the log.
            if (Sizes.IsMatch(text)) p.SetStatusText("Downloading  " + text);
            return;
        }

        p.AppendLog(text);
        p.SetStatusText(text.Length > 90 ? text[..90] + "…" : text);
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
        var text = (await RunAsync("show", "--id", id, "-e", "--accept-source-agreements")).Replace("\r\n", "\n");

        string Field(string key)
        {
            var m = Regex.Match(text, $@"^\s*{Regex.Escape(key)}:[ \t]*(.+?)\s*$", RegexOptions.Multiline);
            return m.Success ? m.Groups[1].Value : "";
        }

        // Multi-line fields: the key on its own line, then indented lines until the next top-level key.
        List<string> Block(string key)
        {
            var m = Regex.Match(text, $@"^{Regex.Escape(key)}:[ \t]*\n((?:[ \t]+.*(?:\n|$))+)", RegexOptions.Multiline);
            return m.Success
                ? m.Groups[1].Value.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList()
                : new List<string>();
        }

        var desc = Block("Description");
        string notes = string.Join("\n", Block("Release Notes"));
        if (notes.Length > 1800) notes = notes[..1800].TrimEnd() + "…";

        return new PkgDetail
        {
            Version = Field("Version"),
            Description = desc.Count > 0 ? string.Join(" ", desc) : Field("Description"),
            Publisher = Field("Publisher"),
            PublisherUrl = Field("Publisher Url"),
            Author = Field("Author"),
            License = Field("License"),
            LicenseUrl = Field("License Url"),
            Homepage = Field("Homepage"),
            ReleaseNotes = notes.Length > 0 ? notes : Field("Release Notes"),
            ReleaseNotesUrl = Field("Release Notes Url"),
            ReleaseDate = Field("Release Date"),
            InstallerType = Field("Installer Type"),
            Tags = Block("Tags").Take(12).ToList(),
        };
    }

    /// <summary>Every version winget can install for this package, newest first (from <c>winget show --versions</c>).</summary>
    public static async Task<List<string>> VersionsAsync(string id)
    {
        var text = await RunAsync("show", "--id", id, "-e", "--versions", "--accept-source-agreements");
        var lines = text.Split('\n')
            .Select(l => (l.Split('\r').LastOrDefault(x => x.Trim().Length > 0) ?? "").Trim())
            .ToList();
        int sep = lines.FindIndex(l => Regex.IsMatch(l, @"^-{3,}$"));
        if (sep < 0) return new List<string>();
        return lines.Skip(sep + 1).TakeWhile(l => l.Length > 0).Where(Versions.IsSafe).Distinct().ToList();
    }
}

