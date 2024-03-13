using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackHitbox : MonoBehaviour {
    [NonSerialized] public GameObject CollidedObject;
    public ProjectileController.Owner ProjectileOwner = ProjectileController.Owner.Player;

    // Start is called before the first frame update
    void OnTriggerEnter(Collider other) {
        // ReactUnityBridge.Instance.UpdateDebugString("MeleeHitBox Target Detected", "true");
        if (ProjectileOwner == ProjectileController.Owner.Player && other.CompareTag("Targetable")) {
            CollidedObject = other.gameObject;
        } else if (ProjectileOwner == ProjectileController.Owner.Enemy && other.CompareTag("Player")) {
            CollidedObject = other.gameObject;
        }
    }

    // Update is called once per frame
    private void OnTriggerExit(Collider other) {
        // ReactUnityBridge.Instance.UpdateDebugString("MeleeHitBox Target Detected", "false");
        CollidedObject = null;
    }
}
