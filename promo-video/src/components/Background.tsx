import { AbsoluteFill, useCurrentFrame, useVideoConfig } from "remotion";
import { C } from "../theme";

/**
 * Deep navy backdrop with slow-drifting brand-coloured light, shared by every scene so cuts feel continuous.
 */
export const Background = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const t = frame / fps;

  const a = `${46 + Math.sin(t * 0.32) * 14}% ${24 + Math.cos(t * 0.27) * 10}%`;
  const b = `${78 + Math.cos(t * 0.24) * 10}% ${74 + Math.sin(t * 0.3) * 10}%`;
  const c = `${14 + Math.sin(t * 0.21 + 1) * 8}% ${80 + Math.cos(t * 0.26) * 8}%`;

  return (
    <AbsoluteFill
      style={{
        background: [
          `radial-gradient(1100px 760px at ${a}, rgba(79, 95, 232, 0.30), transparent 70%)`,
          `radial-gradient(1000px 820px at ${b}, rgba(20, 151, 221, 0.22), transparent 70%)`,
          `radial-gradient(900px 700px at ${c}, rgba(124, 58, 237, 0.18), transparent 70%)`,
          C.bg,
        ].join(", "),
      }}
    >
      <AbsoluteFill
        style={{
          backgroundImage: "radial-gradient(rgba(255,255,255,0.075) 1.4px, transparent 1.4px)",
          backgroundSize: "40px 40px",
          backgroundPosition: `0px ${-(t * 6) % 40}px`,
          maskImage: "radial-gradient(ellipse 70% 65% at 50% 50%, black 20%, transparent 80%)",
          WebkitMaskImage: "radial-gradient(ellipse 70% 65% at 50% 50%, black 20%, transparent 80%)",
        }}
      />
      <AbsoluteFill
        style={{
          background: "radial-gradient(ellipse 85% 80% at 50% 50%, transparent 55%, rgba(0,0,0,0.55) 100%)",
        }}
      />
    </AbsoluteFill>
  );
};
