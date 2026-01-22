using UnityEngine;
using NaughtyAttributes;
using Unity.Mathematics;
using Unity.Collections;

[RequireComponent(typeof(Chunking))]
public class ChunkerDisplay : MonoBehaviour
{
    [SerializeField, MinMaxSlider(0, 10)] Vector2Int _displayHierarchyLevels = new Vector2Int(0, 10);
    
    [SerializeField] bool _showHierarchyLevelChunks = false;
    [SerializeField] bool _showUsedChunks = false;
    [SerializeField] bool _showCircularCoords = false;
    [SerializeField] bool _showWorldBounds = false;

    Chunking _chunker;

    void Start()
    {
        _chunker = GetComponent<Chunking>();
    }

    // void OnDrawGizmosSelected()
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        if (_showHierarchyLevelChunks) DrawHierarchyLevel();
        if (_showUsedChunks) DrawUsedChunks();
        if (_showWorldBounds) DrawWorldBounds();
        if (_showCircularCoords) DrawCircularCoords();
    }

    void DrawHierarchyLevel()
    {
        void Draw(int index)
        {
            ChunkData chunk = _chunker.GetChunk(index);
            Gizmos.DrawWireCube(chunk.Bounds.center, chunk.Bounds.size);

            for (int i = 0; i < chunk.Children.Length; i++)
                Draw(chunk.Children[i]);
        }

        ChunkLevel root = _chunker.GetChunkLevel(_chunker.GetHierarchyLevels() - 1);
        for (int i = 0; i < root.UsedChunksCount; i++)
        {
            UnityEngine.Random.InitState(i);
            Gizmos.color = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);

            Draw(root.UsedChunks[i]);
        }
    }

    void DrawUsedChunks()
    {
        for (int level = _displayHierarchyLevels.x; level <= _displayHierarchyLevels.y; level++)
        {
            if (level >= _chunker.GetHierarchyLevels()) break;

            Gizmos.color = Color.Lerp(Color.red, Color.green, (float)level / _chunker.GetHierarchyLevels());

            ChunkLevel chunkLevel = _chunker.GetChunkLevel(level);
            NativeArray<int> used = chunkLevel.UsedChunks;

            for (int i = 0; i < chunkLevel.UsedChunksCount; i++)
            {
                ChunkData chunk = _chunker.GetChunk(used[i]);
                Gizmos.DrawWireCube(chunk.Bounds.center, chunk.Bounds.size);
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
            if (level >= _chunker.GetHierarchyLevels()) break;

            Gizmos.color = Color.Lerp(Color.gray, Color.white, (float)level / _chunker.GetHierarchyLevels());

            ChunkLevel chunkLevel = _chunker.GetChunkLevel(level);
            float scale = chunkLevel.GridScale;

            Vector3 offset = new(
                transform.position.x + (0.5f * scale * _chunker.TileSize.x),
                transform.position.y + (0.5f * scale * _chunker.TileSize.y),
                transform.position.z + (0.5f * scale * _chunker.TileSize.z)
            );

            for (int i = 0; i < chunkLevel.CircularCoords.Length; i++)
            {
                int2 coord = chunkLevel.CircularCoords[i];

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
