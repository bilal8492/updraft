import type React from "react";
import { Img, staticFile } from "remotion";
import { C, FONT, MONO, PALETTE, TONES, type Tone } from "../theme";

// ---------------------------------------------------------------------------------------------
// Icons: simple stroked glyphs in a 24×24 box, in the spirit of Segoe Fluent Icons.
// ---------------------------------------------------------------------------------------------

const circle = (cx: number, cy: number, r: number) =>
  `M ${cx - r} ${cy} a ${r} ${r} 0 1 0 ${2 * r} 0 a ${r} ${r} 0 1 0 ${-2 * r} 0`;

const ICONS = {
  updates: ["M12 20V7", "M6.5 12.5 12 7l5.5 5.5", "M5 3.5h14"],
  installed: ["M4 6h1.5", "M9 6h11", "M4 12h1.5", "M9 12h11", "M4 18h1.5", "M9 18h11"],
  discover: [circle(10.5, 10.5, 6.5), "M15.5 15.5 20 20"],
  history: [circle(12, 12, 8.5), "M12 7.5V12l3 2"],
  settings: ["M4 7h9", "M17 7h3", "M4 17h3", "M11 17h9", circle(15, 7, 2), circle(9, 17, 2)],
  refresh: ["M19.5 12a7.5 7.5 0 1 1-2.2-5.3", "M19.5 4.5v4h-4"],
  check: ["M5 12.5l4.5 4.5L19 7.5"],
  copy: ["M9 9h10v11H9z", "M15 9V5H5v11h4"],
  terminal: ["M3.5 5.5h17v13h-17z", "M7.5 10l2.5 2-2.5 2", "M12.5 15h4"],
  shield: ["M12 3.5l7 2.8v5.2c0 4.3-2.9 7.6-7 9-4.1-1.4-7-4.7-7-9V6.3z", "M9 12l2.2 2.2L15.5 10"],
  pin: ["M9.5 4h5l-.8 5.5 3.3 3V14H7v-1.5l3.3-3z", "M12 14v6.5"],
  skip: ["M5.5 6l8 6-8 6z", "M17.5 6v12"],
  download: ["M12 4v11", "M7 10.5l5 5 5-5", "M5 20h14"],
  upload: ["M12 15V4", "M7 8.5l5-5 5 5", "M5 20h14"],
  file: ["M6.5 3h7.5l4.5 4.5V21h-12z", "M14 3v4.5h4.5"],
  globe: [circle(12, 12, 8.5), "M3.5 12h17", "M12 3.5c2.6 2.6 2.6 14.4 0 17", "M12 3.5c-2.6 2.6-2.6 14.4 0 17"],
  code: ["M8.5 7.5 4 12l4.5 4.5", "M15.5 7.5 20 12l-4.5 4.5", "M13.5 5l-3 14"],
  chip: ["M7 7h10v10H7z", "M10 3.5V7", "M14 3.5V7", "M10 17v3.5", "M14 17v3.5", "M3.5 10H7", "M3.5 14H7", "M17 10h3.5", "M17 14h3.5"],
  keyboard: ["M3 6.5h18v11H3z", "M6.5 10h1", "M10 10h1", "M13.5 10h1", "M17 10h1", "M8 14h8"],
  palette: [circle(12, 12, 8.5), circle(8.5, 10, 1.2), circle(12, 7.5, 1.2), circle(15.5, 10, 1.2), "M12 20.5c-1.5-1.5-.5-4 2-4h2.5"],
  chat: ["M4 5.5h16v10H10l-4.5 3.5v-3.5H4z"],
  gamepad: ["M7 8h10a4 4 0 0 1 4 4v1a3.5 3.5 0 0 1-6.3 2.1L14 14h-4l-.7 1.1A3.5 3.5 0 0 1 3 13v-1a4 4 0 0 1 4-4z", "M7.5 10.5v3", "M6 12h3"],
  star: ["M12 3.8l2.5 5.2 5.7.8-4.1 4 1 5.7L12 16.8l-5.1 2.7 1-5.7-4.1-4 5.7-.8z"],
  sparkle: ["M12 3v4", "M12 17v4", "M3 12h4", "M17 12h4", "M12 8.5l1.3 2.2 2.2 1.3-2.2 1.3L12 15.5l-1.3-2.2L8.5 12l2.2-1.3z"],
  chevronRight: ["M9.5 5.5 16 12l-6.5 6.5"],
  chevronDown: ["M5.5 9.5 12 16l6.5-6.5"],
  arrowRight: ["M4 12h15", "M13.5 6.5 19 12l-5.5 5.5"],
  minimize: ["M5 12h14"],
  maximize: ["M6 6h12v12H6z"],
  close: ["M6 6l12 12", "M18 6 6 18"],
  tag: ["M4 4h8l8 8-8 8-8-8z", circle(8.5, 8.5, 1.2)],
  backup: ["M4 7.5h16v12H4z", "M4 7.5l2-3.5h12l2 3.5", "M12 10.5v6", "M9.5 13l2.5-2.5 2.5 2.5"],
  moon: ["M19.5 14.5A8 8 0 0 1 9.5 4.5a8 8 0 1 0 10 10z"],
  sun: [circle(12, 12, 4), "M12 2.5v2", "M12 19.5v2", "M2.5 12h2", "M19.5 12h2", "M5.3 5.3l1.4 1.4", "M17.3 17.3l1.4 1.4", "M5.3 18.7l1.4-1.4", "M17.3 6.7l1.4-1.4"],
  lock: ["M6 10.5h12v10H6z", "M8.5 10.5V8a3.5 3.5 0 0 1 7 0v2.5"],
  admin: ["M12 3.5l7 2.8v5.2c0 4.3-2.9 7.6-7 9-4.1-1.4-7-4.7-7-9V6.3z"],
} as const;

