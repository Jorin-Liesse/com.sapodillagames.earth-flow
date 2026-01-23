using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using System;

public class Chunking : MonoBehaviour
{
    public int2 GridSize = new(10000, 10000);
    public int3 TileSize = new(10, 1, 10);
    public int ViewDistance = 100;
    public int BufferDistance = 1;
    public int GroupSize = 4;

    Camera _mainCamera;
    Transform _cameraTransform;
    int _loadingRadius;

    NativeArray<ChunkLevel> _chunkLevels;
    NativeArray<ChunkData> _chunks;
    NativeArray<int> _freeChunks;
    int _freeChunksCount;

    int _hierarchyLevels;
    int2 _playerChunkCoord;
    int2 _lastPlayerChunkCoord;

    public ChunkLevel GetChunkLevel(int level) => _chunkLevels[level];
    public int GetHierarchyLevels() => _hierarchyLevels;
    public ChunkData GetChunk(int index) => _chunks[index];

    void Awake()
    {
        _mainCamera = Camera.main;
        _cameraTransform = _mainCamera.transform;
        _loadingRadius = ViewDistance + BufferDistance;
        _lastPlayerChunkCoord = new(int.MinValue, int.MinValue);

        _hierarchyLevels = ChunkingHelper.CalculateHierarchyLevels(_loadingRadius, GroupSize);

        int poolSize = 0;
        _chunkLevels = new(_hierarchyLevels, Allocator.Persistent);

        for (int level = 0; level < _hierarchyLevels; level++)
        {
            ChunkLevel chunkLevel = ChunkingHelper.CreateChunkLevel(_loadingRadius, level, GroupSize);
            _chunkLevels[level] = chunkLevel;
            poolSize += chunkLevel.UsedChunks.Length;
        }

        _chunks = new(poolSize, Allocator.Persistent);
        _freeChunks = new(poolSize, Allocator.Persistent);
        _freeChunksCount = poolSize;

        for (int i = 0; i < poolSize; i++)
        {
            _chunks[i] = ChunkingHelper.CreateChunkData(GroupSize);
            _freeChunks[i] = i;
        }
    }

    void OnDestroy()
    {
        if (_chunks.IsCreated)
        {
            for (int i = 0; i < _chunks.Length; i++)
                _chunks[i].Dispose();

            _chunks.Dispose();
        }

        if (_chunkLevels.IsCreated)
        {
            for (int i = 0; i < _chunkLevels.Length; i++)
                _chunkLevels[i].Dispose();

            _chunkLevels.Dispose();
        }

        if (_freeChunks.IsCreated) _freeChunks.Dispose();
    }

    void Update()
    {
        _playerChunkCoord.x = (int)math.floor((_cameraTransform.position.x - transform.position.x) / TileSize.x);
        _playerChunkCoord.y = (int)math.floor((_cameraTransform.position.z - transform.position.z) / TileSize.z);

        if (math.all(_playerChunkCoord == _lastPlayerChunkCoord)) return;

        FindChunksToRemove();
        RemoveChunks();
        CleanRelations();

        FindChunksToAdd();
        AddChunks();
        BuildRelations();

        _lastPlayerChunkCoord = _playerChunkCoord;
    }

    void FindChunksToRemove()
    {
        for (int levelIndex = 0; levelIndex < _hierarchyLevels; levelIndex++)
        {
            ChunkLevel level = _chunkLevels[levelIndex];
            level.RemovedChunksCount = 0;
            _chunkLevels[levelIndex] = level;
        }

        for (int level = 0; level < _hierarchyLevels; level++)
        {
            ChunkLevel chunkLevel = _chunkLevels[level];
            int gridScale = chunkLevel.GridScale;

            int radiusGrid = (int)math.ceil((float)_loadingRadius / gridScale + 1f);
            int radiusGridSqr = radiusGrid * radiusGrid;

            float2 playerGridF = (float2)_playerChunkCoord / gridScale - 0.5f;
            int2 playerGrid = new((int)math.round(playerGridF.x), (int)math.round(playerGridF.y));

            for (int i = chunkLevel.UsedChunksCount - 1; i >= 0; i--)
            {
                int chunkIndex = chunkLevel.UsedChunks[i];
                ChunkData chunk = _chunks[chunkIndex];

                int2 delta = chunk.GridCoord - playerGrid;
                int distSqr = delta.x * delta.x + delta.y * delta.y;

                if (distSqr < radiusGridSqr) continue;

                chunkLevel.RemovedChunks[chunkLevel.RemovedChunksCount++] = (chunkIndex, i);
            }

            _chunkLevels[level] = chunkLevel;
        }
    }

