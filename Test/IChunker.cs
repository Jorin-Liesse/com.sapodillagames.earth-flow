using System.Collections.Generic;

public interface IChunker
{
    public int HierarchyLevels { get; }
    IReadOnlyList<int> GetUsedIndices(int level);
    ChunkData GetChunk(int index);
}
