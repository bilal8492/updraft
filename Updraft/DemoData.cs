using System.Text;

namespace Updraft;

/// <summary>
/// Stand-in for winget when Updraft runs with <c>--demo</c>: realistic tables and simulated installs, so the UI can be
/// tried, developed and screenshotted on any PC. It never starts a process, so nothing is installed or removed.
/// </summary>
internal static class DemoData
{
    private sealed record App(string Name, string Id, string Version, string Available = "", string Source = "winget",
        string Publisher = "", string Description = "", string License = "", string Homepage = "");

    private static readonly App[] Upgrades =
    {
        new("Git", "Git.Git", "2.53.0", "2.56.0.4", Publisher: "The Git Development Community", License: "GPL-2.0", Homepage: "https://gitforwindows.org/",
            Description: "Git is a free and open source distributed version control system designed to handle everything from small to very large projects with speed and efficiency."),
        new("Node.js LTS", "OpenJS.NodeJS.LTS", "24.11.0", "26.13.0", Publisher: "Node.js Foundation", License: "MIT", Homepage: "https://nodejs.org/",
            Description: "Node.js is a JavaScript runtime built on Chrome's V8 JavaScript engine."),
        new("Microsoft Visual Studio Code", "Microsoft.VisualStudioCode", "1.141.0", "1.143.1", Publisher: "Microsoft Corporation", License: "Microsoft Software License", Homepage: "https://code.visualstudio.com/",
            Description: "Code editing. Redefined. Free. Built on open source. Runs everywhere."),
        new("PowerToys", "Microsoft.PowerToys", "0.101.2", "0.102.3101", Publisher: "Microsoft Corporation", License: "MIT", Homepage: "https://github.com/microsoft/PowerToys",
            Description: "A set of utilities for power users to tune and streamline their Windows experience for greater productivity."),
        new("Windows Terminal", "Microsoft.WindowsTerminal", "1.24.12741.0", "1.25.2733.0", Publisher: "Microsoft Corporation", License: "MIT", Homepage: "https://github.com/microsoft/terminal",
            Description: "The new Windows Terminal, a tabbed command line experience for Windows."),
        new("Python Launcher", "Python.Launcher", "< 3.14.7", "3.15.2", Publisher: "Python Software Foundation", License: "PSF-2.0", Homepage: "https://www.python.org/"),
        new("7-Zip", "7zip.7zip", "26.05", "26.06", Publisher: "Igor Pavlov", License: "LGPL-2.1-or-later", Homepage: "https://www.7-zip.org/",
            Description: "Free and open source file archiver with a high compression ratio."),
        new("Mozilla Firefox (x64 en-US)", "Mozilla.Firefox", "158.0.1", "159.0", Publisher: "Mozilla", License: "MPL-2.0", Homepage: "https://www.mozilla.org/firefox/",
            Description: "Mozilla Firefox is free and open source software, built by a community of thousands from all over the world."),
        new("Discord", "Discord.Discord", "1.0.9255", "1.0.9267", Publisher: "Discord Inc.", License: "Proprietary", Homepage: "https://discord.com/",
            Description: "Step up your game with a modern voice and text chat app."),
        new("Microsoft Visual C++ v14 Redistributable (x64)", "Microsoft.VCRedist.2015+.x64", "14.44.35211.0", "14.50.35719.0", Publisher: "Microsoft Corporation", License: "Proprietary"),
        new("Microsoft Windows Desktop Runtime - 8.0.21 (x64)", "Microsoft.DotNet.DesktopRuntime.8", "8.0.21", "8.0.22", Publisher: "Microsoft Corporation", License: "MIT"),
        new("Obsidian", "Obsidian.Obsidian", "1.14.7", "1.15.0", Publisher: "Obsidian", License: "Proprietary", Homepage: "https://obsidian.md/",
            Description: "A powerful knowledge base that works on top of a local folder of plain text Markdown files."),
        new("Spotify", "Spotify.Spotify", "1.3.6.512", "1.3.8.620", Publisher: "Spotify AB", License: "Proprietary", Homepage: "https://www.spotify.com/"),
        new("Microsoft Teams Meeting Add-in", "XP8BT8DW290MPQ", "1.24.28402", "1.25.24601", "msstore", Publisher: "Microsoft Corporation"),
    };

