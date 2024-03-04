using System;
using UnityEngine;

public class EntityStats : MonoBehaviour {
    public enum Owner {
        Player,
        Enemy
    }

    public Owner EntityOwner = Owner.Player;
    public int MaxHealth = 100;
    public int CurrentHealth = 100;

    public event EventHandler<float> OnHealthChanged;

    public float GetHealthPercentage() {
        return Mathf.Clamp(CurrentHealth / (float)MaxHealth, 0, 1);
    }

    public void NotifyHealthChanged() {
        OnHealthChanged?.Invoke(this, GetHealthPercentage());
    }

    public void ApplyDamage(int damageAmount) {
        CurrentHealth = Mathf.Clamp(CurrentHealth - damageAmount, 0, MaxHealth);
        OnHealthChanged?.Invoke(this, GetHealthPercentage());
    }

    public void ApplyHealing(int healingAmount) {
        CurrentHealth = Mathf.Clamp(CurrentHealth + healingAmount, 0, MaxHealth);
        OnHealthChanged?.Invoke(this, GetHealthPercentage());
    }

    public void SetHealthToPercentage(float percent) {
        CurrentHealth = Mathf.RoundToInt(percent * MaxHealth);
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
        OnHealthChanged?.Invoke(this, GetHealthPercentage());
    }

    public void Reset() {
        SetHealthToPercentage(1);
    }
}
