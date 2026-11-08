import type React from "react";
import { AbsoluteFill, interpolate, useCurrentFrame } from "remotion";
import { AppWindow } from "../components/AppWindow";
import { SceneTitle } from "../components/SceneTitle";
import { Avatar, ClickRipple, Cursor, Icon, type IconName, Pill } from "../components/ui";
import { KIND_TONE, UPGRADES } from "../data";
import { mix, progress, reveal, travel } from "../anim";
import { C, FONT, MONO } from "../theme";

const WIN = { x: 720, y: 100, w: 1110, h: 880 };
const MENU = { x: 1010, y: 300, w: 390 };

type MenuItem = { icon: IconName; label: string } | "divider";
const MENU_ITEMS: MenuItem[] = [
  { icon: "download", label: "Update now" },
  "divider",
  { icon: "skip", label: "Skip this version" },
  { icon: "close", label: "Skip this app" },
  { icon: "pin", label: "Pin version" },
  { icon: "tag", label: "Choose version…" },
  "divider",
  { icon: "copy", label: "Copy id" },
  { icon: "globe", label: "Open homepage" },
];
const ITEM_H = 52;
const DIVIDER_H = 13;

// Top of each menu entry, relative to the menu.
const itemTops = (() => {
  let y = 8;
  return MENU_ITEMS.map((it) => {
    const top = y;
    y += it === "divider" ? DIVIDER_H : ITEM_H;
    return top;
  });
})();
const itemCenterY = (index: number) => MENU.y + itemTops[index] + ITEM_H / 2;

