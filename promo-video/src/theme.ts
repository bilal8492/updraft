// Colours and type taken from the UpInstall app (UpInstall/Styles.xaml, dark theme) so the video matches the product.

export const FONT =
  '"Segoe UI Variable Display", "Segoe UI Variable Text", "Segoe UI", system-ui, sans-serif';
export const MONO = '"Cascadia Code", "Cascadia Mono", Consolas, monospace';

export const C = {
  bg: "#060A16",
  text: "#F4F7FC",
  textDim: "rgba(226, 233, 246, 0.72)",
  textFaint: "rgba(226, 233, 246, 0.46)",
  brandText: "#8AB8F7",
  brand1: "#4F5FE8",
  brand2: "#2F7BE6",
  brand3: "#1497DD",
  window: "#1B1E26",
  windowBar: "#16191F",
  pane: "#20242D",
  card: "rgba(255, 255, 255, 0.045)",
  cardStrong: "rgba(255, 255, 255, 0.075)",
  edge: "rgba(255, 255, 255, 0.09)",
  divider: "rgba(255, 255, 255, 0.07)",
  terminal: "#0B1117",
  terminalText: "#D8E0E8",
  prompt: "#7FD1A6",
  success: "#4ADE80",
};

export const BRAND_GRADIENT =
  "linear-gradient(135deg, #4F5FE8 0%, #2F7BE6 55%, #1497DD 100%)";

// Avatar palette from Pkg.cs (Avatars.Colors).
export const PALETTE: readonly [string, string][] = [
  ["#4F46E5", "#2F7BE6"],
  ["#0284C7", "#0891B2"],
  ["#0D9488", "#059669"],
  ["#7C3AED", "#C026D3"],
  ["#E11D48", "#EA580C"],
  ["#D97706", "#DC2626"],
  ["#4D7C0F", "#15803D"],
  ["#475569", "#1E293B"],
  ["#DB2777", "#9333EA"],
  ["#2563EB", "#7C3AED"],
];

export type Tone = "accent" | "warning" | "success" | "neutral" | "danger" | "violet";

// Pill tones from Styles.xaml (dark).
export const TONES: Record<Tone, { bg: string; fg: string }> = {
  accent: { bg: "rgba(57, 134, 237, 0.20)", fg: "#9CC3FF" },
  warning: { bg: "rgba(245, 158, 11, 0.18)", fg: "#F7C266" },
  success: { bg: "rgba(34, 197, 94, 0.16)", fg: "#71DFA0" },
  neutral: { bg: "rgba(255, 255, 255, 0.10)", fg: "#C5CAD1" },
  danger: { bg: "rgba(239, 68, 68, 0.18)", fg: "#FF9F97" },
  violet: { bg: "rgba(139, 92, 246, 0.20)", fg: "#C7B8FF" },
};
