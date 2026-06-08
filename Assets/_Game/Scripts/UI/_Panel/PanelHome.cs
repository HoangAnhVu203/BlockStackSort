using System;
using UnityEngine;
using UnityEngine.UI;

public class PanelHome : UICanvas
{
    [SerializeField] private Button playButton;
    [SerializeField] private Text coinText;

    [Header("Level Slots")]
    [SerializeField] private Text[] levelSlotTexts;

    private void OnEnable()
    {
        ResolveButton();
        ResolveLevelSlots();
        PopulateLevelItems();
        BindCoinText();

        if (playButton != null)
        {
            playButton.onClick.RemoveListener(HandlePlayClicked);
            playButton.onClick.AddListener(HandlePlayClicked);
        }
    }

    private void OnDisable()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(HandlePlayClicked);
        }

        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.CoinChanged -= RefreshCoinText;
        }
    }

    private void HandlePlayClicked()
    {
        GameManager.Instance.PlayCurrentLevel();
    }

    private void ResolveButton()
    {
        if (playButton != null)
        {
            return;
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].name.IndexOf("Play", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                playButton = buttons[i];
                return;
            }
        }
    }

    private void BindCoinText()
    {
        if (CoinManager.Instance == null)
        {
            return;
        }

        CoinManager.Instance.CoinChanged -= RefreshCoinText;
        CoinManager.Instance.CoinChanged += RefreshCoinText;
        RefreshCoinText(CoinManager.Instance.CoinCount);
    }

    public void RefreshCoinText()
    {
        if (CoinManager.Instance != null)
        {
            RefreshCoinText(CoinManager.Instance.CoinCount);
        }
    }

    private void RefreshCoinText(int coinCount)
    {
        if (coinText != null)
        {
            coinText.text = coinCount.ToString();
        }
    }

    public void OpenSetting()
    {
        var panelSetting = UIManager.Instance.OpenUI<PanelSetting>();
        panelSetting.SetOpenedFromHome(true);
    }

    public void RefreshLevelItems()
    {
        PopulateLevelItems();
    }

    private void PopulateLevelItems()
    {
        ResolveLevelSlots();

        if (levelSlotTexts == null || levelSlotTexts.Length == 0 || LevelManager.Instance == null)
        {
            return;
        }

        int levelCount = LevelManager.Instance.LevelCount;
        int currentLevelIndex = LevelManager.Instance.CurrentLevelIndex;
        int slotCount = Mathf.Min(5, levelSlotTexts.Length);

        for (int i = 0; i < slotCount; i++)
        {
            int levelIndex = currentLevelIndex + i;
            bool hasLevel = levelIndex >= 0 && levelIndex < levelCount;

            if (levelSlotTexts[i] != null)
            {
                levelSlotTexts[i].gameObject.SetActive(hasLevel);
                levelSlotTexts[i].text = hasLevel ? (levelIndex + 1).ToString() : string.Empty;
            }
        }
    }

    private void ResolveLevelSlots()
    {
        if (levelSlotTexts != null && levelSlotTexts.Length >= 5)
        {
            return;
        }

        Text[] foundTexts = GetComponentsInChildren<Text>(true);
        levelSlotTexts = new Text[5];
        int slotCount = 0;
        for (int i = 0; i < foundTexts.Length && slotCount < levelSlotTexts.Length; i++)
        {
            if (foundTexts[i] == coinText)
            {
                continue;
            }

            if (foundTexts[i].name.IndexOf("Level", StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            levelSlotTexts[slotCount] = foundTexts[i];
            slotCount++;
        }
    }
}
