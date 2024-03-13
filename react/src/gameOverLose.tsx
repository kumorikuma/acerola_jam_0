import { useReactiveValue, useGlobals } from "@reactunity/renderer";

import Button from "./button";
import "./index.scss";

export default function GameOverLose(): React.ReactNode {
  const globals = useGlobals();
  const gameLifecycleManager = globals.gameLifecycleManager;

  return (
    <view className="gameover">
      <view className="left-column">
        <view className="title">{"Planet Consumed :("}</view>
        <view className="subtext">{"Try Again?"}</view>
        <Button
          className="mainMenuButton"
          text="[ Play Again ]"
          onClick={() => {
            gameLifecycleManager.ReturnToMainMenu();
          }}
        />
        <Button
          className="mainMenuButton"
          text="[ Quit ]"
          onClick={() => {
            gameLifecycleManager.ReturnToMainMenu();
          }}
        />
      </view>
      <view className="spacer" />
      <view className="right-column"></view>
    </view>
  );
}