export type IconName = keyof typeof ICONS;

export const Icon: React.FC<{
  name: IconName;
  size?: number;
  color?: string;
  strokeWidth?: number;
  style?: React.CSSProperties;
}> = ({ name, size = 24, color = "currentColor", strokeWidth = 1.8, style }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" style={{ flexShrink: 0, ...style }}>
    {ICONS[name].map((d, i) => (
      <path
        key={i}
        d={d}
        stroke={color}
        strokeWidth={strokeWidth}
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    ))}
  </svg>
);

// ---------------------------------------------------------------------------------------------
// Small building blocks of the UpInstall UI.
// ---------------------------------------------------------------------------------------------

// Same rules as Pkg.MakeInitials in the app.
const VENDORS = ["microsoft", "mozilla", "google", "adobe", "the", "apple", "amazon"];
export const initials = (name: string) => {
  const words = name.split(/[\s\-_.()[\]/,:+]+/).filter((w) => w.length > 0 && /[A-Za-z0-9]/.test(w[0]));
  if (words.length === 0) return "?";
  if (words.length > 1 && VENDORS.includes(words[0].toLowerCase())) words.shift();
  const first = words[0];
  if (words.length > 1 && /[A-Z0-9]/.test(words[1][0])) return (first[0] + words[1][0]).toUpperCase();
  const inner = [...first.slice(1)].find((c) => /[A-Z]/.test(c));
  if (inner && /[a-z]/.test(first)) return first[0].toUpperCase() + inner;
  return first[0].toUpperCase();
};

export const Avatar: React.FC<{ name: string; palette: number; size?: number; style?: React.CSSProperties }> = ({
  name,
  palette,
  size = 44,
  style,
}) => {
  const [from, to] = PALETTE[palette % PALETTE.length];
  return (
    <div
      style={{
        width: size,
        height: size,
        borderRadius: size * 0.26,
        background: `linear-gradient(135deg, ${from}, ${to})`,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        color: "white",
        fontFamily: FONT,
        fontWeight: 650,
        fontSize: size * 0.4,
        letterSpacing: "0.01em",
        flexShrink: 0,
        boxShadow: "inset 0 1px 0 rgba(255,255,255,0.22)",
        ...style,
      }}
    >
      {initials(name)}
    </div>
  );
};

