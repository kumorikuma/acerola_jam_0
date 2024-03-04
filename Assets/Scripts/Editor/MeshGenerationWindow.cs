using System.Collections;
using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;

// Access from toolbar: Custom -> Mesh Generation
public class MeshGeneration : EditorWindow {
    MeshGenerator.Settings settings = MeshGenerator.Settings.DefaultSettings();

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
        settings.WidthMeters.value = EditorGUILayout.IntSlider(new GUIContent("Width of tile (m)", settings.WidthMeters.tooltip), settings.WidthMeters.value, 10, 2000);
        settings.LengthMeters.value = EditorGUILayout.IntSlider(new GUIContent("Length of tile (m)", settings.LengthMeters.tooltip), settings.LengthMeters.value, 10, 2000);
        settings.QuadsPerMeter.value = EditorGUILayout.IntSlider(new GUIContent("Quads per Meter", settings.QuadsPerMeter.tooltip), settings.QuadsPerMeter.value, 1, 4);
        settings.GroundMaterial.value = EditorGUILayout.ObjectField(new GUIContent("Ground Material", settings.GroundMaterial.tooltip), settings.GroundMaterial.value, typeof(Material), true) as Material;
        GUILayout.EndVertical();
        if (GUILayout.Button("Generate Ground Mesh")) {
            this.StartCoroutine(GenerateGroundMesh());
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
}