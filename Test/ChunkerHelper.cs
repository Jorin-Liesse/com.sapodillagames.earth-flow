using UnityEngine;
using System.Collections.Generic;

public static class ChunkerHelper
{
    // Setting
    public static Vector2Int GridSize = new Vector2Int(1000, 1000);
    public static Vector3 TileSize = new(10, 1, 10);
    public static int ViewDistance = 100;
    public static int BufferDistance = 1;
    public static int GroupSize = 4;

    // Cached
    public static Camera MainCamera;
    public static Transform CameraTransform;
    public static int LoadingRadius;
    public static int LoadingRadiusSqr;
    public static int ViewDistanceSqr;
    public static Vector2Int[] CircularCoords;
    public static int HierarchyLevels;
    public static int PoolSize;

    public static void Initialize()
    {
        MainCamera = Camera.main;
        CameraTransform = MainCamera.transform;

        LoadingRadius = ViewDistance + BufferDistance;
        LoadingRadiusSqr = LoadingRadius * LoadingRadius;
        ViewDistanceSqr = ViewDistance * ViewDistance;

        CircularCoords = CalculateCircularCoords(LoadingRadius);

        HierarchyLevels = CalculateHierarchyLevels();
        PoolSize = CalculatePoolSize(CircularCoords.Length);
    }

    public static Vector2Int[] CalculateCircularCoords(int radius)
    {
        int radiusSqr = radius * radius;
        List<Vector2Int> coords = new(radiusSqr * 4);

        for (int x = -radius; x <= radius; x++)
            for (int y = -radius; y <= radius; y++)
                if (x * x + y * y <= radiusSqr)
                    coords.Add(new Vector2Int(x, y));

        return coords.ToArray();
    }

    public static int CalculateHierarchyLevels()
    {
        return Mathf.CeilToInt(Mathf.Log(LoadingRadius, GroupSize));
    }

    public static int CalculatePoolSize(int circularCoordsCount)
    {
        int size = 0;

        for (int level = 0; level < HierarchyLevels; level++)
            size += (circularCoordsCount + GroupSize * level) / (GroupSize * level + 1);

        return size;
    }

    public static void SetChunkData(ref ChunkData chunk,Vector3 offset, Vector2Int gridCoord, int hierarchyLevel, int gridScale)
    {
        chunk.IsVisible = false;
        chunk.Parent = -1;
        chunk.Children.Clear();

        chunk.GridCoord = gridCoord;

        chunk.Bounds.size = TileSize * gridScale;
        chunk.Bounds.center = new Vector3(
            (chunk.GridCoord.x + 0.5f) * TileSize.x * gridScale + offset.x,
            offset.y,
            (chunk.GridCoord.y + 0.5f) * TileSize.z * gridScale + offset.z
        );
    }

    public static ChunkData CreateChunkData()
    {
        ChunkData chunk = new();

        chunk.Bounds = new();
        chunk.Children = new(GroupSize * GroupSize);

        return chunk;
    }

    public static ChunkLevel CreateChunkLevel(int capacity, int hierarchyLevel)
    {
        ChunkLevel chunkLevel = new();

        chunkLevel.UsedChunks = new int[capacity];
        chunkLevel.UsedChunksMap = new(capacity);
        chunkLevel.AddedRemoved = new int[capacity];

        chunkLevel.AddedRemovedCount = 0;
        chunkLevel.UsedChunksCount = 0;

        chunkLevel.HierarchyLevel = hierarchyLevel;
        chunkLevel.GridScale = (hierarchyLevel == 0) ? 1 : (int)Mathf.Pow(GroupSize, hierarchyLevel);

        return chunkLevel;
    }
}
