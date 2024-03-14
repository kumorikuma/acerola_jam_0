import { useReactiveValue, useGlobals } from "@reactunity/renderer";

import Button from "./button";
import "./index.scss";

export default function GameOver(): React.ReactNode {
  const globals = useGlobals();
  const gameLifecycleManager = globals.gameLifecycleManager;
  const percentageDestroyed: number = useReactiveValue(
    globals.percentageDestroyed
  );
  const damageTaken: number = useReactiveValue(globals.damageTaken);

  return (
    <view className="gameoverLose">
      <view className="left-column">
        <view className="title">Planet saved!</view>
        <view className="subtext">{"Thanks for playing <3"}</view>
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
            gameLifecycleManager.QuitGame();
          }}
        />
      </view>
      <view className="spacer" />
      <view className="right-column">
        <view className="body-text">{`${Math.round(
          percentageDestroyed * 100
        )}% of planet destroyed`}</view>
        <view className="body-text">{`${damageTaken} damage taken`}</view>
      </view>
    </view>
  );
}
