using Tripolygon.UModelerX.Runtime;
using Tripolygon.UModelerX.UModelerClassic;
using UnityEngine;
using UnityEditor;

public class ComponentRemover : EditorWindow {
    [MenuItem("Tools/Remove Specific Component from Selection")]
    public static void ShowWindow() {
        GetWindow<ComponentRemover>("Remove Component");
    }

    void OnGUI() {
        if (GUILayout.Button("Remove from Selected GameObject and Children")) {
            RemoveComponentFromSelection();
        }
    }

    void RemoveComponentFromSelection() {
        GameObject selection = Selection.activeGameObject;

        if (selection == null) {
            Debug.LogError("You must select a GameObject!");
            return;
        }

        RemoveComponentRecursive(selection);
    }

    void RemoveComponentRecursive(GameObject gameObject) {
        // Assuming you want to remove a component like YourComponent from all children
        foreach (var component in gameObject.GetComponentsInChildren<UModelerXEditableMesh>(true)) {
            DestroyImmediate(component);
        }
    }
}
