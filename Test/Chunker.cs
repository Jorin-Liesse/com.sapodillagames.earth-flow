#region Using Statements
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
#endregion

public class Chunker
{
    const float TOLERANCE = 0.01f;
    const int BATCH_COUNT = 64;

    #region Private Fields
    int2 _gridSize;
    int3 _tileSize;
    int _viewDistance;
    int _bufferDistance;
    Vector3 _position;

    NativeArray<int2> _chunksGridCoord;
    NativeArray<AABB> _chunksBounds;
    NativeArray<bool> _chunksIsVisible;

    NativeArray<bool> _usedChunkVisibility;
    NativeArray<int2> _circularCoords;
    NativeList<int> _usedChunks;
    NativeArray<byte> _chunkStamp;
    NativeList<int> _removeIndices;
    NativeList<int2> _addIndices;
    NativeList<int> _freeChunks;

    Camera _mainCamera;
    Transform _cameraTransform;
    float3 _extents;

    int2 _playerChunkCoord;
    int2 _lastPlayerChunkCoord;

    Vector3 _lastCameraPosition;
    Quaternion _lastCameraRotation;
    
    int _viewDistanceSqr;
    int _loadingDistance;
    int _loadingDistanceSqr;
    NativeArray<float4> _frustumFloat4s;
    Plane[] _frustumPlanes;
    #endregion

    #region Public Properties
    public int2 GridSize => _gridSize;
    public int3 TileSize => _tileSize;
    public int ViewDistance => _viewDistance;

    public NativeArray<AABB> ChunksBounds => _chunksBounds;
    public NativeArray<bool> ChunksIsVisible => _chunksIsVisible;
    public NativeArray<int> UsedChunks => _usedChunks.AsArray();
    public NativeArray<int2> CircularCoords => _circularCoords;
    #endregion

    #region Public Methods
    public void Initialize(int2 gridSize, int3 tileSize, int viewDistance, int bufferDistance, Vector3 position)
    {
        _gridSize = gridSize;
        _tileSize = tileSize;
        _viewDistance = viewDistance;
        _bufferDistance = bufferDistance;
        _position = position;

        _mainCamera = Camera.main;
        _cameraTransform = _mainCamera.transform;
        _viewDistanceSqr = _viewDistance * _viewDistance;
        _loadingDistance = _viewDistance + _bufferDistance;
        _loadingDistanceSqr = _loadingDistance * _loadingDistance;
        _lastPlayerChunkCoord = new(int.MinValue, int.MinValue);
        _extents = new(_tileSize.x * 0.5f, _tileSize.y * 0.5f, _tileSize.z * 0.5f);

        _circularCoords = CalculateCircularCoords(_loadingDistance);

        int totalGridSize = _gridSize.x * _gridSize.y;
        _chunkStamp = new(totalGridSize, Allocator.Persistent);

        _usedChunkVisibility = new(_circularCoords.Length, Allocator.Persistent);
        _usedChunks = new(_circularCoords.Length, Allocator.Persistent);
        _removeIndices = new(_circularCoords.Length, Allocator.Persistent);
        _addIndices = new(_circularCoords.Length, Allocator.Persistent);
        _freeChunks = new(_circularCoords.Length, Allocator.Persistent);

        _chunksGridCoord = new(_circularCoords.Length, Allocator.Persistent);
        _chunksBounds = new(_circularCoords.Length, Allocator.Persistent);
        _chunksIsVisible = new(_circularCoords.Length, Allocator.Persistent);

        _frustumFloat4s = new(6, Allocator.Persistent);
        _frustumPlanes = new Plane[6];

        for (int i = 0; i < _circularCoords.Length; i++)
        {
            _chunksGridCoord[i] = new();
            _chunksBounds[i] = new();
            _chunksIsVisible[i] = false;
            _freeChunks.Add(i);
        }
    }

    public void Cleanup()
    {
        _usedChunkVisibility.Dispose();
        _circularCoords.Dispose();
        _usedChunks.Dispose();
        _chunkStamp.Dispose();

        _removeIndices.Dispose();
        _addIndices.Dispose();

        _chunksGridCoord.Dispose();
        _chunksBounds.Dispose();
        _chunksIsVisible.Dispose();

        _freeChunks.Dispose();
        _frustumFloat4s.Dispose();
    }

    public void Execute()
    {
        ChunkingUpdate();
        CullingUpdate();
    }
    #endregion

