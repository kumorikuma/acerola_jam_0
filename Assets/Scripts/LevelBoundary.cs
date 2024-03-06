using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelBoundary : MonoBehaviour {
    public float DistanceThreshold = 10.0f;
    public float PlayableAreaSize = 150.0f;
    public float PlayableAreaMargin = 10.0f;

    [Tooltip("Use the X axis if false, otherwise use the Z axis.")]
    public bool IsHorizontal = false;

    private MeshRenderer _meshRenderer;
    private Material _levelBoundaryMaterialInstance;
    private Color _levelBoundaryColor;

    // Start is called before the first frame update
    void Awake() {
        _meshRenderer = GetComponent<MeshRenderer>();
        _levelBoundaryMaterialInstance = _meshRenderer.material;
        _levelBoundaryColor = _levelBoundaryMaterialInstance.GetColor("_Tint");
        SetMaterialAlpha(0);
    }

    // Update is called once per frame
    void Update() {
        if (PlayerManager.Instance == null) {
            return;
        }

        Transform playerTransform = PlayerManager.Instance.PlayerController.PlayerModel.transform;
        // Check the player's distance. If it's past the distance threshold, lerp the alpha.
        Vector3 playerToBoundaryVector = this.transform.position - playerTransform.position;
        float distance = playerToBoundaryVector.magnitude;
        // Set the alpha of the material
        if (distance <= DistanceThreshold) {
            float t = distance / DistanceThreshold;
            float alpha = Mathf.Lerp(1, 0, t);
            SetMaterialAlpha(alpha);
        }

        // Match the player's movement
        Vector3 currentPosition = this.transform.position;
        float playableAreaSizeMinusMargin = PlayableAreaSize - PlayableAreaMargin;
        if (IsHorizontal) {
            bool playerWithinPlayableAreaMinusMargin = playerTransform.position.x > -playableAreaSizeMinusMargin &&
                                                       playerTransform.position.x < playableAreaSizeMinusMargin;
            if (playerWithinPlayableAreaMinusMargin) {
                this.transform.position =
                    new Vector3(playerTransform.position.x, currentPosition.y, currentPosition.z);
            }
        } else {
            bool playerWithinPlayableAreaMinusMargin = playerTransform.position.z > -playableAreaSizeMinusMargin &&
                                                       playerTransform.position.z < playableAreaSizeMinusMargin;
            if (playerWithinPlayableAreaMinusMargin) {
                this.transform.position =
                    new Vector3(currentPosition.x, currentPosition.y, playerTransform.position.z);
            }
        }
    }

    private void SetMaterialAlpha(float alpha) {
        Color newColor = _levelBoundaryColor;
        newColor.a = _levelBoundaryColor.a * alpha;
        _levelBoundaryMaterialInstance.SetColor("_Tint", newColor);
    }
}
