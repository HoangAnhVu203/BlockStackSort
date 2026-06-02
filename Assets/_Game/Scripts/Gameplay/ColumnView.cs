using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ColumnView : MonoBehaviour
{
    [SerializeField] private List<Transform> slots = new List<Transform>();

    public event Action<ColumnView> Clicked;

    public IReadOnlyList<Transform> Slots => slots;
    public int SlotCount => slots.Count;

    private readonly List<BlockView> blocks = new List<BlockView>();
    private BoxCollider2D tapCollider;
    private bool isLocked;
    private bool hasAcceptedColor;
    private BlockColorId acceptedColor;

    public bool IsLocked => isLocked;
    public IReadOnlyList<BlockView> Blocks => blocks;

    public void Initialize()
    {
        CollectSlots();
        EnsureTapCollider();
    }

    public void SetBlocks(List<BlockView> newBlocks)
    {
        blocks.Clear();
        blocks.AddRange(newBlocks);
        SnapBlocksToSlots();
    }

    public void SetAcceptedColorRule(bool enabled, BlockColorId colorId)
    {
        hasAcceptedColor = enabled;
        acceptedColor = colorId;
    }

    public bool CanAccept(BlockColorId colorId)
    {
        return !hasAcceptedColor || acceptedColor == colorId;
    }

    public bool ContainsBlock(BlockView targetBlock)
    {
        return blocks.Contains(targetBlock);
    }

    public bool CanAcceptAll(IReadOnlyList<BlockView> targetBlocks)
    {
        for (int i = 0; i < targetBlocks.Count; i++)
        {
            if (!CanAccept(targetBlocks[i].ColorId))
            {
                return false;
            }
        }

        return true;
    }

    public List<BlockView> SnapshotBlocks()
    {
        return new List<BlockView>(blocks);
    }

    public BlockView PushFromTray(BlockView incomingBlock)
    {
        BlockView outgoingBlock = blocks[blocks.Count - 1];

        for (int i = blocks.Count - 1; i > 0; i--)
        {
            blocks[i] = blocks[i - 1];
        }

        blocks[0] = incomingBlock;
        SnapBlocksToSlots();

        return outgoingBlock;
    }

    public Sequence ReplaceBlocksAnimated(List<BlockView> newBlocks, float duration)
    {
        blocks.Clear();
        blocks.AddRange(newBlocks);

        Sequence sequence = DOTween.Sequence();
        for (int i = 0; i < blocks.Count; i++)
        {
            BlockView block = blocks[i];
            Transform targetSlot = slots[i];
            block.transform.SetParent(targetSlot, true);
            block.SetSortingOrder(20);
            sequence.Join(block.transform.DOMove(targetSlot.position, duration).SetEase(Ease.InOutQuad));
        }

        sequence.AppendCallback(() =>
        {
            for (int i = 0; i < blocks.Count; i++)
            {
                blocks[i].SetSortingOrder(10);
            }

            SnapBlocksToSlots();
        });

        return sequence;
    }

    public Sequence SetMagnetRaised(bool raised, float offset, float duration)
    {
        Sequence sequence = DOTween.Sequence();
        float targetLocalY = raised ? offset : 0f;
        int sortingOrder = raised ? 45 : 10;

        for (int i = 0; i < blocks.Count; i++)
        {
            BlockView block = blocks[i];
            block.SetSortingOrder(sortingOrder);
            sequence.Join(block.transform.DOLocalMoveY(targetLocalY, duration).SetEase(Ease.OutQuad));
        }

        if (!raised)
        {
            sequence.AppendCallback(SnapBlocksToSlots);
        }

        return sequence;
    }

    public Sequence PushFromTrayAnimated(
        BlockView incomingBlock,
        Transform traySlot,
        float trayToColumnDuration,
        float pushDuration,
        float returnToTrayDuration)
    {
        List<BlockView> oldBlocks = new List<BlockView>(blocks);
        BlockView outgoingBlock = oldBlocks[oldBlocks.Count - 1];
        Vector3 entryPosition = GetEntryPosition();
        Vector3 exitPosition = GetExitPosition();

        for (int i = blocks.Count - 1; i > 0; i--)
        {
            blocks[i] = blocks[i - 1];
        }

        blocks[0] = incomingBlock;

        incomingBlock.transform.SetParent(slots[0], true);
        outgoingBlock.transform.SetParent(traySlot, true);
        incomingBlock.SetSortingOrder(30);
        outgoingBlock.SetSortingOrder(25);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(incomingBlock.transform.DOMove(entryPosition, trayToColumnDuration).SetEase(Ease.OutQuad));
        sequence.Append(incomingBlock.transform.DOMove(slots[0].position, pushDuration).SetEase(Ease.OutQuad));

        for (int i = oldBlocks.Count - 2; i >= 0; i--)
        {
            BlockView block = oldBlocks[i];
            Transform targetSlot = slots[i + 1];
            block.transform.SetParent(targetSlot, true);
            sequence.Join(block.transform.DOMove(targetSlot.position, pushDuration).SetEase(Ease.OutQuad));
        }

        sequence.Join(outgoingBlock.transform.DOMove(exitPosition, pushDuration).SetEase(Ease.OutQuad));
        sequence.Append(outgoingBlock.transform.DOMove(traySlot.position, returnToTrayDuration).SetEase(Ease.InOutQuad));
        sequence.AppendCallback(() =>
        {
            SnapBlocksToSlots();
            incomingBlock.SetSortingOrder(10);
            outgoingBlock.SetSortingOrder(10);
            outgoingBlock.transform.SetParent(traySlot, true);
            outgoingBlock.transform.position = traySlot.position;
            outgoingBlock.transform.localScale = Vector3.one;
        });

        return sequence;
    }

    public Sequence ReversePushFromTrayAnimated(
        List<BlockView> previousBlocks,
        BlockView previousTrayBlock,
        BlockView currentTrayBlock,
        Transform traySlot,
        float trayToColumnDuration,
        float pushDuration,
        float returnToTrayDuration)
    {
        Sequence sequence = DOTween.Sequence();
        Vector3 entryPosition = GetEntryPosition();
        Vector3 exitPosition = GetExitPosition();
        int lastIndex = slots.Count - 1;

        currentTrayBlock.transform.SetParent(slots[lastIndex], true);
        previousTrayBlock.transform.SetParent(traySlot, true);
        currentTrayBlock.SetSortingOrder(25);
        previousTrayBlock.SetSortingOrder(30);

        sequence.Append(currentTrayBlock.transform.DOMove(exitPosition, returnToTrayDuration).SetEase(Ease.InOutQuad));
        sequence.Append(currentTrayBlock.transform.DOMove(slots[lastIndex].position, pushDuration).SetEase(Ease.OutQuad));

        for (int i = 0; i < previousBlocks.Count - 1; i++)
        {
            BlockView block = previousBlocks[i];
            Transform targetSlot = slots[i];
            block.transform.SetParent(targetSlot, true);
            sequence.Join(block.transform.DOMove(targetSlot.position, pushDuration).SetEase(Ease.OutQuad));
        }

        previousTrayBlock.transform.SetParent(traySlot, true);
        sequence.Join(previousTrayBlock.transform.DOMove(entryPosition, pushDuration).SetEase(Ease.OutQuad));
        sequence.Append(previousTrayBlock.transform.DOMove(traySlot.position, trayToColumnDuration).SetEase(Ease.InOutQuad));
        sequence.AppendCallback(() =>
        {
            blocks.Clear();
            blocks.AddRange(previousBlocks);
            SnapBlocksToSlots();

            previousTrayBlock.SetSortingOrder(10);
            currentTrayBlock.SetSortingOrder(10);
            previousTrayBlock.transform.SetParent(traySlot, true);
            previousTrayBlock.transform.position = traySlot.position;
            previousTrayBlock.transform.localScale = Vector3.one;
        });

        return sequence;
    }

    public bool TryGetAutoPrepareScore(BlockColorId targetColor, out int matchCount)
    {
        matchCount = 0;

        if (isLocked || blocks.Count == 0)
        {
            return false;
        }

        int mismatchCount = 0;
        int mismatchIndex = -1;

        for (int i = 0; i < blocks.Count; i++)
        {
            if (blocks[i].ColorId == targetColor)
            {
                matchCount++;
                continue;
            }

            mismatchCount++;
            mismatchIndex = i;

            if (mismatchCount > 1)
            {
                return false;
            }
        }

        return mismatchCount == 1 && mismatchIndex != blocks.Count - 1;
    }

    public Sequence PrepareSingleMismatchToBottomAnimated(BlockColorId targetColor, float duration)
    {
        Sequence sequence = DOTween.Sequence();
        int mismatchIndex = GetSingleMismatchIndex(targetColor);
        int lastIndex = blocks.Count - 1;

        if (mismatchIndex < 0 || mismatchIndex == lastIndex)
        {
            return sequence;
        }

        BlockView mismatchBlock = blocks[mismatchIndex];
        mismatchBlock.SetSortingOrder(26);

        for (int i = mismatchIndex; i < lastIndex; i++)
        {
            blocks[i] = blocks[i + 1];
        }

        blocks[lastIndex] = mismatchBlock;

        for (int i = mismatchIndex; i <= lastIndex; i++)
        {
            BlockView block = blocks[i];
            Transform targetSlot = slots[i];
            block.transform.SetParent(targetSlot, true);
            sequence.Join(block.transform.DOMove(targetSlot.position, duration).SetEase(Ease.OutQuad));
        }

        sequence.AppendCallback(() =>
        {
            mismatchBlock.SetSortingOrder(10);
            SnapBlocksToSlots();
        });

        return sequence;
    }

    public bool TryGetSingleMismatchIndex(BlockColorId targetColor, out int mismatchIndex)
    {
        mismatchIndex = GetSingleMismatchIndex(targetColor);
        return mismatchIndex >= 0;
    }

    public Sequence ReverseSingleMismatchToBottomAnimated(int originalMismatchIndex, float duration)
    {
        Sequence sequence = DOTween.Sequence();
        int lastIndex = blocks.Count - 1;

        if (originalMismatchIndex < 0 || originalMismatchIndex >= lastIndex)
        {
            return sequence;
        }

        BlockView mismatchBlock = blocks[lastIndex];
        mismatchBlock.SetSortingOrder(26);

        for (int i = lastIndex; i > originalMismatchIndex; i--)
        {
            blocks[i] = blocks[i - 1];
        }

        blocks[originalMismatchIndex] = mismatchBlock;

        for (int i = originalMismatchIndex; i <= lastIndex; i++)
        {
            BlockView block = blocks[i];
            Transform targetSlot = slots[i];
            block.transform.SetParent(targetSlot, true);
            sequence.Join(block.transform.DOMove(targetSlot.position, duration).SetEase(Ease.OutQuad));
        }

        sequence.AppendCallback(() =>
        {
            mismatchBlock.SetSortingOrder(10);
            SnapBlocksToSlots();
        });

        return sequence;
    }

    private int GetSingleMismatchIndex(BlockColorId targetColor)
    {
        int mismatchIndex = -1;
        int mismatchCount = 0;

        for (int i = 0; i < blocks.Count; i++)
        {
            if (blocks[i].ColorId == targetColor)
            {
                continue;
            }

            mismatchIndex = i;
            mismatchCount++;

            if (mismatchCount > 1)
            {
                return -1;
            }
        }

        return mismatchCount == 1 ? mismatchIndex : -1;
    }

    public bool IsSolved()
    {
        if (blocks.Count == 0 || blocks.Count != slots.Count)
        {
            return false;
        }

        BlockColorId firstColor = blocks[0].ColorId;
        for (int i = 1; i < blocks.Count; i++)
        {
            if (blocks[i].ColorId != firstColor)
            {
                return false;
            }
        }

        return true;
    }

    public void TriggerClick()
    {
        Clicked?.Invoke(this);
    }

    private void CollectSlots()
    {
        slots.Clear();

        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Slot", StringComparison.OrdinalIgnoreCase))
            {
                slots.Add(child);
            }
        }

        slots.Sort((a, b) => ExtractSlotIndex(a.name).CompareTo(ExtractSlotIndex(b.name)));
    }

    private void EnsureTapCollider()
    {
        tapCollider = GetComponent<BoxCollider2D>();
        if (tapCollider == null)
        {
            tapCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        if (slots.Count == 0)
        {
            tapCollider.size = Vector2.one;
            return;
        }

        Bounds bounds = new Bounds(slots[0].localPosition, Vector3.zero);
        for (int i = 1; i < slots.Count; i++)
        {
            bounds.Encapsulate(slots[i].localPosition);
        }

        Vector2 slotSize = GetSlotSize();
        tapCollider.offset = bounds.center;
        tapCollider.size = new Vector2(slotSize.x, bounds.size.y + slotSize.y);
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;

        if (tapCollider != null)
        {
            tapCollider.enabled = !locked;
        }
    }

    private void SnapBlocksToSlots()
    {
        for (int i = 0; i < blocks.Count; i++)
        {
            blocks[i].transform.SetParent(slots[i], true);
            blocks[i].transform.position = slots[i].position;
            blocks[i].transform.localScale = Vector3.one;
        }
    }

    private Vector3 GetExitPosition()
    {
        if (slots.Count < 2)
        {
            return slots[slots.Count - 1].position + Vector3.down;
        }

        Vector3 lastSlotPosition = slots[slots.Count - 1].position;
        Vector3 previousSlotPosition = slots[slots.Count - 2].position;
        Vector3 slotStep = lastSlotPosition - previousSlotPosition;

        if (slotStep.sqrMagnitude < 0.01f)
        {
            slotStep = Vector3.down;
        }

        return lastSlotPosition + slotStep;
    }

    private Vector3 GetEntryPosition()
    {
        if (slots.Count < 2)
        {
            return slots[0].position + Vector3.up;
        }

        Vector3 firstSlotPosition = slots[0].position;
        Vector3 secondSlotPosition = slots[1].position;
        Vector3 slotStep = firstSlotPosition - secondSlotPosition;

        if (slotStep.sqrMagnitude < 0.01f)
        {
            slotStep = Vector3.up;
        }

        return firstSlotPosition + slotStep;
    }

    private int ExtractSlotIndex(string slotName)
    {
        int value = 0;
        bool hasDigit = false;

        for (int i = 0; i < slotName.Length; i++)
        {
            if (!char.IsDigit(slotName[i]))
            {
                continue;
            }

            hasDigit = true;
            value = (value * 10) + (slotName[i] - '0');
        }

        return hasDigit ? value : int.MaxValue;
    }

    private Vector2 GetSlotSize()
    {
        if (slots.Count == 0)
        {
            return Vector2.one;
        }

        SpriteRenderer spriteRenderer = slots[0].GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            return spriteRenderer.bounds.size;
        }

        return new Vector2(Mathf.Abs(slots[0].lossyScale.x), Mathf.Abs(slots[0].lossyScale.y));
    }
}
