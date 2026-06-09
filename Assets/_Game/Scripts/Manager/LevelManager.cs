using System;
using UnityEditor;
using UnityEngine;

public class LevelManager : Singleton<LevelManager>
{
    private const string CurrentLevelKey = "BlockStackSort.CurrentLevelIndex";
    private const string LevelStarKeyPrefix = "BlockStackSort.LevelStars.";

    [SerializeField] private BoardGameplay boardGameplay;
    [SerializeField] private LevelData[] levels;

    private LevelData currentLevelData;

    public int CurrentLevelIndex { get; private set; }
    public int CurrentNormalLevelNumber => CurrentLevelIndex + 1;
    public int LevelCount => levels != null ? levels.Length : 0;
    public LevelData CurrentLevelData => currentLevelData;
    public BoardGameplay CurrentBoardGameplay => boardGameplay;

    private void Awake()
    {
        TryAutoFillLevelsInEditor();
        CurrentLevelIndex = Mathf.Clamp(PlayerPrefs.GetInt(CurrentLevelKey, 0), 0, Mathf.Max(0, LevelCount - 1));
    }

    public void LoadCurrentLevel(bool restart)
    {
        LoadLevel(CurrentLevelIndex, restart);
        RefreshLoadedLevelViews();
    }

    public void LoadNextLevel(bool restart)
    {
        SelectNextLevel();
        LoadLevel(CurrentLevelIndex, restart);
        RefreshLoadedLevelViews();
    }

    public void LoadPreviousLevel(bool restart)
    {
        SelectPreviousLevel();
        LoadLevel(CurrentLevelIndex, restart);
        RefreshLoadedLevelViews();
    }

    public void CompleteCurrentLevel()
    {
        CompleteCurrentLevel(0);
    }

    public void CompleteCurrentLevel(int earnedStars)
    {
        if (LevelCount == 0)
        {
            return;
        }

        SaveBestStars(CurrentLevelIndex, earnedStars);
        // LeaderBoardService.Instance?.ReportLevelCompleted(CurrentNormalLevelNumber);
    }

    public void SelectLevel(int levelIndex)
    {
        SetCurrentLevelIndex(levelIndex);
        RefreshLoadedLevelViews();
    }

    public void SelectNextLevel()
    {
        int nextIndex = LevelCount > 0 ? (CurrentLevelIndex + 1) % LevelCount : 0;
        SetCurrentLevelIndex(nextIndex);
        RefreshLoadedLevelViews();
    }

    public void SelectPreviousLevel()
    {
        int previousIndex = 0;
        if (LevelCount > 0)
        {
            previousIndex = CurrentLevelIndex - 1;
            if (previousIndex < 0)
            {
                previousIndex = LevelCount - 1;
            }
        }

        SetCurrentLevelIndex(previousIndex);
        RefreshLoadedLevelViews();
    }

    public void NextLevel()
    {
        ChangeLevel(1);
    }

    public void PreviousLevel()
    {
        ChangeLevel(-1);
    }

    public void PlayLevel(int levelIndex)
    {
        SelectLevel(levelIndex);
        GameManager.Instance.PlayCurrentLevel();
    }

    public int GetBestStars(int levelIndex)
    {
        if (LevelCount == 0)
        {
            return 0;
        }

        levelIndex = Mathf.Clamp(levelIndex, 0, LevelCount - 1);
        return Mathf.Clamp(PlayerPrefs.GetInt(GetLevelStarKey(levelIndex), 0), 0, 3);
    }

    public void SaveBestStars(int levelIndex, int stars)
    {
        if (LevelCount == 0)
        {
            return;
        }

        levelIndex = Mathf.Clamp(levelIndex, 0, LevelCount - 1);
        stars = Mathf.Clamp(stars, 0, 3);
        if (stars <= GetBestStars(levelIndex))
        {
            return;
        }

        PlayerPrefs.SetInt(GetLevelStarKey(levelIndex), stars);
        PlayerPrefs.Save();
    }

    public void ClearCurrentLevel()
    {
        currentLevelData = null;

        if (boardGameplay == null)
        {
            return;
        }

        boardGameplay.ClearFromManager();
    }

