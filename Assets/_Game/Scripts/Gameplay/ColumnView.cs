using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ColumnView : MonoBehaviour
{
    private const int MaxFreezeLayerCount = 3;
    private const int OnlyColorIndicatorSortingOrder = 8;
    private const int OnlyColorStatusIndicatorSortingOrder = 9;
    private const float FreezeCounterOutlineOffset = 0.035f;

    [SerializeField] private List<Transform> slots = new List<Transform>();
    [SerializeField] private SpriteRenderer freezeOverlay;
    [SerializeField] private TextMesh freezeCounterText;
    [SerializeField] private Font freezeCounterFont;
    [SerializeField] private float freezeCounterCharacterSize = 0.22f;
    [SerializeField] private int freezeCounterFontSize = 48;
    [SerializeField] private Color freezeCounterColor = new Color(0.3882353f, 0.3882353f, 0.3882353f, 1f);
    [SerializeField] private Color freezeCounterOutlineColor = Color.white;
    [SerializeField] private SpriteRenderer onlyColorIndicator;
    [SerializeField] private SpriteRenderer onlyColorStatusIndicator;

    public event Action<ColumnView> Clicked;

    public IReadOnlyList<Transform> Slots => slots;
    public int SlotCount => slots.Count;

    private readonly List<BlockView> blocks = new List<BlockView>();
    private BoxCollider2D tapCollider;
    private bool isLocked;
    private bool hasAcceptedColor;
    private BlockColorId acceptedColor;
    private int freezeUnlockColumnCount;
    private bool isFreezeLocked;
    private readonly List<TextMesh> freezeCounterOutlines = new List<TextMesh>();

    public bool IsLocked => isLocked;
    public bool IsFreezeLocked => isFreezeLocked;
    public bool HasFreezeLock => freezeUnlockColumnCount > 0;
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

    public void SetOnlyColorIndicator(
        Sprite indicatorSprite,
        Color indicatorColor,
        Vector2 localOffset,
        Vector2 localScale,
        Sprite statusSprite,
        Vector2 statusLocalOffset,
        Vector2 statusLocalScale)
    {
        ResolveOnlyColorIndicator(indicatorSprite);
        if (onlyColorIndicator == null)
        {
            return;
        }

        onlyColorIndicator.sprite = indicatorSprite;
        onlyColorIndicator.color = indicatorColor;
        onlyColorIndicator.transform.localPosition = GetIndicatorLocalPosition(localOffset);
        onlyColorIndicator.transform.localScale = new Vector3(localScale.x, localScale.y, 1f);
        onlyColorIndicator.sortingOrder = OnlyColorIndicatorSortingOrder;
        onlyColorIndicator.gameObject.SetActive(indicatorSprite != null);

        ResolveOnlyColorStatusIndicator(statusSprite);
        SetOnlyColorStatusIndicator(statusSprite, statusLocalOffset, statusLocalScale);
    }

    public void SetOnlyColorStatusSolved(
        bool isSolved,
        Sprite normalSprite,
        Sprite solvedSprite,
        Vector2 statusLocalOffset,
        Vector2 statusLocalScale)
    {
        Sprite targetSprite = isSolved && solvedSprite != null ? solvedSprite : normalSprite;
        ResolveOnlyColorStatusIndicator(targetSprite);
        SetOnlyColorStatusIndicator(targetSprite, statusLocalOffset, statusLocalScale);
    }

    public void ConfigureFreezeLock(
        int unlockColumnCount,
        Font counterFont,
        float counterCharacterSize,
        int counterFontSize,
        Color counterColor)
    {
        freezeUnlockColumnCount = Mathf.Max(0, unlockColumnCount);
        freezeCounterFont = counterFont;
        freezeCounterCharacterSize = Mathf.Max(0.01f, counterCharacterSize);
        freezeCounterFontSize = Mathf.Max(1, counterFontSize);
        freezeCounterColor = counterColor;
        ResolveFreezeVisual();
        UpdateFreezeLock(0);
    }

    public void UpdateFreezeLock(int solvedColumnCount)
    {
        if (freezeUnlockColumnCount <= 0)
        {
            isFreezeLocked = false;
            SetFreezeVisualActive(false);
            return;
        }

        int remaining = Mathf.Max(0, freezeUnlockColumnCount - solvedColumnCount);
        bool shouldLock = remaining > 0;
        isFreezeLocked = shouldLock;

        if (!shouldLock)
        {
            HideFreezeVisual();
            return;
        }

        SetFreezeLayerCount(GetVisibleFreezeLayerCount(solvedColumnCount));

        if (freezeCounterText != null)
        {
            freezeCounterText.gameObject.SetActive(true);
            freezeCounterText.text = remaining.ToString();
            SetFreezeCounterOutlineActive(true);
            UpdateFreezeCounterOutlines();
            SyncFreezeCounterSorting();
        }
    }

    public bool CanAccept(BlockColorId colorId)
    {
        return !isFreezeLocked
            && (!hasAcceptedColor || acceptedColor == colorId || colorId == BlockColorId.RainBow);
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
        isLocked = locked || isFreezeLocked;

        if (tapCollider != null)
        {
            tapCollider.enabled = !isLocked;
        }
    }

    private void ResolveFreezeVisual()
    {
        if (freezeOverlay != null)
        {
            freezeOverlay.gameObject.SetActive(false);
        }

        if (freezeCounterText == null)
        {
            Transform found = transform.Find("FreezeCounter");
            freezeCounterText = found != null ? found.GetComponent<TextMesh>() : null;
        }

        if (freezeCounterText == null)
        {
            GameObject textObject = new GameObject("FreezeCounter");
            textObject.transform.SetParent(transform, false);
            freezeCounterText = textObject.AddComponent<TextMesh>();
            freezeCounterText.anchor = TextAnchor.MiddleCenter;
            freezeCounterText.alignment = TextAlignment.Center;
        }

        if (freezeCounterText != null)
        {
            Bounds bounds = GetColumnLocalBounds();
            if (freezeCounterFont != null)
            {
                freezeCounterText.font = freezeCounterFont;
                MeshRenderer textRenderer = freezeCounterText.GetComponent<MeshRenderer>();
                if (textRenderer != null)
                {
                    textRenderer.sharedMaterial = freezeCounterFont.material;
                }
            }

            freezeCounterText.characterSize = freezeCounterCharacterSize;
            freezeCounterText.fontSize = freezeCounterFontSize;
            freezeCounterText.color = freezeCounterColor;
            freezeCounterText.transform.localPosition = bounds.center + new Vector3(0f, 0f, -0.01f);
            MeshRenderer sortingRenderer = freezeCounterText.GetComponent<MeshRenderer>();
            if (sortingRenderer != null)
            {
                sortingRenderer.sortingOrder = 100;
            }

            EnsureFreezeCounterOutlines();
            UpdateFreezeCounterOutlines();
            SyncFreezeCounterSorting();
        }
    }

    private void SetFreezeVisualActive(bool active)
    {
        SetFreezeLayerCount(active ? MaxFreezeLayerCount : 0);

        if (freezeCounterText != null)
        {
            freezeCounterText.gameObject.SetActive(active);
        }

        SetFreezeCounterOutlineActive(active);
    }

    private void HideFreezeVisual()
    {
        for (int i = 0; i < blocks.Count; i++)
        {
            if (blocks[i] == null)
            {
                continue;
            }

            blocks[i].SetFreezeLayerCount(0);
        }

        if (freezeCounterText != null)
        {
            freezeCounterText.gameObject.SetActive(false);
        }

        SetFreezeCounterOutlineActive(false);
    }

    private void SetFreezeLayerCount(int activeLayerCount)
    {
        for (int i = 0; i < blocks.Count; i++)
        {
            if (blocks[i] == null)
            {
                continue;
            }

            blocks[i].SetFreezeLayerCount(activeLayerCount);
        }

        SyncFreezeCounterSorting();
    }

    private void SyncFreezeCounterSorting()
    {
        if (freezeCounterText == null)
        {
            return;
        }

        MeshRenderer counterRenderer = freezeCounterText.GetComponent<MeshRenderer>();
        if (counterRenderer == null)
        {
            return;
        }

        for (int i = 0; i < blocks.Count; i++)
        {
            if (blocks[i] == null)
            {
                continue;
            }

            SpriteRenderer blockRenderer = blocks[i].GetComponent<SpriteRenderer>();
            if (blockRenderer == null)
            {
                continue;
            }

            counterRenderer.sortingLayerID = blockRenderer.sortingLayerID;
            counterRenderer.sortingOrder = blockRenderer.sortingOrder + 90;
            SyncFreezeCounterOutlineSorting(blockRenderer.sortingLayerID, counterRenderer.sortingOrder - 1);
            return;
        }

        counterRenderer.sortingOrder = 100;
        SyncFreezeCounterOutlineSorting(counterRenderer.sortingLayerID, counterRenderer.sortingOrder - 1);
    }

    private void EnsureFreezeCounterOutlines()
    {
        if (freezeCounterText == null || freezeCounterOutlines.Count > 0)
        {
            return;
        }

        for (int i = 0; i < 8; i++)
        {
            Transform found = transform.Find($"FreezeCounterOutline{i + 1}");
            TextMesh outlineText = found != null ? found.GetComponent<TextMesh>() : null;
            if (outlineText == null)
            {
                GameObject outlineObject = new GameObject($"FreezeCounterOutline{i + 1}");
                outlineObject.transform.SetParent(transform, false);
                outlineText = outlineObject.AddComponent<TextMesh>();
            }

            freezeCounterOutlines.Add(outlineText);
        }
    }

    private void UpdateFreezeCounterOutlines()
    {
        EnsureFreezeCounterOutlines();
        if (freezeCounterText == null)
        {
            return;
        }

        Vector2[] offsets =
        {
            new Vector2(-1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, -1f),
            new Vector2(0f, 1f),
            new Vector2(-1f, -1f),
            new Vector2(-1f, 1f),
            new Vector2(1f, -1f),
            new Vector2(1f, 1f)
        };

        for (int i = 0; i < freezeCounterOutlines.Count; i++)
        {
            TextMesh outline = freezeCounterOutlines[i];
            if (outline == null)
            {
                continue;
            }

            outline.text = freezeCounterText.text;
            outline.anchor = freezeCounterText.anchor;
            outline.alignment = freezeCounterText.alignment;
            outline.font = freezeCounterText.font;
            outline.characterSize = freezeCounterText.characterSize;
            outline.fontSize = freezeCounterText.fontSize;
            outline.color = freezeCounterOutlineColor;
            outline.transform.localRotation = freezeCounterText.transform.localRotation;
            outline.transform.localScale = freezeCounterText.transform.localScale;

            Vector2 offset = offsets[i % offsets.Length] * FreezeCounterOutlineOffset;
            Vector3 basePosition = freezeCounterText.transform.localPosition;
            outline.transform.localPosition = basePosition + new Vector3(offset.x, offset.y, 0.001f);

            MeshRenderer outlineRenderer = outline.GetComponent<MeshRenderer>();
            MeshRenderer counterRenderer = freezeCounterText.GetComponent<MeshRenderer>();
            if (outlineRenderer != null)
            {
                if (freezeCounterFont != null)
                {
                    outlineRenderer.sharedMaterial = freezeCounterFont.material;
                }

                if (counterRenderer != null)
                {
                    outlineRenderer.sortingLayerID = counterRenderer.sortingLayerID;
                    outlineRenderer.sortingOrder = counterRenderer.sortingOrder - 1;
                }
            }
        }
    }

    private void SetFreezeCounterOutlineActive(bool active)
    {
        EnsureFreezeCounterOutlines();
        for (int i = 0; i < freezeCounterOutlines.Count; i++)
        {
            if (freezeCounterOutlines[i] != null)
            {
                freezeCounterOutlines[i].gameObject.SetActive(active);
            }
        }
    }

    private void SyncFreezeCounterOutlineSorting(int sortingLayerId, int sortingOrder)
    {
        EnsureFreezeCounterOutlines();
        for (int i = 0; i < freezeCounterOutlines.Count; i++)
        {
            if (freezeCounterOutlines[i] == null)
            {
                continue;
            }

            MeshRenderer outlineRenderer = freezeCounterOutlines[i].GetComponent<MeshRenderer>();
            if (outlineRenderer == null)
            {
                continue;
            }

            outlineRenderer.sortingLayerID = sortingLayerId;
            outlineRenderer.sortingOrder = sortingOrder;
        }
    }

    private int GetVisibleFreezeLayerCount(int solvedColumnCount)
    {
        if (freezeUnlockColumnCount <= 0)
        {
            return 0;
        }

        int solvedProgress = Mathf.Clamp(solvedColumnCount, 0, freezeUnlockColumnCount);
        int hiddenLayerCount = Mathf.CeilToInt(solvedProgress * MaxFreezeLayerCount / (float)freezeUnlockColumnCount);
        return Mathf.Clamp(MaxFreezeLayerCount - hiddenLayerCount, 0, MaxFreezeLayerCount);
    }

    private Bounds GetColumnLocalBounds()
    {
        if (slots.Count == 0)
        {
            return new Bounds(Vector3.zero, Vector3.one);
        }

        Bounds bounds = new Bounds(slots[0].localPosition, Vector3.zero);
        for (int i = 1; i < slots.Count; i++)
        {
            bounds.Encapsulate(slots[i].localPosition);
        }

        Vector2 slotSize = GetSlotSize();
        bounds.Expand(new Vector3(slotSize.x, slotSize.y, 0f));
        return bounds;
    }

    private void ResolveOnlyColorIndicator(Sprite indicatorSprite)
    {
        if (onlyColorIndicator != null)
        {
            return;
        }

        SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            string childName = spriteRenderers[i].name.ToLowerInvariant();
            if (childName.Contains("onlycolorindicator") || childName.Contains("onlyindicator"))
            {
                onlyColorIndicator = spriteRenderers[i];
                return;
            }
        }

        if (indicatorSprite == null)
        {
            return;
        }

        GameObject indicatorObject = new GameObject("OnlyColorIndicator");
        indicatorObject.transform.SetParent(transform, false);
        onlyColorIndicator = indicatorObject.AddComponent<SpriteRenderer>();
    }

    private void ResolveOnlyColorStatusIndicator(Sprite statusSprite)
    {
        if (onlyColorStatusIndicator != null)
        {
            return;
        }

        if (onlyColorIndicator != null)
        {
            SpriteRenderer[] childRenderers = onlyColorIndicator.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < childRenderers.Length; i++)
            {
                if (childRenderers[i] == onlyColorIndicator)
                {
                    continue;
                }

                string childName = childRenderers[i].name.ToLowerInvariant();
                if (childName.Contains("onlycolorstatus") || childName.Contains("statusindicator"))
                {
                    onlyColorStatusIndicator = childRenderers[i];
                    return;
                }
            }
        }

        if (statusSprite == null || onlyColorIndicator == null)
        {
            return;
        }

        GameObject statusObject = new GameObject("OnlyColorStatusIndicator");
        statusObject.transform.SetParent(onlyColorIndicator.transform, false);
        onlyColorStatusIndicator = statusObject.AddComponent<SpriteRenderer>();
    }

    private void SetOnlyColorStatusIndicator(Sprite statusSprite, Vector2 statusLocalOffset, Vector2 statusLocalScale)
    {
        if (onlyColorStatusIndicator == null)
        {
            return;
        }

        onlyColorStatusIndicator.sprite = statusSprite;
        onlyColorStatusIndicator.color = Color.white;
        onlyColorStatusIndicator.transform.localPosition = new Vector3(statusLocalOffset.x, statusLocalOffset.y, 0f);
        onlyColorStatusIndicator.transform.localScale = new Vector3(statusLocalScale.x, statusLocalScale.y, 1f);
        onlyColorStatusIndicator.sortingOrder = OnlyColorStatusIndicatorSortingOrder;
        onlyColorStatusIndicator.gameObject.SetActive(statusSprite != null);
    }

    private Vector3 GetIndicatorLocalPosition(Vector2 localOffset)
    {
        if (slots.Count == 0)
        {
            return new Vector3(localOffset.x, localOffset.y, 0f);
        }

        return slots[0].localPosition + new Vector3(localOffset.x, localOffset.y, 0f);
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