export const Pill: React.FC<{
  tone?: Tone;
  size?: number;
  children: React.ReactNode;
  style?: React.CSSProperties;
}> = ({ tone = "accent", size = 16, children, style }) => (
  <div
    style={{
      display: "inline-flex",
      alignItems: "center",
      gap: size * 0.35,
      padding: `${size * 0.22}px ${size * 0.62}px`,
      borderRadius: 999,
      background: TONES[tone].bg,
      color: TONES[tone].fg,
      fontFamily: FONT,
      fontWeight: 600,
      fontSize: size,
      lineHeight: 1.15,
      whiteSpace: "nowrap",
      flexShrink: 0,
      ...style,
    }}
  >
    {children}
  </div>
);

/** Checkbox; `checked` goes 0 → 1 so the tick can be animated. */
export const Checkbox: React.FC<{ checked: number; size?: number }> = ({ checked, size = 24 }) => (
  <div
    style={{
      width: size,
      height: size,
      borderRadius: size * 0.22,
      border: `2px solid ${checked > 0.01 ? "transparent" : "rgba(255,255,255,0.38)"}`,
      background: `rgba(47, 123, 230, ${checked})`,
      boxSizing: "border-box",
      display: "flex",
      alignItems: "center",
      justifyContent: "center",
      flexShrink: 0,
    }}
  >
    <svg width={size * 0.78} height={size * 0.78} viewBox="0 0 24 24" fill="none">
      <path
        d="M5 12.5l4.5 4.5L19 7.5"
        stroke="white"
        strokeWidth={3}
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeDasharray={24}
        strokeDashoffset={24 * (1 - checked)}
      />
    </svg>
  </div>
);

export const ProgressRing: React.FC<{ value: number; size?: number; color?: string }> = ({
  value,
  size = 34,
  color = C.brand2,
}) => {
  const r = 15;
  const len = 2 * Math.PI * r;
  return (
    <svg width={size} height={size} viewBox="0 0 36 36" style={{ flexShrink: 0 }}>
      <circle cx={18} cy={18} r={r} stroke="rgba(255,255,255,0.12)" strokeWidth={3.5} fill="none" />
      <circle
        cx={18}
        cy={18}
        r={r}
        stroke={color}
        strokeWidth={3.5}
        fill="none"
        strokeLinecap="round"
        strokeDasharray={len}
        strokeDashoffset={len * (1 - value)}
        transform="rotate(-90 18 18)"
      />
    </svg>
  );
};

/** Green tick in a circle, scaled by `t` (0 → 1). */
export const DoneBadge: React.FC<{ t: number; size?: number }> = ({ t, size = 34 }) => (
  <div
    style={{
      width: size,
      height: size,
      borderRadius: size,
      background: "rgba(34,197,94,0.18)",
      display: "flex",
      alignItems: "center",
      justifyContent: "center",
      scale: String(0.6 + 0.4 * t),
      opacity: t,
      flexShrink: 0,
    }}
  >
    <svg width={size * 0.62} height={size * 0.62} viewBox="0 0 24 24" fill="none">
      <path
        d="M5 12.5l4.5 4.5L19 7.5"
        stroke={C.success}
        strokeWidth={3}
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeDasharray={24}
        strokeDashoffset={24 * (1 - t)}
      />
    </svg>
  </div>
);

export const Keycap: React.FC<{ label: string; pressed?: number; size?: number; minWidth?: number }> = ({
  label,
  pressed = 0,
  size = 30,
  minWidth,
}) => (
  <div
    style={{
      minWidth: minWidth ?? size * 2.1,
      height: size * 2.1,
      padding: `0 ${size * 0.6}px`,
      boxSizing: "border-box",
      borderRadius: size * 0.42,
      background: `linear-gradient(180deg, rgba(255,255,255,${0.16 + 0.1 * pressed}), rgba(255,255,255,0.06))`,
      border: "1px solid rgba(255,255,255,0.18)",
      boxShadow: `0 ${6 * (1 - pressed) + 1}px 0 rgba(0,0,0,0.45), inset 0 1px 0 rgba(255,255,255,0.18)`,
      translate: `0px ${5 * pressed}px`,
      display: "flex",
      alignItems: "center",
      justifyContent: "center",
      fontFamily: FONT,
      fontWeight: 600,
      fontSize: size,
      color: pressed > 0.5 ? "#CFE2FF" : C.text,
    }}
  >
    {label}
  </div>
);

