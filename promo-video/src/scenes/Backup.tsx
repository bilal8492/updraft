import type React from "react";
import { AbsoluteFill, useCurrentFrame } from "remotion";
import { SceneTitle } from "../components/SceneTitle";
import { Avatar, Checkbox, Icon, type IconName, Pill } from "../components/ui";
import { paletteFor } from "../data";
import { mix, progress, reveal, travel } from "../anim";
import { C, FONT, MONO } from "../theme";

const APPS = ["Visual Studio Code", "Git", "Windows Terminal", "7-Zip", "Discord", "Obsidian"];
const ALREADY = new Set(["Windows Terminal"]);
const CARD = { y: 360, w: 560, h: 590 };
const LEFT_X = 180;
const RIGHT_X = 1920 - 180 - CARD.w;
const FILE_NAME = "winget-apps-2026-11-08.json";

/** Scene: export the installed apps to a file, carry it to a new PC and restore. */
export const Backup = () => {
  const frame = useCurrentFrame();

  const fly = travel(frame, 38, 40);
  const start = { x: LEFT_X + CARD.w - 30, y: CARD.y + 330 };
  const end = { x: RIGHT_X + 30, y: CARD.y + 330 };
  const peak = 560;
  const fx = mix(start.x, end.x, fly);
  const fy = (1 - fly) * (1 - fly) * start.y + 2 * (1 - fly) * fly * peak + fly * fly * end.y;
  const fileIn = reveal(frame, 30, 10) * (1 - reveal(frame, 80, 10));
  const exportPress = reveal(frame, 26, 6) * (1 - reveal(frame, 34, 6));
  const landed = frame >= 82;

  return (
    <AbsoluteFill style={{ fontFamily: FONT, color: C.text }}>
      <SceneTitle
        left={180}
        top={70}
        eyebrow="Backup & restore"
        headline="Moving PCs? Bring your apps."
        subline="Export your apps to a file, then restore them on the new PC."
      />

      {/* dotted flight path */}
      <svg width={1920} height={1080} style={{ position: "absolute", left: 0, top: 0 }}>
        <path
          d={`M ${start.x} ${start.y} Q ${(start.x + end.x) / 2} ${2 * peak - (start.y + end.y) / 2} ${end.x} ${end.y}`}
          stroke="rgba(138,184,247,0.55)"
          strokeWidth={3}
          strokeDasharray="2 14"
          strokeLinecap="round"
          fill="none"
          opacity={reveal(frame, 34, 12) * (1 - reveal(frame, 110, 20))}
        />
      </svg>

      <PcCard
        title="This PC"
        icon="upload"
        action="Export installed apps…"
        actionPress={exportPress}
        style={{ left: LEFT_X, ...slideIn(frame, 4, -80) }}
        footer={
          <div style={{ display: "flex", alignItems: "center", gap: 12, fontSize: 20, color: C.textDim, opacity: reveal(frame, 34, 12) }}>
            <Icon name="check" size={22} color={C.success} strokeWidth={2.4} />
            6 apps exported to a JSON file
          </div>
        }
      >
        {APPS.map((app, i) => (
          <AppLine key={app} name={app} appear={reveal(frame, 10 + i * 3, 14)}>
            <Pill tone="success" size={15}>
              Installed
            </Pill>
          </AppLine>
        ))}
      </PcCard>

      <PcCard
        title="New PC"
        icon="download"
        action="Restore from file…"
        actionPress={0}
        style={{ left: RIGHT_X, ...slideIn(frame, 8, 80) }}
        footer={
          <div style={{ display: "flex", alignItems: "center", gap: 14, opacity: reveal(frame, 112, 14) }}>
            <div style={{ flex: 1, fontSize: 20, color: C.textDim }}>5 apps to install · 1 already here</div>
            <div style={{ height: 46, padding: "0 22px", borderRadius: 10, background: "#2F7BE6", display: "flex", alignItems: "center", gap: 10, fontSize: 19, fontWeight: 600 }}>
              <Icon name="download" size={20} color="white" />
              Install 5
            </div>
          </div>
        }
      >
        {APPS.map((app, i) => {
          const already = ALREADY.has(app);
          const tick = landed ? reveal(frame, 86 + i * 4, 10) : 0;
          return (
            <AppLine key={app} name={app} appear={reveal(frame, 14 + i * 3, 14)} dim={already && landed}>
              {already ? (
                <Pill tone="neutral" size={15} style={{ opacity: tick }}>
                  Already installed
                </Pill>
              ) : (
                <Checkbox checked={tick} size={26} />
              )}
            </AppLine>
          );
        })}
      </PcCard>

      {/* the backup file */}
      <div
        style={{
          position: "absolute",
          left: fx,
          top: fy,
          translate: "-50% -50%",
          opacity: fileIn,
          scale: String(mix(0.7, 1, reveal(frame, 30, 12)) * mix(1, 0.6, progress(frame, 76, 12))),
          height: 70,
          padding: "0 24px",
          borderRadius: 16,
          background: "linear-gradient(135deg, #2B3550, #1E2638)",
          border: "1px solid rgba(138,184,247,0.45)",
          boxShadow: "0 24px 50px rgba(0,0,0,0.5), 0 0 40px rgba(47,123,230,0.35)",
          display: "flex",
          alignItems: "center",
          gap: 14,
          whiteSpace: "nowrap",
          zIndex: 10,
        }}
      >
        <Icon name="file" size={32} color="#8AB8F7" />
        <span style={{ fontFamily: MONO, fontSize: 20, color: C.text }}>{FILE_NAME}</span>
      </div>

    </AbsoluteFill>
  );
};

