# Updraft promo video

A ~47 second, 1920×1080 / 30 fps explainer for Updraft, built with [Remotion](https://www.remotion.dev).
It has no audio track yet, so music or a voice-over can be added later.

## Commands

```console
npm i                 # install dependencies
npm run dev           # open Remotion Studio to preview and tweak
npm run render        # render out/updraft-promo.mp4 (full quality)
npm run site-assets   # refresh the website media in ../docs/assets (run after render)
```

`site-assets` writes the web-encoded video, its poster, the social preview image and the product stills used on
the website. The stills come from the `Website` compositions, which reuse the video's scenes without their
headlines.

Render a few frames as PNGs to check a layout quickly:

```console
npx remotion render UpdraftPromo out/frames --frames=70,300,810 --image-format=png
```

## Scenes

| # | Scene | Frames | What it shows |
| - | ----- | ------ | ------------- |
| 1 | Intro | 105 | Logo, wordmark and tagline |
| 2 | Problem | 135 | `winget upgrade` table in a terminal: "Squinting at tables?" |
| 3 | Updates | 180 | Scan on launch, major / minor / patch breakdown, "Update all" |
| 4 | Live progress | 165 | Progress rings per app and taskbar progress |
| 5 | Details | 165 | Detail pane and right-click menu: skip a version, skip an app, pin, choose a version |
| 6 | Discover | 180 | Catalogue search, popular searches and the six starter packs |
| 7 | Backup | 150 | Export to JSON, restore on a new PC (already-installed apps skipped) |
| 8 | Script | 180 | Copy script, run as administrator, run in Windows Terminal |
| 9 | Native | 165 | Fluent design, keyboard shortcuts, history, x64 + ARM64, privacy, open source |
| 10 | Outro | 150 | Logo, website and repository |

The scenes sit in one `TransitionSeries` (`src/UpdraftPromo.tsx`). Each of the 9 transitions overlaps by
18 frames, so the total is 1575 − 162 = **1413 frames**. If you change a scene length, update
`durationInFrames` in `src/Root.tsx` too.

## Where things live

```
src/
  Root.tsx              composition registration (1920×1080, 30 fps)
  UpdraftPromo.tsx      scene order and transitions
  theme.ts              colours and fonts taken from Updraft/Styles.xaml
  data.ts               sample apps and starter packs (mirrors DemoData.cs / Catalog.cs)
  components/           app window, updates page, terminal, text, icons, cursor, background
  scenes/               one file per scene
public/icon.png         the app icon
```

Text uses Segoe UI Variable and Cascadia Code, which ship with Windows 11. Rendering on another OS falls back to
system fonts.

Remotion is free for individuals and teams of up to 3; larger companies need a
[company license](https://www.remotion.pro/license).
