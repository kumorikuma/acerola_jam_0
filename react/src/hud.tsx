import { useGlobals, useReactiveValue } from "@reactunity/renderer";

import Reticle from "./reticle";
import "./index.scss";
import { Vector2Int } from "./types/types";

export default function Hud(): React.ReactNode {
  const globals = useGlobals();
  const screenSpaceAimPosition: Vector2Int = useReactiveValue(
    globals.screenSpaceAimPosition
  );
  console.log(screenSpaceAimPosition.x);

  return (
    <view className="hud">
      <view className="flex-row padding-md">
        <view>HUD</view>
        <view className="spacer" />
      </view>
      <view className="fullscreen-overlay">
        <Reticle
          posX={screenSpaceAimPosition.x}
          posY={screenSpaceAimPosition.y}
        />
      </view>
    </view>
  );
}
