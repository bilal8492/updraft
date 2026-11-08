import { Composition, Folder } from "remotion";
import { SiteBackup, SiteDetails, SiteDiscover, SiteScript, SiteUpdates } from "./site/SiteStills";
import { UpInstallPromo } from "./UpInstallPromo";

export const RemotionRoot: React.FC = () => {
  return (
    <>
      <Composition
        id="UpInstallPromo"
        component={UpInstallPromo}
        durationInFrames={1413}
        fps={30}
        width={1920}
        height={1080}
      />
      <Folder name="Website">
        <Composition id="SiteUpdates" component={SiteUpdates} durationInFrames={1} fps={30} width={1680} height={1120} />
        <Composition id="SiteDetails" component={SiteDetails} durationInFrames={165} fps={30} width={1920} height={1080} />
        <Composition id="SiteDiscover" component={SiteDiscover} durationInFrames={180} fps={30} width={1920} height={1080} />
        <Composition id="SiteBackup" component={SiteBackup} durationInFrames={150} fps={30} width={1920} height={1080} />
        <Composition id="SiteScript" component={SiteScript} durationInFrames={180} fps={30} width={1920} height={1080} />
      </Folder>
    </>
  );
};
