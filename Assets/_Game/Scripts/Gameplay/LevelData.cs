using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Block Stack Sort/Level Data")]
public class LevelData : ScriptableObject
{
    [SerializeField, Min(1)] private int maxMoves = 20;
    [SerializeField] private BlockColorId trayColor = BlockColorId.White;
    [SerializeField] private List<LevelColumnData> columns = new List<LevelColumnData>();

    public int MaxMoves => maxMoves > 0 ? maxMoves : 20;
    public BlockColorId TrayColor => trayColor;
    public IReadOnlyList<LevelColumnData> Columns => columns;

    public void SetMaxMoves(int moves)
    {
        maxMoves = Mathf.Max(1, moves);
    }

    public void SetTrayColor(BlockColorId colorId)
    {
        trayColor = colorId;
    }

    public List<LevelColumnData> GetMutableColumns()
    {
        return columns;
    }
}

[Serializable]
public class LevelColumnData
{
    [SerializeField] private List<BlockColorId> blocks = new List<BlockColorId>();
    [SerializeField] private List<bool> coveredBlocks = new List<bool>();
    [SerializeField] private bool hasAcceptedColor;
    [SerializeField] private BlockColorId acceptedColor = BlockColorId.Red;
    [SerializeField, Min(0)] private int freezeUnlockColumnCount;

    public IReadOnlyList<BlockColorId> Blocks => blocks;
    public bool HasAcceptedColor => hasAcceptedColor;
    public BlockColorId AcceptedColor => acceptedColor;
    public int FreezeUnlockColumnCount => Mathf.Max(0, freezeUnlockColumnCount);

    public List<BlockColorId> GetMutableBlocks()
    {
        return blocks;
    }

    public List<bool> GetMutableCoveredBlocks()
    {
        EnsureCoverSize();
        return coveredBlocks;
    }

    public bool IsBlockCovered(int blockIndex)
    {
        EnsureCoverSize();
        return blockIndex >= 0 && blockIndex < coveredBlocks.Count && coveredBlocks[blockIndex];
    }

    public void SetAcceptedColorRule(bool enabled, BlockColorId colorId)
    {
        hasAcceptedColor = enabled;
        acceptedColor = colorId;
    }

    public void SetFreezeUnlockColumnCount(int unlockColumnCount)
    {
        freezeUnlockColumnCount = Mathf.Max(0, unlockColumnCount);
    }

    public void EnsureCoverSize()
    {
        while (coveredBlocks.Count < blocks.Count)
        {
            coveredBlocks.Add(false);
        }

        while (coveredBlocks.Count > blocks.Count)
        {
            coveredBlocks.RemoveAt(coveredBlocks.Count - 1);
        }
    }
}