    public void GoHome()
    {
        GameManager.Instance.GoHome();
    }

    public void SetLevels(LevelData[] newLevels)
    {
        levels = newLevels ?? Array.Empty<LevelData>();
        CurrentLevelIndex = Mathf.Clamp(CurrentLevelIndex, 0, Mathf.Max(0, LevelCount - 1));
        RefreshLoadedLevelViews();
    }

    private void ChangeLevel(int direction)
    {
        if (direction > 0)
        {
            SelectNextLevel();
        }
        else if (direction < 0)
        {
            SelectPreviousLevel();
        }
        else
        {
            RefreshLoadedLevelViews();
            return;
        }

        if (ShouldReloadCurrentLevel())
        {
            LoadLevel(CurrentLevelIndex, true);
        }

        RefreshLoadedLevelViews();
    }

    private void LoadLevel(int levelIndex, bool restart)
    {
        if (levels == null || levels.Length == 0)
        {
            Debug.LogWarning("[LevelManager] No levels assigned.");
            return;
        }

        LevelData level = GetLevel(levelIndex);
        if (level == null)
        {
            Debug.LogWarning($"[LevelManager] LevelData {levelIndex + 1} chưa được gán.");
            return;
        }

        ResolveBoardGameplay();
        if (boardGameplay == null)
        {
            Debug.LogWarning("[LevelManager] Không tìm thấy BoardGameplay hoặc BoardRoot trong scene.");
            return;
        }

        currentLevelData = level;
        boardGameplay.InitializeFromManager(currentLevelData);
    }

    private LevelData GetLevel(int levelIndex)
    {
        if (levels == null || levels.Length == 0)
        {
            return null;
        }

        return levels[Mathf.Clamp(levelIndex, 0, levels.Length - 1)];
    }

    private void SetCurrentLevelIndex(int levelIndex)
    {
        CurrentLevelIndex = Mathf.Clamp(levelIndex, 0, Mathf.Max(0, LevelCount - 1));
        PlayerPrefs.SetInt(CurrentLevelKey, CurrentLevelIndex);
        PlayerPrefs.Save();
    }

    private bool ShouldReloadCurrentLevel()
    {
        return currentLevelData != null
            || (GameManager.instance != null && GameManager.Instance.State == GameState.GamePlay);
    }

    private void RefreshLoadedLevelViews()
    {
        UIManager uiManager = UIManager.instance;
        if (uiManager == null)
        {
            return;
        }

        if (uiManager.IsUILoaded<PanelGameplay>())
        {
            uiManager.GetUI<PanelGameplay>().RefreshLevelMoveInfo();
        }

        if (uiManager.IsUILoaded<PanelHome>())
        {
            uiManager.GetUI<PanelHome>().RefreshLevelItems();
        }
    }

    private void ResolveBoardGameplay()
    {
        if (boardGameplay != null)
        {
            return;
        }

        boardGameplay = FindFirstObjectByType<BoardGameplay>();
        if (boardGameplay == null)
        {
            GameObject boardRoot = GameObject.Find("BoardRoot");
            if (boardRoot == null)
            {
                boardRoot = new GameObject("BoardRoot");
            }

            boardGameplay = boardRoot.AddComponent<BoardGameplay>();
        }
    }

    private void TryAutoFillLevelsInEditor()
    {
#if UNITY_EDITOR
        if (levels != null && levels.Length > 0)
        {
            return;
        }

        var guids = AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/_Game" });
        Array.Sort(guids, (left, right) =>
        {
            var leftPath = AssetDatabase.GUIDToAssetPath(left);
            var rightPath = AssetDatabase.GUIDToAssetPath(right);
            return string.Compare(leftPath, rightPath, StringComparison.OrdinalIgnoreCase);
        });

        levels = new LevelData[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            levels[i] = AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guids[i]));
        }
#endif
    }

    private static string GetLevelStarKey(int levelIndex)
    {
        return $"{LevelStarKeyPrefix}{levelIndex}";
    }
}
