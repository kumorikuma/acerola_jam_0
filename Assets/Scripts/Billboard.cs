using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour {
    // Update is called once per frame
    void LateUpdate() {
        Vector3 cameraLookVectorProjectedOntoXZPlane = Camera.main.transform.forward;
        cameraLookVectorProjectedOntoXZPlane.y = 0;
        this.transform.rotation = Quaternion.LookRotation(cameraLookVectorProjectedOntoXZPlane);
    }
}
