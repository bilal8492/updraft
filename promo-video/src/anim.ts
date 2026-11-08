import { Easing, interpolate } from "remotion";

// Shared easing curves for the mock-UI internals. Headlines keep their interpolate() calls inline so
// they stay editable in Remotion Studio.
export const easeOut = Easing.bezier(0.16, 1, 0.3, 1);
export const easeInOut = Easing.bezier(0.65, 0, 0.35, 1);

const clamp = { extrapolateLeft: "clamp", extrapolateRight: "clamp" } as const;

/** 0 → 1 between `start` and `start + duration`, eased out. */
export const reveal = (frame: number, start: number, duration = 18) =>
  interpolate(frame, [start, start + duration], [0, 1], { ...clamp, easing: easeOut });

/** 0 → 1 between `start` and `start + duration`, eased in and out (for movements). */
export const travel = (frame: number, start: number, duration = 24) =>
  interpolate(frame, [start, start + duration], [0, 1], { ...clamp, easing: easeInOut });

/** Linear 0 → 1 progress, clamped. */
export const progress = (frame: number, start: number, duration: number) =>
  interpolate(frame, [start, start + duration], [0, 1], clamp);

export const mix = (a: number, b: number, t: number) => a + (b - a) * t;

/** Style for an element that fades and rises into place. */
export const riseIn = (frame: number, start: number, distance = 30, duration = 18) => {
  const t = reveal(frame, start, duration);
  return { opacity: t, translate: `0px ${(1 - t) * distance}px` };
};

/** Style for an element that pops in with a small scale overshoot-free settle. */
export const popIn = (frame: number, start: number, from = 0.92, duration = 18) => {
  const t = reveal(frame, start, duration);
  return { opacity: t, scale: String(mix(from, 1, t)) };
};
