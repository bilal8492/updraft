// Sample data mirroring UpInstall's --demo mode (UpInstall/DemoData.cs, Catalog.cs).

import type { IconName } from "./components/ui";

export type Kind = "Major" | "Minor" | "Patch";

export type Upgrade = {
  name: string;
  id: string;
  from: string;
  to: string;
  kind: Kind;
  palette: number;
};

export const UPGRADES: Upgrade[] = [
  { name: "Git", id: "Git.Git", from: "2.53.0", to: "2.56.0.4", kind: "Minor", palette: 4 },
  { name: "Node.js LTS", id: "OpenJS.NodeJS.LTS", from: "24.11.0", to: "26.13.0", kind: "Major", palette: 6 },
  { name: "Visual Studio Code", id: "Microsoft.VisualStudioCode", from: "1.141.0", to: "1.143.1", kind: "Minor", palette: 0 },
  { name: "PowerToys", id: "Microsoft.PowerToys", from: "0.101.2", to: "0.102.3101", kind: "Minor", palette: 0 },
  { name: "Windows Terminal", id: "Microsoft.WindowsTerminal", from: "1.24.12741.0", to: "1.25.2733.0", kind: "Minor", palette: 0 },
  { name: "Mozilla Firefox", id: "Mozilla.Firefox", from: "158.0.1", to: "159.0", kind: "Major", palette: 5 },
  { name: "Discord", id: "Discord.Discord", from: "1.0.9255", to: "1.0.9267", kind: "Patch", palette: 9 },
  { name: "Obsidian", id: "Obsidian.Obsidian", from: "1.14.7", to: "1.15.0", kind: "Minor", palette: 3 },
];

export const KIND_TONE = { Major: "warning", Minor: "accent", Patch: "neutral" } as const;

export type Pack = {
  title: string;
  blurb: string;
  icon: IconName;
  palette: number;
  apps: string[];
};

export const PACKS: Pack[] = [
  {
    title: "Essentials",
    blurb: "Browser, media player, archiver and everyday tools.",
    icon: "star",
    palette: 0,
    apps: ["Firefox", "7-Zip", "VLC media player", "Notepad++", "PowerToys", "Everything", "SumatraPDF", "ShareX"],
  },
  {
    title: "Developer",
    blurb: "Editor, Git, terminals and language runtimes.",
    icon: "code",
    palette: 9,
    apps: ["Visual Studio Code", "Git", "GitHub CLI", "Windows Terminal", "PowerShell", "Node.js LTS", "Python 3.14", ".NET SDK 10", "Docker Desktop"],
  },
  {
    title: "Creative",
    blurb: "Photos, illustration, 3D, audio and video.",
    icon: "palette",
    palette: 3,
    apps: ["GIMP", "Inkscape", "Krita", "Blender", "darktable", "Audacity", "OBS Studio", "HandBrake"],
  },
  {
    title: "Chat & meetings",
    blurb: "Messaging, calls, video meetings and email.",
    icon: "chat",
    palette: 2,
    apps: ["Discord", "Slack", "Zoom Workplace", "Microsoft Teams", "Telegram", "Signal", "Thunderbird"],
  },
  {
    title: "Gaming",
    blurb: "Every major launcher, plus one library for all.",
    icon: "gamepad",
    palette: 4,
    apps: ["Steam", "Epic Games Launcher", "GOG GALAXY", "Ubisoft Connect", "EA app", "Playnite", "Discord"],
  },
  {
    title: "Privacy & security",
    blurb: "Password managers, private browser, VPN.",
    icon: "shield",
    palette: 7,
    apps: ["Bitwarden", "KeePassXC", "Brave", "Proton VPN", "VeraCrypt", "Malwarebytes", "Signal"],
  },
];

// Deterministic palette pick for an app name (stands in for Avatars.For in Pkg.cs).
export const paletteFor = (name: string) => {
  let h = 2166136261;
  for (let i = 0; i < name.length; i++) {
    h ^= name.charCodeAt(i);
    h = Math.imul(h, 16777619) >>> 0;
  }
  return h % 10;
};