export const AppIcon: React.FC<{ size: number; style?: React.CSSProperties }> = ({ size, style }) => (
  <Img src={staticFile("icon.png")} style={{ width: size, height: size, flexShrink: 0, ...style }} />
);

/** The UpInstall mark drawn as SVG so each chevron can rise on its own. `rise` holds 0 → 1 per chevron (top first). */
export const LogoMark: React.FC<{ size: number; rise?: readonly number[]; style?: React.CSSProperties }> = ({
  size,
  rise = [1, 1, 1],
  style,
}) => {
  const chevrons = [
    { y: 100, opacity: 0.5 },
    { y: 140, opacity: 0.75 },
    { y: 180, opacity: 1 },
  ];
  return (
    <svg width={size} height={size} viewBox="0 0 256 256" style={style}>
      <defs>
        <linearGradient id="logo-bg" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stopColor="#5D63F0" />
          <stop offset="0.55" stopColor="#2F7BE6" />
          <stop offset="1" stopColor="#13A5E8" />
        </linearGradient>
      </defs>
      <rect x="8" y="8" width="240" height="240" rx="58" fill="url(#logo-bg)" />
      <rect x="8.5" y="8.5" width="239" height="239" rx="57.5" fill="none" stroke="rgba(255,255,255,0.22)" />
      {chevrons.map((c, i) => {
        const t = rise[i] ?? 1;
        return (
          <path
            key={i}
            d={`M 62 ${c.y + 36} L 128 ${c.y} L 194 ${c.y + 36}`}
            fill="none"
            stroke="white"
            strokeOpacity={c.opacity * t}
            strokeWidth={22}
            strokeLinecap="round"
            strokeLinejoin="round"
            transform={`translate(0 ${(1 - t) * 40})`}
          />
        );
      })}
    </svg>
  );
};

/** Windows-style arrow cursor. `press` (0 → 1) squashes it for a click. */
export const Cursor: React.FC<{ x: number; y: number; press?: number; opacity?: number }> = ({
  x,
  y,
  press = 0,
  opacity = 1,
}) => (
  <svg
    width={44}
    height={60}
    viewBox="0 0 22 30"
    style={{
      position: "absolute",
      left: x,
      top: y,
      opacity,
      scale: String(1 - 0.12 * press),
      transformOrigin: "0 0",
      filter: "drop-shadow(0 6px 10px rgba(0,0,0,0.45))",
      zIndex: 50,
    }}
  >
    <path
      d="M1.5 1.5 L1.5 23 L7 17.6 L10.6 26 L14.2 24.4 L10.6 16.2 L18 16.2 Z"
      fill="white"
      stroke="#111"
      strokeWidth={1.4}
      strokeLinejoin="round"
    />
  </svg>
);

/** Expanding ring where a click lands. `t` goes 0 → 1 over the ripple. */
export const ClickRipple: React.FC<{ x: number; y: number; t: number }> = ({ x, y, t }) =>
  t <= 0 || t >= 1 ? null : (
    <div
      style={{
        position: "absolute",
        left: x - 40,
        top: y - 40,
        width: 80,
        height: 80,
        borderRadius: 80,
        border: "3px solid rgba(138,184,247,0.9)",
        scale: String(0.2 + 0.9 * t),
        opacity: 1 - t,
        zIndex: 49,
      }}
    />
  );

export const Mono: React.FC<{ children: React.ReactNode; style?: React.CSSProperties }> = ({ children, style }) => (
  <span style={{ fontFamily: MONO, ...style }}>{children}</span>
);
