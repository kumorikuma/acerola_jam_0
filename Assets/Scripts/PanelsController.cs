using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelsController : Singleton<PanelsController> {
    [NonNullField] public Terrain ColliderTerrain;
    [NonNullField] public Terrain LowerColliderTerrain;
    
    public int PanelsPerSide = 100;
    public float PanelSize = 5.0f;
    public int PanelsPerCell = 2;

    // Size of all the panels one side combined.
    private Vector3 _worldSpaceToPanelSpaceOffset;
    private float _panelsContainerSize;
    private float _cellSize;
    private int _cellSizeTexels;
    bool[,] _holePunchData;

    // A Cell is composed of N=2 panels.
    
    // TODO: Iterate over all the panels and build a 2D array of them.

    protected override void Awake() {
        base.Awake();

        _cellSize = PanelSize * PanelsPerCell;
        _panelsContainerSize = PanelsPerSide * PanelSize;
        _worldSpaceToPanelSpaceOffset = new Vector3(_panelsContainerSize / 2.0f, 0, _panelsContainerSize / 2.0f);

        // Measure how many terrain texels are in this size.
        Vector3 pos1 = Vector3.zero;
        Vector3 pos2 = new Vector3(_cellSize, 0, 0);
        int pos1Col = ConvertToAlphamapCoordinates(pos1).x;
        int pos2Col = ConvertToAlphamapCoordinates(pos2).x;
        _cellSizeTexels = pos2Col - pos1Col;
        Debug.Log("Cell Size in Texels: " + _cellSizeTexels);

        _holePunchData = new bool[_cellSizeTexels, _cellSizeTexels];
        for (int row = 0; row < _cellSizeTexels; row++) {
            for (int col = 0; col < _cellSizeTexels; col++) {
                _holePunchData[row, col] = false;
            }
        }

        // Clone the terrain data so we can revert it back
        // See: https://forum.unity.com/threads/solved-how-to-modify-a-terrain-at-runtime-and-get-back-to-original-terrain-on-exit.487505/
        ColliderTerrain.terrainData = TerrainDataCloner.Clone(ColliderTerrain.terrainData);
        ColliderTerrain.GetComponent<TerrainCollider>().terrainData =
            ColliderTerrain.terrainData; // Don't forget to update the TerrainCollider as well
        // Set the lower collider terrain as well
        LowerColliderTerrain.terrainData = ColliderTerrain.terrainData;
        LowerColliderTerrain.GetComponent<TerrainCollider>().terrainData = LowerColliderTerrain.terrainData;
    }

    // Update is called once per frame
    void Update() {
        // Periodically, pick a random group of four cells to destroy
    }


    public void PunchHoleInTerrain(Vector3 worldPosition) {
        Vector2Int cellCoordinates = WorldPositionToCellCoordinates(worldPosition);
        Debug.Log("World Pos: " + worldPosition);
        Debug.Log("Cell Coordinates: " + cellCoordinates);
        PunchHoleInTerrainAtCellCoordinates(cellCoordinates.x, cellCoordinates.y);
    }

    public void PunchHoleInTerrainAtCellCoordinates(int col, int row) {
        // Coordinates are in panel space, need to convert back to world space.
        float x = col * _cellSize;
        float y = row * _cellSize;
        Vector3 panelSpaceCoordinates = new Vector3(x, 0, y);
        Debug.Log("panelSpaceCoordinates: " + panelSpaceCoordinates);
        Vector3 worldSpacePosition = panelSpaceCoordinates - _worldSpaceToPanelSpaceOffset;
        _PunchHoleInTerrain(worldSpacePosition);
    }

    public Vector2Int WorldPositionToCellCoordinates(Vector3 worldPosition) {
        Vector3 panelSpacePosition =
            worldPosition + _worldSpaceToPanelSpaceOffset;
        int col = (int)(panelSpacePosition.x / _cellSize);
        int row = (int)(panelSpacePosition.z / _cellSize);
        return new Vector2Int(col, row);
    }

    private void _PunchHoleInTerrain(Vector3 worldPosition) {
        Debug.Log("PunchHoleInTerrain World Pos: " + worldPosition);
        // The size of the hole we need to punch is PanelSize * PanelsPerCell by PanelSize * PanelsPerCell.
        // Then we need to pass in the Row/Col of where to punch it.
        Vector2Int coordinates = ConvertToAlphamapCoordinates(worldPosition);
        ColliderTerrain.terrainData.SetHoles(coordinates.x, coordinates.y, _holePunchData);
    }

    Vector2Int ConvertToAlphamapCoordinates(Vector3 worldPosition) {
        Vector3 terrainPos = ColliderTerrain.transform.position;
        TerrainData terrainData = ColliderTerrain.terrainData;

        // Convert position in world space to terrain space
        float xPosTerrainSpace = worldPosition.x - terrainPos.x;
        float zPosTerrainSpace = worldPosition.z - terrainPos.z;

        // Convert to UV terrain coordinates [0, 1]
        float u = xPosTerrainSpace / terrainData.size.x;
        float v = zPosTerrainSpace / terrainData.size.z;

        // Convert to XY terrain coordinates [0, alphamapWidth/alphamapHeight - 1]
        int terrainX = Mathf.RoundToInt(u * (terrainData.alphamapWidth - 1));
        int terrainY = Mathf.RoundToInt(v * (terrainData.alphamapHeight - 1));

        // Clamp the values to make sure this is never invalid.
        return new Vector2Int(Mathf.Clamp(terrainX, 0, terrainData.alphamapWidth - 1),
            Mathf.Clamp(terrainY, 0, terrainData.alphamapHeight - 1));
    }
}
