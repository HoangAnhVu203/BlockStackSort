using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class BoardGameplay : MonoBehaviour
{
    private static readonly Color FreezeCounterTextColor = new Color32(0x63, 0x63, 0x63, 0xFF);

    private const string RedPrefabPath = "Assets/_Game/Prefabs/Block/BlockRed.prefab";
    private const string YellowPrefabPath = "Assets/_Game/Prefabs/Block/BlockYellow.prefab";
    private const string GreenPrefabPath = "Assets/_Game/Prefabs/Block/BlocGreen.prefab";
    private const string WhitePrefabPath = "Assets/_Game/Prefabs/Block/BlockWhite.prefab";
    private const string PinkPrefabPath = "Assets/_Game/Prefabs/Block/BlockPink.prefab";
    private const string BlackPrefabPath = "Assets/_Game/Prefabs/Block/BlocBlack.prefab";
    private const string BrownPrefabPath = "Assets/_Game/Prefabs/Block/BlockBrown.prefab";
    private const string PurplePrefabPath = "Assets/_Game/Prefabs/Block/BlockPurple.prefab";
    private const string BlueSkyPrefabPath = "Assets/_Game/Prefabs/Block/BlockBlueSky.prefab";
    private const string RainbowPrefabPath = "Assets/_Game/Prefabs/Block/BlockRainbow.prefab";
    private const string BluePrefabPath = "Assets/_Game/Prefabs/Block/BlockBlue.prefab";
    private const string OrangePrefabPath = "Assets/_Game/Prefabs/Block/BlockOrange.prefab";
    private const string TutorialHandPrefabPath = "Assets/_Game/Prefabs/Hand.prefab";

    [Header("Scene")]
    [SerializeField] private Transform boardRoot;
    [SerializeField] private Transform columnRoot;
    [SerializeField] private Transform traySlot;
    [SerializeField] private SpriteRenderer bgBoard;

    [Header("Level")]
    [SerializeField] private LevelData currentLevel;

    [Header("Runtime Layout")]
    [SerializeField] private float columnSpacing = 0.9f;
    [SerializeField] private float minColumnGap = 0.15f;
    [SerializeField] private float slotSpacing = 1f;
    [SerializeField] private Vector2 slotScale = new Vector2(0.75f, 1f);
    [SerializeField] private Vector2 firstColumnPosition = new Vector2(-1.2f, 2.2f);
    [SerializeField] private Vector2 trayPosition = new Vector2(0f, -2.0f);
    [SerializeField] private bool fitColumnsToBoardLimit = true;
    [SerializeField] private Vector2 boardLimitCenter = new Vector2(0f, 0.6f);
    [SerializeField] private Vector2 boardLimitSize = new Vector2(4f, 5f);
    [SerializeField] private Vector2 bgBoardPadding = new Vector2(0.18f, 0.18f);
    [SerializeField] private int bgBoardSortingOrder = 0;

    [Header("Only Color Indicator")]
    [SerializeField] private Sprite onlyColorIndicatorSprite;
    [SerializeField] private Color onlyColorNeutralColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    [SerializeField] private Vector2 onlyColorIndicatorOffset = new Vector2(0f, 0.62f);
    [SerializeField] private Vector2 onlyColorIndicatorScale = new Vector2(0.22f, 0.22f);
    [SerializeField] private Sprite onlyColorStatusNormalSprite;
    [SerializeField] private Sprite onlyColorStatusSolvedSprite;
    [SerializeField] private Vector2 onlyColorStatusOffset = new Vector2(0.18f, 0.18f);
    [SerializeField] private Vector2 onlyColorStatusScale = new Vector2(0.45f, 0.45f);

    [Header("Prefabs")]
    [SerializeField] private GameObject redBlockPrefab;
    [SerializeField] private GameObject yellowBlockPrefab;
    [SerializeField] private GameObject greenBlockPrefab;
    [SerializeField] private GameObject whiteBlockPrefab;
    [SerializeField] private GameObject pinkBlockPrefab;
    [SerializeField] private GameObject blackBlockPrefab;
    [SerializeField] private GameObject brownBlockPrefab;
    [SerializeField] private GameObject purpleBlockPrefab;
    [SerializeField] private GameObject blueSkyBlockPrefab;
    [SerializeField] private GameObject rainbowBlockPrefab;
    [SerializeField] private GameObject blueBlockPrefab;
    [SerializeField] private GameObject orangeBlockPrefab;

    [Header("Freeze Counter")]
    [SerializeField] private Font freezeCounterFont;
    [SerializeField] private float freezeCounterCharacterSize = 0.22f;
    [SerializeField] private int freezeCounterFontSize = 48;
    [SerializeField] private Color freezeCounterColor = new Color32(0x63, 0x63, 0x63, 0xFF);

    [Header("Animation")]
    [SerializeField] private float trayToColumnDuration = 0.25f;
    [SerializeField] private float pushDuration = 0.225f;
    [SerializeField] private float returnToTrayDuration = 0.3125f;
    [SerializeField] private float shuffleDuration = 0.35f;
    [SerializeField] private float magnetLiftOffset = 0.18f;
    [SerializeField] private float magnetLiftDuration = 0.18f;
    [SerializeField] private float magnetFillDuration = 0.24f;
    [SerializeField] private float magnetDimAlpha = 0.55f;
    [SerializeField] private float winScaleMultiplier = 1.15f;
    [SerializeField] private float winScaleDuration = 0.16f;
    [SerializeField] private float winWaveDelay = 0.08f;

    [Header("Level 1 Tutorial")]
    [SerializeField] private bool enableLevelOneTutorial = true;
    [SerializeField] private GameObject tutorialHandPrefab;
    [SerializeField] private int[] levelOneTutorialColumnNumbers = { 2, 4, 3, 1 };
    [SerializeField] private Vector2 tutorialHandOffset = new Vector2(0.25f, 0.25f);
    [SerializeField] private int tutorialHandSortingOrder = 100;
    [SerializeField] private float tutorialHandMoveDuration = 0.35f;

    private readonly List<ColumnView> columns = new List<ColumnView>();
    private readonly List<bool> solvedColumnStates = new List<bool>();
    private readonly List<BlockView> bottomBlockStates = new List<BlockView>();
    private readonly Stack<MoveRecord> moveHistory = new Stack<MoveRecord>();
    private BlockView trayBlock;
    private bool isAnimating;
    private bool hasWon;
    private bool isMagnetMode;
    private bool suppressBottomCoverBurn;
    private readonly HashSet<BlockView> magnetCoverBurnTargets = new HashSet<BlockView>();
    private SpriteRenderer magnetOverlay;
    private MoveRecord pendingMoveRecord;
    private bool isInitialized;
    private bool solvedColumnStatesInitialized;
    private int maxMoves;
    private int remainingMoves;
    private GameObject tutorialHandInstance;
    private int tutorialStepIndex;
    private bool isLevelOneTutorialActive;

    public int MaxMoves => maxMoves;
    public int RemainingMoves => remainingMoves;

    private void Awake()
    {
        ResolveSceneReferences();
        ResolvePrefabReferences();
    }

    private void Start()
    {
        if (currentLevel != null)
        {
            InitializeFromManager();
        }
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryClickBoard(Mouse.current.position.ReadValue());
        }

        if (Touchscreen.current != null)
        {
            foreach (TouchControl touch in Touchscreen.current.touches)
            {
                if (touch.press.wasPressedThisFrame)
                {
                    TryClickBoard(touch.position.ReadValue());
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

        DestroyTutorialHand();
    }

    public void InitializeFromManager()
    {
        if (isInitialized)
        {
            return;
        }

        ResolveSceneReferences();
        ResolvePrefabReferences();
        SpawnInitialBoard();
        isInitialized = true;
    }

    public void InitializeFromManager(LevelData levelData)
    {
        currentLevel = levelData;
        ResolveSceneReferences();
        ResolvePrefabReferences();
        SpawnInitialBoard();
        isInitialized = true;
    }

    public void ClearFromManager()
    {
        ClearExistingBlocks();
        solvedColumnStates.Clear();
        bottomBlockStates.Clear();
        solvedColumnStatesInitialized = false;
        moveHistory.Clear();
        pendingMoveRecord = null;
        trayBlock = null;
        maxMoves = 0;
        remainingMoves = 0;
        hasWon = false;
        isAnimating = false;
        isMagnetMode = false;
        StopLevelOneTutorial();
        HideMagnetOverlay();
        NotifyGameplayPanelChanged();
    }

    private void ResolveSceneReferences()
    {
        if (boardRoot == null)
        {
            GameObject boardObject = GameObject.Find("BoardRoot");
            if (boardObject == null)
            {
                boardObject = new GameObject("BoardRoot");
            }

            boardRoot = boardObject.transform;
        }

        if (columnRoot == null)
        {
            Transform foundColumnRoot = boardRoot.Find("ColumnRoot");
            if (foundColumnRoot == null)
            {
                foundColumnRoot = new GameObject("ColumnRoot").transform;
                foundColumnRoot.SetParent(boardRoot, false);
            }

            columnRoot = foundColumnRoot;
        }

        if (traySlot == null)
        {
            Transform tray = boardRoot.Find("Tray");
            if (tray == null)
            {
                tray = new GameObject("Tray").transform;
                tray.SetParent(boardRoot, false);
            }

            Transform foundTraySlot = tray.Find("TraySlot");
            if (foundTraySlot == null)
            {
                foundTraySlot = new GameObject("TraySlot").transform;
                foundTraySlot.SetParent(tray, false);
            }

            traySlot = foundTraySlot;
        }
    }

    private void ResolvePrefabReferences()
    {
#if UNITY_EDITOR
        redBlockPrefab = redBlockPrefab != null ? redBlockPrefab : LoadEditorBlockPrefab(RedPrefabPath);
        yellowBlockPrefab = yellowBlockPrefab != null ? yellowBlockPrefab : LoadEditorBlockPrefab(YellowPrefabPath);
        greenBlockPrefab = greenBlockPrefab != null ? greenBlockPrefab : LoadEditorBlockPrefab(GreenPrefabPath);
        whiteBlockPrefab = whiteBlockPrefab != null ? whiteBlockPrefab : LoadEditorBlockPrefab(WhitePrefabPath);
        pinkBlockPrefab = pinkBlockPrefab != null ? pinkBlockPrefab : LoadEditorBlockPrefab(PinkPrefabPath);
        blackBlockPrefab = blackBlockPrefab != null ? blackBlockPrefab : LoadEditorBlockPrefab(BlackPrefabPath);
        brownBlockPrefab = brownBlockPrefab != null ? brownBlockPrefab : LoadEditorBlockPrefab(BrownPrefabPath);
        purpleBlockPrefab = purpleBlockPrefab != null ? purpleBlockPrefab : LoadEditorBlockPrefab(PurplePrefabPath);
        blueSkyBlockPrefab = blueSkyBlockPrefab != null ? blueSkyBlockPrefab : LoadEditorBlockPrefab(BlueSkyPrefabPath);
        rainbowBlockPrefab = rainbowBlockPrefab != null ? rainbowBlockPrefab : LoadEditorBlockPrefab(RainbowPrefabPath);
        blueBlockPrefab = blueBlockPrefab != null ? blueBlockPrefab : LoadEditorBlockPrefab(BluePrefabPath);
        orangeBlockPrefab = orangeBlockPrefab != null ? orangeBlockPrefab : LoadEditorBlockPrefab(OrangePrefabPath);
        tutorialHandPrefab = tutorialHandPrefab != null ? tutorialHandPrefab : LoadEditorBlockPrefab(TutorialHandPrefabPath);
#endif
    }

    private void SpawnInitialBoard()
    {
        if (currentLevel != null)
        {
            EnsureRuntimeBoardLayout(currentLevel);
        }

        if (columnRoot == null || traySlot == null)
        {
            Debug.LogWarning("BoardGameplay: Thiếu ColumnRoot hoặc TraySlot.");
            return;
        }

        ClearExistingBlocks();
        solvedColumnStates.Clear();
        bottomBlockStates.Clear();
        solvedColumnStatesInitialized = false;
        moveHistory.Clear();
        pendingMoveRecord = null;
        ResetMoveLimit();
        hasWon = false;
        isAnimating = false;
        isMagnetMode = false;
        HideMagnetOverlay();
        BuildColumns();

        if (currentLevel != null)
        {
            SpawnLevel(currentLevel);
            RefreshLockedColumns();
            NotifyGameplayPanelChanged();
            ConfigureLevelOneTutorial();
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
            ConfigureColumnAcceptedColor(column, false, BlockColorId.Red);
            List<BlockView> spawnedBlocks = new List<BlockView>();

            for (int slotIndex = 0; slotIndex < column.SlotCount; slotIndex++)
            {
                BlockColorId colorId = layout[slotIndex % layout.GetLength(0), columnIndex % layout.GetLength(1)];
                spawnedBlocks.Add(CreateBlock(colorId, column.Slots[slotIndex], false));
            }

            column.SetBlocks(spawnedBlocks);
        }

        trayBlock = CreateBlock(BlockColorId.White, traySlot, false);
        trayBlock.SnapToSlot(traySlot);

        FitBgBoardToColumns();
        RefreshLockedColumns();
        NotifyGameplayPanelChanged();
        ConfigureLevelOneTutorial();
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

    private void EnsureRuntimeBoardLayout(LevelData levelData)
    {
        ResolveSceneReferences();

        columnRoot.position = Vector3.zero;
        columnRoot.localScale = Vector3.one;
        ClearChildren(columnRoot);

        IReadOnlyList<LevelColumnData> levelColumns = levelData.Columns;
        float effectiveColumnSpacing = Mathf.Max(columnSpacing, Mathf.Abs(slotScale.x) + minColumnGap);
        for (int columnIndex = 0; columnIndex < levelColumns.Count; columnIndex++)
        {
            GameObject columnObject = new GameObject($"Column{columnIndex + 1}");
            columnObject.transform.SetParent(columnRoot, false);
            columnObject.transform.localPosition = new Vector3(
                firstColumnPosition.x + (columnIndex * effectiveColumnSpacing),
                firstColumnPosition.y,
                0f);
            columnObject.AddComponent<ColumnView>();

            int slotCount = levelColumns[columnIndex].Blocks.Count;
            for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
            {
                GameObject slotObject = new GameObject($"Slot{slotIndex}");
                slotObject.transform.SetParent(columnObject.transform, false);
                slotObject.transform.localPosition = new Vector3(0f, -slotIndex * slotSpacing, 0f);
                slotObject.transform.localScale = new Vector3(slotScale.x, slotScale.y, 1f);
            }
        }

        if (fitColumnsToBoardLimit)
        {
            FitColumnRootToBoardLimit();
        }

        FitBgBoardToColumns();

        Transform tray = traySlot.parent;
        if (tray != null)
        {
            tray.position = new Vector3(trayPosition.x, trayPosition.y, 0f);
        }

        traySlot.localPosition = Vector3.zero;
        float columnFitScale = Mathf.Abs(columnRoot.lossyScale.x);
        traySlot.localScale = new Vector3(slotScale.x * columnFitScale, slotScale.y * columnFitScale, 1f);
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }

    private void FitColumnRootToBoardLimit()
    {
        if (columnRoot.childCount == 0)
        {
            return;
        }

        Bounds limitBounds = GetBoardLimitBounds();
        Bounds columnBounds = CalculateColumnBounds(false);
        float limitWidth = Mathf.Max(0.01f, limitBounds.size.x);
        float limitHeight = Mathf.Max(0.01f, limitBounds.size.y);
        float scale = Mathf.Min(
            1f,
            limitWidth / Mathf.Max(0.01f, columnBounds.size.x),
            limitHeight / Mathf.Max(0.01f, columnBounds.size.y));

        columnRoot.localScale = new Vector3(scale, scale, 1f);

        Bounds scaledBounds = CalculateColumnBounds(false);
        Vector3 offset = limitBounds.center - scaledBounds.center;
        offset.z = 0f;
        columnRoot.position += offset;
    }

    private Bounds GetBoardLimitBounds()
    {
        Transform boardLimit = FindBoardLimitTransform();
        if (boardLimit != null)
        {
            SpriteRenderer spriteRenderer = boardLimit.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                return spriteRenderer.bounds;
            }

            Collider2D collider2D = boardLimit.GetComponent<Collider2D>();
            if (collider2D != null)
            {
                return collider2D.bounds;
            }

            return new Bounds(
                boardLimit.position,
                new Vector3(Mathf.Abs(boardLimit.lossyScale.x), Mathf.Abs(boardLimit.lossyScale.y), 0f));
        }

        return new Bounds(
            new Vector3(boardLimitCenter.x, boardLimitCenter.y, 0f),
            new Vector3(boardLimitSize.x, boardLimitSize.y, 0f));
    }

    private Transform FindBoardLimitTransform()
    {
        Transform exact = boardRoot != null ? boardRoot.Find("BoardLimit") : null;
        if (exact != null)
        {
            return exact;
        }

        GameObject boardLimit = GameObject.Find("BoardLimit");
        if (boardLimit != null)
        {
            return boardLimit.transform;
        }

        boardLimit = GameObject.Find("boardLimit");
        return boardLimit != null ? boardLimit.transform : null;
    }

    private void FitBgBoardToColumns()
    {
        ResolveBgBoard();
        if (bgBoard == null || columnRoot.childCount == 0)
        {
            return;
        }

        Bounds columnBounds = CalculateColumnBounds(true);
        Vector2 targetSize = new Vector2(
            Mathf.Max(0.01f, columnBounds.size.x + bgBoardPadding.x * 2f),
            Mathf.Max(0.01f, columnBounds.size.y + bgBoardPadding.y * 2f));

        Transform bgTransform = bgBoard.transform;
        Vector3 center = columnBounds.center;
        center.z = bgTransform.position.z;
        bgTransform.position = center;

        if (bgBoard.drawMode == SpriteDrawMode.Sliced || bgBoard.drawMode == SpriteDrawMode.Tiled)
        {
            bgBoard.size = targetSize;
            bgTransform.localScale = Vector3.one;
        }
        else if (bgBoard.sprite != null)
        {
            Vector2 spriteSize = bgBoard.sprite.bounds.size;
            Vector3 parentScale = bgTransform.parent != null ? bgTransform.parent.lossyScale : Vector3.one;
            float parentScaleX = Mathf.Approximately(parentScale.x, 0f) ? 1f : Mathf.Abs(parentScale.x);
            float parentScaleY = Mathf.Approximately(parentScale.y, 0f) ? 1f : Mathf.Abs(parentScale.y);
            bgTransform.localScale = new Vector3(
                targetSize.x / Mathf.Max(0.01f, spriteSize.x) / parentScaleX,
                targetSize.y / Mathf.Max(0.01f, spriteSize.y) / parentScaleY,
                bgTransform.localScale.z);
        }

        bgBoard.sortingOrder = bgBoardSortingOrder;
        bgBoard.gameObject.SetActive(true);
    }

    private void ResolveBgBoard()
    {
        if (bgBoard != null)
        {
            return;
        }

        Transform found = boardRoot != null ? boardRoot.Find("BGBoard") : null;
        if (found == null)
        {
            GameObject foundObject = GameObject.Find("BGBoard");
            found = foundObject != null ? foundObject.transform : null;
        }

        if (found != null)
        {
            bgBoard = found.GetComponent<SpriteRenderer>();
        }
    }

    private Bounds CalculateColumnBounds(bool includeIndicators = false)
    {
        bool hasBounds = false;
        Bounds bounds = new Bounds();

        foreach (Transform column in columnRoot)
        {
            foreach (Transform slot in column)
            {
                if (!slot.name.StartsWith("Slot"))
                {
                    continue;
                }

                Vector3 center = slot.position;
                Vector3 halfSize = new Vector3(
                    Mathf.Abs(slot.lossyScale.x) * 0.5f,
                    Mathf.Abs(slot.lossyScale.y) * 0.5f,
                    0f);
                Bounds slotBounds = new Bounds(center, Vector3.zero);
                slotBounds.Encapsulate(center - halfSize);
                slotBounds.Encapsulate(center + halfSize);

                if (!hasBounds)
                {
                    bounds = slotBounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(slotBounds);
                }
            }
        }

        if (includeIndicators)
        {
            SpriteRenderer[] renderers = columnRoot.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || !ShouldIncludeRendererInBoardBackground(renderers[i]))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderers[i].bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }
        }

        return hasBounds ? bounds : new Bounds(columnRoot.position, Vector3.one);
    }

    private bool ShouldIncludeRendererInBoardBackground(SpriteRenderer spriteRenderer)
    {
        string rendererName = spriteRenderer.name.ToLowerInvariant();
        return rendererName.Contains("onlycolorindicator")
            || rendererName.Contains("onlyindicator")
            || rendererName.Contains("onlycolorstatus")
            || rendererName.Contains("statusindicator");
    }

    private void HandleColumnClicked(ColumnView column)
    {
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null && gameManager.State != GameState.GamePlay)
        {
            return;
        }

        if (trayBlock == null || column.IsLocked || isAnimating || hasWon || remainingMoves <= 0 || !column.CanAccept(trayBlock.ColorId))
        {
            return;
        }

        if (!CanClickLevelOneTutorialColumn(column))
        {
            return;
        }

        BlockView incomingBlock = trayBlock;
        MoveRecord moveRecord = new MoveRecord
        {
            Column = column,
            PreviousBlocks = column.SnapshotBlocks(),
            PreviousTrayBlock = incomingBlock
        };
        pendingMoveRecord = moveRecord;
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
            moveRecord.CurrentTrayBlock = trayBlock;
            RefreshLockedColumns();

            if (!TryAutoPrepareBestColumn())
            {
                moveHistory.Push(moveRecord);
                pendingMoveRecord = null;
                isAnimating = false;
                CheckWinLog();
                CompletePlayerMove();
                AdvanceLevelOneTutorial();
            }
        });
    }

    public bool RollbackLastMove()
    {
        if (isAnimating || moveHistory.Count == 0)
        {
            return false;
        }

        MoveRecord moveRecord = moveHistory.Pop();
        hasWon = false;
        isAnimating = true;

        Sequence sequence = DOTween.Sequence();
        if (moveRecord.AutoPrepareRecord != null)
        {
            sequence.Append(moveRecord.AutoPrepareRecord.Column.ReverseSingleMismatchToBottomAnimated(
                moveRecord.AutoPrepareRecord.OriginalMismatchIndex,
                pushDuration));
        }

        sequence.Append(moveRecord.Column.ReversePushFromTrayAnimated(
            moveRecord.PreviousBlocks,
            moveRecord.PreviousTrayBlock,
            moveRecord.CurrentTrayBlock,
            traySlot,
            trayToColumnDuration,
            pushDuration,
            returnToTrayDuration));

        sequence.OnComplete(() =>
        {
            trayBlock = moveRecord.PreviousTrayBlock;
            isAnimating = false;
            RefreshLockedColumns();
            NotifyGameplayPanelChanged();
        });

        return true;
    }

    public bool ShuffleBlocks()
    {
        if (isAnimating || hasWon)
        {
            return false;
        }

        List<ColumnView> shuffleColumns = GetUnsolvedColumns(null);
        if (shuffleColumns.Count == 0)
        {
            return false;
        }

        ShuffleColumnsAnimated(shuffleColumns, true);
        return true;
    }

    public bool ActivateMagnetMode()
    {
        if (isAnimating || hasWon || isMagnetMode)
        {
            return false;
        }

        List<ColumnView> magnetColumns = GetMagnetAvailableColumns(null);
        if (magnetColumns.Count == 0)
        {
            return false;
        }

        isMagnetMode = true;
        isAnimating = true;
        ShowMagnetOverlay();

        Sequence sequence = DOTween.Sequence();
        for (int i = 0; i < magnetColumns.Count; i++)
        {
            sequence.Join(magnetColumns[i].SetMagnetRaised(true, magnetLiftOffset, magnetLiftDuration));
        }

        sequence.OnComplete(() =>
        {
            isAnimating = false;
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.WinLevel();
            }
        });

        return true;
    }

    private void TryClickBoard(Vector2 screenPosition)
    {
        if (isMagnetMode)
        {
            TryClickMagnetBlock(screenPosition);
            return;
        }

        TryClickColumn(screenPosition);
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
            if (!CanClickLevelOneTutorialColumn(column))
            {
                return;
            }

            column.TriggerClick();
        }
    }

    private void TryClickMagnetBlock(Vector2 screenPosition)
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
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPosition);
        BlockView selectedBlock = null;
        ColumnView selectedColumn = null;
        int bestSortingOrder = int.MinValue;

        for (int i = 0; i < hits.Length; i++)
        {
            BlockView block = hits[i].GetComponent<BlockView>();
            if (block == null)
            {
                block = hits[i].GetComponentInParent<BlockView>();
            }

            if (block == null)
            {
                continue;
            }

            ColumnView column = GetColumnContainingBlock(block);
            if (column == null || column.IsSolved() || !column.CanAccept(block.ColorId))
            {
                continue;
            }

            int sortingOrder = GetSortingOrder(block);
            if (sortingOrder <= bestSortingOrder)
            {
                continue;
            }

            selectedBlock = block;
            selectedColumn = column;
            bestSortingOrder = sortingOrder;
        }

        if (selectedBlock == null || selectedColumn == null)
        {
            return;
        }

        ApplyMagnetSelection(selectedColumn, selectedBlock);
    }

    private void ApplyMagnetSelection(ColumnView selectedColumn, BlockView selectedBlock)
    {
        List<ColumnView> unsolvedColumns = GetMagnetAvailableColumns(null);
        List<ColumnView> shuffleColumns = GetMagnetAvailableColumns(selectedColumn);
        List<BlockView> boardBlocks = CollectBlocks(unsolvedColumns);
        List<BlockView> availableBlocks = new List<BlockView>(boardBlocks);
        if (trayBlock != null && trayBlock.ColorId == selectedBlock.ColorId)
        {
            availableBlocks.Add(trayBlock);
        }

        List<BlockView> selectedColumnBlocks = PickMagnetColumnBlocks(
            availableBlocks,
            selectedBlock,
            selectedColumn.SlotCount);

        if (selectedColumnBlocks == null)
        {
            Debug.LogWarning($"BoardGameplay: Không đủ block màu {selectedBlock.ColorId} để lắp full column.");
            CancelMagnetMode();
            return;
        }

        bool usesTrayBlock = selectedColumnBlocks.Contains(trayBlock);
        List<BlockView> remainingBlocks = new List<BlockView>(boardBlocks);
        for (int i = 0; i < selectedColumnBlocks.Count; i++)
        {
            remainingBlocks.Remove(selectedColumnBlocks[i]);
        }

        BlockView replacementTrayBlock = null;
        if (usesTrayBlock && !TryPickReplacementTrayBlock(remainingBlocks, selectedBlock.ColorId, out replacementTrayBlock))
        {
            Debug.LogWarning($"BoardGameplay: Không tìm thấy block khác màu {selectedBlock.ColorId} để thay vào tray.");
            CancelMagnetMode();
            return;
        }

        if (replacementTrayBlock != null)
        {
            remainingBlocks.Remove(replacementTrayBlock);
        }

        Dictionary<ColumnView, List<BlockView>> shuffledLayout = BuildMagnetShuffledLayout(shuffleColumns, remainingBlocks);
        if (shuffledLayout == null)
        {
            Debug.LogWarning("BoardGameplay: Không thể magnet vì rule màu của column không đủ block hợp lệ.");
            CancelMagnetMode();
            return;
        }

        isMagnetMode = false;
        isAnimating = true;
        moveHistory.Clear();
        pendingMoveRecord = null;
        HideMagnetOverlay();

        Sequence sequence = DOTween.Sequence();
        for (int i = 0; i < columns.Count; i++)
        {
            if (columns[i].IsSolved())
            {
                continue;
            }

            sequence.Join(columns[i].SetMagnetRaised(false, magnetLiftOffset, magnetLiftDuration));
        }

        if (replacementTrayBlock != null)
        {
            sequence.Append(MoveBlockToTrayAnimated(replacementTrayBlock, magnetFillDuration));
        }

        BlockColorId magnetColor = selectedBlock.ColorId;
        magnetCoverBurnTargets.Clear();
        for (int i = 0; i < selectedColumnBlocks.Count; i++)
        {
            if (selectedColumnBlocks[i] != null && selectedColumnBlocks[i].ColorId == magnetColor)
            {
                magnetCoverBurnTargets.Add(selectedColumnBlocks[i]);
            }
        }

        suppressBottomCoverBurn = true;
        sequence.Append(selectedColumn.ReplaceBlocksAnimated(selectedColumnBlocks, magnetFillDuration));
        sequence.AppendCallback(BurnMagnetCoverTargets);
        sequence.AppendCallback(RefreshLockedColumns);
        sequence.Append(BuildApplyShuffledLayoutSequence(shuffledLayout));
        sequence.OnComplete(() =>
        {
            isAnimating = false;
            RefreshLockedColumns();
            suppressBottomCoverBurn = false;
            magnetCoverBurnTargets.Clear();
            CheckWinLog();
            NotifyGameplayPanelChanged();
        });
    }

    private void CancelMagnetMode()
    {
        isMagnetMode = false;
        isAnimating = true;
        HideMagnetOverlay();

        List<ColumnView> unsolvedColumns = GetMagnetAvailableColumns(null);
        Sequence sequence = DOTween.Sequence();
        for (int i = 0; i < unsolvedColumns.Count; i++)
        {
            sequence.Join(unsolvedColumns[i].SetMagnetRaised(false, magnetLiftOffset, magnetLiftDuration));
        }

        sequence.OnComplete(() =>
        {
            isAnimating = false;
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.WinLevel(this);
            }
        });
    }

    private void SpawnLevel(LevelData levelData)
    {
        IReadOnlyList<LevelColumnData> levelColumns = levelData.Columns;

        for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
        {
            ColumnView column = columns[columnIndex];
            ConfigureColumnAcceptedColor(column, false, BlockColorId.Red);
            ConfigureColumnFreeze(column, 0);

            if (columnIndex >= levelColumns.Count)
            {
                Debug.LogWarning($"BoardGameplay: Scene có nhiều column hơn level data tại index {columnIndex}.");
                continue;
            }

            LevelColumnData columnData = levelColumns[columnIndex];
            ConfigureColumnAcceptedColor(column, columnData.HasAcceptedColor, columnData.AcceptedColor);
            ConfigureColumnFreeze(column, columnData.FreezeUnlockColumnCount);

            List<BlockView> spawnedBlocks = new List<BlockView>();
            int spawnCount = Mathf.Min(column.SlotCount, columnData.Blocks.Count);

            for (int slotIndex = 0; slotIndex < spawnCount; slotIndex++)
            {
                spawnedBlocks.Add(CreateBlock(
                    columnData.Blocks[slotIndex],
                    column.Slots[slotIndex],
                    columnData.IsBlockCovered(slotIndex)));
            }

            if (spawnCount != column.SlotCount)
            {
                Debug.LogWarning($"BoardGameplay: Column {columnIndex} có {column.SlotCount} slot nhưng level có {columnData.Blocks.Count} block.");
            }

            column.SetBlocks(spawnedBlocks);
        }

        trayBlock = CreateBlock(levelData.TrayColor, traySlot, false);
        trayBlock.SnapToSlot(traySlot);

        FitBgBoardToColumns();
    }

    private void ConfigureColumnAcceptedColor(ColumnView column, bool hasAcceptedColor, BlockColorId acceptedColor)
    {
        column.SetAcceptedColorRule(hasAcceptedColor, acceptedColor);
        Color indicatorColor = hasAcceptedColor ? GetIndicatorColor(acceptedColor) : onlyColorNeutralColor;
        column.SetOnlyColorIndicator(
            onlyColorIndicatorSprite,
            indicatorColor,
            onlyColorIndicatorOffset,
            onlyColorIndicatorScale,
            onlyColorStatusNormalSprite,
            onlyColorStatusOffset,
            onlyColorStatusScale);
    }

    private void ConfigureColumnFreeze(ColumnView column, int unlockColumnCount)
    {
        freezeCounterColor = FreezeCounterTextColor;
        column.ConfigureFreezeLock(
            unlockColumnCount,
            freezeCounterFont,
            freezeCounterCharacterSize,
            freezeCounterFontSize,
            freezeCounterColor);
    }

    private void ShuffleColumnsAnimated(List<ColumnView> shuffleColumns, bool clearHistory)
    {
        if (shuffleColumns.Count == 0)
        {
            return;
        }

        if (clearHistory)
        {
            moveHistory.Clear();
            pendingMoveRecord = null;
            hasWon = false;
        }

        isAnimating = true;
        Sequence sequence = BuildShuffleSequence(shuffleColumns);
        sequence.OnComplete(() =>
        {
            isAnimating = false;
            RefreshLockedColumns();
            CheckWinLog();
            NotifyGameplayPanelChanged();
        });
    }

    private Sequence BuildShuffleSequence(List<ColumnView> shuffleColumns)
    {
        Sequence sequence = DOTween.Sequence();
        List<BlockView> shuffleBlocks = new List<BlockView>();

        for (int i = 0; i < shuffleColumns.Count; i++)
        {
            shuffleBlocks.AddRange(shuffleColumns[i].Blocks);
        }

        if (shuffleBlocks.Count == 0)
        {
            return sequence;
        }

        ShuffleList(shuffleBlocks);

        Dictionary<ColumnView, List<BlockView>> shuffledLayout = BuildShuffledLayout(shuffleColumns, shuffleBlocks);
        if (shuffledLayout == null)
        {
            Debug.LogWarning("BoardGameplay: Không thể shuffle vì rule màu của column không đủ block hợp lệ.");
            return sequence;
        }

        for (int i = 0; i < shuffleColumns.Count; i++)
        {
            ColumnView column = shuffleColumns[i];
            sequence.Join(column.ReplaceBlocksAnimated(shuffledLayout[column], shuffleDuration));
        }

        return sequence;
    }

    private Sequence BuildApplyShuffledLayoutSequence(Dictionary<ColumnView, List<BlockView>> shuffledLayout)
    {
        Sequence sequence = DOTween.Sequence();

        foreach (KeyValuePair<ColumnView, List<BlockView>> layoutPair in shuffledLayout)
        {
            sequence.Join(layoutPair.Key.ReplaceBlocksAnimated(layoutPair.Value, shuffleDuration));
        }

        return sequence;
    }

    private List<BlockView> CollectBlocks(List<ColumnView> sourceColumns)
    {
        List<BlockView> collectedBlocks = new List<BlockView>();

        for (int i = 0; i < sourceColumns.Count; i++)
        {
            collectedBlocks.AddRange(sourceColumns[i].Blocks);
        }

        return collectedBlocks;
    }

    private List<BlockView> PickMagnetColumnBlocks(List<BlockView> availableBlocks, BlockView selectedBlock, int requiredCount)
    {
        List<BlockView> pickedBlocks = new List<BlockView>();
        pickedBlocks.Add(selectedBlock);

        for (int i = 0; i < availableBlocks.Count && pickedBlocks.Count < requiredCount; i++)
        {
            BlockView block = availableBlocks[i];
            if (block == selectedBlock || block.ColorId != selectedBlock.ColorId)
            {
                continue;
            }

            pickedBlocks.Add(block);
        }

        return pickedBlocks.Count == requiredCount ? pickedBlocks : null;
    }

    private bool TryPickReplacementTrayBlock(List<BlockView> remainingBlocks, BlockColorId excludedColor, out BlockView replacementBlock)
    {
        List<BlockView> candidates = new List<BlockView>();

        for (int i = 0; i < remainingBlocks.Count; i++)
        {
            if (remainingBlocks[i].ColorId != excludedColor)
            {
                candidates.Add(remainingBlocks[i]);
            }
        }

        if (candidates.Count == 0)
        {
            replacementBlock = null;
            return false;
        }

        replacementBlock = candidates[Random.Range(0, candidates.Count)];
        return true;
    }

    private Sequence MoveBlockToTrayAnimated(BlockView block, float duration)
    {
        Sequence sequence = DOTween.Sequence();
        block.transform.SetParent(traySlot, true);
        block.SetSortingOrder(55);
        sequence.Append(block.transform.DOMove(block.GetSlotPosition(traySlot), duration).SetEase(Ease.InOutQuad));
        sequence.AppendCallback(() =>
        {
            trayBlock = block;
            block.SetSortingOrder(10);
            block.SnapToSlot(traySlot);
        });

        return sequence;
    }

    private List<ColumnView> GetUnsolvedColumns(ColumnView excludedColumn)
    {
        List<ColumnView> unsolvedColumns = new List<ColumnView>();

        for (int i = 0; i < columns.Count; i++)
        {
            if (columns[i] == excludedColumn || columns[i].IsSolved())
            {
                continue;
            }

            unsolvedColumns.Add(columns[i]);
        }

        return unsolvedColumns;
    }

    private List<ColumnView> GetMagnetAvailableColumns(ColumnView excludedColumn)
    {
        List<ColumnView> availableColumns = new List<ColumnView>();

        for (int i = 0; i < columns.Count; i++)
        {
            if (columns[i] == excludedColumn || columns[i].IsSolved() || columns[i].IsFreezeLocked)
            {
                continue;
            }

            availableColumns.Add(columns[i]);
        }

        return availableColumns;
    }

    private ColumnView GetColumnContainingBlock(BlockView block)
    {
        for (int i = 0; i < columns.Count; i++)
        {
            if (columns[i].ContainsBlock(block))
            {
                return columns[i];
            }
        }

        return null;
    }

    private int GetSortingOrder(BlockView block)
    {
        SpriteRenderer spriteRenderer = block.GetComponent<SpriteRenderer>();
        return spriteRenderer != null ? spriteRenderer.sortingOrder : 0;
    }

    private void ShowMagnetOverlay()
    {
        if (magnetOverlay == null)
        {
            GameObject overlayObject = new GameObject("MagnetDimOverlay");
            overlayObject.transform.SetParent(boardRoot, false);

            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            magnetOverlay = overlayObject.AddComponent<SpriteRenderer>();
            magnetOverlay.sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            magnetOverlay.sortingOrder = 35;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            float height = mainCamera.orthographicSize * 2f;
            float width = height * mainCamera.aspect;
            Vector3 cameraPosition = mainCamera.transform.position;
            magnetOverlay.transform.position = new Vector3(cameraPosition.x, cameraPosition.y, 0f);
            magnetOverlay.transform.localScale = new Vector3(width, height, 1f);
        }

        magnetOverlay.color = new Color(0f, 0f, 0f, magnetDimAlpha);
        magnetOverlay.enabled = true;
    }

    private void HideMagnetOverlay()
    {
        if (magnetOverlay != null)
        {
            magnetOverlay.enabled = false;
        }
    }

    public bool IsBoardSolved()
    {
        if (currentLevel != null && columns.Count != currentLevel.Columns.Count)
        {
            return false;
        }

        for (int i = 0; i < columns.Count; i++)
        {
            if (!columns[i].IsSolved())
            {
                return false;
            }
        }

        return columns.Count > 0;
    }

    private bool IsWin()
    {
        return IsBoardSolved();
    }

    private void RefreshLockedColumns()
    {
        EnsureSolvedColumnStateSize();
        EnsureBottomBlockStateSize();
        int solvedUnlockColumnCount = CountSolvedUnlockColumns();
        bool canPlaySolvedWave = solvedColumnStatesInitialized && !IsBoardSolved();

        for (int i = 0; i < columns.Count; i++)
        {
            columns[i].UpdateFreezeLock(solvedUnlockColumnCount);
            bool isSolved = columns[i].IsSolved();
            columns[i].SetLocked(isSolved || columns[i].IsFreezeLocked);
            columns[i].SetOnlyColorStatusSolved(
                isSolved,
                onlyColorStatusNormalSprite,
                onlyColorStatusSolvedSprite,
                onlyColorStatusOffset,
                onlyColorStatusScale);

            if (canPlaySolvedWave && isSolved && !solvedColumnStates[i])
            {
                PlayColumnSolvedWave(columns[i]);
            }

            solvedColumnStates[i] = isSolved;
            CheckBottomCoverBurn(i);
        }

        solvedColumnStatesInitialized = true;
    }

    private int CountSolvedUnlockColumns()
    {
        int count = 0;
        for (int i = 0; i < columns.Count; i++)
        {
            if (columns[i].IsSolved() && !columns[i].IsFreezeLocked)
            {
                count++;
            }
        }

        return count;
    }

    private void EnsureSolvedColumnStateSize()
    {
        while (solvedColumnStates.Count < columns.Count)
        {
            solvedColumnStates.Add(false);
        }

        while (solvedColumnStates.Count > columns.Count)
        {
            solvedColumnStates.RemoveAt(solvedColumnStates.Count - 1);
        }
    }

    private void EnsureBottomBlockStateSize()
    {
        while (bottomBlockStates.Count < columns.Count)
        {
            bottomBlockStates.Add(null);
        }

        while (bottomBlockStates.Count > columns.Count)
        {
            bottomBlockStates.RemoveAt(bottomBlockStates.Count - 1);
        }
    }

    private void CheckBottomCoverBurn(int columnIndex)
    {
        IReadOnlyList<BlockView> blocks = columns[columnIndex].Blocks;
        BlockView bottomBlock = blocks.Count > 0 ? blocks[blocks.Count - 1] : null;
        BlockView previousBottomBlock = bottomBlockStates[columnIndex];

        if (!suppressBottomCoverBurn && solvedColumnStatesInitialized && bottomBlock != null && bottomBlock != previousBottomBlock)
        {
            bottomBlock.BurnCoverTopToBottom();
        }

        bottomBlockStates[columnIndex] = bottomBlock;
    }

    private void BurnMagnetCoverTargets()
    {
        foreach (BlockView block in magnetCoverBurnTargets)
        {
            if (block != null)
            {
                block.BurnCoverTopToBottom();
            }
        }
    }

    private void PlayColumnSolvedWave(ColumnView column)
    {
        if (AudioManager.IsVibrationOn())
        {
            CandyCoded.HapticFeedback.HapticFeedback.LightFeedback();
        }

        IReadOnlyList<BlockView> blocks = column.Blocks;
        for (int i = 0; i < blocks.Count; i++)
        {
            Transform blockTransform = blocks[i].transform;
            blockTransform.DOKill();
            blockTransform.localScale = Vector3.one;

            Vector3 targetScale = Vector3.one * winScaleMultiplier;
            float delay = i * winWaveDelay;
            blocks[i].PlaySolvedSpineAnimation(delay);
            blockTransform.DOScale(targetScale, winScaleDuration)
                .SetDelay(delay)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    blockTransform.DOScale(Vector3.one, winScaleDuration).SetEase(Ease.InQuad);
                });
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

        AutoPrepareRecord autoPrepareRecord = null;
        if (pendingMoveRecord != null && bestColumn.TryGetSingleMismatchIndex(trayBlock.ColorId, out int originalMismatchIndex))
        {
            autoPrepareRecord = new AutoPrepareRecord
            {
                Column = bestColumn,
                OriginalMismatchIndex = originalMismatchIndex
            };
            pendingMoveRecord.AutoPrepareRecord = autoPrepareRecord;
        }

        isAnimating = true;
        Sequence sequence = bestColumn.PrepareSingleMismatchToBottomAnimated(trayBlock.ColorId, pushDuration);
        sequence.OnComplete(() =>
        {
            RefreshLockedColumns();
            if (pendingMoveRecord != null)
            {
                moveHistory.Push(pendingMoveRecord);
                pendingMoveRecord = null;
            }

            isAnimating = false;
            CheckWinLog();
            CompletePlayerMove();
            AdvanceLevelOneTutorial();
        });

        return true;
    }

    private void ConfigureLevelOneTutorial()
    {
        StopLevelOneTutorial();

        if (!ShouldRunLevelOneTutorial())
        {
            return;
        }

        isLevelOneTutorialActive = true;
        tutorialStepIndex = 0;
        EnsureTutorialHand();
        MoveTutorialHandToCurrentColumn();
    }

    private bool ShouldRunLevelOneTutorial()
    {
        if (!enableLevelOneTutorial || columns.Count == 0 || levelOneTutorialColumnNumbers == null || levelOneTutorialColumnNumbers.Length == 0)
        {
            return false;
        }

        LevelManager levelManager = LevelManager.instance != null ? LevelManager.Instance : FindFirstObjectByType<LevelManager>();
        if (levelManager == null || levelManager.CurrentNormalLevelNumber != 1)
        {
            return false;
        }

        for (int i = 0; i < levelOneTutorialColumnNumbers.Length; i++)
        {
            int columnIndex = levelOneTutorialColumnNumbers[i] - 1;
            if (columnIndex < 0 || columnIndex >= columns.Count)
            {
                Debug.LogWarning($"BoardGameplay: Tutorial level 1 có cột {levelOneTutorialColumnNumbers[i]} nhưng board chỉ có {columns.Count} cột.");
                return false;
            }
        }

        return true;
    }

    private bool CanClickLevelOneTutorialColumn(ColumnView column)
    {
        if (!isLevelOneTutorialActive)
        {
            return true;
        }

        int targetColumnIndex = GetCurrentTutorialColumnIndex();
        return targetColumnIndex >= 0 && targetColumnIndex < columns.Count && columns[targetColumnIndex] == column;
    }

    private int GetCurrentTutorialColumnIndex()
    {
        if (!isLevelOneTutorialActive
            || levelOneTutorialColumnNumbers == null
            || tutorialStepIndex < 0
            || tutorialStepIndex >= levelOneTutorialColumnNumbers.Length)
        {
            return -1;
        }

        return levelOneTutorialColumnNumbers[tutorialStepIndex] - 1;
    }

    private void AdvanceLevelOneTutorial()
    {
        if (!isLevelOneTutorialActive)
        {
            return;
        }

        tutorialStepIndex++;
        if (tutorialStepIndex >= levelOneTutorialColumnNumbers.Length)
        {
            StopLevelOneTutorial();
            return;
        }

        MoveTutorialHandToCurrentColumn();
    }

    private void StopLevelOneTutorial()
    {
        isLevelOneTutorialActive = false;
        tutorialStepIndex = 0;

        if (tutorialHandInstance != null)
        {
            tutorialHandInstance.transform.DOKill();
            tutorialHandInstance.SetActive(false);
        }
    }

    private void EnsureTutorialHand()
    {
        if (tutorialHandInstance != null || tutorialHandPrefab == null)
        {
            return;
        }

        Transform parent = boardRoot != null ? boardRoot : transform;
        tutorialHandInstance = Instantiate(tutorialHandPrefab, parent);
        SetTutorialHandSortingOrder();
    }

    private void MoveTutorialHandToCurrentColumn()
    {
        EnsureTutorialHand();
        if (tutorialHandInstance == null)
        {
            return;
        }

        int columnIndex = GetCurrentTutorialColumnIndex();
        if (columnIndex < 0 || columnIndex >= columns.Count)
        {
            StopLevelOneTutorial();
            return;
        }

        tutorialHandInstance.SetActive(true);
        Vector3 targetPosition = GetTutorialHandPosition(columns[columnIndex]);
        tutorialHandInstance.transform.DOKill();

        if (tutorialStepIndex <= 0)
        {
            tutorialHandInstance.transform.position = targetPosition;
        }
        else
        {
            tutorialHandInstance.transform
                .DOMove(targetPosition, tutorialHandMoveDuration)
                .SetEase(Ease.InOutSine);
        }

        SetTutorialHandSortingOrder();
    }

    private Vector3 GetTutorialHandPosition(ColumnView column)
    {
        if (column != null && column.Slots.Count > 0 && column.Slots[0] != null)
        {
            return column.Slots[0].position + new Vector3(tutorialHandOffset.x, tutorialHandOffset.y, 0f);
        }

        return column != null
            ? column.transform.position + new Vector3(tutorialHandOffset.x, tutorialHandOffset.y, 0f)
            : transform.position;
    }

    private void SetTutorialHandSortingOrder()
    {
        if (tutorialHandInstance == null)
        {
            return;
        }

        SpriteRenderer[] renderers = tutorialHandInstance.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingOrder = tutorialHandSortingOrder;
        }
    }

    private void DestroyTutorialHand()
    {
        if (tutorialHandInstance == null)
        {
            return;
        }

        Destroy(tutorialHandInstance);
        tutorialHandInstance = null;
    }

    private void ResetMoveLimit()
    {
        maxMoves = currentLevel != null ? currentLevel.MaxMoves : 20;
        remainingMoves = maxMoves;
    }

    private void CompletePlayerMove()
    {
        if (maxMoves <= 0 || remainingMoves <= 0)
        {
            NotifyGameplayPanelChanged();
            return;
        }

        remainingMoves = Mathf.Max(0, remainingMoves - 1);
        NotifyGameplayPanelChanged();

        if (remainingMoves > 0 || hasWon || IsBoardSolved())
        {
            return;
        }

        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.LoseLevel();
        }
    }

    private void NotifyGameplayPanelChanged()
    {
        PanelGameplay panelGameplay = FindFirstObjectByType<PanelGameplay>(FindObjectsInactive.Include);
        if (panelGameplay != null)
        {
            panelGameplay.RefreshLevelMoveInfo();
        }
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
        if (AudioManager.IsVibrationOn())
        {
            CandyCoded.HapticFeedback.HapticFeedback.LightFeedback();
        }

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
                blockTransform.DOKill();
                blockTransform.localScale = Vector3.one;
                Vector3 startScale = Vector3.one;
                Vector3 targetScale = Vector3.one * winScaleMultiplier;
                float delay = (slotIndex * winWaveDelay) + (columnIndex * winWaveDelay * 0.35f);

                blocks[slotIndex].PlaySolvedSpineAnimation(delay);
                sequence.Insert(delay, blockTransform.DOScale(targetScale, winScaleDuration).SetEase(Ease.OutQuad));
                sequence.Insert(delay + winScaleDuration, blockTransform.DOScale(startScale, winScaleDuration).SetEase(Ease.InQuad));
            }
        }

        sequence.OnComplete(() =>
        {
            isAnimating = false;
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.WinLevel(this);
            }
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

    private Dictionary<ColumnView, List<BlockView>> BuildShuffledLayout(List<ColumnView> shuffleColumns, List<BlockView> shuffledBlocks)
    {
        Dictionary<ColumnView, List<BlockView>> layout = new Dictionary<ColumnView, List<BlockView>>();

        for (int i = 0; i < shuffleColumns.Count; i++)
        {
            layout.Add(shuffleColumns[i], new List<BlockView>());
        }

        for (int columnIndex = 0; columnIndex < shuffleColumns.Count; columnIndex++)
        {
            ColumnView column = shuffleColumns[columnIndex];
            for (int slotIndex = 0; slotIndex < column.SlotCount; slotIndex++)
            {
                int blockIndex = FindAllowedBlockIndex(shuffledBlocks, column);
                if (blockIndex < 0)
                {
                    return null;
                }

                layout[column].Add(shuffledBlocks[blockIndex]);
                shuffledBlocks.RemoveAt(blockIndex);
            }
        }

        return layout;
    }

    private Dictionary<ColumnView, List<BlockView>> BuildMagnetShuffledLayout(List<ColumnView> shuffleColumns, List<BlockView> remainingBlocks)
    {
        if (shuffleColumns.Count == 0)
        {
            return new Dictionary<ColumnView, List<BlockView>>();
        }

        Dictionary<ColumnView, List<BlockView>> fallbackLayout = null;
        const int maxAttempts = 30;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            List<BlockView> shuffledBlocks = new List<BlockView>(remainingBlocks);
            ShuffleList(shuffledBlocks);

            Dictionary<ColumnView, List<BlockView>> layout = BuildShuffledLayout(shuffleColumns, shuffledBlocks);
            if (layout == null)
            {
                continue;
            }

            if (fallbackLayout == null)
            {
                fallbackLayout = layout;
            }

            if (!DoesLayoutSolveAllColumns(layout))
            {
                return layout;
            }

            if (TryBreakSolvedLayout(layout))
            {
                return layout;
            }
        }

        if (fallbackLayout != null && TryBreakSolvedLayout(fallbackLayout))
        {
            return fallbackLayout;
        }

        return fallbackLayout;
    }

    private bool DoesLayoutSolveAllColumns(Dictionary<ColumnView, List<BlockView>> layout)
    {
        foreach (KeyValuePair<ColumnView, List<BlockView>> pair in layout)
        {
            if (!WouldColumnBeSolved(pair.Key, pair.Value))
            {
                return false;
            }
        }

        return layout.Count > 0;
    }

    private bool WouldColumnBeSolved(ColumnView column, List<BlockView> targetBlocks)
    {
        if (targetBlocks.Count == 0 || targetBlocks.Count != column.SlotCount)
        {
            return false;
        }

        BlockColorId firstColor = targetBlocks[0].ColorId;
        for (int i = 1; i < targetBlocks.Count; i++)
        {
            if (targetBlocks[i].ColorId != firstColor)
            {
                return false;
            }
        }

        return true;
    }

    private bool TryBreakSolvedLayout(Dictionary<ColumnView, List<BlockView>> layout)
    {
        ColumnView firstColumn = null;
        ColumnView secondColumn = null;

        foreach (KeyValuePair<ColumnView, List<BlockView>> pair in layout)
        {
            if (!WouldColumnBeSolved(pair.Key, pair.Value))
            {
                return true;
            }

            if (pair.Value.Count == 0)
            {
                continue;
            }

            if (firstColumn == null)
            {
                firstColumn = pair.Key;
                continue;
            }

            BlockView firstSwapBlock = layout[firstColumn][layout[firstColumn].Count - 1];
            BlockView secondSwapBlock = pair.Value[pair.Value.Count - 1];
            if (layout[firstColumn][0].ColorId != pair.Value[0].ColorId
                && firstColumn.CanAccept(secondSwapBlock.ColorId)
                && pair.Key.CanAccept(firstSwapBlock.ColorId))
            {
                secondColumn = pair.Key;
                break;
            }
        }

        if (firstColumn == null || secondColumn == null)
        {
            return false;
        }

        List<BlockView> firstBlocks = layout[firstColumn];
        List<BlockView> secondBlocks = layout[secondColumn];
        if (firstBlocks.Count == 0 || secondBlocks.Count == 0)
        {
            return false;
        }

        BlockView temporaryBlock = firstBlocks[firstBlocks.Count - 1];
        firstBlocks[firstBlocks.Count - 1] = secondBlocks[secondBlocks.Count - 1];
        secondBlocks[secondBlocks.Count - 1] = temporaryBlock;
        return true;
    }

    private int FindAllowedBlockIndex(List<BlockView> blocksToPlace, ColumnView targetColumn)
    {
        List<int> allowedIndices = new List<int>();

        for (int i = 0; i < blocksToPlace.Count; i++)
        {
            if (targetColumn.CanAccept(blocksToPlace[i].ColorId))
            {
                allowedIndices.Add(i);
            }
        }

        if (allowedIndices.Count == 0)
        {
            return -1;
        }

        return allowedIndices[Random.Range(0, allowedIndices.Count)];
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            T temporaryValue = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temporaryValue;
        }
    }

    private BlockView CreateBlock(BlockColorId colorId, Transform parentSlot, bool hasCover)
    {
        GameObject prefab = GetPrefab(colorId);
        GameObject blockObject = Instantiate(prefab, parentSlot.position, Quaternion.identity, parentSlot);
        BlockView block = blockObject.GetComponent<BlockView>();
        if (block == null)
        {
            block = blockObject.AddComponent<BlockView>();
        }

        block.name = $"Block_{colorId}";
        block.SetColorId(colorId);
        block.SetCoverActive(hasCover);
        block.SnapToSlot(parentSlot);
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
            case BlockColorId.Pink:
                return pinkBlockPrefab != null ? pinkBlockPrefab : redBlockPrefab;
            case BlockColorId.Black:
                return blackBlockPrefab != null ? blackBlockPrefab : redBlockPrefab;
            case BlockColorId.Brown:
                return brownBlockPrefab != null ? brownBlockPrefab : redBlockPrefab;
            case BlockColorId.Purple:
                return purpleBlockPrefab != null ? purpleBlockPrefab : redBlockPrefab;
            case BlockColorId.BlueSky:
                return blueSkyBlockPrefab != null ? blueSkyBlockPrefab : redBlockPrefab;
            case BlockColorId.RainBow:
                return rainbowBlockPrefab != null ? rainbowBlockPrefab : redBlockPrefab;
            case BlockColorId.Blue:
                return blueBlockPrefab != null ? blueBlockPrefab : redBlockPrefab;
            case BlockColorId.Orange:
                return orangeBlockPrefab != null ? orangeBlockPrefab : redBlockPrefab;
            default:
                return redBlockPrefab;
        }
    }

    private Color GetIndicatorColor(BlockColorId colorId)
    {
        switch (colorId)
        {
            case BlockColorId.Red:
                return new Color(1f, 0.12f, 0.02f, 1f);
            case BlockColorId.Yellow:
                return new Color(1f, 0.9f, 0f, 1f);
            case BlockColorId.Green:
                return new Color(0f, 0.95f, 0.12f, 1f);
            case BlockColorId.White:
                return Color.white;
            case BlockColorId.Pink:
                return new Color(1f, 0.35f, 0.75f, 1f);
            case BlockColorId.Black:
                return Color.black;
            case BlockColorId.Brown:
                return new Color(0.45f, 0.22f, 0.08f, 1f);
            case BlockColorId.Purple:
                return new Color(0.45f, 0.15f, 0.9f, 1f);
            case BlockColorId.BlueSky:
                return new Color(0.2f, 0.75f, 1f, 1f);
            case BlockColorId.RainBow:
                return Color.magenta;
            case BlockColorId.Blue:
                return new Color(0.05f, 0.25f, 1f, 1f);
            case BlockColorId.Orange:
                return new Color(1f, 0.45f, 0f, 1f);
            default:
                return onlyColorNeutralColor;
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

    private class MoveRecord
    {
        public ColumnView Column;
        public List<BlockView> PreviousBlocks;
        public BlockView PreviousTrayBlock;
        public BlockView CurrentTrayBlock;
        public AutoPrepareRecord AutoPrepareRecord;
    }

    private class AutoPrepareRecord
    {
        public ColumnView Column;
        public int OriginalMismatchIndex;
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
