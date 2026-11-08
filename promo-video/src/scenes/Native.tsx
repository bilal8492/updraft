import type React from "react";
import { AbsoluteFill, interpolate, useCurrentFrame } from "remotion";
import { SceneTitle } from "../components/SceneTitle";
import { AppIcon, DoneBadge, Icon, type IconName, Keycap } from "../components/ui";
import { mix, reveal } from "../anim";
import { C, FONT } from "../theme";

const GRID = { x: 180, y: 350, w: 1560, h: 630, gap: 24 };
const COL = (GRID.w - 3 * GRID.gap) / 4;
const ROW = (GRID.h - GRID.gap) / 2;

const cell = (col: number, row: number, span = 1): React.CSSProperties => ({
  position: "absolute",
  left: GRID.x + col * (COL + GRID.gap),
  top: GRID.y + row * (ROW + GRID.gap),
  width: COL * span + GRID.gap * (span - 1),
  height: ROW,
});

/** Scene: a bento grid of what makes UpInstall feel at home on Windows. */
export const Native = () => {
  const frame = useCurrentFrame();

  const theme = interpolate(frame, [40, 60, 100, 120], [0, 1, 1, 0], { extrapolateLeft: "clamp", extrapolateRight: "clamp" });
  const ctrl = interpolate(frame, [50, 54, 92, 96], [0, 1, 1, 0], { extrapolateLeft: "clamp", extrapolateRight: "clamp" });
  const enter = interpolate(frame, [62, 66, 80, 84], [0, 1, 1, 0], { extrapolateLeft: "clamp", extrapolateRight: "clamp" });

  return (
    <AbsoluteFill style={{ fontFamily: FONT, color: C.text }}>
      <SceneTitle
        left={GRID.x}
        top={70}
        eyebrow="Made for Windows"
        headline="Native, fast and private."
        subline="Built with WinUI 3 for Windows 10 and 11."
      />

      <Tile style={cell(0, 0, 2)} t={reveal(frame, 20, 22)} title="Fluent design" caption="Mica, light and dark themes, and motion that respects your settings.">
        <div style={{ position: "absolute", right: 34, top: 30, display: "flex", gap: 18 }}>
          <MiniWindow dark={false} t={1 - theme} />
          <MiniWindow dark t={theme} />
        </div>
      </Tile>

      <Tile style={cell(2, 0)} t={reveal(frame, 25, 22)} title="Keyboard first" caption="Ctrl+1–4, F5, Ctrl+F, F1…">
        <div style={{ position: "absolute", left: 30, top: 40, display: "flex", alignItems: "center", gap: 14 }}>
          <Keycap label="Ctrl" pressed={ctrl} size={26} />
          <div style={{ fontSize: 28, color: C.textFaint }}>+</div>
          <Keycap label="Enter" pressed={enter} size={26} />
        </div>
      </Tile>

      <Tile style={cell(3, 0)} t={reveal(frame, 30, 22)} title="History" caption="Every run, kept locally.">
        <div style={{ position: "absolute", left: 30, right: 30, top: 30, display: "flex", flexDirection: "column", gap: 10 }}>
          <div style={{ fontSize: 16, letterSpacing: "0.12em", color: C.textFaint, fontWeight: 600 }}>TODAY</div>
          {["Updated Git", "Updated Node.js LTS", "Installed Obsidian"].map((line, i) => (
            <div key={line} style={{ display: "flex", alignItems: "center", gap: 12, fontSize: 19, opacity: reveal(frame, 46 + i * 8, 12) }}>
              <DoneBadge t={reveal(frame, 46 + i * 8, 12)} size={26} />
              {line}
            </div>
          ))}
        </div>
      </Tile>

      <Tile style={cell(0, 1)} t={reveal(frame, 35, 22)} title="x64 + ARM64" caption="Windows 10 (1809+) and 11.">
        <BigIcon name="chip" frame={frame} />
      </Tile>

      <Tile style={cell(1, 1)} t={reveal(frame, 40, 22)} title="Private" caption="No telemetry. No accounts.">
        <BigIcon name="shield" frame={frame} />
      </Tile>

      <Tile style={cell(2, 1, 2)} t={reveal(frame, 45, 22)} title="Free & open source" caption="MIT licensed. It only talks to winget, nothing else.">
        <div style={{ position: "absolute", left: 30, top: 34, display: "flex", gap: 12 }}>
          {["MIT license", ".NET 9", "WinUI 3", "Windows App SDK"].map((tag, i) => (
            <div
              key={tag}
              style={{
                height: 44,
                padding: "0 18px",
                borderRadius: 22,
                background: "rgba(57,134,237,0.18)",
                color: "#9CC3FF",
                fontSize: 19,
                fontWeight: 600,
                display: "flex",
                alignItems: "center",
                opacity: reveal(frame, 56 + i * 5, 12),
              }}
            >
              {tag}
            </div>
          ))}
        </div>
        <div style={{ position: "absolute", right: 40, top: 40, display: "flex", alignItems: "center", gap: 22 }}>
          <AppIcon size={86} style={{ filter: "drop-shadow(0 12px 24px rgba(47,123,230,0.45))" }} />
        </div>
      </Tile>
    </AbsoluteFill>
  );
};

