using UnityEngine;
using NaughtyAttributes;

[RequireComponent(typeof(IChunker))]
public class ChunkerDisplay : MonoBehaviour
{
    [SerializeField, MinMaxSlider(0, 10)] Vector2Int _displayHierarchyLevels = new Vector2Int(0, 10);
    [SerializeField] bool _showWorldBounds = false;
    [SerializeField] bool _showCircularCoords = false;
    [SerializeField] bool _showUsedChunks = false;

    [HorizontalLine]

    [SerializeField, ReadOnly] Vector2Int _gridSize = new Vector2Int(100, 100);
    [SerializeField, ReadOnly] Vector3 _tileSize = new(10, 1, 10);
    [SerializeField, ReadOnly] int _viewDistance = 10;
    [SerializeField, ReadOnly] int _bufferDistance = 1;
    [SerializeField, ReadOnly] int _groupSize = 4;

    [SerializeField, ReadOnly] int _loadingRadius;
    [SerializeField, ReadOnly] int _loadingRadiusSqr;
    [SerializeField, ReadOnly] int _viewDistanceSqr;
    [SerializeField, ReadOnly] Vector2Int[] _circularCoords;
    [SerializeField, ReadOnly] int _hierarchyLevels;
    [SerializeField, ReadOnly] int _poolSize;

    IChunker _chunker;

    void Start()
    {
        _chunker = GetComponent<IChunker>();
        ChunkerHelper.Initialize();

        _gridSize = ChunkerHelper.GridSize;
        _tileSize = ChunkerHelper.TileSize;
        _viewDistance = ChunkerHelper.ViewDistance;
        _bufferDistance = ChunkerHelper.BufferDistance;
        _groupSize = ChunkerHelper.GroupSize;

        _loadingRadius = ChunkerHelper.LoadingRadius;
        _loadingRadiusSqr = ChunkerHelper.LoadingRadiusSqr;
        _viewDistanceSqr = ChunkerHelper.ViewDistanceSqr;
        _circularCoords = ChunkerHelper.CircularCoords;
        _hierarchyLevels = ChunkerHelper.HierarchyLevels;
        _poolSize = ChunkerHelper.PoolSize;
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        if (_showUsedChunks) DrawUsedChunks();
        if (_showWorldBounds) DrawWorldBounds();
        if (_showCircularCoords) DrawCircularCoords();
    }

    void DrawUsedChunks()
    {
        for (int level = _displayHierarchyLevels.x; level <= _displayHierarchyLevels.y; level++)
        {
            if (level >= _chunker.HierarchyLevels) break;

            Gizmos.color = Color.Lerp(Color.red, Color.green, (float)level / _chunker.HierarchyLevels);

            var used = _chunker.GetUsedIndices(level);
            for (int i = 0; i < used.Count; i++)
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
            ChunkerHelper.GridSize.x * ChunkerHelper.TileSize.x,
            1,
            ChunkerHelper.GridSize.y * ChunkerHelper.TileSize.z
        ));
    }

    void DrawCircularCoords()
    {
        Gizmos.color = Color.cyan;

        foreach (var coord in ChunkerHelper.CircularCoords)
        {
            Vector3 center = new Vector3(
                (coord.x + 0.5f) * ChunkerHelper.TileSize.x + transform.position.x,
                transform.position.y,
                (coord.y + 0.5f) * ChunkerHelper.TileSize.z + transform.position.z
            );

            Gizmos.DrawWireCube(center, ChunkerHelper.TileSize);
        }
    }
}