    void RemoveChunks()
    {
        for (int level = 0; level < _hierarchyLevels; level++)
        {
            ChunkLevel chunkLevel = _chunkLevels[level];

            for (int i = 0; i < chunkLevel.RemovedChunksCount; i++)
            {
                int chunkIndex = chunkLevel.RemovedChunks[i].chunksIndex;
                int usedIndex = chunkLevel.RemovedChunks[i].usedIndex;
                ChunkData chunk = _chunks[chunkIndex];

                chunkLevel.UsedChunksMap.Remove(chunk.GridCoord);
                chunkLevel.UsedChunks[usedIndex] = chunkLevel.UsedChunks[--chunkLevel.UsedChunksCount];
                _freeChunks[_freeChunksCount++] = chunkIndex;
            }

            _chunkLevels[level] = chunkLevel;
        }
    }
    
    void CleanRelations()
    {
        for (int level = 0; level < _hierarchyLevels; level++)
        {
            ChunkLevel chunkLevel = _chunkLevels[level];

            for (int i = 0; i < chunkLevel.RemovedChunksCount; i++)
            {
                int chunkIndex = chunkLevel.RemovedChunks[i].chunksIndex;
                ChunkData chunk = _chunks[chunkIndex];

                if (chunk.Parent != -1)
                {
                    int parentIndex = chunk.Parent;
                    ChunkData parent = _chunks[parentIndex];

                    int childPos = parent.Children.IndexOf(chunkIndex);
                    if (childPos != -1)
                        parent.Children.RemoveAtSwapBack(childPos);

                    chunk.Parent = -1;

                    _chunks[parentIndex] = parent;
                }

                for (int c = 0; c < chunk.Children.Length; c++)
                {
                    int childIndex = chunk.Children[c];
                    ChunkData child = _chunks[childIndex];
                    child.Parent = -1;
                    _chunks[childIndex] = child;
                }
                chunk.Children.Clear();
                _chunks[chunkIndex] = chunk;
            }

            _chunkLevels[level] = chunkLevel;
        }
    }

    void FindChunksToAdd()
    {
        for (int levelIndex = 0; levelIndex < _hierarchyLevels; levelIndex++)
        {
            ChunkLevel level = _chunkLevels[levelIndex];
            level.AddedChunksCount = 0;
            _chunkLevels[levelIndex] = level;
        }

        for (int level = 0; level < _hierarchyLevels; level++)
        {
            ChunkLevel chunkLevel = _chunkLevels[level];
            int gridScale = chunkLevel.GridScale;

            int radiusGrid = (int)math.ceil((float)_loadingRadius / gridScale + 1);
            int radiusGridSqr = radiusGrid * radiusGrid;

            float2 playerGridF = (float2)_playerChunkCoord / gridScale - 0.5f;
            int2 playerGrid = new((int)math.round(playerGridF.x), (int)math.round(playerGridF.y));

            for (int i = 0; i < chunkLevel.CircularCoords.Length; i++)
            {
                int2 coord = chunkLevel.CircularCoords[i] + playerGrid;
                int2 delta = coord - playerGrid;
                int distSqr = delta.x * delta.x + delta.y * delta.y;

                if (distSqr >= radiusGridSqr) continue;
                if (chunkLevel.UsedChunksMap.ContainsKey(coord)) continue;
                if (_freeChunksCount <= 0) break;
                
                chunkLevel.AddedChunks[chunkLevel.AddedChunksCount++] = coord;
            }

            _chunkLevels[level] = chunkLevel;
        }
    }

    void AddChunks()
    {
        Vector3 offset = transform.position;

        for (int level = 0; level < _hierarchyLevels; level++)
        {
            ChunkLevel chunkLevel = _chunkLevels[level];
            int gridScale = chunkLevel.GridScale;

            for (int i = 0; i < chunkLevel.AddedChunksCount; i++)
            {
                int2 coord = chunkLevel.AddedChunks[i];

                int index = _freeChunks[--_freeChunksCount];
                chunkLevel.UsedChunks[chunkLevel.UsedChunksCount++] = index;
                chunkLevel.UsedChunksMap.Add(coord, index);

                ChunkData chunk = _chunks[index];
                ChunkingHelper.SetChunkData(ref chunk, offset, coord, gridScale, TileSize);
                _chunks[index] = chunk;
            }

            _chunkLevels[level] = chunkLevel;
        }
    }

