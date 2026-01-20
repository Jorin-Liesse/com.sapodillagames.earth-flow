using UnityEngine;
using System.Collections.Generic;

public class Chunking : MonoBehaviour
{
    public Vector2Int GridSize = new(10000, 10000);
    public Vector3 TileSize = new(10, 1, 10);
    public int ViewDistance = 100;
    public int BufferDistance = 1;
    public int GroupSize = 4;

    Camera _mainCamera;
    Transform _cameraTransform;
    int _loadingRadius;

    ChunkLevel[] _chunkLevels;
    ChunkData[] _chunks;
    int[] _freeChunks;
    int _freeChunksCount;

    int _hierarchyLevels;
    Vector2Int _playerChunkCoord;

    public ChunkLevel GetChunkLevelIndices(int level) => _chunkLevels[level];
    public int GetHierarchyLevels() => _hierarchyLevels;
    public ChunkData GetChunkIndices(int index) => _chunks[index];

    void Awake()
    {
        _mainCamera = Camera.main;
        _cameraTransform = _mainCamera.transform;
        _loadingRadius = ViewDistance + BufferDistance;

        _hierarchyLevels = CalculateHierarchyLevels();

        int circularCoordsCount = CalculateCircularCoords(_loadingRadius, 1).Length;
        int poolSize = CalculatePoolSize(circularCoordsCount, _hierarchyLevels);

        _chunkLevels = new ChunkLevel[_hierarchyLevels];
        for (int level = 0; level < _hierarchyLevels; level++)
        {
            int capacity = Mathf.CeilToInt(poolSize / Mathf.Pow(GroupSize, level));
            _chunkLevels[level] = CreateChunkLevel(capacity, level);
        }

        _chunks = new ChunkData[poolSize];
        _freeChunks = new int[poolSize];
        _freeChunksCount = poolSize;

        for (int i = 0; i < poolSize; i++)
        {
            _chunks[i] = CreateChunkData();
            _freeChunks[i] = i;
        }
    }

    void Update()
    {
        _playerChunkCoord.x = Mathf.FloorToInt((_cameraTransform.position.x - transform.position.x) / TileSize.x);
        _playerChunkCoord.y = Mathf.FloorToInt((_cameraTransform.position.z - transform.position.z) / TileSize.z);

        FreeInactiveChunks();
        ActivateNewChunks();
    }

    void FreeInactiveChunks()
    {
        for (int levelIndex = 0; levelIndex < _chunkLevels.Length; levelIndex++)
            _chunkLevels[levelIndex].AddedRemovedCount = 0;

        for (int level = 0; level < _chunkLevels.Length; level++)
        {
            ref ChunkLevel chunkLevel = ref _chunkLevels[level];
            int[] used = chunkLevel.UsedChunks;
            int gridScale = chunkLevel.GridScale;

            int radiusGrid = Mathf.CeilToInt((float)_loadingRadius / gridScale + 1f);
            int radiusGridSqr = radiusGrid * radiusGrid;

            float playerGridXf = _playerChunkCoord.x / (float)gridScale - 0.5f;
            float playerGridYf = _playerChunkCoord.y / (float)gridScale - 0.5f;

            int playerGridX = Mathf.RoundToInt(playerGridXf);
            int playerGridY = Mathf.RoundToInt(playerGridYf);

            for (int i = 0; i < chunkLevel.UsedChunksCount; i++)
            {
                int index = used[i];
                ChunkData chunk = _chunks[index];

                int dx = chunk.GridCoord.x - playerGridX;
                int dy = chunk.GridCoord.y - playerGridY;

                if (dx * dx + dy * dy < radiusGridSqr) continue;

                chunkLevel.UsedChunksMap.Remove(chunk.GridCoord);

                int last = --chunkLevel.UsedChunksCount;
                chunkLevel.UsedChunks[i] = chunkLevel.UsedChunks[last];

                _freeChunks[_freeChunksCount++] = index;
                chunkLevel.AddedRemoved[chunkLevel.AddedRemovedCount++] = index;
            }
        }
    }

