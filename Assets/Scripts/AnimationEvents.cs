using System;
using UnityEngine;
using UnityEngine.VFX;

public class AnimationEvents : MonoBehaviour {
    public GameObject SlashAttack1VFX;
    public GameObject SlashAttack2VFX;
    public Animator DummyMechaAnimator;

    public event EventHandler<float> OnSwordGlowStart;
    public event EventHandler<float> OnSwordGlowEnd;

    public event Action OnSlashAttack1Hit;
    public event Action OnSlashAttack1SoftEnd;
    public event Action OnSlashAttack1End;

    public void SwordGlowStart(float durationSeconds) {
        OnSwordGlowStart?.Invoke(this, durationSeconds);
    }

    public void SwordGlowEnd(float durationSeconds) {
        OnSwordGlowEnd?.Invoke(this, durationSeconds);
    }

    // Soft end means we can perform a follow up attack or do a dash-cancel
    public void SlashAttack1SoftEnd() {
        OnSlashAttack1SoftEnd?.Invoke();
    }

    public void SlashAttack1End() {
        OnSlashAttack1End?.Invoke();
    }

    public void SlashAttack1Hit() {
        OnSlashAttack1Hit?.Invoke();
    }

    public void PlaySlashAttack1VFX() {
        SlashAttack1VFX.SetActive(true);
        SlashAttack1VFX.GetComponentInChildren<VisualEffect>().Play();
    }

    public void PlaySlashAttack2VFX() {
        SlashAttack2VFX.SetActive(true);
        SlashAttack2VFX.GetComponentInChildren<VisualEffect>().Play();
    }

    public void DummyMechaJumpPose() {
        DummyMechaAnimator.SetTrigger("JumpPose");
    }

    public void DummyMechaIdlePose() {
        DummyMechaAnimator.SetTrigger("IdlePose");
    }

    public void EndIntroSequence() {
        GameLifecycleManager.Instance.OnIntroSequenceEnd();
        PlayerManager.Instance.PlayerController.SetProcessedEnabled(true);
    }

    public void DissolveBoss(float durationSeconds) {
        BossController.Instance.PlayDissolveAnimation(durationSeconds);
    }

    public void DropPanels() {
        ProjectileController.Instance.DropAllPanels();
        PanelsController.Instance.StopDestroyingLevel();
    }

    public void PointCameraTowardsBlackHole() {
        PlayerManager.Instance.CameraController.PointCameraTowardsBlackHole();
    }

    public void SwitchToFrontCamera() {
        PlayerManager.Instance.PlayerController.SetProcessedEnabled(false);
        PlayerManager.Instance.CameraController.EnableFrontCamera(true);
    }

    public void VictoryPose() {
        PlayerManager.Instance.PlayerController.VictoryPose();
    }

    public void EndWinOutroSequence() {
        ReactUnityBridge.Instance.UpdateStats(PanelsController.Instance.GetPercentageDestroyed(),
            PlayerManager.Instance.PlayerController.GetDamageTaken());
        GameLifecycleManager.Instance.GameOver();
    }
}