const Tile: React.FC<{ style: React.CSSProperties; t: number; title: string; caption: string; children: React.ReactNode }> = ({
  style,
  t,
  title,
  caption,
  children,
}) => (
  <div
    style={{
      ...style,
      boxSizing: "border-box",
      borderRadius: 26,
      background: "linear-gradient(160deg, rgba(255,255,255,0.08), rgba(255,255,255,0.025))",
      border: "1px solid rgba(255,255,255,0.09)",
      boxShadow: "0 30px 60px rgba(0,0,0,0.3), inset 0 1px 0 rgba(255,255,255,0.06)",
      overflow: "hidden",
      opacity: t,
      scale: String(mix(0.9, 1, t)),
      translate: `0px ${(1 - t) * 40}px`,
    }}
  >
    {children}
    <div style={{ position: "absolute", left: 30, right: 30, bottom: 28 }}>
      <div style={{ fontSize: 32, fontWeight: 650 }}>{title}</div>
      <div style={{ fontSize: 20, color: C.textDim, marginTop: 6, lineHeight: 1.3 }}>{caption}</div>
    </div>
  </div>
);

const BigIcon: React.FC<{ name: IconName; frame: number }> = ({ name, frame }) => (
  <div
    style={{
      position: "absolute",
      left: 30,
      top: 30,
      width: 92,
      height: 92,
      borderRadius: 24,
      background: "linear-gradient(135deg, rgba(79,95,232,0.35), rgba(20,151,221,0.25))",
      border: "1px solid rgba(138,184,247,0.3)",
      display: "flex",
      alignItems: "center",
      justifyContent: "center",
      translate: `0px ${Math.sin(frame / 18) * 4}px`,
    }}
  >
    <Icon name={name} size={52} color="#CFE2FF" strokeWidth={1.7} />
  </div>
);

/** A thumbnail of the UpInstall window in light or dark theme; `t` lifts the one that's "on". */
const MiniWindow: React.FC<{ dark: boolean; t: number }> = ({ dark, t }) => {
  const bg = dark ? "#1E2129" : "#F3F5F9";
  const fg = dark ? "rgba(255,255,255,0.14)" : "rgba(0,0,0,0.1)";
  return (
    <div
      style={{
        width: 250,
        height: 158,
        borderRadius: 14,
        background: bg,
        border: `1px solid ${dark ? "rgba(255,255,255,0.12)" : "rgba(0,0,0,0.08)"}`,
        boxShadow: `0 ${10 + 14 * t}px ${24 + 20 * t}px rgba(0,0,0,${0.3 + 0.2 * t})`,
        translate: `0px ${-10 * t}px`,
        outline: t > 0.5 ? "3px solid #60A5FA" : "none",
        outlineOffset: 4,
        padding: 14,
        boxSizing: "border-box",
        display: "flex",
        flexDirection: "column",
        gap: 10,
      }}
    >
      <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
        <AppIcon size={16} />
        <div style={{ width: 60, height: 8, borderRadius: 4, background: fg }} />
        <div style={{ flex: 1 }} />
        <Icon name={dark ? "moon" : "sun"} size={18} color={dark ? "#C7D2FE" : "#D97706"} />
      </div>
      <div style={{ height: 34, borderRadius: 7, background: "linear-gradient(135deg, #4F5FE8, #2F7BE6 55%, #1497DD)" }} />
      {[0.9, 0.75].map((w, i) => (
        <div key={i} style={{ display: "flex", alignItems: "center", gap: 8 }}>
          <div style={{ width: 18, height: 18, borderRadius: 5, background: ["#E11D48", "#4D7C0F", "#4F46E5"][i] }} />
          <div style={{ width: `${w * 70}%`, height: 8, borderRadius: 4, background: fg }} />
        </div>
      ))}
    </div>
  );
};
