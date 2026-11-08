import { AbsoluteFill, Easing, interpolate, random, useCurrentFrame } from "remotion";
import { Headline, Subline } from "../components/Text";
import { Icon, LogoMark } from "../components/ui";
import { mix, reveal } from "../anim";
import { BRAND_GRADIENT, C, FONT } from "../theme";

const PARTICLES = new Array(16).fill(0).map((_, i) => ({
  x: random(`x${i}`) * 1920,
  speed: 1.2 + random(`s${i}`) * 2.2,
  size: 26 + random(`z${i}`) * 46,
  offset: random(`o${i}`) * 1200,
  opacity: 0.05 + random(`a${i}`) * 0.12,
}));

/** Closing card: logo, name and where to get it, with chevrons drifting upward. */
export const Outro = () => {
  const frame = useCurrentFrame();

  const logo = reveal(frame, 0, 24);
  const url = reveal(frame, 34, 18);

  return (
    <AbsoluteFill style={{ fontFamily: FONT, color: C.text, alignItems: "center", justifyContent: "center" }}>
      {PARTICLES.map((p, i) => {
        const y = 1200 - ((frame * p.speed + p.offset) % 1400);
        return (
          <svg key={i} width={p.size} height={p.size * 0.6} viewBox="0 0 60 36" style={{ position: "absolute", left: p.x, top: y, opacity: p.opacity * reveal(frame, 0, 20) }}>
            <path d="M 6 30 L 30 8 L 54 30" stroke="#8AB8F7" strokeWidth={8} fill="none" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
        );
      })}

      <div
        style={{
          position: "absolute",
          width: 1000,
          height: 1000,
          borderRadius: 1000,
          background: "radial-gradient(circle, rgba(47,123,230,0.38), rgba(79,95,232,0.1) 45%, transparent 70%)",
          opacity: logo,
          top: -40,
        }}
      />

      <div style={{ display: "flex", flexDirection: "column", alignItems: "center" }}>
        <LogoMark
          size={210}
          rise={[reveal(frame, 12, 18), reveal(frame, 8, 18), reveal(frame, 4, 18)]}
          style={{ opacity: logo, scale: String(mix(0.75, 1, logo)), filter: "drop-shadow(0 30px 60px rgba(47,123,230,0.55))" }}
        />
        <Headline
          name="Outro wordmark"
          style={{
            marginTop: 26,
            fontSize: 168,
            fontWeight: 700,
            lineHeight: 1,
            letterSpacing: "-0.04em",
            opacity: interpolate(frame, [10, 30], [0, 1], { extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(0.16, 1, 0.3, 1) }),
            translate: interpolate(frame, [10, 30], ["0px 50px", "0px 0px"], { extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(0.16, 1, 0.3, 1) }),
          }}
        >
          UpInstall
        </Headline>
        <Subline
          name="Outro tagline"
          style={{
            marginTop: 20,
            fontSize: 48,
            textAlign: "center",
            opacity: interpolate(frame, [20, 40], [0, 1], { extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(0.16, 1, 0.3, 1) }),
            translate: interpolate(frame, [20, 40], ["0px 30px", "0px 0px"], { extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(0.16, 1, 0.3, 1) }),
          }}
        >
          Free and open source for Windows 10 &amp; 11
        </Subline>

        <div
          style={{
            marginTop: 48,
            height: 84,
            padding: "0 40px",
            borderRadius: 42,
            background: BRAND_GRADIENT,
            display: "flex",
            alignItems: "center",
            gap: 16,
            fontSize: 38,
            fontWeight: 650,
            boxShadow: "0 20px 50px rgba(47,123,230,0.45), inset 0 1px 0 rgba(255,255,255,0.3)",
            opacity: url,
            scale: String(mix(0.9, 1, url)),
          }}
        >
          <Icon name="download" size={36} color="white" strokeWidth={2.2} />
          bilalansari.in/upinstall
        </div>
        <div style={{ marginTop: 22, fontSize: 30, color: C.textFaint, opacity: reveal(frame, 44, 18) }}>
          github.com/bilal8492/upinstall
        </div>
      </div>
    </AbsoluteFill>
  );
};
