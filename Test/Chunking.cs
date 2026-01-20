using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

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

    [NativeDisableContainerSafetyRestriction]
    NativeArray<ChunkLevel> _chunkLevels;
    
    [NativeDisableContainerSafetyRestriction]
    NativeArray<ChunkData> _chunks;
    
    NativeArray<int> _freeChunks;
    int _freeChunksCount;

    int _hierarchyLevels;
    int2 _playerChunkCoord;
    int2 _lastPlayerChunkCoord;

    void Awake()
    {
        _mainCamera = Camera.main;
        _cameraTransform = _mainCamera.transform;
        _loadingRadius = ViewDistance + BufferDistance;
        _lastPlayerChunkCoord = new int2(int.MinValue, int.MinValue);

        _hierarchyLevels = ChunkingHelper.CalculateHierarchyLevels(_loadingRadius, GroupSize);

        int poolSize = 0;
        _chunkLevels = new NativeArray<ChunkLevel>(_hierarchyLevels, Allocator.Persistent);
        
        for (int level = 0; level < _hierarchyLevels; level++)
        {
            ChunkLevel chunkLevel = ChunkingHelper.CreateChunkLevel(_loadingRadius, level, GroupSize);
            _chunkLevels[level] = chunkLevel;
            poolSize += chunkLevel.UsedChunks.Length;
        }

        _chunks = new NativeArray<ChunkData>(poolSize, Allocator.Persistent);
        _freeChunks = new NativeArray<int>(poolSize, Allocator.Persistent);
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
            {
                if (_chunks[i].Children.IsCreated)
                    _chunks[i].Children.Dispose();
            }
            _chunks.Dispose();
        }
        
        if (_freeChunks.IsCreated) _freeChunks.Dispose();
        
        if (_chunkLevels.IsCreated)
        {
            for (int i = 0; i < _chunkLevels.Length; i++)
            {
                ChunkLevel level = _chunkLevels[i];
                if (level.UsedChunks.IsCreated) level.UsedChunks.Dispose();
                if (level.UsedChunksMap.IsCreated) level.UsedChunksMap.Dispose();
                if (level.AddedRemoved.IsCreated) level.AddedRemoved.Dispose();
                if (level.CircularCoords.IsCreated) level.CircularCoords.Dispose();
            }
            _chunkLevels.Dispose();
        }
    }

    void Update()
    {
        _playerChunkCoord.x = (int)math.floor((_cameraTransform.position.x - transform.position.x) / TileSize.x);
        _playerChunkCoord.y = (int)math.floor((_cameraTransform.position.z - transform.position.z) / TileSize.z);

        if (math.all(_playerChunkCoord == _lastPlayerChunkCoord)) return;

        FreeInactiveChunks();
        ActivateNewChunks();

        _lastPlayerChunkCoord = _playerChunkCoord;
    }

    unsafe void FreeInactiveChunks()
    {
        ChunkLevel* chunkLevelsPtr = (ChunkLevel*)_chunkLevels.GetUnsafePtr();
        ChunkData* chunksPtr = (ChunkData*)_chunks.GetUnsafePtr();
        int* freeChunksPtr = (int*)_freeChunks.GetUnsafePtr();

        for (int levelIndex = 0; levelIndex < _hierarchyLevels; levelIndex++)
            chunkLevelsPtr[levelIndex].AddedRemovedCount = 0;

        for (int level = 0; level < _hierarchyLevels; level++)
        {
            ref ChunkLevel chunkLevel = ref chunkLevelsPtr[level];
            int gridScale = chunkLevel.GridScale;

            int radiusGrid = (int)math.ceil((float)_loadingRadius / gridScale + 1f);
            int radiusGridSqr = radiusGrid * radiusGrid;

            float2 playerGridF = new float2(_playerChunkCoord) / gridScale - 0.5f;
            int2 playerGrid = new int2((int)math.round(playerGridF.x), (int)math.round(playerGridF.y));

            int* usedChunks = (int*)chunkLevel.UsedChunks.GetUnsafePtr();

            for (int i = chunkLevel.UsedChunksCount - 1; i >= 0; i--)
            {
                int index = usedChunks[i];
                ref ChunkData chunk = ref chunksPtr[index];

                int2 delta = chunk.GridCoord - playerGrid;
                int distSqr = delta.x * delta.x + delta.y * delta.y;

                if (distSqr < radiusGridSqr) continue;

                chunkLevel.UsedChunksMap.Remove(chunk.GridCoord);
                
                int last = --chunkLevel.UsedChunksCount;
                usedChunks[i] = usedChunks[last];

                freeChunksPtr[_freeChunksCount++] = index;
                
                int* addedRemoved = (int*)chunkLevel.AddedRemoved.GetUnsafePtr();
                addedRemoved[chunkLevel.AddedRemovedCount++] = index;
            }
        }
    }

    unsafe void ActivateNewChunks()
    {
        ChunkLevel* chunkLevelsPtr = (ChunkLevel*)_chunkLevels.GetUnsafePtr();
        ChunkData* chunksPtr = (ChunkData*)_chunks.GetUnsafePtr();
        int* freeChunksPtr = (int*)_freeChunks.GetUnsafePtr();

        for (int levelIndex = 0; levelIndex < _hierarchyLevels; levelIndex++)
            chunkLevelsPtr[levelIndex].AddedRemovedCount = 0;

        Vector3 offset = transform.position;

        for (int level = 0; level < _hierarchyLevels; level++)
        {
            ref ChunkLevel chunkLevel = ref chunkLevelsPtr[level];
            int gridScale = chunkLevel.GridScale;

            int radiusGrid = (int)math.ceil((float)_loadingRadius / gridScale + 1);
            int radiusGridSqr = radiusGrid * radiusGrid;

            float2 playerGridF = new float2(_playerChunkCoord) / gridScale - 0.5f;
            int2 playerGrid = new int2((int)math.round(playerGridF.x), (int)math.round(playerGridF.y));

            int* usedChunks = (int*)chunkLevel.UsedChunks.GetUnsafePtr();
            int* addedRemoved = (int*)chunkLevel.AddedRemoved.GetUnsafePtr();
            int2* circularCoords = (int2*)chunkLevel.CircularCoords.GetUnsafeReadOnlyPtr();

            for (int i = 0; i < chunkLevel.CircularCoords.Length; i++)
            {
                int2 coord = circularCoords[i] + playerGrid;
                int2 delta = coord - playerGrid;
                int distSqr = delta.x * delta.x + delta.y * delta.y;

                if (distSqr >= radiusGridSqr) continue;
                if (chunkLevel.UsedChunksMap.ContainsKey(coord)) continue;
                if (_freeChunksCount <= 0) break;

                int index = freeChunksPtr[--_freeChunksCount];
                usedChunks[chunkLevel.UsedChunksCount++] = index;
                chunkLevel.UsedChunksMap.Add(coord, index);
                addedRemoved[chunkLevel.AddedRemovedCount++] = index;

                ref ChunkData chunk = ref chunksPtr[index];
                ChunkingHelper.SetChunkData(ref chunk, offset, coord, gridScale, TileSize);
            }
        }
    }

    public ChunkLevel GetChunkLevel(int level) => _chunkLevels[level];
    public int GetHierarchyLevels() => _hierarchyLevels;
    public ChunkData GetChunk(int index) => _chunks[index];
}

