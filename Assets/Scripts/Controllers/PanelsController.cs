using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class PanelsController : Singleton<PanelsController> {
    [NonNullField] public Terrain ColliderTerrain;
    [NonNullField] public Terrain LowerColliderTerrain;
    [NonNullField] public Transform BlackHoleObject;
    [NonNullField] public GameObject DummyProjectilesContainer;
    [NonNullField] public GameObject DummyPanelPrefab;

    public float TerrainSize = 640.0f;
    public float PanelSize = 5.0f;

    // A Cell is composed of N=2 panels.
    public int PanelsPerCell = 2;

    // Panel Destruction Settings
    public float CellDestructionCooldown = 3.0f;
    public int SectorsPerSide = 4;

    // Panel Projectile Settings
    public float PanelInitialSpeed = 1.0f;
    public float PanelSpeedRandomOffsetFactor = 1.0f;
    public float PanelAcceleration = 0.5f;
    public float PanelTurningSpeed = 1.0f;
    public Vector3 PanelInitialAngleVelocity = new Vector3(0, 0, 1.0f);
    public float PanelAngularVelocityDecay = 1.0f;
    public Vector3 PanelVelocityOffset = Vector3.zero;
    public float PanelAccelerationRandomOffsetFactor = 1.0f;
    public float PanelTurningSpeedRandomOffsetFactor = 1.0f;
    public float PanelTurnTowardsTargetSpeed = 1.0f;

    // Size of all the panels one side combined.
    private int _panelsPerSide;
    private int _cellsPerSide;
    private Vector3 _worldSpaceToPanelSpaceOffset;
    private Vector3 _panelSizeOffset;
    private float _panelsContainerSize;
    private float _cellSize;
    private int _cellSizeTexels;
    private bool[,] _holePunchData;

    // State
    private bool[,] _panelProjectileSpawned;
    private GameObject[,] _dummyPanelObjects;
    private List<List<int>> _sectors = new();
    private bool _isDestroyingLevel = false;
    private float _CellDestructionCooldownCountdown = 0.0f;

    protected override void Awake() {
        base.Awake();

        // Constants
        _panelsPerSide = Mathf.RoundToInt(TerrainSize / PanelSize);
        _cellSize = PanelSize * PanelsPerCell;
        _cellsPerSide = Mathf.RoundToInt(TerrainSize / _cellSize);
        _panelsContainerSize = _panelsPerSide * PanelSize;
        _worldSpaceToPanelSpaceOffset = new Vector3(_panelsContainerSize / 2.0f, 0, _panelsContainerSize / 2.0f);
        // Debug.Log($"[PanelsController] PanelsPerSide: " + _panelsPerSide);
        // Debug.Log($"[PanelsController] _panelsContainerSize: " + _panelsContainerSize);
        // Debug.Log($"[PanelsController] _worldSpaceToPanelSpaceOffset: " + _worldSpaceToPanelSpaceOffset);
        _panelSizeOffset = new Vector3(PanelSize / 2.0f, -0.5f, PanelSize / 2.0f);
        // Measure how many terrain texels are in this size.
        Vector3 pos1 = Vector3.zero;
        Vector3 pos2 = new Vector3(_cellSize, 0, 0);
        int pos1Col = ConvertToAlphamapCoordinates(pos1).x;
        int pos2Col = ConvertToAlphamapCoordinates(pos2).x;
        _cellSizeTexels = pos2Col - pos1Col;

        // Compute this once since it never changes.
        _holePunchData = new bool[_cellSizeTexels, _cellSizeTexels];
        for (int row = 0; row < _cellSizeTexels; row++) {
            for (int col = 0; col < _cellSizeTexels; col++) {
                _holePunchData[row, col] = false;
            }
        }

        Reset();
    }

    public void Reset() {
        _isDestroyingLevel = false;
        _CellDestructionCooldownCountdown = 0.0f;

        // Reset the spawned states
        _panelProjectileSpawned = new bool[_panelsPerSide, _panelsPerSide];
        _dummyPanelObjects = new GameObject[_panelsPerSide, _panelsPerSide];
        for (int row = 0; row < _panelsPerSide; row++) {
            for (int col = 0; col < _panelsPerSide; col++) {
                _panelProjectileSpawned[row, col] = false;
                _dummyPanelObjects[row, col] = null;
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

        // Iterate through all the indices and all them to the appropriate sector.
        _sectors.Clear();
        for (int sector = 0; sector < SectorsPerSide * SectorsPerSide; sector++) {
            _sectors.Add(new List<int>());
        }

        int cellsPerSector = _cellsPerSide / SectorsPerSide;
        for (int row = 0; row < _cellsPerSide; row++) {
            int sectorRow = (int)(row / (float)cellsPerSector);
            for (int col = 0; col < _cellsPerSide; col++) {
                int sectorCol = (int)(col / (float)cellsPerSector);
                int sector = SectorFromCoordinates(sectorCol, sectorRow);
                int cellIdx = CellIndexFromCoordinates(col, row);
                _sectors[sector].Add(cellIdx);
            }
        }
    }

    private int CellIndexFromCoordinates(int cellCol, int cellRow) {
        return cellCol + cellRow * _cellsPerSide;
    }

    private Vector2Int CellCoordinatesFromIndex(int cellIdx) {
        int cellRow = cellIdx / _cellsPerSide;
        int cellCol = cellIdx % _cellsPerSide;
        return new Vector2Int(cellCol, cellRow);
    }

    private int SectorFromCoordinates(int sectorCol, int sectorRow) {
        return sectorCol + sectorRow * SectorsPerSide;
    }

    public void StartDestroyingLevel() {
        _isDestroyingLevel = true;
    }

    public void StopDestroyingLevel() {
        _isDestroyingLevel = false;
    }

    public bool IsDestroyingLevel() {
        return _isDestroyingLevel;
    }

    void FixedUpdate() {
        if (!_isDestroyingLevel) {
            return;
        }

        if (_CellDestructionCooldownCountdown > 0.0f) {
            _CellDestructionCooldownCountdown -= Time.fixedDeltaTime;
        }

        if (_CellDestructionCooldownCountdown <= 0.0f) {
            _CellDestructionCooldownCountdown = CellDestructionCooldown;
            DestroyRandomCellInEachSector();
        }
    }

    public void DestroyRandomCellInEachSector() {
        // Maybe we divide up into sectors (4).
        // Within each sector, we have a list of all the indices in there.
        // We will pick a random index from the list everytime.
        for (int sector = 0; sector < _sectors.Count; sector++) {
            List<int> cellList = _sectors[sector];
            if (cellList.Count == 0) {
                continue;
            }

            // Pick a random cell
            int cellIdx = Random.Range(0, cellList.Count);
            Vector2Int cellCoordinates = CellCoordinatesFromIndex(cellList[cellIdx]);
            DestroyCell(cellCoordinates.x, cellCoordinates.y);

            // Remove it so it's not picked again
            cellList.RemoveAt(cellIdx);
        }
    }

    public void DestroyCellAt(Vector3 worldPosition) {
        Vector2Int cellCoordinates = WorldPositionToCellCoordinates(worldPosition);
        DestroyCell(cellCoordinates.x, cellCoordinates.y);
    }

    public void DestroyCell(int cellCol, int cellRow) {
        if (cellCol < 0 || cellCol >= _cellsPerSide || cellRow < 0 || cellRow >= _cellsPerSide) {
            Debug.LogError($"[PanelsController] Destroy Cell out of bounds: {cellCol}, {cellRow}");
            return;
        }

        // Firstly, punch a hole in the terrain.
        // Coordinates are in panel space, need to convert back to world space.
        Vector3 panelSpaceCoordinates = new Vector3(cellCol * _cellSize, 0, cellRow * _cellSize);
        Vector3 worldSpacePosition = panelSpaceCoordinates - _worldSpaceToPanelSpaceOffset;
        _PunchHoleInTerrain(worldSpacePosition);

        Vector3 cellCenterWorldSpace = panelSpaceCoordinates + new Vector3(_cellSize / 2.0f, 0, _cellSize / 2.0f) -
                                       _worldSpaceToPanelSpaceOffset;

        // Next, handle the panels. This affects all panels that are in this cell.
        int panelRow, panelCol;
        for (int j = 0; j < PanelsPerCell; j++) {
            for (int i = 0; i < PanelsPerCell; i++) {
                panelRow = cellRow * PanelsPerCell + j;
                panelCol = cellCol * PanelsPerCell + i;

                // Ignore this if it's out of bounds.
                if (panelCol < 0 || panelCol >= _panelsPerSide || panelRow < 0 || panelRow >= _panelsPerSide) {
                    continue;
                }

                // Detect if there's a dummy panel spawned for that location and destroy it if there is.
                GameObject dummyPanel = _dummyPanelObjects[panelRow, panelCol];
                if (dummyPanel != null) {
                    Object.Destroy(dummyPanel);
                }

                // Spawn in a new projectile panel
                panelSpaceCoordinates = new Vector3(panelCol * PanelSize, 0, panelRow * PanelSize) + _panelSizeOffset;
                worldSpacePosition = panelSpaceCoordinates - _worldSpaceToPanelSpaceOffset;
                float randomSpeedOffset = Random.value * PanelSpeedRandomOffsetFactor;
                // Vector from center of 
                Vector3 centerToSpawn = worldSpacePosition - cellCenterWorldSpace;
                Quaternion rotation = Quaternion.Euler(0,
                    Mathf.Atan2(centerToSpawn.x, centerToSpawn.z) * Mathf.Rad2Deg, 0);
                Vector3 angularVelocity = rotation * PanelInitialAngleVelocity;
                // VectorDebug.Instance.DrawDebugVector($"{panelCol}, {panelRow}", centerToSpawn, cellCenterWorldSpace,
                //     Color.red);
                // VectorDebug.Instance.DrawDebugVector($"Rotation", rotation * Vector3.forward, cellCenterWorldSpace,
                //     Color.green);
                GameObject projectilePanel = ProjectileController.Instance
                    .SpawnPanel(worldSpacePosition, PanelInitialSpeed + randomSpeedOffset,
                        angularVelocity, PanelAngularVelocityDecay, rotation * PanelVelocityOffset).gameObject;
                _panelProjectileSpawned[panelRow, panelCol] = true;

                // Change the behavior after a delay
                StartCoroutine(PanelProjectileSecondStage(projectilePanel.GetComponent<Projectile>(),
                    BlackHoleObject));
            }
        }

        // Finally, spawn in dummy panels around the hole (no need for diagonals).
        panelRow = cellRow * PanelsPerCell - 1;
        for (int i = 0; i < PanelsPerCell; i++) {
            panelCol = cellCol * PanelsPerCell + i;
            SpawnDummyPanelAt(panelCol, panelRow);
        }

        panelRow = cellRow * PanelsPerCell + PanelsPerCell;
        for (int i = 0; i < PanelsPerCell; i++) {
            panelCol = cellCol * PanelsPerCell + i;
            SpawnDummyPanelAt(panelCol, panelRow);
        }

        panelCol = cellCol * PanelsPerCell - 1;
        for (int j = 0; j < PanelsPerCell; j++) {
            panelRow = cellRow * PanelsPerCell + j;
            SpawnDummyPanelAt(panelCol, panelRow);
        }

        panelCol = cellCol * PanelsPerCell + PanelsPerCell;
        for (int j = 0; j < PanelsPerCell; j++) {
            panelRow = cellRow * PanelsPerCell + j;
            SpawnDummyPanelAt(panelCol, panelRow);
        }
    }

    IEnumerator PanelProjectileSecondStage(Projectile panel, Transform trackedTarget) {
        yield return new WaitForSeconds(2.0f);

        if (_isDestroyingLevel) {
            panel.TrackedTarget = trackedTarget;
            panel.Acceleration = PanelAcceleration + Random.value * PanelAccelerationRandomOffsetFactor;
            panel.TurningSpeed = PanelTurningSpeed + Random.value * PanelTurningSpeedRandomOffsetFactor;
            panel.TurnTowardsTarget = true;
            panel.TurnTowardsTargetSpeed = PanelTurnTowardsTargetSpeed;
        }
    }

    private void SpawnDummyPanelAt(int panelCol, int panelRow) {
        // Ignore this if it's out of bounds.
        if (panelCol < 0 || panelCol >= _panelsPerSide || panelRow < 0 || panelRow >= _panelsPerSide) {
            return;
        }

        GameObject dummyPanel = _dummyPanelObjects[panelRow, panelCol];
        if (dummyPanel != null || _panelProjectileSpawned[panelRow, panelCol]) {
            return;
        }

        // Spawn in a new dummy panel
        Vector3 panelSpaceCoordinates = new Vector3(panelCol * PanelSize, 0, panelRow * PanelSize) + _panelSizeOffset;
        Vector3 worldSpacePosition = panelSpaceCoordinates - _worldSpaceToPanelSpaceOffset;
        dummyPanel = Instantiate(DummyPanelPrefab, worldSpacePosition,
            Quaternion.identity, DummyProjectilesContainer.transform);
        _dummyPanelObjects[panelRow, panelCol] = dummyPanel;
    }

    public Vector2Int WorldPositionToCellCoordinates(Vector3 worldPosition) {
        Vector3 panelSpacePosition =
            worldPosition + _worldSpaceToPanelSpaceOffset;
        int col = (int)(panelSpacePosition.x / _cellSize);
        int row = (int)(panelSpacePosition.z / _cellSize);
        return new Vector2Int(col, row);
    }

    private void _PunchHoleInTerrain(Vector3 worldPosition) {
        // The size of the hole we need to punch is PanelSize * PanelsPerCell by PanelSize * PanelsPerCell.
        // Then we need to pass in the Row/Col of where to punch it.
        Vector2Int coordinates = ConvertToAlphamapCoordinates(worldPosition);
        try {
            ColliderTerrain.terrainData.SetHoles(coordinates.x, coordinates.y, _holePunchData);
        } catch (Exception e) {
            Debug.LogError(
                $"[PanelsController] Error Destroying at Coordinates: {coordinates}. World Pos: {worldPosition}");
            Debug.Log(
                $"AlphaMap Width: {ColliderTerrain.terrainData.alphamapWidth}. Height: {ColliderTerrain.terrainData.alphamapWidth}");
            throw;
        }
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
        int terrainX = (int)(u * (terrainData.alphamapWidth - 1)) + 1;
        int terrainY = (int)(v * (terrainData.alphamapHeight - 1)) + 1;

        // Clamp the values to make sure this is never invalid.
        return new Vector2Int(Mathf.Clamp(terrainX, 0, terrainData.alphamapWidth - 1),
            Mathf.Clamp(terrainY, 0, terrainData.alphamapHeight - 1));
    }
}
