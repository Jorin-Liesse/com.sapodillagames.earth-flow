using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;

public class Chunking : MonoBehaviour
{
    const float TOLERANCE = 0.01f;

    public int2 GridSize = new(10000, 10000);
    public int3 TileSize = new(10, 1, 10);
    public int ViewDistance = 100;
    public int BufferDistance = 1;
    public int GroupSize = 4;

    [HideInInspector] public int HierarchyLevels;

    [HideInInspector] public NativeParallelMultiHashMap<int, int> ChunksChildren;
    [HideInInspector] public NativeArray<int> ChunksParent;
    [HideInInspector] public NativeArray<int2> ChunksGridCoord;
    [HideInInspector] public NativeArray<AABB> ChunksBounds;
    [HideInInspector] public NativeArray<bool> ChunksIsVisible;

    [HideInInspector] public NativeParallelHashMap<int3, int> LevelUsedChunksMap;
    [HideInInspector] public NativeArray<int> LevelUsedChunks;
    [HideInInspector] public NativeArray<int2> LevelCircularCoords;
    [HideInInspector] public NativeArray<int> LevelUsedChunksCount;
    [HideInInspector] public NativeArray<int> LevelCircularCoordsCount;
    [HideInInspector] public NativeArray<int> LevelGridScale;
    [HideInInspector] public NativeArray<int> LevelStartIndices;

    NativeArray<(int chunksIndex, int usedIndex)> _levelRemovedChunks;
    NativeArray<int> _levelRemovedChunksCount;
    NativeArray<int2> _levelAddedChunks;
    NativeArray<int> _levelAddedChunksCount;

    NativeArray<int> _freeChunks;
    int _freeChunksCount;

    NativeArray<float4> _frustumFloat4s;
    Plane[] _frustumPlanes;

    Camera _mainCamera;
    Transform _cameraTransform;
    int _loadingDistance;

    int _viewDistanceSqr;
    int _loadingDistanceSqr;

    int2 _playerChunkCoord;
    int2 _lastPlayerChunkCoord;

    Vector3 _lastCameraPosition;
    Quaternion _lastCameraRotation;

    void Awake()
    {
        _mainCamera = Camera.main;
        _cameraTransform = _mainCamera.transform;
        _loadingDistance = ViewDistance + BufferDistance;
        _lastPlayerChunkCoord = new(int.MinValue, int.MinValue);
        _lastCameraPosition = _cameraTransform.position + Vector3.one * 1000f;
        _lastCameraRotation = _cameraTransform.rotation * Quaternion.Euler(10f, 10f, 10f);

        _viewDistanceSqr = ViewDistance * ViewDistance;
        _loadingDistanceSqr = _loadingDistance * _loadingDistance;

        HierarchyLevels = CalculateHierarchyLevels(_loadingDistance, GroupSize);

        LevelStartIndices = new(HierarchyLevels, Allocator.Persistent);
        LevelGridScale = new(HierarchyLevels, Allocator.Persistent);
        LevelUsedChunksCount = new(HierarchyLevels, Allocator.Persistent);
        LevelCircularCoordsCount = new(HierarchyLevels, Allocator.Persistent);
        _levelRemovedChunksCount = new(HierarchyLevels, Allocator.Persistent);
        _levelAddedChunksCount = new(HierarchyLevels, Allocator.Persistent);

        NativeList<int2> allCircularCoords = new(Allocator.Temp);
        int poolSize = 0;

        for (int level = 0; level < HierarchyLevels; level++)
        {
            int gridScale = (level == 0) ? 1 : (int)math.pow(GroupSize, level);
            NativeArray<int2> circularCoords = CalculateCircularCoords(_loadingDistance, gridScale, Allocator.Persistent);
            int capacity = circularCoords.Length;

            LevelGridScale[level] = gridScale;
            allCircularCoords.AddRange(circularCoords);
            LevelCircularCoordsCount[level] = circularCoords.Length;
            LevelStartIndices[level] = poolSize;

            poolSize += capacity;
            circularCoords.Dispose();
        }

        LevelCircularCoords = new(poolSize, Allocator.Persistent);
        allCircularCoords.AsArray().CopyTo(LevelCircularCoords);
        allCircularCoords.Dispose();

        LevelUsedChunksMap = new(poolSize, Allocator.Persistent);
        LevelUsedChunks = new(poolSize, Allocator.Persistent);
        _levelRemovedChunks = new(poolSize, Allocator.Persistent);
        _levelAddedChunks = new(poolSize, Allocator.Persistent);

        ChunksGridCoord = new(poolSize, Allocator.Persistent);
        ChunksBounds = new(poolSize, Allocator.Persistent);
        ChunksIsVisible = new(poolSize, Allocator.Persistent);
        ChunksParent = new(poolSize, Allocator.Persistent);
        ChunksChildren = new(poolSize * GroupSize * GroupSize, Allocator.Persistent);

        _freeChunks = new(poolSize, Allocator.Persistent);
        _freeChunksCount = poolSize;

        _frustumFloat4s = new(6, Allocator.Persistent);
        _frustumPlanes = new Plane[6];

        for (int i = 0; i < poolSize; i++)
        {
            ChunksGridCoord[i] = new int2();
            ChunksBounds[i] = new AABB();
            ChunksIsVisible[i] = false;
            ChunksParent[i] = -1;
            _freeChunks[i] = i;
        }
    }

