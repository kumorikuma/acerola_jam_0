using System;
using UnityEngine;

public class AnimationEvents : MonoBehaviour {
    public event Action OnSlashAttack1Hit;
    public event Action OnSlashAttack1SoftEnd;
    public event Action OnSlashAttack1End;

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
