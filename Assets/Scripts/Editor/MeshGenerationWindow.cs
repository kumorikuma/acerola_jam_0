using System.Collections;
using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;

// Access from toolbar: Custom -> Mesh Generation
public class MeshGeneration : EditorWindow {
    [SerializeField] MeshGenerator.Settings settings = MeshGenerator.Settings.DefaultSettings();

    [MenuItem("Custom/Mesh Generation")]
    public static void OpenWindow() {
        GetWindow<MeshGeneration>();
    }

    void OnEnable() {
    }

    void OnGUI() {
        GUILayout.BeginVertical("HelpBox");
        GUILayout.Label("Tile Mesh Settings");
        GUILayout.BeginVertical("GroupBox");
        settings.WidthMeters.value = EditorGUILayout.IntSlider(
            new GUIContent("Width of tile (m)", settings.WidthMeters.tooltip), settings.WidthMeters.value, 10, 2000);
        settings.LengthMeters.value = EditorGUILayout.IntSlider(
            new GUIContent("Length of tile (m)", settings.LengthMeters.tooltip), settings.LengthMeters.value, 10, 2000);
        settings.QuadsPerMeter.value = EditorGUILayout.IntSlider(
            new GUIContent("Quads per Meter", settings.QuadsPerMeter.tooltip), settings.QuadsPerMeter.value, 1, 4);
        settings.GroundMaterial.value =
            EditorGUILayout.ObjectField(new GUIContent("Ground Material", settings.GroundMaterial.tooltip),
                settings.GroundMaterial.value, typeof(Material), true) as Material;
        GUILayout.EndVertical();
        if (GUILayout.Button("Generate Ground Mesh")) {
            this.StartCoroutine(GenerateGroundMesh());
        }

        GUILayout.EndVertical();

        GUILayout.BeginVertical("HelpBox");
        GUILayout.Label("Sphere Mesh Settings");
        GUILayout.BeginVertical("GroupBox");
        settings.SphereSubdivs.value = EditorGUILayout.IntSlider(
            new GUIContent("Subdivisions", settings.SphereSubdivs.tooltip), settings.SphereSubdivs.value, 2, 10);
        GUILayout.EndVertical();
        if (GUILayout.Button("Generate Sphere Mesh")) {
            this.StartCoroutine(GenerateSphereMesh());
        }

        GUILayout.EndVertical();

        GUILayout.BeginVertical("HelpBox");
        GUILayout.Label("Stars Box Settings");
        GUILayout.BeginVertical("GroupBox");
        settings.StarsContainer.value =
            EditorGUILayout.ObjectField(new GUIContent("Stars Container", settings.StarsContainer.tooltip),
                settings.StarsContainer.value, typeof(GameObject), true) as GameObject;
        settings.StarsPrefab.value =
            EditorGUILayout.ObjectField(new GUIContent("Star Prefab", settings.StarsPrefab.tooltip),
                settings.StarsPrefab.value, typeof(GameObject), false) as GameObject;
        settings.StarsCount.value = EditorGUILayout.IntSlider(
            new GUIContent("Number of Stars", settings.StarsCount.tooltip), settings.StarsCount.value, 1, 1000);
        settings.StarsMinDistance.value = EditorGUILayout.Slider(
            new GUIContent("Min Distance", settings.StarsMinDistance.tooltip), settings.StarsMinDistance.value, 0,
            1000);
        settings.StarsMaxDistance.value = EditorGUILayout.Slider(
            new GUIContent("Max Distance", settings.StarsMaxDistance.tooltip), settings.StarsMaxDistance.value, 0,
            1000);
        settings.PerturbScale.value = EditorGUILayout.Toggle(
            new GUIContent("Perturb Scale", settings.PerturbScale.tooltip), settings.PerturbScale.value);
        settings.RandomizeStarRotation.value = EditorGUILayout.Toggle(
            new GUIContent("Randomize Rotation", settings.RandomizeStarRotation.tooltip),
            settings.RandomizeStarRotation.value);
        settings.FaceStarTowardsOrigin.value = EditorGUILayout.Toggle(
            new GUIContent("Billboard", settings.FaceStarTowardsOrigin.tooltip), settings.FaceStarTowardsOrigin.value);
        GUILayout.EndVertical();
        if (GUILayout.Button("Generate Stars")) {
            this.StartCoroutine(GenerateStars());
        }

        if (GUILayout.Button("Clear Stars Container")) {
            this.StartCoroutine(ClearStarsContainer());
        }

        GUILayout.EndVertical();
    }

    // Generates a plane with certain density.
    // Variables:
    // - Width: Default 1000m
    // - Length: Default 300m (max view distance is 1000, can break up into smaller chunks)
    // - Density: Quads per m^2. Specified as quads per meter. i.e. 2 becomes 4.
    IEnumerator GenerateGroundMesh() {
        MeshGenerator.GenerateGroundMesh(settings);
        yield return null;
    }

    // Generates a sphere using Rounded Cube method.
    IEnumerator GenerateSphereMesh() {
        MeshGenerator.GenerateSphereMesh(settings);
        yield return null;
    }

    // Generates stars.
    IEnumerator GenerateStars() {
        MeshGenerator.GenerateStars(settings);
        yield return null;
    }

    IEnumerator ClearStarsContainer() {
        settings.StarsContainer.value.DestroyChildrenImmediate();
        yield return null;
    }
}
