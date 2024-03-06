using UnityEditor;
using UnityEngine;

[InitializeOnLoad] // Ensure the class initializer is called whenever scripts are recompiled.
public class SceneViewRefresher {
    private static double lastUpdateTime = 0.0;
    private static readonly int FPS = 60;
    private static readonly float refreshRate = 1.0f / FPS;

    static SceneViewRefresher() {
        EditorApplication.update += Update;
    }

    static void Update() {
        if (EditorApplication.isPlaying)
            return;

        var currentTime = EditorApplication.timeSinceStartup;
        if (currentTime - lastUpdateTime > refreshRate) {
            SceneView.RepaintAll();
            lastUpdateTime = currentTime;
        }
    }
}