    void ActivateNewChunks()
    {
        for (int levelIndex = 0; levelIndex < _chunkLevels.Length; levelIndex++)
            _chunkLevels[levelIndex].AddedRemovedCount = 0;

        for (int level = 0; level < _chunkLevels.Length; level++)
        {
            ref ChunkLevel chunkLevel = ref _chunkLevels[level];
            int gridScale = chunkLevel.GridScale;

            int radiusGrid = Mathf.CeilToInt((float)_loadingRadius / gridScale + 1);
            int radiusGridSqr = radiusGrid * radiusGrid;

            float playerGridXf = _playerChunkCoord.x / (float)gridScale - 0.5f;
            float playerGridYf = _playerChunkCoord.y / (float)gridScale - 0.5f;

            int playerGridX = Mathf.RoundToInt(playerGridXf);
            int playerGridY = Mathf.RoundToInt(playerGridYf);

            for (int i = 0; i < chunkLevel.CircularCoords.Length; i++)
            {
                Vector2Int coord = new(chunkLevel.CircularCoords[i].x + playerGridX, chunkLevel.CircularCoords[i].y + playerGridY);

                int dx = coord.x - playerGridX;
                int dy = coord.y - playerGridY;

                if (dx * dx + dy * dy >= radiusGridSqr) continue;
                if (chunkLevel.UsedChunksMap.ContainsKey(coord)) continue;
                if (_freeChunksCount <= 0) break;

                int index = _freeChunks[--_freeChunksCount];
                chunkLevel.UsedChunks[chunkLevel.UsedChunksCount++] = index;
                chunkLevel.UsedChunksMap.Add(coord, index);
                chunkLevel.AddedRemoved[chunkLevel.AddedRemovedCount++] = index;

                SetChunkData(ref _chunks[index], transform.position, coord, gridScale);
            }
        }
    }

    Vector2Int[] CalculateCircularCoords(int radius, int scale)
    {
        int scaledRadius = Mathf.CeilToInt(((float)radius / (float)scale) + 1f);

        int radiusSqr = scaledRadius * scaledRadius;
        List<Vector2Int> coords = new(radiusSqr * 4);

        for (int x = -scaledRadius; x <= scaledRadius; x++)
            for (int y = -scaledRadius; y <= scaledRadius; y++)
                if ((x + 0.5f) * (x + 0.5f) + (y + 0.5f) * (y + 0.5f) <= radiusSqr)
                    coords.Add(new Vector2Int(x, y));

        return coords.ToArray();
    }

    int CalculateHierarchyLevels()
    {
        return Mathf.CeilToInt(Mathf.Log(_loadingRadius, GroupSize));
    }

    int CalculatePoolSize(int circularCoordsCount, int hierarchyLevels)
    {
        int size = 0;

        for (int level = 0; level < hierarchyLevels; level++)
            size += (circularCoordsCount + GroupSize * level) / (GroupSize * level + 1);

        return size;
    }

    void SetChunkData(ref ChunkData chunk, Vector3 offset, Vector2Int gridCoord, int gridScale)
    {
        chunk.IsVisible = false;
        chunk.Parent = -1;
        chunk.Children.Clear();

        chunk.GridCoord = gridCoord;

        chunk.Bounds.size = TileSize * gridScale;
        chunk.Bounds.center = new Vector3(
            (chunk.GridCoord.x + 0.5f) * TileSize.x * gridScale + offset.x,
            offset.y,
            (chunk.GridCoord.y + 0.5f) * TileSize.z * gridScale + offset.z
        );
    }

    ChunkData CreateChunkData()
    {
        ChunkData chunk = new();

        chunk.Bounds = new();
        chunk.Children = new(GroupSize * GroupSize);

        return chunk;
    }

    ChunkLevel CreateChunkLevel(int capacity, int hierarchyLevel)
    {
        ChunkLevel chunkLevel = new();

        chunkLevel.UsedChunks = new int[capacity];
        chunkLevel.UsedChunksMap = new(capacity);
        chunkLevel.AddedRemoved = new int[capacity];

        chunkLevel.AddedRemovedCount = 0;
        chunkLevel.UsedChunksCount = 0;

        chunkLevel.HierarchyLevel = hierarchyLevel;
        chunkLevel.GridScale = (hierarchyLevel == 0) ? 1 : (int)Mathf.Pow(GroupSize, hierarchyLevel);

        chunkLevel.CircularCoords = CalculateCircularCoords(_loadingRadius, chunkLevel.GridScale);

        return chunkLevel;
    }
}

public struct ChunkLevel
{
    public Dictionary<Vector2Int, int> UsedChunksMap;

    public int[] UsedChunks;
    public int UsedChunksCount;

    public int[] AddedRemoved;
    public int AddedRemovedCount;

    public int HierarchyLevel;
    public int GridScale;
    public Vector2Int[] CircularCoords;
}

public struct ChunkData
{
    public Vector2Int GridCoord;
    public Bounds Bounds;
    public bool IsVisible;
    public int Parent;
    public List<int> Children;
}
