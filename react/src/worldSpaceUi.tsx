import { useGlobals, useReactiveValue } from "@reactunity/renderer";

import Reticle from "./reticle";
import "./index.scss";
import { Vector2Int } from "./types/types";

export default function WorldSpaceUi(): React.ReactNode {
  const globals = useGlobals();
  const screenSpaceAimPosition: Vector2Int = useReactiveValue(
    globals.screenSpaceAimPosition
  );
  return (
    <view className="fullscreen-overlay">
      <Reticle
        posX={screenSpaceAimPosition.x}
        posY={screenSpaceAimPosition.y}
      />
    </view>
  );
}
