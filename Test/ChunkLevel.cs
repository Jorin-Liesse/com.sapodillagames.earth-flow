using UnityEngine;
using System.Collections.Generic;

public struct ChunkLevel
{
    public Dictionary<Vector2Int, int> UsedChunksMap;

    public int[] UsedChunks;
    public int UsedChunksCount;

    public int[] AddedRemoved;
    public int AddedRemovedCount;

    public int HierarchyLevel;
    public int GridScale;
}
