import type { TransitionPresentation, TransitionPresentationComponentProps } from "@remotion/transitions";
import { AbsoluteFill, interpolate } from "remotion";

type Props = Record<string, never>;

/** Cross-dissolve with a gentle push-in and blur: the outgoing scene recedes, the next one settles into place. */
const SoftZoomPresentation = ({
  children,
  presentationDirection,
  presentationProgress: p,
}: TransitionPresentationComponentProps<Props>) => {
  const entering = presentationDirection === "entering";
  return (
    <AbsoluteFill
      style={{
        opacity: entering ? interpolate(p, [0, 0.6], [0, 1], { extrapolateRight: "clamp" }) : interpolate(p, [0.2, 1], [1, 0], { extrapolateLeft: "clamp" }),
        scale: String(entering ? interpolate(p, [0, 1], [1.06, 1]) : interpolate(p, [0, 1], [1, 0.95])),
        filter: `blur(${entering ? (1 - p) * 10 : p * 10}px)`,
      }}
    >
      {children}
    </AbsoluteFill>
  );
};

export const softZoom = (): TransitionPresentation<Props> => ({
  component: SoftZoomPresentation,
  props: {},
});
