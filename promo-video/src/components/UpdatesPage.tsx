import type React from "react";
import { BRAND_GRADIENT, C, FONT } from "../theme";
import { KIND_TONE, type Upgrade } from "../data";
import { Avatar, Checkbox, DoneBadge, Icon, Pill, ProgressRing } from "./ui";

export type RowState = {
  appear: number; // 0 → 1 entrance
  checked: number; // 0 → 1 checkbox
  ring?: number; // install progress 0 → 1 (live progress)
  done?: number; // 0 → 1 result badge
  queued?: boolean;
};

export type HeroState =
  | { mode: "scanning"; t: number }
  | { mode: "ready"; count: number; meta: string; reveal: number; press: number }
  | { mode: "updating"; done: number; total: number; progress: number };

const Chevrons: React.FC<{ drift: number }> = ({ drift }) => (
  <svg width={360} height={240} viewBox="0 0 360 240" style={{ position: "absolute", right: 210, top: -10, opacity: 0.16 }}>
    {[0, 1, 2].map((i) => {
      const y = 70 + i * 52 - (drift % 52);
      return (
        <path
          key={i}
          d={`M 90 ${y + 52} L 180 ${y} L 270 ${y + 52}`}
          stroke="white"
          strokeWidth={22}
          fill="none"
          strokeLinecap="round"
          strokeLinejoin="round"
          opacity={0.4 + i * 0.3}
        />
      );
    })}
  </svg>
);

const Hero: React.FC<{ hero: HeroState; drift: number }> = ({ hero, drift }) => (
  <div
    style={{
      height: 160,
      borderRadius: 18,
      background: BRAND_GRADIENT,
      position: "relative",
      overflow: "hidden",
      display: "flex",
      alignItems: "center",
      padding: "0 36px",
      boxShadow: "0 18px 40px rgba(47,123,230,0.28), inset 0 1px 0 rgba(255,255,255,0.25)",
    }}
  >
    <Chevrons drift={drift} />
    {hero.mode === "scanning" ? (
      <div style={{ flex: 1 }}>
        <div style={{ fontSize: 36, fontWeight: 600 }}>Checking for updates…</div>
        <div style={{ marginTop: 18, width: 420, height: 6, borderRadius: 6, background: "rgba(255,255,255,0.25)", overflow: "hidden" }}>
          <div
            style={{
              width: 140,
              height: 6,
              borderRadius: 6,
              background: "white",
              translate: `${-140 + ((hero.t * 560) % 560)}px 0px`,
            }}
          />
        </div>
      </div>
    ) : null}
    {hero.mode === "ready" ? (
      <>
        <div style={{ flex: 1, opacity: hero.reveal, translate: `0px ${(1 - hero.reveal) * 16}px` }}>
          <div style={{ display: "flex", alignItems: "baseline", gap: 18 }}>
            <div style={{ fontSize: 78, fontWeight: 650, lineHeight: 1 }}>{hero.count}</div>
            <div style={{ fontSize: 32, fontWeight: 600 }}>updates available</div>
          </div>
          <div style={{ marginTop: 10, fontSize: 21, color: "rgba(255,255,255,0.86)" }}>{hero.meta}</div>
        </div>
        <div
          style={{
            height: 60,
            padding: "0 30px",
            borderRadius: 12,
            background: "white",
            color: "#1F4FC9",
            display: "flex",
            alignItems: "center",
            gap: 12,
            fontSize: 23,
            fontWeight: 650,
            scale: String(1 - 0.06 * hero.press),
            boxShadow: "0 8px 20px rgba(0,0,0,0.18)",
            opacity: hero.reveal,
          }}
        >
          <Icon name="download" size={24} color="#1F4FC9" strokeWidth={2.2} />
          Update all
        </div>
      </>
    ) : null}
    {hero.mode === "updating" ? (
      <div style={{ flex: 1 }}>
        <div style={{ display: "flex", alignItems: "baseline", gap: 16 }}>
          <div style={{ fontSize: 44, fontWeight: 650 }}>
            Updating {Math.min(hero.done + 1, hero.total)} of {hero.total}
          </div>
          <div style={{ fontSize: 22, color: "rgba(255,255,255,0.86)" }}>{Math.round(hero.progress * 100)}%</div>
        </div>
        <div style={{ marginTop: 18, width: 560, height: 8, borderRadius: 8, background: "rgba(255,255,255,0.25)" }}>
          <div style={{ width: 560 * hero.progress, height: 8, borderRadius: 8, background: "white" }} />
        </div>
      </div>
    ) : null}
  </div>
);

