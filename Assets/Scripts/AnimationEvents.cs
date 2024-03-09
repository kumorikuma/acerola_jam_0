using System;
using UnityEngine;

public class AnimationEvents : MonoBehaviour {
    public event Action OnSlashAttack1Hit;
    public event Action OnSlashAttack1Ended;

    public void SlashAttack1Ended() {
        OnSlashAttack1Ended?.Invoke();
    }

    public void SlashAttack1Hit() {
        OnSlashAttack1Hit?.Invoke();
    }
}
