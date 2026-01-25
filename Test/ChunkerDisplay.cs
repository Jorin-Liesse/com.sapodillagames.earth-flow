using UnityEngine;
using NaughtyAttributes;
using Unity.Mathematics;

[RequireComponent(typeof(Chunking))]
public class ChunkerDisplay : MonoBehaviour
{
    [SerializeField, MinMaxSlider(0, 10)] Vector2Int _displayHierarchyLevels = new Vector2Int(0, 10);

    [SerializeField] bool _showVisibleChunks = false;
    [SerializeField] bool _showHierarchyLevelChunks = false;
    [SerializeField] bool _showUsedChunks = false;
    [SerializeField] bool _showCircularCoords = false;
    [SerializeField] bool _showWorldBounds = false;

    Chunking _chunker;

    void Start()
    {
        _chunker = GetComponent<Chunking>();
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        if (_showVisibleChunks) DrawVisibleChunks();
        if (_showHierarchyLevelChunks) DrawHierarchyLevel();
        if (_showUsedChunks) DrawUsedChunks();
        if (_showWorldBounds) DrawWorldBounds();
        if (_showCircularCoords) DrawCircularCoords();
    }

    void DrawVisibleChunks()
    {
        void Draw(int index, int level)
        {
            if (level < _displayHierarchyLevels.x) return;

            bool chunkIsVisible = _chunker.ChunksIsVisible[index];
            AABB chunkBounds = _chunker.ChunksBounds[index];

            if (level <= _displayHierarchyLevels.y)
            {
                Gizmos.color = chunkIsVisible ? Color.green : Color.blue;
                Gizmos.DrawWireCube(chunkBounds.Center, chunkBounds.Extents * 2);
            }

            if (!chunkIsVisible) return;

            if (_chunker.ChunksChildren.TryGetFirstValue(index, out int child, out var it))
            {
                do Draw(child, level - 1);
                while (_chunker.ChunksChildren.TryGetNextValue(out child, ref it));
            }
        }

        int rootLevel = _chunker.HierarchyLevels - 1;
        int startIndex = _chunker.LevelStartIndices[rootLevel];
        int count = _chunker.LevelUsedChunksCount[rootLevel];

        for (int i = startIndex; i < startIndex + count; i++)
            Draw(_chunker.LevelUsedChunks[i], rootLevel);
    }

    void DrawHierarchyLevel()
    {
        void Draw(int index, int level)
        {
            if (level < _displayHierarchyLevels.x) return;

            AABB chunkBounds = _chunker.ChunksBounds[index];

            if (level <= _displayHierarchyLevels.y)
                Gizmos.DrawWireCube(chunkBounds.Center, chunkBounds.Extents * 2);

            if (_chunker.ChunksChildren.TryGetFirstValue(index, out int child, out var it))
            {
                do Draw(child, level - 1);
                while (_chunker.ChunksChildren.TryGetNextValue(out child, ref it));
            }
        }

        int rootLevel = _chunker.HierarchyLevels - 1;
        int startIndex = _chunker.LevelStartIndices[rootLevel];
        int count = _chunker.LevelUsedChunksCount[rootLevel];

        for (int i = startIndex; i < startIndex + count; i++)
        {
            UnityEngine.Random.InitState(i);
            Gizmos.color = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);

            Draw(_chunker.LevelUsedChunks[i], rootLevel);
        }
    }

    void DrawUsedChunks()
    {
        for (int level = _displayHierarchyLevels.x; level <= _displayHierarchyLevels.y; level++)
        {
            if (level >= _chunker.HierarchyLevels) break;

            Gizmos.color = Color.Lerp(Color.red, Color.green, (float)level / _chunker.HierarchyLevels);

            int startIndex = _chunker.LevelStartIndices[level];
            int count = _chunker.LevelUsedChunksCount[level];
            for (int i = startIndex; i < startIndex + count; i++)
            {
                int chunkIndex = _chunker.LevelUsedChunks[i];
                AABB chunkBounds = _chunker.ChunksBounds[chunkIndex];
                Gizmos.DrawWireCube(chunkBounds.Center, chunkBounds.Extents * 2);
            }
        }
    }

    void DrawWorldBounds()
    {
        Gizmos.color = Color.darkRed;

        Gizmos.DrawWireCube(transform.position, new Vector3(
            _chunker.GridSize.x * _chunker.TileSize.x,
            1,
            _chunker.GridSize.y * _chunker.TileSize.z
        ));
    }

    void DrawCircularCoords()
    {
        for (int level = _displayHierarchyLevels.x; level <= _displayHierarchyLevels.y; level++)
        {
            if (level >= _chunker.HierarchyLevels) break;

            Gizmos.color = Color.Lerp(Color.gray, Color.white, (float)level / _chunker.HierarchyLevels);
            float scale = _chunker.LevelGridScale[level];

            Vector3 offset = new(
                transform.position.x + (0.5f * scale * _chunker.TileSize.x),
                transform.position.y + (0.5f * scale * _chunker.TileSize.y),
                transform.position.z + (0.5f * scale * _chunker.TileSize.z)
            );

            int startIndex = _chunker.LevelStartIndices[level];
            int count = _chunker.LevelCircularCoordsCount[level];
            for (int i = startIndex; i < startIndex + count; i++)
            {
                int2 coord = _chunker.LevelCircularCoords[i];

                Vector3 center = new(
                    (coord.x * scale * _chunker.TileSize.x) + offset.x,
                    offset.y,
                    (coord.y * scale * _chunker.TileSize.z) + offset.z
                );

                Vector3 size = new(
                    _chunker.TileSize.x * scale,
                    _chunker.TileSize.y * scale,
                    _chunker.TileSize.z * scale
                );

                Gizmos.DrawWireCube(center, size);
            }
        }
    }
}
