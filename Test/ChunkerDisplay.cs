using UnityEngine;
using Unity.Mathematics;

[RequireComponent(typeof(EarthFlow))]
public class ChunkerDisplay : MonoBehaviour
{
    [SerializeField] bool _showVisibleChunks = false;
    [SerializeField] bool _showUsedChunks = false;
    [SerializeField] bool _showCircularCoords = false;
    [SerializeField] bool _showWorldBounds = false;

    EarthFlow _chunker;
    ChunkerCore _chunkerCore;

    void Start()
    {
        _chunker = GetComponent<EarthFlow>();
        _chunkerCore = _chunker.ChunkerCore;
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        if (_showVisibleChunks) DrawVisibleChunks();
        if (_showUsedChunks) DrawUsedChunks();
        if (_showWorldBounds) DrawWorldBounds();
        if (_showCircularCoords) DrawCircularCoords();
    }

    void DrawVisibleChunks()
    {
        for (int i = 0; i < _chunkerCore.UsedChunks.Length; i++)
        {
            int chunkIndex = _chunkerCore.UsedChunks[i];
            Gizmos.color = _chunkerCore.ChunksIsVisible[chunkIndex] ? Color.green : Color.blue;
            AABB chunkBounds = _chunkerCore.ChunksBounds[chunkIndex];
            Gizmos.DrawWireCube(chunkBounds.Center, chunkBounds.Extents * 2);
        }
    }

    void DrawUsedChunks()
    {
        Gizmos.color = Color.cyan;

        for (int i = 0; i < _chunkerCore.UsedChunks.Length; i++)
        {
            int chunkIndex = _chunkerCore.UsedChunks[i];

            AABB chunkBounds = _chunkerCore.ChunksBounds[chunkIndex];
            Gizmos.DrawWireCube(chunkBounds.Center, chunkBounds.Extents * 2);
        }
    }

    void DrawWorldBounds()
    {
        Gizmos.color = Color.darkRed;

        Gizmos.DrawWireCube(transform.position, new Vector3(
            _chunkerCore.GridSize.x * _chunkerCore.TileSize.x,
            1,
            _chunkerCore.GridSize.y * _chunkerCore.TileSize.z
        ));
    }

    void DrawCircularCoords()
    {
        Gizmos.color = Color.yellow;
        for (int i = 0; i < _chunkerCore.CircularCoords.Length; i++)
        {
            int2 coord = _chunkerCore.CircularCoords[i];
            Vector3 pos = new(
                coord.x * _chunkerCore.TileSize.x + transform.position.x,
                transform.position.y,
                coord.y * _chunkerCore.TileSize.z + transform.position.z
            );

            Vector3 size = new(_chunkerCore.TileSize.x, 1, _chunkerCore.TileSize.z);

            Gizmos.DrawWireCube(pos, size);
        }
    }
}