    void OnDestroy()
    {
        LevelStartIndices.Dispose();
        LevelGridScale.Dispose();
        LevelCircularCoords.Dispose();
        LevelUsedChunks.Dispose();
        LevelUsedChunksMap.Dispose();
        LevelUsedChunksCount.Dispose();
        LevelCircularCoordsCount.Dispose();

        _levelRemovedChunks.Dispose();
        _levelAddedChunks.Dispose();
        _levelRemovedChunksCount.Dispose();
        _levelAddedChunksCount.Dispose();

        ChunksGridCoord.Dispose();
        ChunksBounds.Dispose();
        ChunksIsVisible.Dispose();
        ChunksParent.Dispose();
        ChunksChildren.Dispose();

        _freeChunks.Dispose();
        _frustumFloat4s.Dispose();
    }

    void Update()
    {
        ChunkingUpdate();
        CullingUpdate();
    }

    void ChunkingUpdate()
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

    void CullingUpdate()
    {
        bool rotationChanged = Quaternion.Angle(_cameraTransform.rotation, _lastCameraRotation) >= TOLERANCE;
        bool positionChanged = Vector3.SqrMagnitude(_cameraTransform.position - _lastCameraPosition) >= TOLERANCE;

        if (!rotationChanged && !positionChanged) return;

        GeometryUtility.CalculateFrustumPlanes(_mainCamera, _frustumPlanes);
        for (int i = 0; i < 6; i++)
        {
            Plane p = _frustumPlanes[i];
            _frustumFloat4s[i] = new float4(p.normal.x, p.normal.y, p.normal.z, p.distance);
        }

        // int rootLevel = HierarchyLevels - 1;
        // for (int i = 0; i < LevelUsedChunksCount[rootLevel]; i++)
        //     Culling(LevelUsedChunks[LevelStartIndices[rootLevel] + i], rootLevel);

        CullingJob job = new()
        {
            ChunksBounds = ChunksBounds,
            ChunksGridCoord = ChunksGridCoord,
            LevelGridScale = LevelGridScale,
            PlayerChunkX = _playerChunkCoord.x,
            PlayerChunkY = _playerChunkCoord.y,
            ViewDistanceSqr = _viewDistanceSqr,
            FrustumPlanes = _frustumFloat4s,
            ChunksIsVisible = ChunksIsVisible
        };

        JobHandle handle = job.Schedule(LevelUsedChunks.Length, 64);
        handle.Complete();

        _lastCameraPosition = _cameraTransform.position;
        _lastCameraRotation = _cameraTransform.rotation;
    }

    void FindChunksToRemove()
    {
        for (int levelIndex = 0; levelIndex < HierarchyLevels; levelIndex++)
            _levelRemovedChunksCount[levelIndex] = 0;

        for (int level = 0; level < HierarchyLevels; level++)
        {
            int gridScale = LevelGridScale[level];
            int startIndex = LevelStartIndices[level];

            int radiusGrid = (int)math.ceil((float)_loadingDistance / gridScale + 1f);
            int radiusGridSqr = radiusGrid * radiusGrid;

            float2 playerGridF = (float2)_playerChunkCoord / gridScale - 0.5f;
            int2 playerGrid = new((int)math.round(playerGridF.x), (int)math.round(playerGridF.y));

            for (int i = LevelUsedChunksCount[level] - 1; i >= 0; i--)
            {
                int chunkIndex = LevelUsedChunks[startIndex + i];
                int2 chunkGridCoord = ChunksGridCoord[chunkIndex];

                int2 delta = chunkGridCoord - playerGrid;
                int distSqr = delta.x * delta.x + delta.y * delta.y;

                if (distSqr < radiusGridSqr) continue;

                _levelRemovedChunks[startIndex + _levelRemovedChunksCount[level]++] = (chunkIndex, i);
            }
        }
    }