public static class ChunkingHelper
{
    public static NativeArray<int2> CalculateCircularCoords(int radius, int scale, Allocator allocator)
    {
        int scaledRadius = (int)math.ceil((float)radius / scale + 1f);
        int radiusSqr = scaledRadius * scaledRadius;
        
        NativeList<int2> coords = new NativeList<int2>(radiusSqr * 4, Allocator.Temp);

        for (int x = -scaledRadius; x <= scaledRadius; x++)
        {
            for (int y = -scaledRadius; y <= scaledRadius; y++)
            {
                float distSqr = (x + 0.5f) * (x + 0.5f) + (y + 0.5f) * (y + 0.5f);
                if (distSqr <= radiusSqr)
                    coords.Add(new int2(x, y));
            }
        }

        NativeArray<int2> result = new NativeArray<int2>(coords.Length, allocator);
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

        float3 size = new float3(tileSize) * gridScale;
        float3 center = new float3(
            (gridCoord.x + 0.5f) * tileSize.x * gridScale + offset.x,
            offset.y,
            (gridCoord.y + 0.5f) * tileSize.z * gridScale + offset.z
        );

        chunk.Bounds = new Bounds(center, size);
    }

    public static ChunkData CreateChunkData(int groupSize)
    {
        return new ChunkData
        {
            Bounds = new Bounds(),
            Children = new NativeList<int>(groupSize * groupSize, Allocator.Persistent),
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
            UsedChunks = new NativeArray<int>(capacity, Allocator.Persistent),
            UsedChunksMap = new NativeHashMap<int2, int>(capacity, Allocator.Persistent),
            AddedRemoved = new NativeArray<int>(capacity, Allocator.Persistent),
            AddedRemovedCount = 0,
            UsedChunksCount = 0
        };
    }
}

public struct ChunkLevel
{
    public NativeHashMap<int2, int> UsedChunksMap;
    public NativeArray<int> UsedChunks;
    public int UsedChunksCount;
    public NativeArray<int> AddedRemoved;
    public int AddedRemovedCount;
    public int HierarchyLevel;
    public int GridScale;
    public NativeArray<int2> CircularCoords;
}

public struct ChunkData
{
    public int2 GridCoord;
    public Bounds Bounds;
    public bool IsVisible;
    public int Parent;
    public NativeList<int> Children;
}