    #region Private Methods
    void ChunkingUpdate()
    {
        _playerChunkCoord.x = (int)math.floor((_cameraTransform.position.x - _position.x) / _tileSize.x);
        _playerChunkCoord.y = (int)math.floor((_cameraTransform.position.z - _position.z) / _tileSize.z);

        if (math.all(_playerChunkCoord == _lastPlayerChunkCoord)) return;

        int2 min = new(-(int)(_gridSize.x * 0.5f), -(int)(_gridSize.y * 0.5f));
        int2 max = new((int)(_gridSize.x * 0.5f), (int)(_gridSize.y * 0.5f));

        FindChunksToRemoveJob findChunksToRemoveJob = new()
        {
            UsedChunks = _usedChunks.AsArray(),
            ChunksGridCoord = _chunksGridCoord,
            PlayerGrid = _playerChunkCoord,
            RadiusSqr = _loadingDistanceSqr,
            RemoveIndices = _removeIndices.AsParallelWriter()
        };

        FindChunksToAddJob findChunksToAddJob = new()
        {
            CircularCoords = _circularCoords,
            ChunkStamp = _chunkStamp,
            PlayerGrid = _playerChunkCoord,
            RadiusSqr = _loadingDistanceSqr,
            Min = min,
            Max = max,
            GridWidth = _gridSize.x,
            AddCoords = _addIndices.AsParallelWriter()
        };

        _removeIndices.Clear();
        findChunksToRemoveJob.Schedule(_usedChunks.Length, BATCH_COUNT).Complete();
        RemoveChunks();

        _addIndices.Clear();
        findChunksToAddJob.Schedule(_circularCoords.Length, BATCH_COUNT).Complete();
        AddChunks();

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

        CullingJob job = new()
        {
            UsedChunks = _usedChunks.AsArray(),
            ChunksBounds = _chunksBounds,
            FrustumPlanes = _frustumFloat4s,

            ChunksGridCoord = _chunksGridCoord,
            PlayerGrid = _playerChunkCoord,
            ViewDistanceSqr = _viewDistanceSqr,

            UsedVisibility = _usedChunkVisibility
        };

        job.Schedule(_usedChunks.Length, BATCH_COUNT).Complete();

        for (int i = 0; i < _usedChunks.Length; i++)
        {
            int chunkIndex = _usedChunks[i];
            _chunksIsVisible[chunkIndex] = _usedChunkVisibility[i];
        }

        _lastCameraPosition = _cameraTransform.position;
        _lastCameraRotation = _cameraTransform.rotation;
    }

    int ToIndex(int2 coord)
    {
        int x = coord.x + _gridSize.x / 2;
        int y = coord.y + _gridSize.y / 2;
        return x + y * _gridSize.x;
    }

    void RemoveChunks()
    {
        _removeIndices.Sort();

        for (int i = _removeIndices.Length - 1; i >= 0; i--)
        {
            int usedIndex = _removeIndices[i];
            int chunkIndex = _usedChunks[usedIndex];

            _chunkStamp[ToIndex(_chunksGridCoord[chunkIndex])] = 0;
            _usedChunks.RemoveAtSwapBack(usedIndex);
            _freeChunks.Add(chunkIndex);
        }
    }

    void AddChunks()
    {
        float tileSizeX = _tileSize.x;
        float tileSizeZ = _tileSize.z;

        for (int i = 0; i < _addIndices.Length; i++)
        {
            int2 coord = _addIndices[i];
            int index = _freeChunks[^1];
            _freeChunks.RemoveAtSwapBack(_freeChunks.Length - 1);

            _usedChunks.Add(index);
            _chunkStamp[ToIndex(coord)] = 1;

            float3 center = new(
                (coord.x + 0.5f) * tileSizeX + _position.x,
                _position.y,
                (coord.y + 0.5f) * tileSizeZ + _position.z
            );

            _chunksIsVisible[index] = false;
            _chunksGridCoord[index] = coord;
            _chunksBounds[index] = new()
            {
                Center = center,
                Extents = _extents
            };
        }
    }

    NativeArray<int2> CalculateCircularCoords(int radius)
    {
        int scaledRadius = (int)math.ceil(radius);
        int radiusSqr = scaledRadius * scaledRadius;

        int estimatedCapacity = (int)(radiusSqr * math.PI);
        NativeList<int2> coords = new(estimatedCapacity, Allocator.Temp);

        for (int x = -scaledRadius; x <= scaledRadius; x++)
        {
            for (int y = -scaledRadius; y <= scaledRadius; y++)
            {
                float distSqr = (x + 0.5f) * (x + 0.5f) + (y + 0.5f) * (y + 0.5f);
                if (distSqr <= radiusSqr) coords.Add(new int2(x, y));
            }
        }

        NativeArray<int2> result = new(coords.Length, Allocator.Persistent);
        coords.AsArray().CopyTo(result);
        coords.Dispose();

        return result;
    }
    #endregion
}
