import { useGlobals, useReactiveValue } from "@reactunity/renderer";

import "./index.scss";
import WorldSpaceUi from "./worldSpaceUi";

function lerp(a, b, alpha) {
  return a + alpha * (b - a);
}

export default function Hud(): React.ReactNode {
  const globals = useGlobals();
  const playerHealth: number = useReactiveValue(globals.playerHealth);
  const playerLives: number = useReactiveValue(globals.playerLives);
  const bossHealth: number = useReactiveValue(globals.bossHealth);
  const bossLives: number = useReactiveValue(globals.bossLives);

  const maxPlayerLives = globals.maxPlayerLives;
  const maxBossLives = globals.maxBossLives;

  const playerHealthPercent = lerp(0, 100, playerHealth);
  const bossHealthPercent = lerp(100, 0, bossHealth);

  const playerHealthColorBackground = "rgba(255, 255, 255, 0.1)";
  const bossHealthColorBackground = "rgba(255, 255, 255, 0.1)";

  const numSegments = maxPlayerLives - 1;
  const segments = Array.from({ length: numSegments }, (_, i) => (
    <view key={i} className="segment"></view>
  ));

  return (
    <view className="hud">
      <view className="flex-row padding-md">
        <view className="flex-col">
          <view
            className="player-healthbar"
            style={{
              background: `linear-gradient(90deg, white ${playerHealthPercent}%, ${playerHealthColorBackground} ${playerHealthPercent}%, ${playerHealthColorBackground} 100%)`,
            }}
          />
          <view className="lives">
            <view className="segments-container">
              <view className="segments">{segments}</view>
            </view>
          </view>
        </view>
        <view className="spacer" />
        <view className="boss-stats">
          <view
            className="boss-healthbar"
            style={{
              background: `linear-gradient(90deg, ${bossHealthColorBackground} ${bossHealthPercent}%, white ${bossHealthPercent}%, white 100%)`,
            }}
          />
          <view className="lives">
            <view className="segments-container">
              <view className="segments">{segments}</view>
            </view>
          </view>
        </view>
      </view>
      <WorldSpaceUi />
    </view>
  );
}