    void RemoveChunks()
    {
        for (int level = 0; level < HierarchyLevels; level++)
        {
            int startIndex = LevelStartIndices[level];

            for (int i = 0; i < _levelRemovedChunksCount[level]; i++)
            {
                int chunkIndex = _levelRemovedChunks[startIndex + i].chunksIndex;
                int usedIndex = _levelRemovedChunks[startIndex + i].usedIndex;
                int2 chunkGridCoord = ChunksGridCoord[chunkIndex];

                LevelUsedChunksMap.Remove(new(level, chunkGridCoord.x, chunkGridCoord.y));
                LevelUsedChunks[startIndex + usedIndex] = LevelUsedChunks[startIndex + --LevelUsedChunksCount[level]];
                _freeChunks[_freeChunksCount++] = chunkIndex;
            }
        }
    }

    void CleanRelations()
    {
        for (int level = 0; level < HierarchyLevels; level++)
        {
            int startIndex = LevelStartIndices[level];
            int removedCount = _levelRemovedChunksCount[level];

            for (int i = 0; i < removedCount; i++)
            {
                int chunkIndex = _levelRemovedChunks[startIndex + i].chunksIndex;

                // --- Detach from parent ---
                int parentIndex = ChunksParent[chunkIndex];
                if (parentIndex != -1 && ChunksChildren.TryGetFirstValue(parentIndex, out int child, out NativeParallelMultiHashMapIterator<int> it))
                {
                    do { if (child == chunkIndex) { ChunksChildren.Remove(it); break; } }
                    while (ChunksChildren.TryGetNextValue(out child, ref it));

                    ChunksParent[chunkIndex] = -1;
                }

                // --- Detach children ---
                if (!ChunksChildren.TryGetFirstValue(chunkIndex, out int childIndex, out NativeParallelMultiHashMapIterator<int> cit))
                {
                    ChunksChildren.Remove(chunkIndex);
                    continue;
                }

                do ChunksParent[childIndex] = -1;
                while (ChunksChildren.TryGetNextValue(out childIndex, ref cit));

                ChunksChildren.Remove(chunkIndex);
            }
        }
    }

    void FindChunksToAdd()
    {
        for (int levelIndex = 0; levelIndex < HierarchyLevels; levelIndex++)
            _levelAddedChunksCount[levelIndex] = 0;

        for (int level = 0; level < HierarchyLevels; level++)
        {
            int gridScale = LevelGridScale[level];

            int radiusGrid = (int)math.ceil((float)_loadingDistance / gridScale + 1);
            int radiusGridSqr = radiusGrid * radiusGrid;

            float2 playerGridF = (float2)_playerChunkCoord / gridScale - 0.5f;
            int2 playerGrid = new((int)math.round(playerGridF.x), (int)math.round(playerGridF.y));

            int halfGridSizeX = (GridSize.x / (gridScale * 2)) + 1;
            int halfGridSizeY = (GridSize.y / (gridScale * 2)) + 1;

            int startIndex = LevelStartIndices[level];
            int count = LevelCircularCoordsCount[level];
            for (int i = startIndex; i < startIndex + count; i++)
            {
                int2 coord = LevelCircularCoords[i] + playerGrid;
                int2 delta = coord - playerGrid;
                int distSqr = delta.x * delta.x + delta.y * delta.y;

                if (distSqr >= radiusGridSqr) continue;
                if (coord.x < -halfGridSizeX || coord.x >= halfGridSizeX || coord.y < -halfGridSizeY || coord.y >= halfGridSizeY) continue;
                if (LevelUsedChunksMap.ContainsKey(new(level, coord.x, coord.y))) continue;
                if (_freeChunksCount <= 0) break;

                _levelAddedChunks[startIndex + _levelAddedChunksCount[level]++] = coord;
            }
        }
    }

    void AddChunks()
    {
        Vector3 offset = transform.position;

        for (int level = 0; level < HierarchyLevels; level++)
        {
            int gridScale = LevelGridScale[level];
            int startIndex = LevelStartIndices[level];

            for (int i = 0; i < _levelAddedChunksCount[level]; i++)
            {
                int2 coord = _levelAddedChunks[startIndex + i];
                int index = _freeChunks[--_freeChunksCount];

                LevelUsedChunks[startIndex + LevelUsedChunksCount[level]++] = index;
                LevelUsedChunksMap.Add(new(level, coord.x, coord.y), index);

                float3 size = new(TileSize * gridScale);
                float3 center = new(
                    (coord.x + 0.5f) * TileSize.x * gridScale + offset.x,
                    offset.y,
                    (coord.y + 0.5f) * TileSize.z * gridScale + offset.z
                );

                ChunksIsVisible[index] = false;
                ChunksParent[index] = -1;
                ChunksGridCoord[index] = coord;
                ChunksBounds[index] = new() { Center = center, Extents = size * 0.5f };
            }
        }
    }

