import { AbsoluteFill, Easing, interpolate, useCurrentFrame } from "remotion";
import { Headline, Subline } from "../components/Text";
import { LogoMark } from "../components/ui";
import { reveal } from "../anim";
import { FONT } from "../theme";

export const Intro = () => {
  const frame = useCurrentFrame();

  const logo = reveal(frame, 0, 26);
  const glow = interpolate(frame, [0, 40, 105], [0, 1, 0.75], { extrapolateRight: "clamp" });

  return (
    <AbsoluteFill style={{ alignItems: "center", justifyContent: "center", fontFamily: FONT }}>
      <div
        style={{
          position: "absolute",
          width: 900,
          height: 900,
          borderRadius: 900,
          background: "radial-gradient(circle, rgba(47,123,230,0.45), rgba(79,95,232,0.12) 45%, transparent 70%)",
          opacity: glow,
          top: 40,
        }}
      />
      <div style={{ display: "flex", flexDirection: "column", alignItems: "center" }}>
        <LogoMark
          size={250}
          rise={[reveal(frame, 14, 20), reveal(frame, 9, 20), reveal(frame, 4, 20)]}
          style={{
            opacity: logo,
            scale: String(0.7 + 0.3 * logo),
            rotate: `${(1 - logo) * -8}deg`,
            filter: "drop-shadow(0 30px 60px rgba(47,123,230,0.55))",
          }}
        />
        <div style={{ overflow: "hidden", marginTop: 34, paddingBottom: 12 }}>
          <Headline
            name="Intro wordmark"
            style={{
              fontSize: 200,
              fontWeight: 700,
              lineHeight: 1,
              letterSpacing: "-0.04em",
              translate: interpolate(frame, [16, 40], ["0px 220px", "0px 0px"], {
                extrapolateLeft: "clamp",
                extrapolateRight: "clamp",
                easing: Easing.bezier(0.16, 1, 0.3, 1),
              }),
            }}
          >
            Updraft
          </Headline>
        </div>
        <Subline
          name="Intro tagline"
          style={{
            marginTop: 18,
            fontSize: 52,
            textAlign: "center",
            opacity: interpolate(frame, [32, 52], [0, 1], {
              extrapolateLeft: "clamp",
              extrapolateRight: "clamp",
              easing: Easing.bezier(0.16, 1, 0.3, 1),
            }),
            translate: interpolate(frame, [32, 52], ["0px 30px", "0px 0px"], {
              extrapolateLeft: "clamp",
              extrapolateRight: "clamp",
              easing: Easing.bezier(0.16, 1, 0.3, 1),
            }),
          }}
        >
          The simple, native Windows app for winget
        </Subline>
      </div>
    </AbsoluteFill>
  );
};
