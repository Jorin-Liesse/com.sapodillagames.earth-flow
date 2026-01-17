using UnityEngine;
using System.Collections.Generic;

public struct ChunkData
{
    public Vector2Int GridCoord;
    public Bounds Bounds;
    public bool IsVisible;
    public int Parent;
    public List<int> Children;
}
