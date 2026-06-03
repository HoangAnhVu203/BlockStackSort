using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LevelSetupWindow : EditorWindow
{
    private const string DefaultLevelFolder = "Assets/_Game/Levels";

    private LevelData levelData;
    private int maxMoves = 20;
    private int columnCount = 3;
    private int slotCount = 5;
    private float columnSpacing = 0.9f;
    private float minColumnGap = 0.15f;
    private float slotSpacing = 1f;
    private Vector2 slotScale = new Vector2(0.75f, 1f);
    private Vector2 firstColumnPosition = new Vector2(-1.2f, 2.2f);
    private Vector2 trayPosition = new Vector2(0f, -2.8f);
    private bool fitColumnsToBoardLimit = true;
    private Transform boardLimitTransform;
    private Vector2 boardLimitCenter = new Vector2(0f, 0.6f);
    private Vector2 boardLimitSize = new Vector2(4f, 5f);
    private Vector2 scrollPosition;

    [MenuItem("Tools/Block Stack Sort/Level Setup")]
    public static void Open()
    {
        GetWindow<LevelSetupWindow>("Level Setup");
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawAssetSection();

        if (levelData != null)
        {
            EditorGUILayout.Space(10f);
            DrawSizeSection();
            EditorGUILayout.Space(10f);
            DrawLevelGrid();
            EditorGUILayout.Space(10f);
            DrawSceneTools();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawAssetSection()
    {
        EditorGUILayout.LabelField("Level Asset", EditorStyles.boldLabel);
        levelData = (LevelData)EditorGUILayout.ObjectField("Level Data", levelData, typeof(LevelData), false);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create New Level"))
        {
            CreateNewLevelAsset();
        }

        using (new EditorGUI.DisabledScope(levelData == null))
        {
            if (GUILayout.Button("Save Level"))
            {
                SaveLevelAsset();
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSizeSection()
    {
        EditorGUILayout.LabelField("Board Size", EditorStyles.boldLabel);
        int nextMaxMoves = Mathf.Max(1, EditorGUILayout.IntField("Moves", maxMoves));
        if (nextMaxMoves != maxMoves)
        {
            maxMoves = nextMaxMoves;
            Undo.RecordObject(levelData, "Change Level Moves");
            levelData.SetMaxMoves(maxMoves);
            EditorUtility.SetDirty(levelData);
        }

        columnCount = Mathf.Max(1, EditorGUILayout.IntField("Column Count", columnCount));
        slotCount = Mathf.Max(1, EditorGUILayout.IntField("Slots Per Column", slotCount));

        if (GUILayout.Button("Apply Size To Level Data"))
        {
            EnsureLevelSize(columnCount, slotCount);
            SaveLevelAsset();
        }

        EditorGUILayout.Space(4f);
        columnSpacing = EditorGUILayout.FloatField("Column Spacing", columnSpacing);
        minColumnGap = Mathf.Max(0f, EditorGUILayout.FloatField("Min Column Gap", minColumnGap));
        slotSpacing = EditorGUILayout.FloatField("Slot Spacing", slotSpacing);
        slotScale = EditorGUILayout.Vector2Field("Slot Scale", slotScale);
        firstColumnPosition = EditorGUILayout.Vector2Field("First Column Top Slot", firstColumnPosition);
        trayPosition = EditorGUILayout.Vector2Field("Tray Slot", trayPosition);

        EditorGUILayout.Space(4f);
        fitColumnsToBoardLimit = EditorGUILayout.Toggle("Fit Columns To Board Limit", fitColumnsToBoardLimit);
        using (new EditorGUI.DisabledScope(!fitColumnsToBoardLimit))
        {
            boardLimitTransform = (Transform)EditorGUILayout.ObjectField("Board Limit", boardLimitTransform, typeof(Transform), true);
            boardLimitCenter = EditorGUILayout.Vector2Field("Board Limit Center", boardLimitCenter);
            boardLimitSize = EditorGUILayout.Vector2Field("Board Limit Size", boardLimitSize);
        }

        if (GUILayout.Button("Use BoardLimit From Scene"))
        {
            UseBoardLimitFromScene();
        }

        if (GUILayout.Button("Read Layout From Current Scene Board"))
        {
            ReadLayoutFromSceneBoard();
        }
    }

    private void DrawLevelGrid()
    {
        EditorGUILayout.LabelField("Blocks", EditorStyles.boldLabel);

        levelData.SetTrayColor((BlockColorId)EditorGUILayout.EnumPopup("Tray Color", levelData.TrayColor));
        maxMoves = Mathf.Max(1, levelData.MaxMoves);

        List<LevelColumnData> columns = levelData.GetMutableColumns();
        for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
        {
            LevelColumnData column = columns[columnIndex];

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Column {columnIndex + 1}", EditorStyles.boldLabel);

            bool hasRule = EditorGUILayout.Toggle("Only Accept One Color", column.HasAcceptedColor);
            BlockColorId acceptedColor = (BlockColorId)EditorGUILayout.EnumPopup("Accepted Color", column.AcceptedColor);
            column.SetAcceptedColorRule(hasRule, acceptedColor);

            List<BlockColorId> blocks = column.GetMutableBlocks();
            List<bool> coveredBlocks = column.GetMutableCoveredBlocks();
            for (int slotIndex = 0; slotIndex < blocks.Count; slotIndex++)
            {
                EditorGUILayout.BeginHorizontal();
                blocks[slotIndex] = (BlockColorId)EditorGUILayout.EnumPopup($"Slot {slotIndex} (Top -> Bottom)", blocks[slotIndex]);
                coveredBlocks[slotIndex] = EditorGUILayout.ToggleLeft("Cover", coveredBlocks[slotIndex], GUILayout.Width(80f));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }
    }

    private void DrawSceneTools()
    {
        EditorGUILayout.LabelField("Scene Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("Build Board In Current Scene"))
        {
            BuildBoardInScene();
        }

        if (GUILayout.Button("Assign Level To BoardGameplay"))
        {
            AssignLevelToBoardGameplay();
        }
    }

    private void CreateNewLevelAsset()
    {
        if (!AssetDatabase.IsValidFolder(DefaultLevelFolder))
        {
            AssetDatabase.CreateFolder("Assets/_Game", "Levels");
        }

        string path = EditorUtility.SaveFilePanelInProject(
            "Create Level Data",
            "LevelData",
            "asset",
            "Choose where to save the level asset.",
            DefaultLevelFolder);

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        levelData = CreateInstance<LevelData>();
        AssetDatabase.CreateAsset(levelData, path);
        levelData.SetMaxMoves(maxMoves);
        EnsureLevelSize(columnCount, slotCount);
        SaveLevelAsset();
        Selection.activeObject = levelData;
    }

    private void EnsureLevelSize(int targetColumnCount, int targetSlotCount)
    {
        if (levelData == null)
        {
            return;
        }

        Undo.RecordObject(levelData, "Resize Level Data");

        List<LevelColumnData> columns = levelData.GetMutableColumns();
        while (columns.Count < targetColumnCount)
        {
            columns.Add(new LevelColumnData());
        }

        while (columns.Count > targetColumnCount)
        {
            columns.RemoveAt(columns.Count - 1);
        }

        for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
        {
            List<BlockColorId> blocks = columns[columnIndex].GetMutableBlocks();
            while (blocks.Count < targetSlotCount)
            {
                blocks.Add(GetDefaultColor(columnIndex));
            }

            while (blocks.Count > targetSlotCount)
            {
                blocks.RemoveAt(blocks.Count - 1);
            }

            columns[columnIndex].EnsureCoverSize();
        }

        EditorUtility.SetDirty(levelData);
    }

    private BlockColorId GetDefaultColor(int columnIndex)
    {
        switch (columnIndex % 3)
        {
            case 0:
                return BlockColorId.Red;
            case 1:
                return BlockColorId.Yellow;
            default:
                return BlockColorId.Green;
        }
    }

    private void BuildBoardInScene()
    {
        if (levelData == null)
        {
            return;
        }

        EnsureLevelSize(columnCount, slotCount);

        Transform boardRoot = GetOrCreateRoot("BoardRoot", null);
        Transform columnRoot = GetOrCreateRoot("ColumnRoot", boardRoot);
        Transform tray = GetOrCreateRoot("Tray", boardRoot);
        Transform traySlot = GetOrCreateRoot("TraySlot", tray);
        traySlot.localPosition = Vector3.zero;
        tray.position = new Vector3(trayPosition.x, trayPosition.y, 0f);

        columnRoot.position = Vector3.zero;
        columnRoot.localScale = Vector3.one;
        ClearChildren(columnRoot);

        IReadOnlyList<LevelColumnData> columns = levelData.Columns;
        float effectiveColumnSpacing = Mathf.Max(columnSpacing, Mathf.Abs(slotScale.x) + minColumnGap);
        for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
        {
            GameObject columnObject = new GameObject($"Column{columnIndex + 1}");
            Undo.RegisterCreatedObjectUndo(columnObject, "Create Column");
            columnObject.transform.SetParent(columnRoot, false);
            columnObject.transform.localPosition = new Vector3(firstColumnPosition.x + (columnIndex * effectiveColumnSpacing), firstColumnPosition.y, 0f);
            columnObject.AddComponent<ColumnView>();

            int localSlotCount = columns[columnIndex].Blocks.Count;
            for (int slotIndex = 0; slotIndex < localSlotCount; slotIndex++)
            {
                GameObject slotObject = new GameObject($"Slot{slotIndex}");
                Undo.RegisterCreatedObjectUndo(slotObject, "Create Slot");
                slotObject.transform.SetParent(columnObject.transform, false);
                slotObject.transform.localPosition = new Vector3(0f, -slotIndex * slotSpacing, 0f);
                slotObject.transform.localScale = new Vector3(slotScale.x, slotScale.y, 1f);
            }
        }

        if (fitColumnsToBoardLimit)
        {
            FitColumnRootToBoardLimit(columnRoot);
        }

        float columnFitScale = Mathf.Abs(columnRoot.lossyScale.x);
        traySlot.localScale = new Vector3(slotScale.x * columnFitScale, slotScale.y * columnFitScale, 1f);

        BoardGameplay boardGameplay = boardRoot.GetComponent<BoardGameplay>();
        if (boardGameplay == null)
        {
            boardGameplay = boardRoot.gameObject.AddComponent<BoardGameplay>();
        }

        AssignLevelToBoardGameplay(boardGameplay);
        Selection.activeGameObject = boardRoot.gameObject;
        EditorUtility.SetDirty(boardRoot.gameObject);
    }

    private void FitColumnRootToBoardLimit(Transform columnRoot)
    {
        if (columnRoot.childCount == 0)
        {
            return;
        }

        Bounds limitBounds = GetBoardLimitBounds();
        Bounds bounds = CalculateColumnBounds(columnRoot);
        float limitWidth = Mathf.Max(0.01f, limitBounds.size.x);
        float limitHeight = Mathf.Max(0.01f, limitBounds.size.y);
        float scale = Mathf.Min(1f, limitWidth / Mathf.Max(0.01f, bounds.size.x), limitHeight / Mathf.Max(0.01f, bounds.size.y));

        columnRoot.localScale = new Vector3(scale, scale, 1f);

        Bounds scaledBounds = CalculateColumnBounds(columnRoot);
        Vector3 offset = limitBounds.center - scaledBounds.center;
        offset.z = 0f;
        columnRoot.position += offset;
    }

    private Bounds GetBoardLimitBounds()
    {
        if (boardLimitTransform == null)
        {
            Transform sceneLimit = FindBoardLimitTransform();
            if (sceneLimit != null)
            {
                boardLimitTransform = sceneLimit;
            }
        }

        if (boardLimitTransform != null)
        {
            SpriteRenderer spriteRenderer = boardLimitTransform.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                Bounds spriteBounds = spriteRenderer.bounds;
                boardLimitCenter = spriteBounds.center;
                boardLimitSize = spriteBounds.size;
                return spriteBounds;
            }

            Collider2D collider2D = boardLimitTransform.GetComponent<Collider2D>();
            if (collider2D != null)
            {
                Bounds colliderBounds = collider2D.bounds;
                boardLimitCenter = colliderBounds.center;
                boardLimitSize = colliderBounds.size;
                return colliderBounds;
            }

            Bounds transformBounds = new Bounds(
                boardLimitTransform.position,
                new Vector3(Mathf.Abs(boardLimitTransform.lossyScale.x), Mathf.Abs(boardLimitTransform.lossyScale.y), 0f));
            boardLimitCenter = transformBounds.center;
            boardLimitSize = transformBounds.size;
            return transformBounds;
        }

        return new Bounds(new Vector3(boardLimitCenter.x, boardLimitCenter.y, 0f), new Vector3(boardLimitSize.x, boardLimitSize.y, 0f));
    }

    private void UseBoardLimitFromScene()
    {
        Transform sceneLimit = FindBoardLimitTransform();
        if (sceneLimit == null)
        {
            Debug.LogWarning("LevelSetupWindow: Không tìm thấy object BoardLimit trong scene.");
            return;
        }

        boardLimitTransform = sceneLimit;
        GetBoardLimitBounds();
        Repaint();
    }

    private Transform FindBoardLimitTransform()
    {
        GameObject exact = GameObject.Find("BoardLimit");
        if (exact != null)
        {
            return exact.transform;
        }

        exact = GameObject.Find("boardLimit");
        if (exact != null)
        {
            return exact.transform;
        }

        return null;
    }

    private Bounds CalculateColumnBounds(Transform columnRoot)
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
                Vector3 halfSize = new Vector3(Mathf.Abs(slot.lossyScale.x) * 0.5f, Mathf.Abs(slot.lossyScale.y) * 0.5f, 0f);
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

        if (!hasBounds)
        {
            bounds = new Bounds(columnRoot.position, Vector3.one);
        }

        return bounds;
    }

    private void ReadLayoutFromSceneBoard()
    {
        Transform boardRoot = GameObject.Find("BoardRoot")?.transform;
        if (boardRoot == null)
        {
            Debug.LogWarning("LevelSetupWindow: Không tìm thấy BoardRoot trong scene.");
            return;
        }

        Transform columnRoot = boardRoot.Find("ColumnRoot");
        if (columnRoot == null || columnRoot.childCount == 0)
        {
            Debug.LogWarning("LevelSetupWindow: Không tìm thấy ColumnRoot hoặc column mẫu.");
            return;
        }

        Transform firstColumn = columnRoot.GetChild(0);
        Transform firstSlot = FindFirstSlot(firstColumn);
        if (firstSlot == null)
        {
            Debug.LogWarning("LevelSetupWindow: Column mẫu không có slot.");
            return;
        }

        firstColumnPosition = firstSlot.position;

        if (columnRoot.childCount >= 2)
        {
            Transform secondColumn = columnRoot.GetChild(1);
            Transform secondSlot = FindFirstSlot(secondColumn);
            if (secondSlot != null)
            {
                columnSpacing = Mathf.Abs(secondSlot.position.x - firstSlot.position.x);
            }
        }

        Transform secondVerticalSlot = FindSlotByOrder(firstColumn, 1);
        if (secondVerticalSlot != null)
        {
            slotSpacing = Mathf.Abs(firstSlot.position.y - secondVerticalSlot.position.y);
        }

        Transform tray = boardRoot.Find("Tray");
        Transform traySlot = tray != null ? tray.Find("TraySlot") : null;
        if (traySlot != null)
        {
            trayPosition = traySlot.position;
        }

        Repaint();
    }

    private Transform FindFirstSlot(Transform column)
    {
        return FindSlotByOrder(column, 0);
    }

    private Transform FindSlotByOrder(Transform column, int order)
    {
        List<Transform> slots = new List<Transform>();
        foreach (Transform child in column)
        {
            if (child.name.StartsWith("Slot"))
            {
                slots.Add(child);
            }
        }

        slots.Sort((a, b) => ExtractSlotIndex(a.name).CompareTo(ExtractSlotIndex(b.name)));
        return order >= 0 && order < slots.Count ? slots[order] : null;
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

    private Transform GetOrCreateRoot(string objectName, Transform parent)
    {
        Transform existing = parent == null ? GameObject.Find(objectName)?.transform : parent.Find(objectName);
        if (existing != null)
        {
            return existing;
        }

        GameObject newObject = new GameObject(objectName);
        Undo.RegisterCreatedObjectUndo(newObject, $"Create {objectName}");
        if (parent != null)
        {
            newObject.transform.SetParent(parent, false);
        }

        return newObject.transform;
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(parent.GetChild(i).gameObject);
        }
    }

    private void AssignLevelToBoardGameplay()
    {
        Transform boardRoot = GameObject.Find("BoardRoot")?.transform;
        if (boardRoot == null)
        {
            Debug.LogWarning("LevelSetupWindow: Không tìm thấy BoardRoot trong scene.");
            return;
        }

        BoardGameplay boardGameplay = boardRoot.GetComponent<BoardGameplay>();
        if (boardGameplay == null)
        {
            boardGameplay = boardRoot.gameObject.AddComponent<BoardGameplay>();
        }

        AssignLevelToBoardGameplay(boardGameplay);
    }

    private void AssignLevelToBoardGameplay(BoardGameplay boardGameplay)
    {
        SerializedObject serializedObject = new SerializedObject(boardGameplay);
        serializedObject.FindProperty("currentLevel").objectReferenceValue = levelData;
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(boardGameplay);
    }

    private void SaveLevelAsset()
    {
        if (levelData == null)
        {
            return;
        }

        EditorUtility.SetDirty(levelData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
