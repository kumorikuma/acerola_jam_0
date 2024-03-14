import { useGlobals } from "@reactunity/renderer";

import Button from "./button";
import "./index.scss";

export default function Controls(): React.ReactNode {
  const globals = useGlobals();
  const gameLifecycleManager = globals.gameLifecycleManager;

  return (
    <view className="controls">
      <view className="title">Controls</view>
      <view className="container">
        <view className="subtext">Press</view>
        <view className="highlighted">{" [ESCAPE]"}</view>
        <view className="subtext"> at any time to see this again. </view>
      </view>
      <view className="container">
        <view className="subtext">Press</view>
        <view className="highlighted">{" [Left Mouse Click]"}</view>
        <view className="subtext"> and </view>
        <view className="highlighted">{" [Right Mouse Click]"}</view>
        <view className="subtext"> to attack.</view>
      </view>
      <view className="container">
        <view className="subtext">Press</view>
        <view className="highlighted">{" [SPACE]"}</view>
        <view className="subtext"> to jump. Press </view>
        <view className="highlighted">{" [SHIFT]"}</view>
        <view className="subtext"> to dash.</view>
      </view>
      <view className="container">
        <view className="subtext">TIP: Take down the enemy's shield to</view>
        <view className="highlighted">{" stagger"}</view>
        <view className="subtext"> it.</view>
      </view>
      <Button
        className="mainMenuButton"
        text="[ Start Game ]"
        onClick={() => {
          gameLifecycleManager.StartGame();
        }}
      />
    </view>
  );
}
