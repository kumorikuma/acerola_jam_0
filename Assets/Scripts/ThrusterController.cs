using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public class ThrusterController : Singleton<ThrusterController> {
    public float GlobalScaleFactor = 1.0f;

    public Color ThrusterColdColor = Color.gray;
    private Color _thrusterHotColor;

    [NonNullField] public GameObject RightUpperBackThruster;
    [NonNullField] public GameObject RightLowerBackThruster;
    [NonNullField] public GameObject RightSideThruster;
    [NonNullField] public GameObject RightJumpThruster;

    [NonNullField] public GameObject LeftUpperBackThruster;
    [NonNullField] public GameObject LeftLowerBackThruster;
    [NonNullField] public GameObject LeftSideThruster;
    [NonNullField] public GameObject LeftJumpThruster;

    [NonNullField] public MeshRenderer RightUpperBackThrusterRenderer;
    [NonNullField] public MeshRenderer RightLowerBackThrusterRenderer;
    [NonNullField] public MeshRenderer RightSideThrusterRenderer;
    [NonNullField] public MeshRenderer RightJumpThrusterRenderer;
    [NonNullField] public MeshRenderer LeftUpperBackThrusterRenderer;
    [NonNullField] public MeshRenderer LeftLowerBackThrusterRenderer;
    [NonNullField] public MeshRenderer LeftSideThrusterRenderer;
    [NonNullField] public MeshRenderer LeftJumpThrusterRenderer;

    public float RightUpperBackThrusterScaleFactor = 1.5f;
    public float RightLowerBackThrusterScaleFactor = 1.5f;
    public float RightSideThrusterScaleFactor = 1.5f;
    public float RightJumpThrusterScaleFactor = 1.5f;

    public float LeftUpperBackThrusterScaleFactor = 1.5f;
    public float LeftLowerBackThrusterScaleFactor = 1.5f;
    public float LeftSideThrusterScaleFactor = 1.5f;
    public float LeftJumpThrusterScaleFactor = 1.5f;

    private List<GameObject> _thrusterObjects = new();
    private List<MeshRenderer> _thrusterRenderers = new();
    private Dictionary<int, Vector3> _originalScales = new();
    private Dictionary<int, float> _scaleFactors = new();
    private bool _stopAnimationTriggered = false;
    private Material _innerThrusterMaterialInstance;

    // Each thruster will have an original scale.
    // Depending on the speed, we will lerp between that original scale a new one.
    // We need to define what the max scale is.
    // We also need to define the min and max domain for the speed.
    // Minimum here would be the regular movement speed (assuming we're dashing always). And max would be the 
    // maximum speed possible.
    // Or we could just take speed / regular speed as the "scale factor". That would also take care of "stopping"
    // if we drop below regular speed.

    private float _playerMovementSpeed;

    // TODO: Change material color of the inner thruster if we ever implement walking.

    void Start() {
        _playerMovementSpeed = PlayerManager.Instance.PlayerController.RunSpeed;

        _thrusterObjects.Add(RightUpperBackThruster);
        _thrusterObjects.Add(RightLowerBackThruster);
        _thrusterObjects.Add(RightSideThruster);
        _thrusterObjects.Add(RightJumpThruster);
        _thrusterObjects.Add(LeftUpperBackThruster);
        _thrusterObjects.Add(LeftLowerBackThruster);
        _thrusterObjects.Add(LeftSideThruster);
        _thrusterObjects.Add(LeftJumpThruster);

        _scaleFactors[RightUpperBackThruster.GetInstanceID()] = RightUpperBackThrusterScaleFactor;
        _scaleFactors[RightLowerBackThruster.GetInstanceID()] = RightLowerBackThrusterScaleFactor;
        _scaleFactors[RightSideThruster.GetInstanceID()] = RightSideThrusterScaleFactor;
        _scaleFactors[RightJumpThruster.GetInstanceID()] = RightJumpThrusterScaleFactor;
        _scaleFactors[LeftUpperBackThruster.GetInstanceID()] = LeftUpperBackThrusterScaleFactor;
        _scaleFactors[LeftLowerBackThruster.GetInstanceID()] = LeftLowerBackThrusterScaleFactor;
        _scaleFactors[LeftSideThruster.GetInstanceID()] = LeftSideThrusterScaleFactor;
        _scaleFactors[LeftJumpThruster.GetInstanceID()] = LeftJumpThrusterScaleFactor;

        _thrusterRenderers.Add(RightUpperBackThrusterRenderer);
        _thrusterRenderers.Add(RightLowerBackThrusterRenderer);
        _thrusterRenderers.Add(RightSideThrusterRenderer);
        _thrusterRenderers.Add(RightJumpThrusterRenderer);
        _thrusterRenderers.Add(LeftUpperBackThrusterRenderer);
        _thrusterRenderers.Add(LeftLowerBackThrusterRenderer);
        _thrusterRenderers.Add(LeftSideThrusterRenderer);
        _thrusterRenderers.Add(LeftJumpThrusterRenderer);

        foreach (GameObject thrusterObject in _thrusterObjects) {
            _originalScales[thrusterObject.GetInstanceID()] = thrusterObject.transform.localScale;
        }

        // Create an instance of the material and then assign it to every part.
        _innerThrusterMaterialInstance = RightUpperBackThrusterRenderer.materials[1];
        _thrusterHotColor = _innerThrusterMaterialInstance.GetColor("_EmissionColor");
        foreach (MeshRenderer thrusterRenderer in _thrusterRenderers) {
            thrusterRenderer.materials[1] = _innerThrusterMaterialInstance;
            thrusterRenderer.materials[1].SetColor("_EmissionColor", ThrusterColdColor);
        }
    }

    public void HandleThrusterUpdates(float moveSpeed) {
        // TODO: We need to handle separate axes.
        // Need the velocity in each separate axis.
        // This should be relative to the player's facing direction.

        // Rethink this: Let's use the movement speed instead of the noisy value.
        // Then we render the thrusters based on direction.
        // Basically choosing which thrusters to use based on direction.
        float scaleRatio = moveSpeed / _playerMovementSpeed;

        // TODO: Sudden changes will need to be attenuated.

        bool shouldUseTween = false;
        if (scaleRatio == 0 && !_stopAnimationTriggered) {
            _stopAnimationTriggered = true;
            foreach (GameObject thrusterObject in _thrusterObjects) {
                SetThrusterScale(thrusterObject, scaleRatio, true);
            }

            foreach (MeshRenderer thrusterRenderer in _thrusterRenderers) {
                thrusterRenderer.materials[1].DOColor(ThrusterColdColor, "_EmissionColor", 1.0f);
            }
        } else if (scaleRatio > 0) {
            _stopAnimationTriggered = false;
            DOTween.KillAll();
            foreach (MeshRenderer thrusterRenderer in _thrusterRenderers) {
                thrusterRenderer.materials[1].SetColor("_EmissionColor", _thrusterHotColor);
            }

            foreach (GameObject thrusterObject in _thrusterObjects) {
                SetThrusterScale(thrusterObject, scaleRatio, false);
            }
        }
    }

    public void SetThrusterScale(GameObject thrusterObject, float scaleRatio, bool shouldUseTween) {
        int id = thrusterObject.GetInstanceID();
        Vector3 originalScale = _originalScales[id];
        float scaleFactor = _scaleFactors[id];

        // Scales up/down the ratio but keeps it centered around 1.
        scaleRatio = 1 + (scaleRatio - 1) * scaleFactor;
        float scaleX = originalScale.x * scaleRatio * GlobalScaleFactor;
        Vector3 targetScale = new Vector3(scaleX, originalScale.y, originalScale.z);

        if (shouldUseTween) {
            // Defer to DOTWEEN to animate this.
            targetScale.x = 0;
            thrusterObject.transform.DOScale(targetScale, 1.0f);
        } else {
            DOTween.Kill(thrusterObject.transform);
            thrusterObject.transform.localScale = targetScale;
        }
    }
}
