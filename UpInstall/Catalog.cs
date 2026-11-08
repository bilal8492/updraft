using Microsoft.UI.Xaml.Media;

namespace UpInstall;

public sealed class StarterApp
{
    public StarterApp(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public string Id { get; }
    public string Name { get; }
    public string Initials => Pkg.MakeInitials(Name);
    public Brush AvatarBrush => Avatars.For(Id, Name);
}

/// <summary>A curated set of popular apps that can be installed together (like setting up a new PC).</summary>
public sealed class StarterPack
{
    public string Title { get; set; } = "";
    public string Blurb { get; set; } = "";
    public string Glyph { get; set; } = "";
    public StarterApp[] Apps { get; set; } = Array.Empty<StarterApp>();
    public int PaletteIndex { get; set; }

    public Brush IconBrush => Avatars.Palette(PaletteIndex);
    public string CountText => $"{Apps.Length} apps";
    public IEnumerable<StarterApp> Preview => Apps.Take(4);
    public string MoreText => Apps.Length > 4 ? $"+{Apps.Length - 4}" : "";
    public string NamesText => string.Join(", ", Apps.Take(4).Select(a => a.Name)) + (Apps.Length > 4 ? "…" : "");

    public override string ToString() => $"{Title} starter pack, {CountText}";
}

/// <summary>
/// Built-in starter packs for Discover. Every id here was checked against the winget community repository;
/// they're installed with the same validated command as any other package.
/// </summary>
public static class Catalog
{
    public static readonly StarterPack[] Packs =
    {
        new()
        {
            Title = "Essentials", Blurb = "A browser, media player, archiver and the everyday tools most PCs need.",
            Glyph = "", PaletteIndex = 0,
            Apps = new StarterApp[]
            {
                new("Mozilla.Firefox", "Firefox"), new("7zip.7zip", "7-Zip"), new("VideoLAN.VLC", "VLC media player"),
                new("Notepad++.Notepad++", "Notepad++"), new("Microsoft.PowerToys", "PowerToys"),
                new("voidtools.Everything", "Everything"), new("SumatraPDF.SumatraPDF", "SumatraPDF"), new("ShareX.ShareX", "ShareX"),
            },
        },
        new()
        {
            Title = "Developer", Blurb = "Editor, Git, terminals and the language runtimes to get coding.",
            Glyph = "", PaletteIndex = 9,
            Apps = new StarterApp[]
            {
                new("Microsoft.VisualStudioCode", "Visual Studio Code"), new("Git.Git", "Git"), new("GitHub.cli", "GitHub CLI"),
                new("Microsoft.WindowsTerminal", "Windows Terminal"), new("Microsoft.PowerShell", "PowerShell"),
                new("OpenJS.NodeJS.LTS", "Node.js LTS"), new("Python.Python.3.14", "Python 3.14"),
                new("Microsoft.DotNet.SDK.10", ".NET SDK 10"), new("Docker.DockerDesktop", "Docker Desktop"),
            },
        },
        new()
        {
            Title = "Creative", Blurb = "Free tools for photos, illustration, 3D, audio and video.",
            Glyph = "", PaletteIndex = 3,
            Apps = new StarterApp[]
            {
                new("GIMP.GIMP.3", "GIMP"), new("Inkscape.Inkscape", "Inkscape"), new("KDE.Krita", "Krita"),
                new("BlenderFoundation.Blender", "Blender"), new("darktable.darktable", "darktable"),
                new("Audacity.Audacity", "Audacity"), new("OBSProject.OBSStudio", "OBS Studio"), new("HandBrake.HandBrake", "HandBrake"),
            },
        },
        new()
        {
            Title = "Chat & meetings", Blurb = "Stay in touch: messaging, calls, video meetings and email.",
            Glyph = "", PaletteIndex = 2,
            Apps = new StarterApp[]
            {
                new("Discord.Discord", "Discord"), new("SlackTechnologies.Slack", "Slack"), new("Zoom.Zoom", "Zoom Workplace"),
                new("Microsoft.Teams", "Microsoft Teams"), new("Telegram.TelegramDesktop", "Telegram"),
                new("OpenWhisperSystems.Signal", "Signal"), new("Mozilla.Thunderbird", "Thunderbird"),
            },
        },
        new()
        {
            Title = "Gaming", Blurb = "Every major game launcher, plus a library to keep them all in one place.",
            Glyph = "", PaletteIndex = 4,
            Apps = new StarterApp[]
            {
                new("Valve.Steam", "Steam"), new("EpicGames.EpicGamesLauncher", "Epic Games Launcher"), new("GOG.Galaxy", "GOG GALAXY"),
                new("Ubisoft.Connect", "Ubisoft Connect"), new("ElectronicArts.EADesktop", "EA app"),
                new("Playnite.Playnite", "Playnite"), new("Discord.Discord", "Discord"),
            },
        },
        new()
        {
            Title = "Privacy & security", Blurb = "Password managers, a private browser, VPN and encryption.",
            Glyph = "", PaletteIndex = 7,
            Apps = new StarterApp[]
            {
                new("Bitwarden.Bitwarden", "Bitwarden"), new("KeePassXCTeam.KeePassXC", "KeePassXC"), new("Brave.Brave", "Brave"),
                new("Proton.ProtonVPN", "Proton VPN"), new("IDRIX.VeraCrypt", "VeraCrypt"),
                new("Malwarebytes.Malwarebytes", "Malwarebytes"), new("OpenWhisperSystems.Signal", "Signal"),
            },
        },
    };

    /// <summary>One-click searches shown on the Discover start page.</summary>
    public static readonly string[] PopularSearches =
    {
        "Visual Studio Code", "PowerToys", "7-Zip", "Firefox", "VLC", "Python", "Node.js", "Steam", "Spotify", "Obsidian",
    };
}
