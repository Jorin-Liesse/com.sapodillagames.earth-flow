using UnityEngine;
using System.Collections.Generic;

public class SingleChunking : MonoBehaviour, IChunker
{
    int _poolSize;
    int _totalChunks;
    Vector2Int _playerChunkCoord;

    ChunkLevel[] _chunkLevels;
    ChunkData[] _chunks;
    
    int[] _freeChunks;
    int _freeChunksCount;

    public int HierarchyLevels => 1;
    public IReadOnlyList<int> GetUsedIndices(int level) => _chunkLevels[level].UsedChunks;
    public ChunkData GetChunk(int index) => _chunks[index];

    void Awake()
    {
        ChunkerHelper.Initialize();

        _totalChunks = ChunkerHelper.GridSize.x * ChunkerHelper.GridSize.y;
        _poolSize = Mathf.RoundToInt(Mathf.PI * ChunkerHelper.LoadingRadiusSqr);
        if (_poolSize > _totalChunks) _poolSize = _totalChunks;

        _chunkLevels = new ChunkLevel[HierarchyLevels];
        for (int level = 0; level < HierarchyLevels; level++)
        {
            int capacity = ChunkerHelper.CircularCoords.Length * ((HierarchyLevels - level) / HierarchyLevels);
            _chunkLevels[level] = ChunkerHelper.CreateChunkLevel(capacity, level);
        }

        _chunks = new ChunkData[_poolSize];
        _freeChunks = new int[_poolSize];
        _freeChunksCount = _poolSize;

        for (int i = 0; i < _poolSize; i++)
        {
            _chunks[i] = ChunkerHelper.CreateChunkData();
            _freeChunks[i] = i;
        }
    }

    void Update()
    {
        _playerChunkCoord.x = Mathf.FloorToInt((ChunkerHelper.CameraTransform.position.x - transform.position.x) / ChunkerHelper.TileSize.x);
        _playerChunkCoord.y = Mathf.FloorToInt((ChunkerHelper.CameraTransform.position.z - transform.position.z) / ChunkerHelper.TileSize.z);

        FreeInactiveChunks();
        ActivateNewChunks();
    }

    void FreeInactiveChunks()
    {
        ref ChunkLevel chunkLevel = ref _chunkLevels[0];
        int[] used = chunkLevel.UsedChunks;
        Dictionary<Vector2Int, int> map = chunkLevel.UsedChunksMap;

        for (int i = used.Length - 1; i >= 0; i--)
        {
            int index = used[i];
            ChunkData chunk = _chunks[index];

            Vector2Int diff = chunk.GridCoord - _playerChunkCoord;
            if (diff.x * diff.x + diff.y * diff.y > ChunkerHelper.LoadingRadiusSqr)
            {
                chunkLevel.UsedChunksMap.Remove(chunk.GridCoord);
                int last = --chunkLevel.UsedChunksCount;
                chunkLevel.UsedChunks[i] = chunkLevel.UsedChunks[last];
                _freeChunks[_freeChunksCount++] = index;
            }
        }
    }

    void ActivateNewChunks()
    {
        ref ChunkLevel chunkLevel = ref _chunkLevels[0];

        float halfX = ChunkerHelper.GridSize.x * 0.5f;
        float halfY = ChunkerHelper.GridSize.y * 0.5f;

        for (int i = 0; i < ChunkerHelper.CircularCoords.Length; i++)
        {
            Vector2Int coord = ChunkerHelper.CircularCoords[i] + _playerChunkCoord;

            if (coord.x < -halfX || coord.x >= halfX || coord.y < -halfY || coord.y >= halfY) continue;

            Vector2Int diff = coord - _playerChunkCoord;
            if (diff.x * diff.x + diff.y * diff.y > ChunkerHelper.LoadingRadiusSqr) continue;
            if (chunkLevel.UsedChunksMap.ContainsKey(coord)) continue;
            if (_freeChunksCount <= 0) break;

            int index = _freeChunks[--_freeChunksCount];
            chunkLevel.UsedChunks[chunkLevel.UsedChunksCount++] = index;
            chunkLevel.UsedChunksMap.Add(coord, index);

            ChunkerHelper.SetChunkData(ref _chunks[index], transform.position, coord, 0, _chunkLevels[0].GridScale);
        }
    }
}
