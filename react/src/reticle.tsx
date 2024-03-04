import "./index.scss";

type ReticleProps = {
  posX: number;
  posY: number;
};

export default function Reticle({ posX, posY }: ReticleProps): React.ReactNode {
  return (
    <view
      className="reticle"
      style={{ bottom: `${posY}px`, left: `${posX}px` }}
    >
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
