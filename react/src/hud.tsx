import { useGlobals, useReactiveValue } from "@reactunity/renderer";

import "./index.scss";
import WorldSpaceUi from "./worldSpaceUi";

function lerp(a, b, alpha) {
  return a + alpha * (b - a);
}

type LivesDisplayProps = {
  numLives: number;
  maxNumLives: number;
  reversedDirection?: boolean;
};

function LivesDisplay({
  numLives,
  maxNumLives,
  reversedDirection = false,
}: LivesDisplayProps): React.ReactNode {
  const maxNumSegments = maxNumLives - 1;
  const numSegments = numLives - 1;
  const segments = Array.from({ length: maxNumSegments }, (_, i) => {
    var activeClassName = i < numSegments ? "active" : "";
    if (reversedDirection) {
      activeClassName = maxNumSegments - i <= numSegments ? "active" : "";
    }

    return <view key={i} className={`segment ${activeClassName}`} />;
  });

  return (
    <view className="lives">
      <view className="segments-container">
        <view className="segments">{segments}</view>
      </view>
    </view>
  );
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
          <LivesDisplay numLives={playerLives} maxNumLives={maxPlayerLives} />
        </view>
        <view className="spacer" />
        <view className="boss-stats">
          <view
            className="boss-healthbar"
            style={{
              background: `linear-gradient(90deg, ${bossHealthColorBackground} ${bossHealthPercent}%, white ${bossHealthPercent}%, white 100%)`,
            }}
          />
          <LivesDisplay
            numLives={bossLives}
            maxNumLives={maxBossLives}
            reversedDirection={true}
          />
        </view>
      </view>
      <WorldSpaceUi />
    </view>
  );
}