const PcCard: React.FC<{
  title: string;
  icon: IconName;
  action: string;
  actionPress: number;
  style: React.CSSProperties;
  footer?: React.ReactNode;
  children: React.ReactNode;
}> = ({ title, icon, action, actionPress, style, footer, children }) => (
  <div
    style={{
      position: "absolute",
      top: CARD.y,
      width: CARD.w,
      height: CARD.h,
      boxSizing: "border-box",
      borderRadius: 24,
      padding: "26px 28px",
      background: "linear-gradient(160deg, rgba(36,40,54,0.96), rgba(24,27,35,0.96))",
      border: `1px solid ${C.edge}`,
      boxShadow: "0 40px 90px rgba(0,0,0,0.5)",
      display: "flex",
      flexDirection: "column",
      ...style,
    }}
  >
    <div style={{ display: "flex", alignItems: "center", gap: 14, marginBottom: 16 }}>
      <div style={{ fontSize: 32, fontWeight: 650, flex: 1 }}>{title}</div>
      <div
        style={{
          height: 44,
          padding: "0 16px",
          borderRadius: 10,
          background: actionPress > 0 ? "rgba(47,123,230,0.35)" : "rgba(255,255,255,0.07)",
          border: "1px solid rgba(255,255,255,0.08)",
          display: "flex",
          alignItems: "center",
          gap: 10,
          fontSize: 18,
          scale: String(1 - 0.05 * actionPress),
        }}
      >
        <Icon name={icon} size={20} color={C.textDim} />
        {action}
      </div>
    </div>
    <div style={{ display: "flex", flexDirection: "column", gap: 6, flex: 1 }}>{children}</div>
    {footer}
  </div>
);

const AppLine: React.FC<{ name: string; appear: number; dim?: boolean; children: React.ReactNode }> = ({
  name,
  appear,
  dim,
  children,
}) => (
  <div
    style={{
      height: 62,
      display: "flex",
      alignItems: "center",
      gap: 16,
      padding: "0 12px",
      borderRadius: 12,
      background: "rgba(255,255,255,0.035)",
      opacity: appear * (dim ? 0.6 : 1),
      translate: `0px ${(1 - appear) * 16}px`,
    }}
  >
    <Avatar name={name} palette={paletteFor(name)} size={40} />
    <div style={{ flex: 1, fontSize: 22, fontWeight: 550 }}>{name}</div>
    {children}
  </div>
);

const slideIn = (frame: number, start: number, dx: number): React.CSSProperties => {
  const t = reveal(frame, start, 24);
  return { opacity: t, translate: `${(1 - t) * dx}px 0px` };
};
