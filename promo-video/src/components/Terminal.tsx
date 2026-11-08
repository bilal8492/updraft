import type React from "react";
import { C, FONT, MONO } from "../theme";
import { Icon } from "./ui";

/** A Windows Terminal style window. Lines are passed in already typed/trimmed by the scene. */
export const Terminal: React.FC<{
  width: number;
  height: number;
  title: string;
  admin?: boolean;
  fontSize?: number;
  children: React.ReactNode;
  style?: React.CSSProperties;
}> = ({ width, height, title, admin = false, fontSize = 20, children, style }) => (
  <div
    style={{
      width,
      height,
      borderRadius: 16,
      overflow: "hidden",
      background: C.terminal,
      border: `1px solid ${C.edge}`,
      boxShadow: "0 50px 120px rgba(0,0,0,0.6), inset 0 1px 0 rgba(255,255,255,0.05)",
      display: "flex",
      flexDirection: "column",
      ...style,
    }}
  >
    <div style={{ height: 56, background: "#151B23", display: "flex", alignItems: "flex-end", padding: "0 0 0 12px", flexShrink: 0 }}>
      <div
        style={{
          height: 44,
          padding: "0 22px",
          borderTopLeftRadius: 10,
          borderTopRightRadius: 10,
          background: C.terminal,
          display: "flex",
          alignItems: "center",
          gap: 12,
          fontFamily: FONT,
          fontSize: 18,
          color: C.textDim,
        }}
      >
        {admin ? <Icon name="admin" size={20} color="#F7C266" /> : <Icon name="terminal" size={20} color={C.brandText} />}
        {title}
        <Icon name="close" size={14} color={C.textFaint} style={{ marginLeft: 18 }} />
      </div>
      <div style={{ flex: 1 }} />
      <div style={{ display: "flex", alignSelf: "stretch", alignItems: "center" }}>
        {(["minimize", "maximize", "close"] as const).map((n) => (
          <div key={n} style={{ width: 62, display: "flex", justifyContent: "center" }}>
            <Icon name={n} size={16} color={C.textDim} strokeWidth={1.4} />
          </div>
        ))}
      </div>
    </div>
    <div
      style={{
        flex: 1,
        padding: "22px 28px",
        fontFamily: MONO,
        fontSize,
        lineHeight: 1.55,
        color: C.terminalText,
        whiteSpace: "pre",
        overflow: "hidden",
      }}
    >
      {children}
    </div>
  </div>
);

/** Text cursor block that blinks every half second. */
export const Caret: React.FC<{ frame: number; color?: string }> = ({ frame, color = C.terminalText }) => (
  <span style={{ background: color, opacity: Math.floor(frame / 15) % 2 === 0 ? 0.9 : 0, color: "transparent" }}>_</span>
);