    void BuildRelations()
    {
        for (int level = 0; level < HierarchyLevels - 1; level++)
        {
            int startIndex = LevelStartIndices[level];
            for (int i = 0; i < _levelAddedChunksCount[level]; i++)
            {
                int2 coord = _levelAddedChunks[startIndex + i];
                int chunkIndex = LevelUsedChunksMap[new(level, coord.x, coord.y)];
                int2 chunkGridCoord = ChunksGridCoord[chunkIndex];

                int2 parentGridCoord = new(
                    (int)math.floor((float)chunkGridCoord.x / GroupSize),
                    (int)math.floor((float)chunkGridCoord.y / GroupSize)
                );

                if (LevelUsedChunksMap.TryGetValue(new(level + 1, parentGridCoord.x, parentGridCoord.y), out int parentIndex))
                {
                    ChunksParent[chunkIndex] = parentIndex;
                    ChunksChildren.Add(parentIndex, chunkIndex);
                }
            }
        }
    }

    void Culling(int index, int level)
    {
        ChunksIsVisible[index] = !(DistanceCull(index, level) || FrustumCull(index, level) || OcclusionCull(index, level));

        if (!ChunksIsVisible[index]) return;

        if (ChunksChildren.TryGetFirstValue(index, out int childIndex, out NativeParallelMultiHashMapIterator<int> it))
        {
            do Culling(childIndex, level - 1);
            while (ChunksChildren.TryGetNextValue(out childIndex, ref it));
        }
    }

    bool DistanceCull(int index, int level)
    {
        int gridScale = LevelGridScale[level];

        int2 _playerChunkCoordLocal = new(
            _playerChunkCoord.x / gridScale,
            _playerChunkCoord.y / gridScale
        );

        int2 diff = ChunksGridCoord[index] - _playerChunkCoordLocal;
        return diff.x * diff.x + diff.y * diff.y > _viewDistanceSqr;
    }

    bool FrustumCull(int index, int level)
    {
        for (int i = 0; i < 6; i++)
        {
            AABB bounds = ChunksBounds[index];
            float4 plane = _frustumFloat4s[i];
            float3 normal = plane.xyz;
            float3 absNormal = math.abs(normal);
            float r = math.dot(bounds.Extents, absNormal);
            float s = math.dot(bounds.Center, normal) + plane.w;

            if (s + r < 0) return true;

        }

        return false;
    }

    bool OcclusionCull(int index, int level)
    {
        return false;
    }

    NativeArray<int2> CalculateCircularCoords(int radius, int scale, Allocator allocator)
    {
        int scaledRadius = (int)math.ceil((float)radius / scale + 1f);
        int radiusSqr = scaledRadius * scaledRadius;

        NativeList<int2> coords = new(radiusSqr * 4, Allocator.Temp);

        for (int x = -scaledRadius; x <= scaledRadius; x++)
        {
            for (int y = -scaledRadius; y <= scaledRadius; y++)
            {
                float distSqr = (x + 0.5f) * (x + 0.5f) + (y + 0.5f) * (y + 0.5f);
                if (distSqr <= radiusSqr) coords.Add(new(x, y));
            }
        }

        NativeArray<int2> result = new(coords.Length, allocator);
        coords.AsArray().CopyTo(result);
        coords.Dispose();

        return result;
    }

    int CalculateHierarchyLevels(int loadingRadius, int groupSize)
    {
        return (int)math.ceil(math.log(loadingRadius) / math.log(groupSize));
    }
}

public struct AABB
{
    public float3 Center;
    public float3 Extents;
}

[BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
struct CullingJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<AABB> ChunksBounds;
    [ReadOnly] public NativeArray<int2> ChunksGridCoord;
    [ReadOnly] public NativeArray<int> LevelGridScale;
    [ReadOnly] public int PlayerChunkX;
    [ReadOnly] public int PlayerChunkY;
    [ReadOnly] public int ViewDistanceSqr;
    [ReadOnly] public NativeArray<float4> FrustumPlanes;

    [WriteOnly] public NativeArray<bool> ChunksIsVisible;

    public void Execute(int index)
    {
        // Distance culling
        int2 diff = ChunksGridCoord[index] - new int2(PlayerChunkX, PlayerChunkY);
        if (diff.x * diff.x + diff.y * diff.y > ViewDistanceSqr)
        {
            ChunksIsVisible[index] = false;
            return;
        }

        // Frustum culling
        AABB bounds = ChunksBounds[index];
        for (int i = 0; i < 6; i++)
        {
            float4 plane = FrustumPlanes[i];
            float3 normal = plane.xyz;
            float3 absNormal = math.abs(normal);
            float r = math.dot(bounds.Extents, absNormal);
            float s = math.dot(bounds.Center, normal) + plane.w;
            if (s + r < 0)
            {
                ChunksIsVisible[index] = false;
                return;
            }
        }

        ChunksIsVisible[index] = true;
    }
}
