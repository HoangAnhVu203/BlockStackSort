using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class BoardGameplay : MonoBehaviour
{
    private const string RedPrefabPath = "Assets/_Game/Prefabs/Block/BlockRed.prefab";
    private const string YellowPrefabPath = "Assets/_Game/Prefabs/Block/BlockYellow.prefab";
    private const string GreenPrefabPath = "Assets/_Game/Prefabs/Block/BlocGreen.prefab";
    private const string WhitePrefabPath = "Assets/_Game/Prefabs/Block/BlockWhite.prefab";

    [Header("Scene")]
    [SerializeField] private Transform boardRoot;
    [SerializeField] private Transform columnRoot;
    [SerializeField] private Transform traySlot;

    [Header("Level")]
    [SerializeField] private LevelData currentLevel;

    [Header("Prefabs")]
    [SerializeField] private GameObject redBlockPrefab;
    [SerializeField] private GameObject yellowBlockPrefab;
    [SerializeField] private GameObject greenBlockPrefab;
    [SerializeField] private GameObject whiteBlockPrefab;

    [Header("Animation")]
    [SerializeField] private float trayToColumnDuration = 0.25f;
    [SerializeField] private float pushDuration = 0.225f;
    [SerializeField] private float returnToTrayDuration = 0.3125f;
    [SerializeField] private float winScaleMultiplier = 1.15f;
    [SerializeField] private float winScaleDuration = 0.16f;
    [SerializeField] private float winWaveDelay = 0.08f;

    private readonly List<ColumnView> columns = new List<ColumnView>();
    private BlockView trayBlock;
    private bool isAnimating;
    private bool hasWon;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<BoardGameplay>() != null)
        {
            return;
        }

        GameObject boardObject = GameObject.Find("BoardRoot");
        if (boardObject == null)
        {
            Debug.LogWarning("BoardGameplay: Không tìm thấy BoardRoot trong scene.");
            return;
        }

        boardObject.AddComponent<BoardGameplay>();
    }

    private void Awake()
    {
        ResolveSceneReferences();
        ResolvePrefabReferences();
    }

    private void Start()
    {
        SpawnInitialBoard();
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryClickColumn(Mouse.current.position.ReadValue());
        }

        if (Touchscreen.current != null)
        {
            foreach (TouchControl touch in Touchscreen.current.touches)
            {
                if (touch.press.wasPressedThisFrame)
                {
                    TryClickColumn(touch.position.ReadValue());
                    break;
                }
            }
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < columns.Count; i++)
        {
            columns[i].Clicked -= HandleColumnClicked;
        }
    }

    private void ResolveSceneReferences()
    {
        if (boardRoot == null)
        {
            GameObject boardObject = GameObject.Find("BoardRoot");
            boardRoot = boardObject != null ? boardObject.transform : transform;
        }

        if (columnRoot == null)
        {
            Transform foundColumnRoot = boardRoot.Find("ColumnRoot");
            columnRoot = foundColumnRoot != null ? foundColumnRoot : boardRoot;
        }

        if (traySlot == null)
        {
            Transform tray = boardRoot.Find("Tray");
            if (tray != null)
            {
                traySlot = tray.Find("TraySlot");
            }
        }
    }

    private void ResolvePrefabReferences()
    {
#if UNITY_EDITOR
        redBlockPrefab = redBlockPrefab != null ? redBlockPrefab : LoadEditorBlockPrefab(RedPrefabPath);
        yellowBlockPrefab = yellowBlockPrefab != null ? yellowBlockPrefab : LoadEditorBlockPrefab(YellowPrefabPath);
        greenBlockPrefab = greenBlockPrefab != null ? greenBlockPrefab : LoadEditorBlockPrefab(GreenPrefabPath);
        whiteBlockPrefab = whiteBlockPrefab != null ? whiteBlockPrefab : LoadEditorBlockPrefab(WhitePrefabPath);
#endif
    }

    private void SpawnInitialBoard()
    {
        if (columnRoot == null || traySlot == null)
        {
            Debug.LogWarning("BoardGameplay: Thiếu ColumnRoot hoặc TraySlot.");
            return;
        }

        ClearExistingBlocks();
        BuildColumns();

        if (currentLevel != null)
        {
            SpawnLevel(currentLevel);
            RefreshLockedColumns();
            return;
        }

        BlockColorId[,] layout =
        {
            { BlockColorId.Red, BlockColorId.Yellow, BlockColorId.Green },
            { BlockColorId.Yellow, BlockColorId.Green, BlockColorId.Red },
            { BlockColorId.Green, BlockColorId.Red, BlockColorId.Yellow },
        };

        for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
        {
            ColumnView column = columns[columnIndex];
            List<BlockView> spawnedBlocks = new List<BlockView>();

            for (int slotIndex = 0; slotIndex < column.SlotCount; slotIndex++)
            {
                BlockColorId colorId = layout[slotIndex % layout.GetLength(0), columnIndex % layout.GetLength(1)];
                spawnedBlocks.Add(CreateBlock(colorId, column.Slots[slotIndex]));
            }

            column.SetBlocks(spawnedBlocks);
        }

        trayBlock = CreateBlock(BlockColorId.White, traySlot);
        trayBlock.transform.position = traySlot.position;
        trayBlock.transform.localScale = Vector3.one;

        RefreshLockedColumns();
    }

    private void BuildColumns()
    {
        for (int i = 0; i < columns.Count; i++)
        {
            columns[i].Clicked -= HandleColumnClicked;
        }

        columns.Clear();

        foreach (Transform child in columnRoot)
        {
            ColumnView column = child.GetComponent<ColumnView>();
            if (column == null)
            {
                column = child.gameObject.AddComponent<ColumnView>();
            }

            column.Initialize();
            column.Clicked += HandleColumnClicked;
            columns.Add(column);
        }
    }

    private void HandleColumnClicked(ColumnView column)
    {
        if (trayBlock == null || column.IsLocked || isAnimating || hasWon || !column.CanAccept(trayBlock.ColorId))
        {
            return;
        }

        BlockView incomingBlock = trayBlock;
        isAnimating = true;

        Sequence sequence = column.PushFromTrayAnimated(
            incomingBlock,
            traySlot,
            trayToColumnDuration,
            pushDuration,
            returnToTrayDuration);

        sequence.OnComplete(() =>
        {
            trayBlock = traySlot.GetComponentInChildren<BlockView>();
            RefreshLockedColumns();

            if (!TryAutoPrepareBestColumn())
            {
                isAnimating = false;
                CheckWinLog();
            }
        });
    }

    private void TryClickColumn(Vector2 screenPosition)
    {
        if (isAnimating || hasWon)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Vector2 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
        RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero);
        if (hit.collider == null)
        {
            return;
        }

        ColumnView column = hit.collider.GetComponent<ColumnView>();
        if (column == null)
        {
            column = hit.collider.GetComponentInParent<ColumnView>();
        }

        if (column != null)
        {
            column.TriggerClick();
        }
    }

    private void SpawnLevel(LevelData levelData)
    {
        IReadOnlyList<LevelColumnData> levelColumns = levelData.Columns;

        for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
        {
            ColumnView column = columns[columnIndex];
            column.SetAcceptedColorRule(false, BlockColorId.Red);

            if (columnIndex >= levelColumns.Count)
            {
                Debug.LogWarning($"BoardGameplay: Scene có nhiều column hơn level data tại index {columnIndex}.");
                continue;
            }

            LevelColumnData columnData = levelColumns[columnIndex];
            column.SetAcceptedColorRule(columnData.HasAcceptedColor, columnData.AcceptedColor);

            List<BlockView> spawnedBlocks = new List<BlockView>();
            int spawnCount = Mathf.Min(column.SlotCount, columnData.Blocks.Count);

            for (int slotIndex = 0; slotIndex < spawnCount; slotIndex++)
            {
                spawnedBlocks.Add(CreateBlock(columnData.Blocks[slotIndex], column.Slots[slotIndex]));
            }

            if (spawnCount != column.SlotCount)
            {
                Debug.LogWarning($"BoardGameplay: Column {columnIndex} có {column.SlotCount} slot nhưng level có {columnData.Blocks.Count} block.");
            }

            column.SetBlocks(spawnedBlocks);
        }

        trayBlock = CreateBlock(levelData.TrayColor, traySlot);
        trayBlock.transform.position = traySlot.position;
        trayBlock.transform.localScale = Vector3.one;
    }

    private bool IsWin()
    {
        for (int i = 0; i < columns.Count; i++)
        {
            if (!columns[i].IsSolved())
            {
                return false;
            }
        }

        return columns.Count > 0;
    }

    private void RefreshLockedColumns()
    {
        for (int i = 0; i < columns.Count; i++)
        {
            columns[i].SetLocked(columns[i].IsSolved());
        }
    }

    private bool TryAutoPrepareBestColumn()
    {
        if (trayBlock == null)
        {
            return false;
        }

        ColumnView bestColumn = null;
        int bestMatchCount = -1;

        for (int i = 0; i < columns.Count; i++)
        {
            if (!columns[i].TryGetAutoPrepareScore(trayBlock.ColorId, out int matchCount))
            {
                continue;
            }

            if (matchCount > bestMatchCount)
            {
                bestMatchCount = matchCount;
                bestColumn = columns[i];
            }
        }

        if (bestColumn == null)
        {
            return false;
        }

        isAnimating = true;
        Sequence sequence = bestColumn.PrepareSingleMismatchToBottomAnimated(trayBlock.ColorId, pushDuration);
        sequence.OnComplete(() =>
        {
            RefreshLockedColumns();
            isAnimating = false;
            CheckWinLog();
        });

        return true;
    }

    private void CheckWinLog()
    {
        if (!hasWon && IsWin())
        {
            hasWon = true;
            isAnimating = true;
            Debug.Log("Win: tất cả column đã cùng màu.");
            PlayWinWave();
        }
    }

    private void PlayWinWave()
    {
        Sequence sequence = DOTween.Sequence();
        int maxSlotCount = GetMaxSlotCount();

        for (int slotIndex = 0; slotIndex < maxSlotCount; slotIndex++)
        {
            for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                IReadOnlyList<BlockView> blocks = columns[columnIndex].Blocks;
                if (slotIndex >= blocks.Count)
                {
                    continue;
                }

                Transform blockTransform = blocks[slotIndex].transform;
                Vector3 startScale = blockTransform.localScale;
                Vector3 targetScale = startScale * winScaleMultiplier;
                float delay = (slotIndex * winWaveDelay) + (columnIndex * winWaveDelay * 0.35f);

                sequence.Insert(delay, blockTransform.DOScale(targetScale, winScaleDuration).SetEase(Ease.OutQuad));
                sequence.Insert(delay + winScaleDuration, blockTransform.DOScale(startScale, winScaleDuration).SetEase(Ease.InQuad));
            }
        }

        sequence.OnComplete(() =>
        {
            isAnimating = false;
        });
    }

    private int GetMaxSlotCount()
    {
        int maxSlotCount = 0;

        for (int i = 0; i < columns.Count; i++)
        {
            if (columns[i].SlotCount > maxSlotCount)
            {
                maxSlotCount = columns[i].SlotCount;
            }
        }

        return maxSlotCount;
    }

    private BlockView CreateBlock(BlockColorId colorId, Transform parentSlot)
    {
        GameObject prefab = GetPrefab(colorId);
        GameObject blockObject = Instantiate(prefab, parentSlot.position, Quaternion.identity, parentSlot);
        BlockView block = blockObject.GetComponent<BlockView>();
        if (block == null)
        {
            block = blockObject.AddComponent<BlockView>();
        }

        block.name = $"Block_{colorId}";
        block.SetColor(colorId, GetColor(colorId));
        return block;
    }

    private GameObject GetPrefab(BlockColorId colorId)
    {
        switch (colorId)
        {
            case BlockColorId.Yellow:
                return yellowBlockPrefab != null ? yellowBlockPrefab : redBlockPrefab;
            case BlockColorId.Green:
                return greenBlockPrefab != null ? greenBlockPrefab : redBlockPrefab;
            case BlockColorId.White:
                return whiteBlockPrefab != null ? whiteBlockPrefab : redBlockPrefab;
            default:
                return redBlockPrefab;
        }
    }

    private Color GetColor(BlockColorId colorId)
    {
        switch (colorId)
        {
            case BlockColorId.Red:
                return Color.red;
            case BlockColorId.Yellow:
                return Color.yellow;
            case BlockColorId.Green:
                return Color.green;
            default:
                return Color.white;
        }
    }

    private void ClearExistingBlocks()
    {
        BlockView[] existingBlocks = boardRoot.GetComponentsInChildren<BlockView>();
        for (int i = existingBlocks.Length - 1; i >= 0; i--)
        {
            Destroy(existingBlocks[i].gameObject);
        }
    }

#if UNITY_EDITOR
    private GameObject LoadEditorBlockPrefab(string assetPath)
    {
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            Debug.LogWarning($"BoardGameplay: Không load được prefab tại {assetPath}");
            return null;
        }

        return prefab;
    }
#endif
}
