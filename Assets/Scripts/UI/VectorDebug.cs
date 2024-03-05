using System.Collections.Generic;
using Cinemachine;
using UnityEditor;
using UnityEngine;


public class VectorDebug : Singleton<VectorDebug> {
    private class DebugVector {
        public Vector3 vector = Vector3.zero;
        public Vector3 position = Vector3.zero;
        public Color color = Color.white;
    }

    private Dictionary<string, DebugVector> _debugVectors = new();

    public void DrawDebugVector(string name, Vector3 vector, Vector3 position, Color color) {
        if (_debugVectors.ContainsKey(name)) {
            _debugVectors[name].vector = vector;
            _debugVectors[name].position = position;
            _debugVectors[name].color = color;
        } else {
            DebugVector entry = new();
            entry.vector = vector;
            entry.position = position;
            entry.color = color;
            _debugVectors.Add(name, entry);
        }
    }

    private void OnDrawGizmos() {
        if (!Application.isPlaying) {
            return;
        }

        foreach (var kv in _debugVectors) {
            Draw(kv.Key, kv.Value);
        }
    }

    private void Draw(string label, DebugVector debugVector) {
#if UNITY_EDITOR
        Vector3 labelPosition = debugVector.position + debugVector.vector * 0.5f;
        Color cacheColor = UnityEditor.Handles.color;
        UnityEditor.Handles.color = debugVector.color;
        UnityEditor.Handles.DrawDottedLine(debugVector.position, debugVector.position + debugVector.vector, 4f);
        GUIStyle style = new GUIStyle();
        style.normal.textColor = debugVector.color;
        UnityEditor.Handles.Label(labelPosition,
            new GUIContent(
                $"{label}\nLength: {debugVector.vector.magnitude:F2}\n<{debugVector.vector.x:F2}, {debugVector.vector.y:F2}, {debugVector.vector.z:F2}"),
            style);
        UnityEditor.Handles.color = cacheColor;
#endif
    }
}
