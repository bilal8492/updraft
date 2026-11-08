import type React from "react";
import { AbsoluteFill } from "remotion";
import { AppWindow } from "../components/AppWindow";
import { ShowcaseContext } from "../components/Showcase";
import { UpdatesPage } from "../components/UpdatesPage";
import { UPGRADES } from "../data";
import { Backup } from "../scenes/Backup";
import { Details } from "../scenes/Details";
import { Discover } from "../scenes/Discover";
import { Script } from "../scenes/Script";

// Transparent stills of the product mockups for the website (docs/). Render them with
// `npm run site-stills` from the promo-video folder.

const Showcase: React.FC<{ children: React.ReactNode }> = ({ children }) => (
  <ShowcaseContext.Provider value={true}>{children}</ShowcaseContext.Provider>
);

/** The Updates page mid-run: two apps done, one installing, the rest queued. */
export const SiteUpdates = () => (
  <AbsoluteFill style={{ alignItems: "center", justifyContent: "center" }}>
    <AppWindow width={1560} height={1000} page="updates" updatesCount={6}>
      <UpdatesPage
        hero={{ mode: "updating", done: 2, total: UPGRADES.length, progress: 0.32 }}
        rows={UPGRADES.map((app, i) => ({
          app,
          state: {
            appear: 1,
            checked: 1,
            done: i < 2 ? 1 : undefined,
            ring: i === 2 ? 0.62 : undefined,
            queued: i > 2,
          },
        }))}
        selected={UPGRADES.length}
        drift={30}
      />
    </AppWindow>
  </AbsoluteFill>
);

export const SiteDetails = () => (
  <Showcase>
    <Details />
  </Showcase>
);

export const SiteDiscover = () => (
  <Showcase>
    <Discover />
  </Showcase>
);

export const SiteBackup = () => (
  <Showcase>
    <Backup />
  </Showcase>
);

export const SiteScript = () => (
  <Showcase>
    <Script />
  </Showcase>
);