    void BuildRelations()
    {
        for (int level = 0; level < _hierarchyLevels - 1; level++)
        {
            ChunkLevel currentLevel = _chunkLevels[level];
            ChunkLevel parentLevel = _chunkLevels[level + 1];

            for (int i = 0; i < currentLevel.AddedChunksCount; i++)
            {
                int chunkIndex = currentLevel.UsedChunksMap[currentLevel.AddedChunks[i]];
                ChunkData chunk = _chunks[chunkIndex];

                int2 parentGridCoord = new(
                    (int)math.floor((float)chunk.GridCoord.x / GroupSize),
                    (int)math.floor((float)chunk.GridCoord.y / GroupSize)
                );

                if (parentLevel.UsedChunksMap.TryGetValue(parentGridCoord, out int parentIndex))
                {
                    ChunkData parent = _chunks[parentIndex];

                    chunk.Parent = parentIndex;
                    parent.Children.Add(chunkIndex);

                    _chunks[parentIndex] = parent;
                    _chunks[chunkIndex] = chunk;
                }
            }
        }
    }
}

public static class ChunkingHelper
{
    public static NativeArray<int2> CalculateCircularCoords(int radius, int scale, Allocator allocator)
    {
        int scaledRadius = (int)math.ceil((float)radius / scale + 1f);
        int radiusSqr = scaledRadius * scaledRadius;

        NativeList<int2> coords = new(radiusSqr * 4, Allocator.Temp);

        for (int x = -scaledRadius; x <= scaledRadius; x++)
        {
            for (int y = -scaledRadius; y <= scaledRadius; y++)
            {
                float distSqr = (x + 0.5f) * (x + 0.5f) + (y + 0.5f) * (y + 0.5f);
                if (distSqr <= radiusSqr)
                    coords.Add(new(x, y));
            }
        }

        NativeArray<int2> result = new(coords.Length, allocator);
        coords.AsArray().CopyTo(result);
        coords.Dispose();

        return result;
    }

    public static int CalculateHierarchyLevels(int loadingRadius, int groupSize)
    {
        return (int)math.ceil(math.log(loadingRadius) / math.log(groupSize));
    }

    public static void SetChunkData(ref ChunkData chunk, Vector3 offset, int2 gridCoord, int gridScale, int3 tileSize)
    {
        chunk.IsVisible = false;
        chunk.Parent = -1;
        chunk.Children.Clear();
        chunk.GridCoord = gridCoord;

        float3 size = new(tileSize * gridScale);
        float3 center = new(
            (gridCoord.x + 0.5f) * tileSize.x * gridScale + offset.x,
            offset.y,
            (gridCoord.y + 0.5f) * tileSize.z * gridScale + offset.z
        );

        chunk.Bounds = new(center, size);
    }

    public static ChunkData CreateChunkData(int groupSize)
    {
        return new ChunkData
        {
            Bounds = new(),
            Children = new(groupSize * groupSize, Allocator.Persistent),
            IsVisible = false,
            Parent = -1
        };
    }

    public static ChunkLevel CreateChunkLevel(int loadingRadius, int hierarchyLevel, int groupSize)
    {
        int gridScale = (hierarchyLevel == 0) ? 1 : (int)math.pow(groupSize, hierarchyLevel);
        NativeArray<int2> circularCoords = CalculateCircularCoords(loadingRadius, gridScale, Allocator.Persistent);
        int capacity = (int)(circularCoords.Length * 1.25f);

        return new ChunkLevel
        {
            HierarchyLevel = hierarchyLevel,
            GridScale = gridScale,
            CircularCoords = circularCoords,
            UsedChunks = new(capacity, Allocator.Persistent),
            UsedChunksMap = new(capacity, Allocator.Persistent),
            RemovedChunks = new(capacity, Allocator.Persistent),
            AddedChunks = new(capacity, Allocator.Persistent),
            RemovedChunksCount = 0,
            AddedChunksCount = 0,
            UsedChunksCount = 0
        };
    }
}

public struct ChunkLevel : IDisposable
{
    public NativeHashMap<int2, int> UsedChunksMap;

    public NativeArray<int> UsedChunks;
    public int UsedChunksCount;

    public NativeArray<(int chunksIndex, int usedIndex)> RemovedChunks;
    public int RemovedChunksCount;

    public NativeArray<int2> AddedChunks;
    public int AddedChunksCount;
    
    public int HierarchyLevel;
    public int GridScale;

    public NativeArray<int2> CircularCoords;

    public void Dispose()
    {
        if (UsedChunksMap.IsCreated) UsedChunksMap.Dispose();
        if (UsedChunks.IsCreated) UsedChunks.Dispose();
        if (RemovedChunks.IsCreated) RemovedChunks.Dispose();
        if (AddedChunks.IsCreated) AddedChunks.Dispose();
        if (CircularCoords.IsCreated) CircularCoords.Dispose();
    }
}

public struct ChunkData : IDisposable
{
    public int2 GridCoord;
    public Bounds Bounds;
    public bool IsVisible;
    public int Parent;
    public NativeList<int> Children;

    public void Dispose()
    {
        if (Children.IsCreated) Children.Dispose();
    }
}
