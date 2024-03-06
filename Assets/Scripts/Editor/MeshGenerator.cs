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

        public Setting<int> SphereSubdivs;

        public static Settings DefaultSettings() {
            Settings defaultSettings = new Settings();

            defaultSettings.WidthMeters = new Setting<int>(200, "Width of tile in meters");
            defaultSettings.LengthMeters = new Setting<int>(400, "Length of tile in meters");
            defaultSettings.QuadsPerMeter = new Setting<int>(1, "Density of the triangles in the tile");
            defaultSettings.GroundMaterial = new Setting<Material>(null, "Material of ground plane");

            defaultSettings.SphereSubdivs = new Setting<int>(20, "Number of divisions in sphere");

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

    public static void GenerateSphereMesh(Settings settings) {
        // Create a cube and subdivide it
        Mesh mesh = CreateUnitSphere(settings.SphereSubdivs.value);

        // Normalize vertices to "inflate" the cube into a sphere
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++) {
            vertices[i] = vertices[i].normalized;
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Save the mesh asset
        AssetDatabase.CreateAsset(mesh, "Assets/Sphere.asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static Mesh CreateUnitSphere(int subdivisions) {
        // Create an initial cube with normalized vertices
        Mesh mesh = CreateNormalizedCube();

        // Subdivide
        for (int i = 0; i < subdivisions; i++) {
            Subdivide(mesh);
        }

        return mesh;
    }

    private static Mesh CreateNormalizedCube() {
        Mesh mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3> {
            new Vector3(-1, -1, 1), new Vector3(1, -1, 1), new Vector3(1, 1, 1), new Vector3(-1, 1, 1),
            new Vector3(-1, -1, -1), new Vector3(1, -1, -1), new Vector3(1, 1, -1), new Vector3(-1, 1, -1),
        };

        List<int> triangles = new List<int> {
            0, 1, 2, 0, 2, 3,
            1, 5, 6, 1, 6, 2,
            5, 4, 7, 5, 7, 6,
            4, 0, 3, 4, 3, 7,
            3, 2, 6, 3, 6, 7,
            4, 5, 1, 4, 1, 0
        };

        mesh.SetVertices(vertices);
        for (int i = 0; i < vertices.Count; i++) {
            mesh.vertices[i] = mesh.vertices[i].normalized;
        }

        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();

        return mesh;
    }

    private static void Subdivide(Mesh mesh) {
        var oldVertices = mesh.vertices;
        var oldTriangles = mesh.triangles;
        var newVertices = new List<Vector3>(oldVertices);
        var newTriangles = new List<int>();

        var midPointCache = new Dictionary<(int, int), int>();

        for (int i = 0; i < oldTriangles.Length; i += 3) {
            int i0 = oldTriangles[i];
            int i1 = oldTriangles[i + 1];
            int i2 = oldTriangles[i + 2];

            // Get the midpoint for each edge of the triangle
            int a = GetMidPointIndex(midPointCache, i0, i1, newVertices, oldVertices);
            int b = GetMidPointIndex(midPointCache, i1, i2, newVertices, oldVertices);
            int c = GetMidPointIndex(midPointCache, i2, i0, newVertices, oldVertices);

            // Create four new triangles from the old one
            newTriangles.AddRange(new int[] { i0, a, c });
            newTriangles.AddRange(new int[] { i1, b, a });
            newTriangles.AddRange(new int[] { i2, c, b });
            newTriangles.AddRange(new int[] { a, b, c });
        }

        mesh.vertices = newVertices.ToArray();
        mesh.triangles = newTriangles.ToArray();
        mesh.RecalculateNormals();
    }

    private static int GetMidPointIndex(Dictionary<(int, int), int> cache, int index1, int index2,
        List<Vector3> vertices, Vector3[] oldVertices) {
        (int, int) edgeKey = index1 < index2 ? (index1, index2) : (index2, index1);

        if (cache.TryGetValue(edgeKey, out int midpointIndex)) {
            return midpointIndex;
        }

        Vector3 midpoint = (oldVertices[index1] + oldVertices[index2]).normalized;
        midpointIndex = vertices.Count;
        vertices.Add(midpoint);

        cache[edgeKey] = midpointIndex;
        return midpointIndex;
    }
}
