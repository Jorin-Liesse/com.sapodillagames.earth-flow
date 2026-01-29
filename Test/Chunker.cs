#region Using Statements
using UnityEngine;
using Unity.Mathematics;
#endregion

public class Chunker : MonoBehaviour
{
    #region Serialize Fields
    [SerializeField] int2 _gridSize = new(10000, 10000);
    [SerializeField] int3 _tileSize = new(10, 1, 10);
    [SerializeField] int _viewDistance = 100;
    #endregion

    #region Private Fields
    ChunkerCore _chunkingCore;
    #endregion

    #region Public Properties
    public ChunkerCore ChunkerCore => _chunkingCore;
    #endregion

    #region Unity Methods
    void Awake()
    {
        _chunkingCore = new ChunkerCore();
        _chunkingCore.Initialize(_gridSize, _tileSize, _viewDistance, transform.position);
    }

    void OnDestroy()
    {
        _chunkingCore.Cleanup();
    }

    void Update()
    {
        _chunkingCore.Execute();
    }
    #endregion

    #region Editor
#if UNITY_EDITOR
    void OnValidate()
    {
        _chunkingCore?.Initialize(_gridSize, _tileSize, _viewDistance, transform.position);
    }
#endif
    #endregion
}
