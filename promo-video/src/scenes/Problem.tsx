import { AbsoluteFill, Easing, interpolate, useCurrentFrame } from "remotion";
import { Eyebrow, Headline, Subline } from "../components/Text";
import { Caret, Terminal } from "../components/Terminal";
import { reveal } from "../anim";
import { C } from "../theme";

const pad = (s: string, n: number) => (s.length > n - 1 ? s.slice(0, n - 2) + "… " : s.padEnd(n));

const TABLE: [string, string, string, string][] = [
  ["Git", "Git.Git", "2.53.0", "2.56.0.4"],
  ["Node.js LTS", "OpenJS.NodeJS.LTS", "24.11.0", "26.13.0"],
  ["Microsoft Visual Studio Code", "Microsoft.VisualStudioCode", "1.141.0", "1.143.1"],
  ["PowerToys", "Microsoft.PowerToys", "0.101.2", "0.102.3101"],
  ["Windows Terminal", "Microsoft.WindowsTerminal", "1.24.12741.0", "1.25.2733.0"],
  ["Mozilla Firefox (x64 en-US)", "Mozilla.Firefox", "158.0.1", "159.0"],
  ["Discord", "Discord.Discord", "1.0.9255", "1.0.9267"],
  ["Microsoft Visual C++ v14 Redist", "Microsoft.VCRedist.2015+.x64", "14.44.35211.0", "14.50.35719.0"],
];

const row = (cells: string[]) => pad(cells[0], 20) + pad(cells[1], 28) + pad(cells[2], 14) + cells[3];

const LINES = [
  row(["Name", "Id", "Version", "Available"]),
  "-".repeat(74),
  ...TABLE.map(row),
  "8 upgrades available.",
];

const FIRST = "winget upgrade";
const SECOND = "winget upgrade --id Microsoft.VisualStudioCode -e";

export const Problem = () => {
  const frame = useCurrentFrame();

  const typed1 = FIRST.slice(0, Math.max(0, Math.floor((frame - 8) * 1.2)));
  const shown = Math.max(0, Math.floor((frame - 24) / 2));
  const typed2 = SECOND.slice(0, Math.max(0, Math.floor((frame - 58) * 1.1)));
  const term = reveal(frame, 0, 26);

  return (
    <AbsoluteFill>
      <div style={{ position: "absolute", left: 110, top: 300, width: 660, display: "flex", flexDirection: "column", gap: 22 }}>
        <Eyebrow
          name="Problem eyebrow"
          style={{
            opacity: interpolate(frame, [4, 22], [0, 1], { extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(0.16, 1, 0.3, 1) }),
            translate: interpolate(frame, [4, 22], ["0px 24px", "0px 0px"], { extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(0.16, 1, 0.3, 1) }),
          }}
        >
          winget, the hard way
        </Eyebrow>
        <Headline
          name="Problem headline"
          style={{
            fontSize: 116,
            opacity: interpolate(frame, [8, 28], [0, 1], { extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(0.16, 1, 0.3, 1) }),
            translate: interpolate(frame, [8, 28], ["0px 40px", "0px 0px"], { extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(0.16, 1, 0.3, 1) }),
          }}
        >
          {"Squinting\nat tables?"}
        </Headline>
        <Subline
          name="Problem subline"
          style={{
            opacity: interpolate(frame, [16, 36], [0, 1], { extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(0.16, 1, 0.3, 1) }),
            translate: interpolate(frame, [16, 36], ["0px 30px", "0px 0px"], { extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(0.16, 1, 0.3, 1) }),
          }}
        >
          Reading winget's output and typing package IDs by hand gets old fast.
        </Subline>
      </div>

      <div style={{ position: "absolute", left: 830, top: 210, perspective: 2200 }}>
        <Terminal
          width={990}
          height={660}
          title="Windows PowerShell"
          fontSize={20}
          style={{
            opacity: term,
            translate: `${(1 - term) * 120}px 0px`,
            rotate: `y ${-14 + 8 * term}deg`,
          }}
        >
          <div>
            <span style={{ color: C.prompt }}>PS C:\&gt; </span>
            {typed1}
            {shown === 0 ? <Caret frame={frame} /> : null}
          </div>
          {LINES.slice(0, shown).map((l, i) => (
            <div key={i} style={{ color: i < 2 ? C.textFaint : C.terminalText }}>
              {l}
            </div>
          ))}
          {shown >= LINES.length ? (
            <div style={{ marginTop: 26 }}>
              <span style={{ color: C.prompt }}>PS C:\&gt; </span>
              <span style={{ color: "#F7C266" }}>{typed2}</span>
              <Caret frame={frame} />
            </div>
          ) : null}
        </Terminal>
      </div>
    </AbsoluteFill>
  );
};