/** Scene: the detail pane and the right-click menu (skip a version, skip an app, pin, choose a version). */
export const Details = () => {
  const frame = useCurrentFrame();

  const enter = reveal(frame, 2, 28);
  const menuOpen = reveal(frame, 46, 12) * (1 - reveal(frame, 124, 8));
  const toast = reveal(frame, 130, 14);
  const rightClick = interpolate(frame, [38, 42, 46], [0, 1, 0], { extrapolateLeft: "clamp", extrapolateRight: "clamp" });
  const click = interpolate(frame, [114, 118, 122], [0, 1, 0], { extrapolateLeft: "clamp", extrapolateRight: "clamp" });

  // Cursor: onto the Git row, then down the menu to "Pin version", back up to "Skip this version".
  const toRow = travel(frame, 10, 26);
  const toPin = travel(frame, 62, 20);
  const toSkip = travel(frame, 90, 18);
  const rowPoint = { x: MENU.x - 6, y: MENU.y - 14 };
  const pinPoint = { x: MENU.x + 150, y: itemCenterY(4) - 8 };
  const skipPoint = { x: MENU.x + 170, y: itemCenterY(2) - 8 };
  let cx = mix(1300, rowPoint.x, toRow);
  let cy = mix(1060, rowPoint.y, toRow);
  cx = mix(cx, pinPoint.x, toPin);
  cy = mix(cy, pinPoint.y, toPin);
  cx = mix(cx, skipPoint.x, toSkip);
  cy = mix(cy, skipPoint.y, toSkip);

  const hovered = menuOpen > 0.5 ? itemTops.findIndex((top, i) => MENU_ITEMS[i] !== "divider" && cy + 8 >= MENU.y + top && cy + 8 < MENU.y + top + ITEM_H) : -1;
  const skipped = frame >= 124;

  return (
    <AbsoluteFill style={{ fontFamily: FONT, color: C.text }}>
      <SceneTitle
        left={110}
        top={290}
        width={580}
        eyebrow="Details & control"
        headline={"Your apps,\nyour rules."}
        subline="Release notes and details, then skip a version, pin an app or pick an exact release."
        headlineSize={100}
      />

      <div style={{ position: "absolute", left: WIN.x, top: WIN.y }}>
        <AppWindow
          width={WIN.w}
          height={WIN.h}
          page="updates"
          compact
          style={{ opacity: enter, translate: `${(1 - enter) * 140}px 0px`, scale: String(mix(0.96, 1, enter)) }}
        >
          <div style={{ display: "flex", height: "100%" }}>
            {/* list */}
            <div style={{ width: 430, padding: "24px 18px 0 22px", borderRight: `1px solid ${C.divider}`, flexShrink: 0 }}>
              <div style={{ fontSize: 34, fontWeight: 650, marginBottom: 22 }}>Updates</div>
              <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
                {UPGRADES.map((app, i) => (
                  <div
                    key={app.id}
                    style={{
                      height: 72,
                      borderRadius: 12,
                      display: "flex",
                      alignItems: "center",
                      gap: 16,
                      padding: "0 16px",
                      position: "relative",
                      background: i === 0 ? "rgba(96,165,250,0.14)" : "rgba(255,255,255,0.035)",
                      opacity: reveal(frame, 10 + i * 3, 14),
                    }}
                  >
                    {i === 0 ? (
                      <div style={{ position: "absolute", left: 0, top: 24, width: 4, height: 24, borderRadius: 4, background: "#60A5FA" }} />
                    ) : null}
                    <Avatar name={app.name} palette={app.palette} size={44} />
                    <div style={{ flex: 1, minWidth: 0 }}>
                      <div style={{ fontSize: 21, fontWeight: 600, whiteSpace: "nowrap" }}>{app.name}</div>
                      <div style={{ fontSize: 16, color: C.textFaint, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>
                        {app.from} → {app.to}
                      </div>
                    </div>
                    {i === 0 && skipped ? (
                      <Pill tone="neutral" size={14} style={{ opacity: reveal(frame, 126, 10) }}>
                        Skipped
                      </Pill>
                    ) : null}
                  </div>
                ))}
              </div>
            </div>

            {/* detail pane */}
            <div style={{ flex: 1, padding: "30px 34px", display: "flex", flexDirection: "column", gap: 24, minWidth: 0 }}>
              <div style={{ display: "flex", alignItems: "center", gap: 20, ...fadeUp(frame, 16) }}>
                <Avatar name="Git" palette={4} size={72} />
                <div>
                  <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
                    <div style={{ fontSize: 40, fontWeight: 650 }}>Git</div>
                    <Pill tone={KIND_TONE.Minor} size={16}>
                      Minor
                    </Pill>
                  </div>
                  <div style={{ fontSize: 19, color: C.textFaint }}>Git.Git · winget</div>
                </div>
              </div>

              <div
                style={{
                  borderRadius: 14,
                  background: "rgba(255,255,255,0.05)",
                  border: `1px solid ${C.divider}`,
                  padding: "18px 24px",
                  display: "flex",
                  alignItems: "center",
                  ...fadeUp(frame, 22),
                }}
              >
                <div style={{ flex: 1 }}>
                  <div style={eyebrow}>INSTALLED</div>
                  <div style={{ fontSize: 32, fontWeight: 600, color: C.textDim }}>2.53.0</div>
                </div>
                <Icon name="arrowRight" size={30} color={C.brandText} />
                <div style={{ flex: 1, textAlign: "right" }}>
                  <div style={eyebrow}>LATEST</div>
                  <div style={{ fontSize: 32, fontWeight: 650, color: "#9CC3FF" }}>2.56.0.4</div>
                </div>
              </div>

              <div style={fadeUp(frame, 28)}>
                <div style={sectionHeader}>What's new</div>
                {[0.94, 0.8, 0.88, 0.5].map((w, i) => (
                  <div key={i} style={{ height: 13, width: `${w * 100}%`, borderRadius: 7, background: "rgba(255,255,255,0.1)", marginTop: 12 }} />
                ))}
              </div>

              <div style={fadeUp(frame, 34)}>
                <div style={sectionHeader}>Details</div>
                {[
                  ["Publisher", "The Git Development Community"],
                  ["License", "GPL-2.0"],
                  ["Homepage", "gitforwindows.org"],
                  ["Installer", "inno"],
                ].map(([k, v]) => (
                  <div key={k} style={{ display: "flex", fontSize: 19, marginTop: 10 }}>
                    <div style={{ width: 140, color: C.textFaint }}>{k}</div>
                    <div style={{ color: k === "Homepage" ? "#8AB8F7" : C.text }}>{v}</div>
                  </div>
                ))}
              </div>

              <div style={fadeUp(frame, 40)}>
                <div style={sectionHeader}>Command</div>
                <div
                  style={{
                    marginTop: 12,
                    borderRadius: 10,
                    background: C.terminal,
                    padding: "16px 20px",
                    fontFamily: MONO,
                    fontSize: 19,
                    color: C.terminalText,
                    display: "flex",
                    alignItems: "center",
                  }}
                >
                  <span style={{ color: C.prompt, marginRight: 12 }}>&gt;</span>
                  <span style={{ flex: 1 }}>winget upgrade --id Git.Git -e</span>
                  <Icon name="copy" size={20} color={C.textFaint} />
                </div>
              </div>
            </div>
          </div>
        </AppWindow>
      </div>

      {/* context menu */}
      <div
        style={{
          position: "absolute",
          left: MENU.x,
          top: MENU.y,
          width: MENU.w,
          padding: "8px 6px",
          boxSizing: "border-box",
          borderRadius: 14,
          background: "#282C38",
          border: "1px solid rgba(255,255,255,0.12)",
          boxShadow: "0 30px 70px rgba(0,0,0,0.55)",
          opacity: menuOpen,
          scale: String(mix(0.94, 1, menuOpen)),
          transformOrigin: "0 0",
          translate: `0px ${(1 - menuOpen) * -10}px`,
        }}
      >
        {MENU_ITEMS.map((it, i) =>
          it === "divider" ? (
            <div key={i} style={{ height: 1, margin: "6px 10px", background: "rgba(255,255,255,0.1)" }} />
          ) : (
            <div
              key={i}
              style={{
                height: ITEM_H,
                borderRadius: 8,
                display: "flex",
                alignItems: "center",
                gap: 16,
                padding: "0 16px",
                fontSize: 21,
                background: hovered === i ? "rgba(255,255,255,0.09)" : "transparent",
                color: i === 2 && click > 0.5 ? "#CFE2FF" : C.text,
              }}
            >
              <Icon name={it.icon} size={22} color={hovered === i ? C.text : C.textDim} />
              {it.label}
            </div>
          ),
        )}
      </div>

      {/* toast */}
      <div
        style={{
          position: "absolute",
          left: WIN.x + WIN.w / 2 - 270,
          top: WIN.y + WIN.h - 120,
          width: 540,
          height: 76,
          borderRadius: 14,
          background: "#282C38",
          border: "1px solid rgba(255,255,255,0.12)",
          boxShadow: "0 24px 60px rgba(0,0,0,0.5)",
          display: "flex",
          alignItems: "center",
          gap: 16,
          padding: "0 24px",
          boxSizing: "border-box",
          fontSize: 21,
          opacity: toast,
          translate: `0px ${(1 - toast) * 30}px`,
        }}
      >
        <Icon name="skip" size={24} color={C.brandText} />
        <div style={{ flex: 1 }}>Skipped Git 2.56.0.4</div>
        <div style={{ color: "#8AB8F7", fontWeight: 600 }}>Undo</div>
      </div>

      <ClickRipple x={rowPoint.x + 6} y={rowPoint.y + 6} t={progress(frame, 41, 14)} />
      <ClickRipple x={skipPoint.x + 6} y={skipPoint.y + 6} t={progress(frame, 117, 14)} />
      <Cursor x={cx} y={cy} press={Math.max(rightClick, click)} opacity={reveal(frame, 8, 8) * (1 - reveal(frame, 140, 10))} />
    </AbsoluteFill>
  );
};

const eyebrow: React.CSSProperties = { fontSize: 14, letterSpacing: "0.12em", color: C.textFaint, fontWeight: 600, marginBottom: 4 };
const sectionHeader: React.CSSProperties = { fontSize: 22, fontWeight: 650 };

const fadeUp = (frame: number, start: number): React.CSSProperties => {
  const t = reveal(frame, start, 16);
  return { opacity: t, translate: `0px ${(1 - t) * 18}px` };
};
