import type React from "react";
import { AbsoluteFill, interpolate, useCurrentFrame } from "remotion";
import { AppWindow } from "../components/AppWindow";
import { SceneTitle } from "../components/SceneTitle";
import { useShowcase } from "../components/Showcase";
import { UpdatesPage, type RowState } from "../components/UpdatesPage";
import { AppIcon, ClickRipple, Cursor, Icon } from "../components/ui";
import { UPGRADES } from "../data";
import { mix, progress, reveal, travel } from "../anim";
import { C } from "../theme";

const WIN = { x: 180, y: 330, w: 1560, h: 900 };
const BUTTON = { x: 1562, y: 568 };
const META = "2 major · 5 minor · 1 patch";

/** Scene: Updraft scans and lists every update, then "Update all" is clicked. */
export const Updates = () => {
  const frame = useCurrentFrame();

  const enter = reveal(frame, 4, 34);
  const ready = frame >= 48;
  const press = interpolate(frame, [124, 128, 134], [0, 1, 0], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });

  const rows = UPGRADES.map((app, i) => ({
    app,
    state: {
      appear: reveal(frame, 54 + i * 5, 16),
      checked: reveal(frame, 134 + i * 2, 8),
    } satisfies RowState,
  }));
  const selected = rows.filter((r) => r.state.checked > 0.5).length;

  const move = travel(frame, 92, 30);
  const cursor = {
    x: mix(1660, BUTTON.x - 8, move),
    y: mix(1120, BUTTON.y - 6, move),
  };

  return (
    <AbsoluteFill>
      <SceneTitle
        left={WIN.x}
        top={70}
        eyebrow="Updates"
        headline="Every update. One click."
        subline="Scans on launch, with a major, minor and patch breakdown."
      />
      <div
        style={{
          position: "absolute",
          left: WIN.x,
          top: WIN.y,
          perspective: 2400,
        }}
      >
        <AppWindow
          width={WIN.w}
          height={WIN.h}
          page="updates"
          updatesCount={ready ? UPGRADES.length : undefined}
          style={{
            opacity: enter,
            translate: `0px ${(1 - enter) * 260}px`,
            rotate: `x ${(1 - enter) * 16}deg`,
            transformOrigin: "50% 0%",
          }}
        >
          <UpdatesPage
            hero={
              ready
                ? {
                    mode: "ready",
                    count: Math.round(
                      interpolate(frame, [48, 70], [0, UPGRADES.length], {
                        extrapolateRight: "clamp",
                      }),
                    ),
                    meta: META,
                    reveal: reveal(frame, 48, 16),
                    press,
                  }
                : { mode: "scanning", t: frame / 30 }
            }
            rows={rows}
            selected={selected}
            drift={frame * 0.6}
          />
        </AppWindow>
      </div>
      <ClickRipple x={BUTTON.x} y={BUTTON.y} t={progress(frame, 127, 16)} />
      <Cursor
        x={cursor.x}
        y={cursor.y}
        press={press}
        opacity={reveal(frame, 92, 8)}
      />
    </AbsoluteFill>
  );
};

/** Scene: every selected app installs with live progress, mirrored on the taskbar button. */
export const LiveProgress = () => {
  const frame = useCurrentFrame();

  const rows = UPGRADES.map((app, i) => {
    const start = 8 + i * 24;
    const ring = progress(frame, start, 26);
    const done = reveal(frame, start + 26, 10);
    return {
      app,
      ring,
      state: {
        appear: 1,
        checked: 1,
        ring: ring > 0 ? ring : undefined,
        done: done > 0 ? done : undefined,
        queued: ring === 0,
      } satisfies RowState,
    };
  });
  const total = rows.reduce((sum, r) => sum + r.ring, 0) / rows.length;
  const doneCount = rows.filter((r) => r.ring >= 1).length;
  const bar = reveal(frame, 10, 22);
  const showcase = useShowcase();

  return (
    <AbsoluteFill>
      <SceneTitle
        left={WIN.x}
        top={70}
        eyebrow="Live progress"
        headline="Watch it happen, live."
        subline="A progress ring for every app, and on the taskbar too."
      />
      <div style={{ position: "absolute", left: WIN.x, top: WIN.y }}>
        <AppWindow
          width={WIN.w}
          height={WIN.h}
          page="updates"
          updatesCount={UPGRADES.length - doneCount}
        >
          <UpdatesPage
            hero={{
              mode: "updating",
              done: doneCount,
              total: UPGRADES.length,
              progress: total,
            }}
            rows={rows.map(({ app, state }) => ({ app, state }))}
            selected={UPGRADES.length}
            drift={(frame + 180) * 0.6}
          />
        </AppWindow>
      </div>

      {/* Windows taskbar with Updraft's progress */}
      {showcase ? null : (
        <div
          style={{
            position: "absolute",
            left: 0,
            right: 0,
            bottom: 0,
            height: 84,
            background: "rgba(22, 26, 36, 0.86)",
            backdropFilter: "blur(24px)",
            borderTop: "1px solid rgba(255,255,255,0.08)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            gap: 14,
            translate: `0px ${(1 - bar) * 90}px`,
          }}
        >
          <WinLogo />
          <TaskbarSlot>
            <Icon name="discover" size={30} color={C.textDim} />
          </TaskbarSlot>
          <TaskbarSlot>
            <div
              style={{
                width: 32,
                height: 26,
                borderRadius: 5,
                background: "linear-gradient(180deg,#FBBF24,#D97706)",
              }}
            />
          </TaskbarSlot>
          <TaskbarSlot>
            <Icon name="terminal" size={32} color="#9CC3FF" />
          </TaskbarSlot>
          <TaskbarSlot active>
            <AppIcon size={38} />
            <div
              style={{
                position: "absolute",
                left: 8,
                right: 8,
                bottom: 5,
                height: 5,
                borderRadius: 5,
                background: "rgba(255,255,255,0.15)",
              }}
            >
              <div
                style={{
                  width: `${total * 100}%`,
                  height: 5,
                  borderRadius: 5,
                  background: "#22C55E",
                }}
              />
            </div>
          </TaskbarSlot>
        </div>
      )}
    </AbsoluteFill>
  );
};

const TaskbarSlot: React.FC<{
  children: React.ReactNode;
  active?: boolean;
}> = ({ children, active }) => (
  <div
    style={{
      width: 64,
      height: 64,
      borderRadius: 10,
      position: "relative",
      background: active ? "rgba(255,255,255,0.09)" : "transparent",
      display: "flex",
      alignItems: "center",
      justifyContent: "center",
    }}
  >
    {children}
  </div>
);

const WinLogo = () => (
  <div
    style={{
      width: 64,
      height: 64,
      display: "flex",
      alignItems: "center",
      justifyContent: "center",
    }}
  >
    <div style={{ display: "grid", gridTemplateColumns: "15px 15px", gap: 3 }}>
      {[0, 1, 2, 3].map((i) => (
        <div
          key={i}
          style={{
            width: 15,
            height: 15,
            borderRadius: 2,
            background: "linear-gradient(135deg,#60A5FA,#2563EB)",
          }}
        />
      ))}
    </div>
  </div>
);
