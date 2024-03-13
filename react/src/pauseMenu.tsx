import { useGlobals } from "@reactunity/renderer";

import Button from "./button";
import "./index.scss";

export default function PauseMenu(): React.ReactNode {
  const globals = useGlobals();
  const gameLifecycleManager = globals.gameLifecycleManager;

  return (
    <view className="pause-menu">
      <view className="black-bar">
        <view className="container flex-row spacer">
          <view className="text">Paused</view>
          <view className="spacer" />
          <Button
            text="Main Menu"
            onClick={() => {
              gameLifecycleManager.ReturnToMainMenu();
            }}
          />
          <Button
            text="Continue"
            onClick={() => {
              gameLifecycleManager.UnpauseGame();
            }}
          />
        </view>
        <view className="gradient-rule"></view>
      </view>
      <view className="controls-container">
        <view className="title">Controls</view>
        <view className="row-container">
          <view className="subtext">Press</view>
          <view className="highlighted">{" [Left Mouse Click]"}</view>
          <view className="subtext"> and </view>
          <view className="highlighted">{" [Right Mouse Click]"}</view>
          <view className="subtext"> to attack.</view>
        </view>
        <view className="row-container">
          <view className="subtext">Press</view>
          <view className="highlighted">{" [SPACE]"}</view>
          <view className="subtext"> to jump. Press </view>
          <view className="highlighted">{" [SHIFT]"}</view>
          <view className="subtext"> to dash.</view>
        </view>
        <Button
          className="mainMenuButton"
          text="[ Continue ]"
          onClick={() => {
            gameLifecycleManager.UnpauseGame();
          }}
        />
      </view>
    </view>
  );
}
