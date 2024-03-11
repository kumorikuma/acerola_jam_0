using System;
using UnityEngine;

public class AnimationEvents : MonoBehaviour {
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
}
