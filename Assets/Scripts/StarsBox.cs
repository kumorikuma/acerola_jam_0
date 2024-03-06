using UnityEngine;

public class StarsBox : Singleton<StarsBox> {
    public float MinDistance = 150.0f;
    public float MaxDistance = 300.0f;

    protected override void Awake() {
        base.Awake();
    }

    private void GenerateStars( /* Param is gameobject, should randomly peturb */) {
        // Compute Random Spherical Coordinates
        float polarAngleRad = UnityEngine.Random.Range(-Mathf.PI / 2.0f, Mathf.PI / 2.0f); // theta
        float azimuthalAngleRad = UnityEngine.Random.Range(-Mathf.PI, Mathf.PI); // phi
        float radialDistance = UnityEngine.Random.Range(MinDistance, MaxDistance);

        // Convert to Cartesian Coordinates
        float x = radialDistance * Mathf.Cos(azimuthalAngleRad) * Mathf.Sin(polarAngleRad);
        float y = radialDistance * Mathf.Sin(azimuthalAngleRad) * Mathf.Cos(polarAngleRad);
        float z = radialDistance * Mathf.Cos(polarAngleRad);
    }
}