    private static readonly App[] OtherInstalled =
    {
        new("Blender", "BlenderFoundation.Blender", "5.2.3", Publisher: "Blender Foundation", License: "GPL-3.0"),
        new("GIMP", "GIMP.GIMP.3", "3.2.8.0", Publisher: "The GIMP Team", License: "GPL-3.0"),
        new("Steam", "Valve.Steam", "2.10.91.91", Publisher: "Valve Corporation"),
        new("VLC media player", "VideoLAN.VLC", "3.0.24", Publisher: "VideoLAN", License: "GPL-2.0"),
        new("Notepad++", "Notepad++.Notepad++", "8.9.9", Publisher: "Notepad++ Team", License: "GPL-3.0"),
        new("Docker Desktop", "Docker.DockerDesktop", "4.98.0", Publisher: "Docker Inc."),
        new("Slack", "SlackTechnologies.Slack", "4.53.280", Publisher: "Slack Technologies Inc."),
        new("Bitwarden", "Bitwarden.Bitwarden", "2026.10.4", Publisher: "Bitwarden Inc.", License: "GPL-3.0"),
        new("GitHub CLI", "GitHub.cli", "2.103.3", Publisher: "GitHub, Inc.", License: "MIT"),
        new("PowerShell 7-x64", "Microsoft.PowerShell", "7.6.9.0", Publisher: "Microsoft Corporation", License: "MIT"),
        new("Everything", "voidtools.Everything", "1.4.1.1032", Publisher: "voidtools"),
        new("Adobe Acrobat (64-bit)", @"ARP\Machine\X64\{AC76BA86-1033-1033-7760-BC15014EA700}", "26.002.22031", Source: ""),
        new("AV1 Video Extension", @"MSIX\Microsoft.AV1VideoExtension_2.0.35.0_x64__8wekyb3d8bbwe", "2.0.35.0", Source: ""),
        new("WhatsApp", "9NKSQGP7F2NH", "2.2540.6.0", Source: "msstore", Publisher: "WhatsApp Inc."),
    };

    private static readonly App[] SearchOnly =
    {
        new("Visual Studio Code Insiders", "Microsoft.VisualStudioCode.Insiders", "1.144.0", Publisher: "Microsoft Corporation"),
        new("VSCodium", "VSCodium.VSCodium", "1.143.0", Publisher: "VSCodium"),
        new("Google Chrome", "Google.Chrome", "157.0.8103.39", Publisher: "Google LLC"),
        new("Brave", "Brave.Brave", "156.1.98.63", Publisher: "Brave Software Inc"),
        new("Python 3.14", "Python.Python.3.14", "3.14.10", Publisher: "Python Software Foundation", License: "PSF-2.0",
            Description: "Python is a programming language that lets you work quickly and integrate systems more effectively."),
        new("Python 3.13", "Python.Python.3.13", "3.13.17", Publisher: "Python Software Foundation", License: "PSF-2.0"),
        new("Inkscape", "Inkscape.Inkscape", "1.4.4", Publisher: "Inkscape", License: "GPL-2.0"),
        new("OBS Studio", "OBSProject.OBSStudio", "33.0.0", Publisher: "OBS Project", License: "GPL-2.0"),
        new("Zoom Workplace", "Zoom.Zoom", "7.2.51704", Publisher: "Zoom"),
        new("ShareX", "ShareX.ShareX", "22.3.0", Publisher: "ShareX Team", License: "GPL-3.0"),
        new("Krita", "KDE.Krita", "5.3.4.1", Publisher: "Krita Foundation", License: "GPL-3.0"),
        new("Audacity", "Audacity.Audacity", "4.0.2", Publisher: "Audacity", License: "GPL-3.0"),
        new("Telegram Desktop", "Telegram.TelegramDesktop", "7.4.1", Publisher: "Telegram FZ-LLC"),
        new("Signal", "OpenWhisperSystems.Signal", "8.34.0", Publisher: "Signal Messenger, LLC", License: "AGPL-3.0"),
        new("Epic Games Launcher", "EpicGames.EpicGamesLauncher", "1.3.210.0", Publisher: "Epic Games, Inc."),
        new("KeePassXC", "KeePassXCTeam.KeePassXC", "2.8.0", Publisher: "KeePassXC Team", License: "GPL-3.0"),
    };

