import type React from "react";
import { C, FONT } from "../theme";
import { AppIcon, Icon, type IconName } from "./ui";

type Page = "updates" | "installed" | "discover" | "history";

const NAV: { key: Page; label: string; icon: IconName }[] = [
  { key: "updates", label: "Updates", icon: "updates" },
  { key: "installed", label: "Installed", icon: "installed" },
  { key: "discover", label: "Discover", icon: "discover" },
  { key: "history", label: "History", icon: "history" },
];

/**
 * A Windows 11 style UpInstall window (dark theme, Mica-like surface), drawn about 1.45× the real UI size so it
 * reads well in a 1080p video.
 */
export const AppWindow: React.FC<{
  width: number;
  height: number;
  page: Page;
  search?: string;
  updatesCount?: number;
  compact?: boolean;
  children: React.ReactNode;
  style?: React.CSSProperties;
}> = ({ width, height, page, search = "Filter updates", updatesCount, compact = false, children, style }) => (
  <div
    style={{
      width,
      height,
      borderRadius: 18,
      overflow: "hidden",
      background: `linear-gradient(160deg, rgba(36,40,54,0.98), ${C.window} 40%, #181B22)`,
      border: `1px solid ${C.edge}`,
      boxShadow: "0 50px 120px rgba(0,0,0,0.55), 0 0 0 1px rgba(0,0,0,0.4), inset 0 1px 0 rgba(255,255,255,0.06)",
      display: "flex",
      flexDirection: "column",
      fontFamily: FONT,
      color: C.text,
      ...style,
    }}
  >
    {/* title bar */}
    <div style={{ height: 62, display: "flex", alignItems: "center", padding: "0 0 0 22px", flexShrink: 0 }}>
      <AppIcon size={26} />
      <div style={{ marginLeft: 14, fontSize: 19, fontWeight: 500, color: C.textDim }}>UpInstall</div>
      <div style={{ flex: 1, display: "flex", justifyContent: "center" }}>
        <div
          style={{
            width: 460,
            height: 42,
            borderRadius: 9,
            background: "rgba(255,255,255,0.06)",
            border: "1px solid rgba(255,255,255,0.07)",
            borderBottom: "1px solid rgba(255,255,255,0.16)",
            display: "flex",
            alignItems: "center",
            padding: "0 16px",
            boxSizing: "border-box",
            fontSize: 18,
            color: C.textFaint,
            gap: 10,
          }}
        >
          <div style={{ flex: 1 }}>{search}</div>
          <Icon name="discover" size={20} color={C.textFaint} />
        </div>
      </div>
      <div style={{ display: "flex", alignItems: "center" }}>
        {(["minimize", "maximize", "close"] as const).map((n) => (
          <div key={n} style={{ width: 66, height: 62, display: "flex", alignItems: "center", justifyContent: "center" }}>
            <Icon name={n} size={18} color={C.textDim} strokeWidth={1.4} />
          </div>
        ))}
      </div>
    </div>

    <div style={{ flex: 1, display: "flex", minHeight: 0 }}>
      {/* navigation */}
      <div style={{ width: compact ? 76 : 252, padding: "8px 12px", display: "flex", flexDirection: "column", gap: 6, flexShrink: 0 }}>
        {NAV.map((item) => {
          const active = item.key === page;
          return (
            <div
              key={item.key}
              style={{
                height: 54,
                borderRadius: 10,
                display: "flex",
                alignItems: "center",
                gap: 16,
                padding: "0 16px",
                position: "relative",
                background: active ? "rgba(255,255,255,0.075)" : "transparent",
                color: active ? C.text : C.textDim,
                fontSize: 21,
                fontWeight: active ? 600 : 400,
              }}
            >
              {active ? (
                <div
                  style={{
                    position: "absolute",
                    left: 0,
                    top: 15,
                    width: 4,
                    height: 24,
                    borderRadius: 4,
                    background: "#60A5FA",
                  }}
                />
              ) : null}
              <Icon name={item.icon} size={24} color={active ? C.text : C.textDim} />
              {compact ? null : <div style={{ flex: 1 }}>{item.label}</div>}
              {!compact && item.key === "updates" && updatesCount !== undefined ? (
                <div
                  style={{
                    minWidth: 30,
                    height: 26,
                    borderRadius: 13,
                    background: C.brand2,
                    color: "white",
                    fontSize: 15,
                    fontWeight: 650,
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    padding: "0 8px",
                    boxSizing: "border-box",
                  }}
                >
                  {updatesCount}
                </div>
              ) : null}
            </div>
          );
        })}
        <div style={{ flex: 1 }} />
        <div
          style={{
            height: 54,
            display: "flex",
            alignItems: "center",
            gap: 16,
            padding: "0 16px",
            color: C.textDim,
            fontSize: 21,
          }}
        >
          <Icon name="settings" size={24} color={C.textDim} />
          {compact ? null : "Settings"}
        </div>
      </div>

      {/* page */}
      <div
        style={{
          flex: 1,
          minWidth: 0,
          borderTopLeftRadius: 14,
          background: "rgba(255,255,255,0.028)",
          borderTop: `1px solid ${C.divider}`,
          borderLeft: `1px solid ${C.divider}`,
          position: "relative",
          overflow: "hidden",
        }}
      >
        {children}
      </div>
    </div>
  </div>
);
