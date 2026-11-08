import type React from "react";
import { Interactive } from "remotion";
import { C, FONT } from "../theme";

// Scene copy. Each instance is its own editable item in Remotion Studio: change the words, size,
// colour and the inline entrance keyframes on the scene's JSX node.

type TextProps = {
  readonly children: React.ReactNode;
  readonly style?: React.CSSProperties;
};

const EyebrowInner = ({ children, style }: TextProps) => (
  <div
    style={{
      fontFamily: FONT,
      fontSize: 30,
      fontWeight: 650,
      letterSpacing: "0.16em",
      textTransform: "uppercase",
      color: C.brandText,
      margin: 0,
      ...style,
    }}
  >
    {children}
  </div>
);

const HeadlineInner = ({ children, style }: TextProps) => (
  <div
    style={{
      fontFamily: FONT,
      fontSize: 104,
      fontWeight: 650,
      lineHeight: 1.04,
      letterSpacing: "-0.025em",
      color: C.text,
      margin: 0,
      whiteSpace: "pre-line",
      ...style,
    }}
  >
    {children}
  </div>
);

const SublineInner = ({ children, style }: TextProps) => (
  <div
    style={{
      fontFamily: FONT,
      fontSize: 44,
      fontWeight: 400,
      lineHeight: 1.3,
      color: C.textDim,
      margin: 0,
      ...style,
    }}
  >
    {children}
  </div>
);

const textSchema = {
  ...Interactive.textSchema,
  ...Interactive.childrenSchema,
} as const;

export const Eyebrow = Interactive.create({ Component: EyebrowInner, layout: "none", schema: textSchema });
export const Headline = Interactive.create({ Component: HeadlineInner, layout: "none", schema: textSchema });
export const Subline = Interactive.create({ Component: SublineInner, layout: "none", schema: textSchema });
