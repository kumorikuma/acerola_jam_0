import { useGlobals, useReactiveValue } from "@reactunity/renderer";
import "./index.scss";

type ReticleProps = {
  posX: number;
  posY: number;
};

function lerp(a, b, alpha) {
  return a + alpha * (b - a);
}

export default function Reticle({ posX, posY }: ReticleProps): React.ReactNode {
  const globals = useGlobals();
  const playerPrimaryFireCooldown = useReactiveValue(
    globals.playerPrimaryFireCooldown
  );
  const playerSecondaryFireCooldown = useReactiveValue(
    globals.playerSecondaryFireCooldown
  );
  const primaryFireCooldownPercent = lerp(60, 78, playerPrimaryFireCooldown);
  const secondaryFireCooldownPercent = lerp(
    22,
    40,
    playerSecondaryFireCooldown
  );

  return (
    <view
      className="reticle"
      style={{ bottom: `${posY}px`, left: `${posX}px` }}
    >
      <view className="cooldowns-container">
        <view
          className="cooldowns"
          style={{
            background: `conic-gradient(transparent 0, transparent 22%, white 22%, white ${secondaryFireCooldownPercent}%, transparent ${secondaryFireCooldownPercent}%, transparent 60%, white 60%, white ${primaryFireCooldownPercent}%, transparent ${primaryFireCooldownPercent}%)`,
          }}
        />
      </view>
      <view className="decoration">
        <view className="circle-outer"></view>
        <view className="circle-outer-2"></view>
        <view className="circle-inner"></view>
      </view>
      <view className="crosshair">
        <view className="crosshair-top" />
        <view className="crosshair-down" />
        <view className="crosshair-right" />
        <view className="crosshair-left" />
      </view>
    </view>
  );
}
