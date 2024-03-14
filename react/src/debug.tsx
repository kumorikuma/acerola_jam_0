import { useGlobals, useReactiveValue } from "@reactunity/renderer";

import "./index.scss";
import Button from "./button";

export default function Debug(): React.ReactNode {
  const globals = useGlobals();
  const route = useReactiveValue(globals.route);
  const gameState = useReactiveValue(globals.debugGameState);
  const gameLifecycleManager = globals.gameLifecycleManager;
  const isDebugModeEnabled = useReactiveValue(globals.debugModeEnabled);
  const debugStrings: Array<string> = useReactiveValue(globals.debugStrings);

  const debugStringElements = debugStrings.map((value, i) => {
    return (
      <view key={i} className={`text`}>
        {value}
      </view>
    );
  });

  return (
    isDebugModeEnabled && (
      <view className="debug-ui">
        <view className="spacer" />
        <view className="footer">
          {/* <view className="flex-column">
            <view className="text">Debug</view>
            <view className="text">{`UI Route: ${route}`}</view>
            <view className="text">{`Game State: ${gameState}`}</view>
            {debugStringElements}
            {gameState === "GamePaused" && (
              <Button
                text="End Game"
                onClick={() => {
                  gameLifecycleManager.EndGame();
                }}
              />
            )}
          </view> */}
        </view>
      </view>
    )
  );
}
