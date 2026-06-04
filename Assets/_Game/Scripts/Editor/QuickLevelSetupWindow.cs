using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class QuickLevelSetupWindow : EditorWindow
{
    private LevelData levelData;
    private LevelData previousLevelData;
    private int columnCount = 4;
    private int slotCount = 5;
    private int maxMoves = 20;
    private BlockColorId trayColor = BlockColorId.White;
    private BlockColorId selectedColor = BlockColorId.Red;
    private Vector2 scrollPosition;

    [MenuItem("Tools/Block Stack Sort/Quick Level Setup")]
    public static void Open()
    {
        GetWindow<QuickLevelSetupWindow>("Quick Level Setup");
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawLevelAsset();

        if (levelData != null)
        {
            SyncFromLevelWhenChanged();
            EditorGUILayout.Space(8f);
            DrawLevelSettings();
            EditorGUILayout.Space(8f);
            DrawPalette();
            EditorGUILayout.Space(8f);
            DrawBoardGrid();
            EditorGUILayout.Space(8f);
            DrawSaveActions();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawLevelAsset()
    {
        EditorGUILayout.LabelField("Level Asset", EditorStyles.boldLabel);
        levelData = (LevelData)EditorGUILayout.ObjectField("Level Data", levelData, typeof(LevelData), false);
    }

    private void SyncFromLevelWhenChanged()
    {
        if (previousLevelData == levelData)
        {
            return;
        }

        previousLevelData = levelData;
        maxMoves = levelData.MaxMoves;
        trayColor = levelData.TrayColor;

        IReadOnlyList<LevelColumnData> columns = levelData.Columns;
        columnCount = Mathf.Max(1, columns.Count);
        slotCount = columns.Count > 0 ? Mathf.Max(1, columns[0].Blocks.Count) : slotCount;
    }

    private void DrawLevelSettings()
    {
        EditorGUILayout.LabelField("Board", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        maxMoves = Mathf.Max(1, EditorGUILayout.IntField("Moves", maxMoves));
        trayColor = (BlockColorId)EditorGUILayout.EnumPopup("Tray Color", trayColor);

        EditorGUILayout.BeginHorizontal();
        columnCount = Mathf.Max(1, EditorGUILayout.IntField("Columns", columnCount));
        slotCount = Mathf.Max(1, EditorGUILayout.IntField("Slots", slotCount));
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            RecordLevel("Change Quick Level Settings");
            levelData.SetMaxMoves(maxMoves);
            levelData.SetTrayColor(trayColor);
            EnsureLevelSize(columnCount, slotCount);
            MarkDirty();
        }

        if (GUILayout.Button($"Apply Board Size {columnCount}x{slotCount}"))
        {
            RecordLevel("Apply Quick Level Size");
            EnsureLevelSize(columnCount, slotCount);
            MarkDirty();
        }
    }

    private void DrawPalette()
    {
        EditorGUILayout.LabelField("Palette", EditorStyles.boldLabel);

        Array values = Enum.GetValues(typeof(BlockColorId));
        const int buttonsPerRow = 6;
        int index = 0;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        while (index < values.Length)
        {
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < buttonsPerRow && index < values.Length; i++, index++)
            {
                BlockColorId colorId = (BlockColorId)values.GetValue(index);
                DrawPaletteButton(colorId);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawPaletteButton(BlockColorId colorId)
    {
        Color previousColor = GUI.backgroundColor;
        GUI.backgroundColor = GetEditorColor(colorId);

        string label = selectedColor == colorId ? $"* {colorId}" : colorId.ToString();
        if (GUILayout.Button(label, GUILayout.Width(110f), GUILayout.Height(28f)))
        {
            selectedColor = colorId;
        }

        GUI.backgroundColor = previousColor;
    }

    private void DrawBoardGrid()
    {
        EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Click cell để set màu đang chọn. Tick Cover để phủ block. Mỗi column có thể bật Only và chọn màu được phép đi vào.", MessageType.Info);

        List<LevelColumnData> columns = levelData.GetMutableColumns();
        EnsureLevelSize(columnCount, slotCount);

        EditorGUILayout.BeginHorizontal();
        for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
        {
            DrawColumn(columns[columnIndex], columnIndex);
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawColumn(LevelColumnData column, int columnIndex)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(118f));
        EditorGUILayout.LabelField($"Column {columnIndex + 1}", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        bool hasRule = EditorGUILayout.ToggleLeft("Only", column.HasAcceptedColor);
        BlockColorId acceptedColor = (BlockColorId)EditorGUILayout.EnumPopup(column.AcceptedColor);
        if (EditorGUI.EndChangeCheck())
        {
            RecordLevel("Change Column Rule");
            column.SetAcceptedColorRule(hasRule, acceptedColor);
            MarkDirty();
        }

        EditorGUI.BeginChangeCheck();
        int freezeUnlockCount = Mathf.Max(0, EditorGUILayout.IntField("Freeze", column.FreezeUnlockColumnCount));
        if (EditorGUI.EndChangeCheck())
        {
            RecordLevel("Change Column Freeze");
            column.SetFreezeUnlockColumnCount(freezeUnlockCount);
            MarkDirty();
        }

        List<BlockColorId> blocks = column.GetMutableBlocks();
        List<bool> coveredBlocks = column.GetMutableCoveredBlocks();
        for (int slotIndex = 0; slotIndex < blocks.Count; slotIndex++)
        {
            DrawCell(blocks, coveredBlocks, slotIndex);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawCell(List<BlockColorId> blocks, List<bool> coveredBlocks, int slotIndex)
    {
        EditorGUILayout.BeginHorizontal();

        Color previousColor = GUI.backgroundColor;
        GUI.backgroundColor = GetEditorColor(blocks[slotIndex]);

        string label = GetShortLabel(blocks[slotIndex]);
        if (GUILayout.Button(label, GUILayout.Width(58f), GUILayout.Height(44f)))
        {
            RecordLevel("Paint Level Cell");
            blocks[slotIndex] = selectedColor;
            MarkDirty();
        }

        GUI.backgroundColor = previousColor;

        EditorGUI.BeginChangeCheck();
        bool covered = GUILayout.Toggle(coveredBlocks[slotIndex], "Cover", GUILayout.Width(54f), GUILayout.Height(44f));
        if (EditorGUI.EndChangeCheck())
        {
            RecordLevel("Toggle Cell Cover");
            coveredBlocks[slotIndex] = covered;
            MarkDirty();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSaveActions()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Level"))
        {
            SaveLevel();
        }

        if (GUILayout.Button("Select Asset"))
        {
            Selection.activeObject = levelData;
        }
        EditorGUILayout.EndHorizontal();
    }

    private void EnsureLevelSize(int targetColumnCount, int targetSlotCount)
    {
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

    private Color GetEditorColor(BlockColorId colorId)
    {
        switch (colorId)
        {
            case BlockColorId.White:
                return Color.white;
            case BlockColorId.Red:
                return new Color(1f, 0.18f, 0.08f);
            case BlockColorId.Yellow:
                return new Color(1f, 0.9f, 0.05f);
            case BlockColorId.Green:
                return new Color(0.05f, 0.85f, 0.18f);
            case BlockColorId.Pink:
                return new Color(1f, 0.35f, 0.72f);
            case BlockColorId.Black:
                return new Color(0.15f, 0.15f, 0.15f);
            case BlockColorId.Brown:
                return new Color(0.45f, 0.25f, 0.12f);
            case BlockColorId.Purple:
                return new Color(0.55f, 0.25f, 0.85f);
            case BlockColorId.BlueSky:
                return new Color(0.25f, 0.75f, 1f);
            case BlockColorId.RainBow:
                return new Color(1f, 0.55f, 0.85f);
            case BlockColorId.Blue:
                return new Color(0.1f, 0.3f, 1f);
            case BlockColorId.Orange:
                return new Color(1f, 0.48f, 0.08f);
            default:
                return Color.gray;
        }
    }

    private string GetShortLabel(BlockColorId colorId)
    {
        switch (colorId)
        {
            case BlockColorId.BlueSky:
                return "Sky";
            case BlockColorId.RainBow:
                return "Rain";
            default:
                return colorId.ToString().Length <= 5 ? colorId.ToString() : colorId.ToString().Substring(0, 5);
        }
    }

    private void RecordLevel(string label)
    {
        Undo.RecordObject(levelData, label);
    }

    private void MarkDirty()
    {
        EditorUtility.SetDirty(levelData);
    }

    private void SaveLevel()
    {
        MarkDirty();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
