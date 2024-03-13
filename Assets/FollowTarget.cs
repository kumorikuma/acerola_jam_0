using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowTarget : MonoBehaviour {
    [NonNullField] public Transform target;

    // Update is called once per frame
    void Update() {
        if (target.gameObject.activeInHierarchy) {
            transform.position = target.position;
        } else {
            transform.position = Vector3.zero;
        }
    }
}
