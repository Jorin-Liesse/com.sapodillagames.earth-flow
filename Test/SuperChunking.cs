using UnityEngine;
using System.Collections.Generic;

public class SuperChunking : MonoBehaviour, IChunker
{
    ChunkLevel[] _chunkLevels;
    ChunkData[] _chunks;

    int[] _freeChunks;
    int _freeChunksCount;
    
    Vector2Int _playerChunkCoord;

    public int HierarchyLevels => ChunkerHelper.HierarchyLevels;
    public IReadOnlyList<int> GetUsedIndices(int level) => _chunkLevels[level].UsedChunks;
    public ChunkData GetChunk(int index) => _chunks[index];

    void Awake()
    {
        ChunkerHelper.Initialize();

        _chunkLevels = new ChunkLevel[HierarchyLevels];
        for (int level = 0; level < HierarchyLevels; level++)
        {
            int capacity = ChunkerHelper.PoolSize;
            _chunkLevels[level] = ChunkerHelper.CreateChunkLevel(capacity, level);
        }

        _chunks = new ChunkData[ChunkerHelper.PoolSize];
        _freeChunks = new int[ChunkerHelper.PoolSize];
        _freeChunksCount = ChunkerHelper.PoolSize;

        for (int i = 0; i < ChunkerHelper.PoolSize; i++)
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
        float radiusSqr = ChunkerHelper.LoadingRadiusSqr;
        Vector2Int playerPos = new((int)(_playerChunkCoord.x * ChunkerHelper.TileSize.x), (int)(_playerChunkCoord.y * ChunkerHelper.TileSize.z));

        for (int level = 0; level < _chunkLevels.Length; level++)
        {
            ref ChunkLevel chunkLevel = ref _chunkLevels[level];
            int[] used = chunkLevel.UsedChunks;
            Dictionary<Vector2Int, int> map = chunkLevel.UsedChunksMap;

            for (int i = chunkLevel.UsedChunksCount - 1; i >= 0; i--)
            {
                int index = used[i];
                ChunkData chunk = _chunks[index];

                Vector2Int offset2D = new((int)(chunk.Bounds.center.x - playerPos.x), (int)(chunk.Bounds.center.z - playerPos.y));
                if (offset2D.x * offset2D.x + offset2D.y * offset2D.y > radiusSqr)
                {
                    chunkLevel.UsedChunksMap.Remove(chunk.GridCoord);

                    int last = --chunkLevel.UsedChunksCount;
                    chunkLevel.UsedChunks[i] = chunkLevel.UsedChunks[last];

                    chunk.Children.Clear();
                    chunk.Parent = -1;

                    _freeChunks[_freeChunksCount++] = index;
                }
            }
        }
    }

    void ActivateNewChunks()
    {
        for (int levelIndex = 0; levelIndex < _chunkLevels.Length; levelIndex++)
            _chunkLevels[levelIndex].AddedRemovedCount = 0;

        ref ChunkLevel chunkLevel = ref _chunkLevels[0];

        int radiusSqr = ChunkerHelper.LoadingRadiusSqr;
        float halfX = ChunkerHelper.GridSize.x * 0.5f;
        float halfY = ChunkerHelper.GridSize.y * 0.5f;

        foreach (var offset in ChunkerHelper.CircularCoords)
        {
            Vector2Int coord = offset + _playerChunkCoord;

            if (coord.x < -halfX || coord.x >= halfX || coord.y < -halfY || coord.y >= halfY) continue;

            int dx = coord.x - _playerChunkCoord.x;
            int dy = coord.y - _playerChunkCoord.y;
            if (dx * dx + dy * dy > radiusSqr) continue;

            if (chunkLevel.UsedChunksMap.ContainsKey(coord)) continue;
            if (_freeChunksCount == 0) break;

            int index = _freeChunks[--_freeChunksCount];
            chunkLevel.UsedChunks[chunkLevel.UsedChunksCount++] = index;
            chunkLevel.UsedChunksMap.Add(coord, index);
            chunkLevel.AddedRemoved[chunkLevel.AddedRemovedCount++] = index;

            ChunkerHelper.SetChunkData(ref _chunks[index], transform.position, coord, 0, chunkLevel.GridScale);
        }

        RepairHierarchyChunks();
    }

    void RepairHierarchyChunks()
    {
        int groupSize = ChunkerHelper.GroupSize;
        float invGroupSize = 1f / groupSize;

        for (int levelIndex = 0; levelIndex < HierarchyLevels - 1; levelIndex++)
        {
            ref ChunkLevel level = ref _chunkLevels[levelIndex];
            ref ChunkLevel parentLevel = ref _chunkLevels[levelIndex + 1];

            for (int i = 0; i < level.AddedRemovedCount; i++)
            {
                int childIndex = level.AddedRemoved[i];
                ChunkData child = _chunks[childIndex];

                Vector2Int parentCoord = new(
                    Mathf.FloorToInt(child.GridCoord.x * invGroupSize),
                    Mathf.FloorToInt(child.GridCoord.y * invGroupSize)
                );

                if (parentLevel.UsedChunksMap.TryGetValue(parentCoord, out int parentIndex))
                {
                    child.Parent = parentIndex;
                    _chunks[parentIndex].Children.Add(childIndex);
                }
                else
                {
                    if (_freeChunksCount == 0) continue;

                    parentIndex = _freeChunks[--_freeChunksCount];

                    parentLevel.UsedChunks[parentLevel.UsedChunksCount++] = parentIndex;
                    parentLevel.UsedChunksMap.Add(parentCoord, parentIndex);
                    parentLevel.AddedRemoved[parentLevel.AddedRemovedCount++] = parentIndex;

                    ChunkerHelper.SetChunkData(ref _chunks[parentIndex], transform.position, parentCoord, levelIndex + 1, parentLevel.GridScale);

                    child.Parent = parentIndex;
                    _chunks[parentIndex].Children.Add(childIndex);
                }
            }
        }
    }
}
