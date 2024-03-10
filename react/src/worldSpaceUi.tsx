import { useGlobals, useReactiveValue } from "@reactunity/renderer";

import Reticle from "./reticle";
import "./index.scss";

export default function WorldSpaceUi(): React.ReactNode {
  const globals = useGlobals();
  const shouldDisplayReticle = useReactiveValue(globals.displayReticle);
  return (
    <view className="fullscreen-overlay">
      {shouldDisplayReticle && <Reticle />}
    </view>
  );
}
