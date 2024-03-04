using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class MeshGenerator {
    public struct Settings {
        public Setting<int> WidthMeters;
        public Setting<int> LengthMeters;
        public Setting<int> QuadsPerMeter;
        public Setting<Material> GroundMaterial;

        public static Settings DefaultSettings() {
            Settings defaultSettings = new Settings();

            defaultSettings.WidthMeters = new Setting<int>(200, "Width of tile in meters");
            defaultSettings.LengthMeters = new Setting<int>(400, "Length of tile in meters");
            defaultSettings.QuadsPerMeter = new Setting<int>(1, "Density of the triangles in the tile");
            defaultSettings.GroundMaterial = new Setting<Material>(null, "Material of ground plane");

            return defaultSettings;
        }
    }

    public static GameObject GenerateGroundMesh(Settings settings) {
        int widthMeters = settings.WidthMeters.value;
        int lengthMeters = settings.LengthMeters.value;
        int quadsPerMeter = settings.QuadsPerMeter.value;
        int quadsPerMeterSquared = quadsPerMeter * quadsPerMeter;

        int widthVerts = widthMeters * quadsPerMeter + 1;
        int lengthVerts = lengthMeters * quadsPerMeter + 1;
        int numVerts = widthVerts * lengthVerts;
        int numQuads = (widthVerts - 1) * (lengthVerts - 1);
        int numTris = numQuads * 2;
        Vector3[] vertices = new Vector3[numVerts];
        Vector2[] uvs = new Vector2[numVerts];
        // Vector3[] normals = new Vector3[numVerts];
        int[] indices = new int[numTris * 3];

        float xOffset = widthMeters / 2;

        // Create vertices
        for (int row = 0; row < lengthVerts; row++) {
            for (int col = 0; col < widthVerts; col++) {
                int vertIdx = row * widthVerts + col;
                Vector2 uv = new Vector2(col / (float)(widthVerts - 1), row / (float)(lengthVerts - 1)); // Range [0, 1]
                Vector3 vertex = new Vector3(uv.x * widthMeters - xOffset, 0, uv.y * lengthMeters);
                vertices[vertIdx] = vertex;
                uvs[vertIdx] = uv;
            }
        }

        // Generate triangles. We only generate triangles in pairs to ensure there are only quads in our topology.
        // The quad has:
        // - Top left corner A
        // - Top right corner B
        // - Bottom left corner C
        // - Bottom right corner D
        // This loops over every "D" vertex. So we start from row = 1, col = 1 to avoid out of bounds.
        int triIdx = 0;
        for (int row = 1; row < lengthVerts; row++) {
            for (int col = 1; col < widthVerts; col++) {
                int vertIdx = row * widthVerts + col;
                int vertA = vertIdx - 1 - widthVerts;
                int vertB = vertIdx - widthVerts;
                int vertC = vertIdx - 1;
                int vertD = vertIdx;
                // Triangle ABC -> CBA
                indices[triIdx++] = vertC;
                indices[triIdx++] = vertB;
                indices[triIdx++] = vertA;
                // Triangle BDC -> CDB
                indices[triIdx++] = vertC;
                indices[triIdx++] = vertD;
                indices[triIdx++] = vertB;
            }
        }

        return Utilities.SpawnMesh("Ground", vertices, uvs, indices, null, settings.GroundMaterial.value);
    }
}