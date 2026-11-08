import type React from "react";
import { Easing, interpolate, useCurrentFrame } from "remotion";
import { useShowcase } from "./Showcase";
import { Eyebrow, Headline, Subline } from "./Text";

/** Eyebrow, headline and supporting line, staggered in. Shared by the feature scenes. */
export const SceneTitle: React.FC<{
  eyebrow: string;
  headline: string;
  subline: string;
  left: number;
  top: number;
  width?: number;
  headlineSize?: number;
  align?: "left" | "center";
}> = ({ eyebrow, headline, subline, left, top, width, headlineSize = 104, align = "left" }) => {
  const frame = useCurrentFrame();
  const showcase = useShowcase();
  if (showcase) return null;
  return (
    <div
      style={{
        position: "absolute",
        left,
        top,
        width,
        display: "flex",
        flexDirection: "column",
        alignItems: align === "center" ? "center" : "flex-start",
        textAlign: align,
        gap: 12,
      }}
    >
      <Eyebrow
        name="Eyebrow"
        style={{
          opacity: interpolate(frame, [2, 20], [0, 1], { extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(0.16, 1, 0.3, 1) }),
          translate: interpolate(frame, [2, 20], ["0px 24px", "0px 0px"], { extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(0.16, 1, 0.3, 1) }),
        }}
      >
        {eyebrow}
      </Eyebrow>
      <Headline
        name="Headline"
        style={{
          fontSize: headlineSize,
          opacity: interpolate(frame, [6, 26], [0, 1], { extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(0.16, 1, 0.3, 1) }),
          translate: interpolate(frame, [6, 26], ["0px 40px", "0px 0px"], { extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(0.16, 1, 0.3, 1) }),
        }}
      >
        {headline}
      </Headline>
      <Subline
        name="Subline"
        style={{
          marginTop: 6,
          opacity: interpolate(frame, [12, 32], [0, 1], { extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(0.16, 1, 0.3, 1) }),
          translate: interpolate(frame, [12, 32], ["0px 30px", "0px 0px"], { extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(0.16, 1, 0.3, 1) }),
        }}
      >
        {subline}
      </Subline>
    </div>
  );
};
