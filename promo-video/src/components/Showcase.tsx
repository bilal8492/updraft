import { createContext, useContext } from "react";

/**
 * When true, scenes render only their product mockups: no headline text and no extras such as the taskbar.
 * Used by the stills rendered for the website (see src/site/SiteStills.tsx).
 */
export const ShowcaseContext = createContext(false);

export const useShowcase = () => useContext(ShowcaseContext);