    private static readonly HashSet<string> Pins = new(StringComparer.OrdinalIgnoreCase);

    private static IEnumerable<App> Everything => Upgrades.Concat(OtherInstalled).Concat(SearchOnly);

    public static async Task<string> RunAsync(string[] args)
    {
        await Task.Delay(args.FirstOrDefault() is "show" ? 250 : 900);   // feel like a real call
        string verb = args.FirstOrDefault() ?? "";
        switch (verb)
        {
            case "upgrade":
                return Table(Upgrades.Where(a => !Pins.Contains(a.Id)).ToArray(), withAvailable: true) + $"{Upgrades.Length} upgrades available.\n";
            case "list":
                return Table(Upgrades.Concat(OtherInstalled).OrderBy(a => a.Name).ToArray(), withAvailable: true);
            case "search":
                var q = args.Length > 1 ? args[1] : "";
                var hits = Everything.Where(a => a.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                                                 || a.Id.Contains(q, StringComparison.OrdinalIgnoreCase)).ToArray();
                if (hits.Length == 0)
                    hits = Catalog.Packs.SelectMany(p => p.Apps).DistinctBy(a => a.Id)
                        .Where(a => a.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || a.Id.Contains(q, StringComparison.OrdinalIgnoreCase))
                        .Select(a => new App(a.Name, a.Id, "1.0.0")).ToArray();
                return hits.Length == 0 ? "No package found matching input criteria.\n" : Table(hits, withAvailable: false);
            case "pin":
                var pinned = Upgrades.Concat(OtherInstalled).Where(a => Pins.Contains(a.Id)).ToArray();
                return pinned.Length == 0 ? "There are no pins configured.\n" : Table(pinned, withAvailable: false);
            case "show":
                string id = args.SkipWhile(a => a != "--id").Skip(1).FirstOrDefault() ?? "";
                return args.Contains("--versions") ? VersionsText(id) : ShowText(id);
            case "--version":
                return "v1.29.380 (demo)\n";
            default:
                return "";
        }
    }

    public static async Task<(int, string)> RunCaptureAsync(string[] args)
    {
        await Task.Delay(700);
        if (args is ["pin", "add", ..])
        {
            Pins.Add(args.SkipWhile(a => a != "--id").Skip(1).FirstOrDefault() ?? "");
            return (0, "Pin added successfully.");
        }
        if (args is ["pin", "remove", ..])
        {
            Pins.Remove(args.SkipWhile(a => a != "--id").Skip(1).FirstOrDefault() ?? "");
            return (0, "Pin removed successfully.");
        }
        if (args is ["export", "-o", var path, ..])
        {
            var pkgs = Upgrades.Concat(OtherInstalled).Where(a => a.Source == "winget")
                .Select(a => $"{{ \"PackageIdentifier\": \"{a.Id}\" }}");
            File.WriteAllText(path, "{ \"$schema\": \"https://aka.ms/winget-packages.schema.2.0.json\", \"Sources\": [ { \"SourceDetails\": { \"Name\": \"winget\" }, \"Packages\": [ "
                                    + string.Join(", ", pkgs) + " ] } ] }");
            return (0, "Installed package is being exported.");
        }
        return (0, "");
    }

    /// <summary>A believable install: hash check, download with a moving progress bar, then install.</summary>
    public static async Task<int> RunPackageAsync(Pkg p, string mode, CancellationToken ct, Action<string, Pkg> handle)
    {
        try
        {
            handle($"Found {p.Name} [{p.Id}] Version {p.TargetText}", p);
            await Task.Delay(500, ct);
            if (mode == "installed")
            {
                handle("Starting package uninstall...", p);
                await Task.Delay(1400, ct);
                handle("Successfully uninstalled", p);
                return 0;
            }
            handle("This application is licensed to you by its owner.", p);
            handle($"Downloading https://example.invalid/{p.Id}/{p.TargetText}/setup.exe", p);
            double size = 18 + (Math.Abs(p.Id.GetHashCode()) % 70);
            for (int i = 1; i <= 20; i++)
            {
                await Task.Delay(90, ct);
                double got = size * i / 20;
                handle($"██████▒▒▒▒  {got:0.0} MB / {size:0.0} MB  {i * 5}%", p);
            }
            handle("Successfully verified installer hash", p);
            handle("Starting package install...", p);
            await Task.Delay(1100, ct);
            if (p.Id.EndsWith(".x64", StringComparison.OrdinalIgnoreCase) && p.Id.Contains("VCRedist"))
            {
                handle("Installer failed with exit code: 1638", p);
                handle("Another version of this product is already installed.", p);
                return unchecked((int)0x8A150006);
            }
            handle(mode == "upgrade" ? "Successfully installed" : "Successfully installed", p);
            return 0;
        }
        catch (OperationCanceledException)
        {
            return -1;
        }
    }

