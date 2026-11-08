import { TransitionSeries, linearTiming } from "@remotion/transitions";
import { fade } from "@remotion/transitions/fade";
import { slide } from "@remotion/transitions/slide";
import { AbsoluteFill, Easing } from "remotion";
import { Background } from "./components/Background";
import { softZoom } from "./components/SoftZoom";
import { Backup } from "./scenes/Backup";
import { Details } from "./scenes/Details";
import { Discover } from "./scenes/Discover";
import { Intro } from "./scenes/Intro";
import { Native } from "./scenes/Native";
import { Outro } from "./scenes/Outro";
import { Problem } from "./scenes/Problem";
import { Script } from "./scenes/Script";
import { LiveProgress, Updates } from "./scenes/Updates";

/**
 * Updraft promo, 1920×1080 at 30 fps (~47 s). Scenes sit in one TransitionSeries over a shared animated
 * background; each of the 9 transitions overlaps its neighbours by 18 frames. No audio yet.
 *
 * Total = 105 + 135 + 180 + 165 + 165 + 180 + 150 + 180 + 165 + 150 − 9 × 18 = 1413 frames.
 */
export const UpdraftPromo = () => {
  return (
    <AbsoluteFill style={{ backgroundColor: "#060A16" }}>
      <Background />
      <TransitionSeries>
        <TransitionSeries.Sequence name="Intro" durationInFrames={105}>
          <Intro />
        </TransitionSeries.Sequence>
        <TransitionSeries.Transition
          presentation={softZoom()}
          timing={linearTiming({ durationInFrames: 18, easing: Easing.bezier(0.65, 0, 0.35, 1) })}
        />
        <TransitionSeries.Sequence name="Problem" durationInFrames={135}>
          <Problem />
        </TransitionSeries.Sequence>
        <TransitionSeries.Transition
          presentation={softZoom()}
          timing={linearTiming({ durationInFrames: 18, easing: Easing.bezier(0.65, 0, 0.35, 1) })}
        />
        <TransitionSeries.Sequence name="Updates" durationInFrames={180}>
          <Updates />
        </TransitionSeries.Sequence>
        <TransitionSeries.Transition
          presentation={fade()}
          timing={linearTiming({ durationInFrames: 18, easing: Easing.bezier(0.65, 0, 0.35, 1) })}
        />
        <TransitionSeries.Sequence name="Live progress" durationInFrames={165}>
          <LiveProgress />
        </TransitionSeries.Sequence>
        <TransitionSeries.Transition
          presentation={slide({ direction: "from-right" })}
          timing={linearTiming({ durationInFrames: 18, easing: Easing.bezier(0.65, 0, 0.35, 1) })}
        />
        <TransitionSeries.Sequence name="Details" durationInFrames={165}>
          <Details />
        </TransitionSeries.Sequence>
        <TransitionSeries.Transition
          presentation={softZoom()}
          timing={linearTiming({ durationInFrames: 18, easing: Easing.bezier(0.65, 0, 0.35, 1) })}
        />
        <TransitionSeries.Sequence name="Discover" durationInFrames={180}>
          <Discover />
        </TransitionSeries.Sequence>
        <TransitionSeries.Transition
          presentation={slide({ direction: "from-right" })}
          timing={linearTiming({ durationInFrames: 18, easing: Easing.bezier(0.65, 0, 0.35, 1) })}
        />
        <TransitionSeries.Sequence name="Backup" durationInFrames={150}>
          <Backup />
        </TransitionSeries.Sequence>
        <TransitionSeries.Transition
          presentation={softZoom()}
          timing={linearTiming({ durationInFrames: 18, easing: Easing.bezier(0.65, 0, 0.35, 1) })}
        />
        <TransitionSeries.Sequence name="Script" durationInFrames={180}>
          <Script />
        </TransitionSeries.Sequence>
        <TransitionSeries.Transition
          presentation={softZoom()}
          timing={linearTiming({ durationInFrames: 18, easing: Easing.bezier(0.65, 0, 0.35, 1) })}
        />
        <TransitionSeries.Sequence name="Native" durationInFrames={165}>
          <Native />
        </TransitionSeries.Sequence>
        <TransitionSeries.Transition
          presentation={softZoom()}
          timing={linearTiming({ durationInFrames: 18, easing: Easing.bezier(0.65, 0, 0.35, 1) })}
        />
        <TransitionSeries.Sequence name="Outro" durationInFrames={150}>
          <Outro />
        </TransitionSeries.Sequence>
      </TransitionSeries>
    </AbsoluteFill>
  );
};
