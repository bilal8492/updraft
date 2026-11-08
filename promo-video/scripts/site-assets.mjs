// Renders the website's media into ../docs/assets: product stills, the video poster, the social preview
// image and a web-encoded copy of the promo video. Run `npm run site-assets` after `npm run render`.

import { execFileSync } from "node:child_process";
import { existsSync, mkdirSync, rmSync } from "node:fs";
import { join } from "node:path";

const tmp = "out/site";
const assets = "../docs/assets";
const video = "out/updraft-promo.mp4";

mkdirSync(tmp, { recursive: true });

const npx = (...args) =>
  execFileSync(process.platform === "win32" ? "npx.cmd" : "npx", args, { stdio: "inherit", shell: process.platform === "win32" });

const still = (id, frame, file) => npx("remotion", "still", id, join(tmp, file), `--frame=${frame}`, "--image-format=png", "--log=error");
const ffmpeg = (...args) => npx("remotion", "ffmpeg", "-y", "-hide_banner", "-loglevel", "error", ...args);

// Product stills with transparent backgrounds, cropped to the mockup.
const stills = [
  { id: "SiteUpdates", frame: 0, file: "updates.png" },
  { id: "SiteDetails", frame: 112, file: "details.png", crop: "1190:960:680:60" },
  { id: "SiteDiscover", frame: 160, file: "discover.png", crop: "1620:670:150:320" },
  { id: "SiteBackup", frame: 149, file: "backup.png", crop: "1620:660:150:330" },
  { id: "SiteScript", frame: 179, file: "script.png", crop: "1120:770:730:160" },
];

for (const s of stills) {
  still(s.id, s.frame, s.file);
  const out = join(assets, `shot-${s.file}`);
  // Full resolution, so the mockups stay sharp on high-DPI screens.
  ffmpeg("-i", join(tmp, s.file), ...(s.crop ? ["-vf", `crop=${s.crop}`] : []), out);
}

// Poster (the Updates scene, just before "Update all" is clicked) and social preview (the intro card).
still("UpdraftPromo", 318, "poster.png");
ffmpeg("-i", join(tmp, "poster.png"), "-vf", "scale=1600:-2", "-q:v", "3", join(assets, "video-poster.jpg"));
still("UpdraftPromo", 70, "og.png");
ffmpeg("-i", join(tmp, "og.png"), "-vf", "scale=1200:675,crop=1200:630:0:22", "-q:v", "3", join(assets, "og.jpg"));

// Web copy of the video: H.264, no audio track, moov atom up front so it starts streaming at once.
if (!existsSync(video)) throw new Error(`Render the video first: ${video} is missing.`);
ffmpeg("-i", video, "-c:v", "libx264", "-preset", "slow", "-crf", "24", "-pix_fmt", "yuv420p", "-movflags", "+faststart", "-an", join(assets, "updraft-promo.mp4"));

rmSync(tmp, { recursive: true, force: true });
console.log("Site assets written to", assets);
