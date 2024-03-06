using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldController : MonoBehaviour {
    [NonNullField] public GameObject ShieldPieceA;
    [NonNullField] public GameObject ShieldPieceB;
    [NonNullField] public GameObject ShieldPieceC;
    [NonNullField] public GameObject ShieldPieceCenter;

    private MeshRenderer _shieldMeshRenderer;

    void Awake() {
        _shieldMeshRenderer = ShieldPieceCenter.GetComponentInChildren<MeshRenderer>();
    }

    public void DeactivateShield() {
        Material material = _shieldMeshRenderer.material;
        // _shieldMeshRenderer.material.color = new Color(material.color.r, material.color.g, material.color.b, 0);
        ShieldPieceA.SetActive(false);
        ShieldPieceB.SetActive(false);
        ShieldPieceC.SetActive(false);
        ShieldPieceCenter.SetActive(false);
    }

    public void ActivateShield() {
        Material material = _shieldMeshRenderer.material;
        // _shieldMeshRenderer.material.color = new Color(material.color.r, material.color.g, material.color.b, 0.1f);
        ShieldPieceA.SetActive(true);
        ShieldPieceB.SetActive(true);
        ShieldPieceC.SetActive(true);
        ShieldPieceCenter.SetActive(true);
    }
}