    // ---------- canned winget output ----------

    private static string Table(App[] apps, bool withAvailable)
    {
        var cols = withAvailable
            ? new[] { "Name", "Id", "Version", "Available", "Source" }
            : new[] { "Name", "Id", "Version", "Source" };
        string[] Row(App a) => withAvailable
            ? new[] { a.Name, a.Id, a.Version, a.Available, a.Source }
            : new[] { a.Name, a.Id, a.Version, a.Source };
        var rows = apps.Select(Row).ToList();
        var widths = cols.Select((c, i) => Math.Max(c.Length, rows.Count == 0 ? 0 : rows.Max(r => r[i].Length)) + 2).ToArray();
        var sb = new StringBuilder();
        sb.AppendLine(string.Concat(cols.Select((c, i) => c.PadRight(widths[i]))).TrimEnd());
        sb.AppendLine(new string('-', widths.Sum()));
        foreach (var r in rows) sb.AppendLine(string.Concat(r.Select((c, i) => c.PadRight(widths[i]))).TrimEnd());
        return sb.ToString();
    }

    private static string ShowText(string id)
    {
        var a = Everything.FirstOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        var starter = Catalog.Packs.SelectMany(p => p.Apps).FirstOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        string name = a?.Name ?? starter?.Name ?? id;
        string version = a is null ? "1.0.0" : a.Available.Length > 0 ? a.Available : a.Version;
        string desc = a?.Description is { Length: > 0 } d ? d : $"{name} is a popular app available from the winget community repository.";
        var sb = new StringBuilder();
        sb.AppendLine($"Found {name} [{id}]");
        sb.AppendLine($"Version: {version}");
        sb.AppendLine($"Publisher: {(a?.Publisher is { Length: > 0 } pub ? pub : name.Split(' ')[0])}");
        sb.AppendLine("Description:");
        sb.AppendLine("  " + desc);
        if (a?.Homepage is { Length: > 0 } home) sb.AppendLine($"Homepage: {home}");
        sb.AppendLine($"License: {(a?.License is { Length: > 0 } lic ? lic : "Proprietary")}");
        if (a?.Available.Length > 0)
        {
            sb.AppendLine("Release Notes:");
            sb.AppendLine($"  {name} {version}");
            sb.AppendLine("  - Faster start-up and lower memory use");
            sb.AppendLine("  - Fixes a crash when the window is restored on a disconnected monitor");
            sb.AppendLine("  - Security fixes and updated dependencies");
            if (a.Homepage.Length > 0) sb.AppendLine($"Release Notes Url: {a.Homepage.TrimEnd('/')}/releases");
        }
        sb.AppendLine("Tags:");
        foreach (var t in new[] { "utilities", "open-source", "productivity" }) sb.AppendLine("  " + t);
        sb.AppendLine("Installer:");
        sb.AppendLine("  Installer Type: " + (id.GetHashCode() % 2 == 0 ? "inno" : "msi"));
        sb.AppendLine("  Release Date: 2026-10-26");
        return sb.ToString();
    }

    private static string VersionsText(string id)
    {
        var a = Everything.FirstOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        var latest = Versions.Parse(a is null ? "3.2.0" : a.Available.Length > 0 ? a.Available : a.Version) ?? new[] { 3, 2, 0 };
        var sb = new StringBuilder($"Found {a?.Name ?? id} [{id}]\nVersion\n-------\n");
        int major = latest[0], minor = latest.Length > 1 ? latest[1] : 0;
        for (int i = 0; i < 9; i++)
        {
            int m = Math.Max(0, minor - i / 3);
            sb.AppendLine($"{major}.{m}.{2 - i % 3}");
        }
        return sb.ToString();
    }
}