const Row: React.FC<{ app: Upgrade; state: RowState }> = ({ app, state }) => {
  const live = state.ring !== undefined || state.done !== undefined || state.queued;
  return (
    <div
      style={{
        height: 78,
        borderRadius: 12,
        background: "rgba(255,255,255,0.045)",
        border: "1px solid rgba(255,255,255,0.04)",
        display: "flex",
        alignItems: "center",
        gap: 20,
        padding: "0 26px",
        opacity: state.appear,
        translate: `0px ${(1 - state.appear) * 24}px`,
        flexShrink: 0,
      }}
    >
      <Checkbox checked={state.checked} size={26} />
      <Avatar name={app.name} palette={app.palette} size={48} />
      <div style={{ flex: 1, minWidth: 0 }}>
        <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
          <div style={{ fontSize: 23, fontWeight: 600 }}>{app.name}</div>
          <Pill tone={KIND_TONE[app.kind]} size={15}>
            {app.kind}
          </Pill>
        </div>
        <div style={{ fontSize: 17, color: C.textFaint, marginTop: 4 }}>{app.id}</div>
      </div>
      {live ? (
        <div style={{ display: "flex", alignItems: "center", gap: 14, minWidth: 250, justifyContent: "flex-end" }}>
          {state.done !== undefined && state.done > 0 ? (
            <>
              <Pill tone="success" size={17} style={{ opacity: state.done }}>
                Updated to {app.to}
              </Pill>
              <DoneBadge t={state.done} size={36} />
            </>
          ) : state.ring !== undefined && state.ring > 0 ? (
            <>
              <div style={{ fontSize: 19, color: C.textDim }}>
                {state.ring < 0.5 ? "Downloading…" : "Installing…"}
              </div>
              <ProgressRing value={state.ring} size={36} color="#60A5FA" />
            </>
          ) : (
            <div style={{ fontSize: 19, color: C.textFaint }}>Queued</div>
          )}
        </div>
      ) : (
        <div style={{ display: "flex", alignItems: "center", gap: 12, fontSize: 20 }}>
          <span style={{ color: C.textFaint }}>{app.from}</span>
          <Icon name="arrowRight" size={20} color={C.textFaint} />
          <span style={{ color: C.text, fontWeight: 600 }}>{app.to}</span>
        </div>
      )}
    </div>
  );
};

/** The Updates page: header, hero card, selection toolbar and the list of available updates. */
export const UpdatesPage: React.FC<{
  hero: HeroState;
  rows: { app: Upgrade; state: RowState }[];
  selected: number;
  drift: number;
}> = ({ hero, rows, selected, drift }) => (
  <div style={{ padding: "28px 36px", fontFamily: FONT, color: C.text, display: "flex", flexDirection: "column" }}>
    <div style={{ display: "flex", alignItems: "center" }}>
      <div style={{ fontSize: 40, fontWeight: 650, flex: 1 }}>Updates</div>
      {[
        { icon: "backup" as const, label: "Backup" },
        { icon: "refresh" as const, label: "Refresh" },
      ].map((b) => (
        <div
          key={b.label}
          style={{
            marginLeft: 12,
            height: 46,
            padding: "0 18px",
            borderRadius: 9,
            background: "rgba(255,255,255,0.06)",
            border: "1px solid rgba(255,255,255,0.07)",
            display: "flex",
            alignItems: "center",
            gap: 10,
            fontSize: 19,
          }}
        >
          <Icon name={b.icon} size={20} color={C.textDim} />
          {b.label}
        </div>
      ))}
    </div>
    <div style={{ marginTop: 22 }}>
      <Hero hero={hero} drift={drift} />
    </div>
    <div style={{ marginTop: 22, display: "flex", alignItems: "center", gap: 14, fontSize: 19, color: C.textDim }}>
      <Checkbox checked={selected >= rows.length ? 1 : 0} size={24} />
      <span style={{ color: C.text }}>Select all</span>
      <span>·</span>
      <span>
        {selected} selected · {rows.length} shown
      </span>
      <div style={{ flex: 1 }} />
      <span>Sort: Update type</span>
      <Icon name="chevronDown" size={18} color={C.textDim} />
    </div>
    <div style={{ marginTop: 16, display: "flex", flexDirection: "column", gap: 8 }}>
      {rows.map((r) => (
        <Row key={r.app.id} app={r.app} state={r.state} />
      ))}
    </div>
  </div>
);
