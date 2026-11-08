import type React from "react";
import { AbsoluteFill, interpolate, useCurrentFrame } from "remotion";
import { SceneTitle } from "../components/SceneTitle";
import { Avatar, ClickRipple, Cursor, Icon, ProgressRing } from "../components/ui";
import { PACKS, paletteFor, type Pack } from "../data";
import { mix, progress, reveal, travel } from "../anim";
import { C, FONT, PALETTE } from "../theme";

const GRID = { x: 180, y: 446, w: 500, h: 240, gap: 30 };
const CHIPS = ["PowerToys", "7-Zip", "Firefox", "VLC", "Python"];
const INSTALL_BTN = { x: GRID.x + GRID.w + GRID.gap + GRID.w - 26 - 70, y: GRID.y + GRID.h - 26 - 23 };

/** Scene: Discover page with catalogue search, popular searches and the six starter packs. */
export const Discover = () => {
  const frame = useCurrentFrame();

  const press = interpolate(frame, [128, 132, 138], [0, 1, 0], { extrapolateLeft: "clamp", extrapolateRight: "clamp" });
  const installing = progress(frame, 136, 40);
  const move = travel(frame, 100, 28);

  return (
    <AbsoluteFill style={{ fontFamily: FONT, color: C.text }}>
      <SceneTitle
        left={GRID.x}
        top={70}
        eyebrow="Discover"
        headline="New PC? Set it up in one go."
        subline="Search the whole winget catalogue, or install a starter pack."
      />

      {/* search + popular searches */}
      <div style={{ position: "absolute", left: GRID.x, top: 340, width: 1560, display: "flex", alignItems: "center", gap: 14 }}>
        <div
          style={{
            width: 720,
            height: 72,
            borderRadius: 14,
            background: "rgba(255,255,255,0.065)",
            border: "1px solid rgba(255,255,255,0.1)",
            borderBottom: "2px solid #60A5FA",
            display: "flex",
            alignItems: "center",
            gap: 16,
            padding: "0 24px",
            boxSizing: "border-box",
            fontSize: 24,
            color: C.textFaint,
            ...grow(frame, 16),
          }}
        >
          <Icon name="discover" size={28} color={C.textDim} />
          Search the winget catalogue
        </div>
        <div style={{ width: 10 }} />
        {CHIPS.map((chip, i) => (
          <div
            key={chip}
            style={{
              height: 52,
              padding: "0 22px",
              borderRadius: 26,
              background: "rgba(255,255,255,0.06)",
              border: "1px solid rgba(255,255,255,0.1)",
              display: "flex",
              alignItems: "center",
              fontSize: 21,
              color: C.textDim,
              ...grow(frame, 22 + i * 3),
            }}
          >
            {chip}
          </div>
        ))}
      </div>

      {PACKS.map((pack, i) => {
        const col = i % 3;
        const row = Math.floor(i / 3);
        const developer = i === 1;
        return (
          <PackCard
            key={pack.title}
            pack={pack}
            style={{
              position: "absolute",
              left: GRID.x + col * (GRID.w + GRID.gap),
              top: GRID.y + row * (GRID.h + GRID.gap),
              ...cardIn(frame, 30 + i * 5),
            }}
            highlight={developer ? reveal(frame, 126, 12) : 0}
            press={developer ? press : 0}
            installing={developer ? installing : 0}
          />
        );
      })}

      <ClickRipple x={INSTALL_BTN.x} y={INSTALL_BTN.y} t={progress(frame, 131, 16)} />
      <Cursor
        x={mix(1500, INSTALL_BTN.x - 8, move)}
        y={mix(1100, INSTALL_BTN.y - 8, move)}
        press={press}
        opacity={reveal(frame, 100, 8)}
      />
    </AbsoluteFill>
  );
};

const PackCard: React.FC<{
  pack: Pack;
  style: React.CSSProperties;
  highlight: number;
  press: number;
  installing: number;
}> = ({ pack, style, highlight, press, installing }) => {
  const [from, to] = PALETTE[pack.palette];
  const extra = pack.apps.length - 4;
  return (
    <div
      style={{
        width: GRID.w,
        height: GRID.h,
        boxSizing: "border-box",
        borderRadius: 20,
        padding: 26,
        background: `linear-gradient(160deg, rgba(255,255,255,${0.075 + 0.03 * highlight}), rgba(255,255,255,0.03))`,
        border: `1px solid rgba(255,255,255,0.09)`,
        boxShadow: `0 0 0 ${3 * highlight}px rgba(96,165,250,${0.9 * highlight}), 0 30px 60px rgba(0,0,0,0.35), 0 0 ${60 * highlight}px rgba(47,123,230,${0.45 * highlight})`,
        display: "flex",
        flexDirection: "column",
        ...style,
      }}
    >
      <div style={{ display: "flex", alignItems: "center", gap: 18 }}>
        <div
          style={{
            width: 64,
            height: 64,
            borderRadius: 16,
            background: `linear-gradient(135deg, ${from}, ${to})`,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            boxShadow: "inset 0 1px 0 rgba(255,255,255,0.25)",
          }}
        >
          <Icon name={pack.icon} size={34} color="white" strokeWidth={2} />
        </div>
        <div>
          <div style={{ fontSize: 30, fontWeight: 650 }}>{pack.title}</div>
          <div style={{ fontSize: 19, color: C.textFaint }}>{pack.apps.length} apps</div>
        </div>
      </div>
      <div style={{ fontSize: 20, color: C.textDim, marginTop: 16, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>
        {pack.blurb}
      </div>
      <div style={{ flex: 1 }} />
      <div style={{ display: "flex", alignItems: "center" }}>
        {pack.apps.slice(0, 4).map((app, i) => (
          <Avatar
            key={app}
            name={app}
            palette={paletteFor(app)}
            size={44}
            style={{ marginLeft: i === 0 ? 0 : -8, border: "2px solid #1F2330" }}
          />
        ))}
        {extra > 0 ? (
          <div style={{ marginLeft: 10, fontSize: 18, color: C.textFaint, fontWeight: 600 }}>+{extra}</div>
        ) : null}
        <div style={{ flex: 1 }} />
        <div
          style={{
            height: 46,
            padding: "0 20px",
            borderRadius: 10,
            background: installing > 0 ? "rgba(47,123,230,0.25)" : highlight > 0.5 ? "#2F7BE6" : "rgba(255,255,255,0.08)",
            color: installing > 0 ? "#CFE2FF" : C.text,
            display: "flex",
            alignItems: "center",
            gap: 10,
            fontSize: 19,
            fontWeight: 600,
            scale: String(1 - 0.06 * press),
          }}
        >
          {installing > 0 ? (
            <>
              <ProgressRing value={installing} size={24} color="#60A5FA" />
              Installing…
            </>
          ) : (
            <>
              <Icon name="download" size={20} color={C.text} />
              Install
            </>
          )}
        </div>
      </div>
    </div>
  );
};

const grow = (frame: number, start: number): React.CSSProperties => {
  const t = reveal(frame, start, 16);
  return { opacity: t, scale: String(mix(0.9, 1, t)) };
};

const cardIn = (frame: number, start: number): React.CSSProperties => {
  const t = reveal(frame, start, 22);
  return { opacity: t, translate: `0px ${(1 - t) * 60}px`, scale: String(mix(0.94, 1, t)) };
};
