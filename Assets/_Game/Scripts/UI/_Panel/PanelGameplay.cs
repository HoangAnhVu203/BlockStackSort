using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PanelGameplay : UICanvas
{
    private const string RollBackFreeUsedKey = "BlockStackSort.ItemFreeUsed.RollBack";
    private const string ShuffleFreeUsedKey = "BlockStackSort.ItemFreeUsed.Shuffle";
    private const string MagnetFreeUsedKey = "BlockStackSort.ItemFreeUsed.Magnet";

    [SerializeField] private Button rollBackBlockBtn;
    [SerializeField] private Button shuffleBlockBtn;
    [SerializeField] private Button magnetBlockBtn;

    [Header("Item Uses")]
    [SerializeField, Min(0)] private int startingFreeItemUses = 1;
    [SerializeField] private Text rollBackItemText;
    [SerializeField] private Text shuffleItemText;
    [SerializeField] private Text magnetItemText;
    [SerializeField] private string addItemText = "+";

    [Header("Level Info")]
    [SerializeField] private Text levelText;
    [SerializeField] private Text moveText;

    [Header("Setting Menu")]
    [SerializeField] private Button settingBtn;
    [SerializeField] private RectTransform settingIcon;
    [SerializeField] private RectTransform settingMenuRoot;
    [SerializeField] private RectTransform[] settingMenuItems;
    [SerializeField] private float settingRotateDuration = 0.25f;
    [SerializeField] private float settingItemDuration = 0.22f;
    [SerializeField] private float settingItemDelay = 0.05f;
    [SerializeField] private float settingItemYOffset = 72f;

    [Header("Setting Toggles")]
    [SerializeField] private Button soundBtn;
    [SerializeField] private Image soundIcon;
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite soundOffSprite;
    [SerializeField] private Button vibrationBtn;
    [SerializeField] private Image vibrationIcon;
    [SerializeField] private Sprite vibrationOnSprite;
    [SerializeField] private Sprite vibrationOffSprite;
    [SerializeField] private Button goHomeBtn;

    private bool isSettingMenuOpen;
    private Vector2[] settingItemClosedPositions;
    private Vector2[] settingItemOpenedPositions;
    private int rollBackItemUses;
    private int shuffleItemUses;
    private int magnetItemUses;

    private void OnEnable()
    {
        ResolveReferences();
        ResetItemUses();
        PrepareSettingMenu();
        RefreshSettingIcons();
        RefreshLevelMoveInfo();
        RefreshItemUseTexts();

        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.Changed -= RefreshItemUseTexts;
            ShopManager.Instance.Changed += RefreshItemUseTexts;
        }

        if (rollBackBlockBtn != null)
        {
            rollBackBlockBtn.onClick.RemoveListener(HandleRollBackBlockClicked);
            rollBackBlockBtn.onClick.AddListener(HandleRollBackBlockClicked);
        }

        if (shuffleBlockBtn != null)
        {
            shuffleBlockBtn.onClick.RemoveListener(HandleShuffleBlockClicked);
            shuffleBlockBtn.onClick.AddListener(HandleShuffleBlockClicked);
        }

        if (magnetBlockBtn != null)
        {
            magnetBlockBtn.onClick.RemoveListener(HandleMagnetBlockClicked);
            magnetBlockBtn.onClick.AddListener(HandleMagnetBlockClicked);
        }

        if (settingBtn != null)
        {
            settingBtn.onClick.RemoveListener(HandleSettingClicked);
            settingBtn.onClick.AddListener(HandleSettingClicked);
        }

        if (soundBtn != null)
        {
            soundBtn.onClick.RemoveListener(HandleSoundClicked);
            soundBtn.onClick.AddListener(HandleSoundClicked);
        }

        if (vibrationBtn != null)
        {
            vibrationBtn.onClick.RemoveListener(HandleVibrationClicked);
            vibrationBtn.onClick.AddListener(HandleVibrationClicked);
        }

        if (goHomeBtn != null)
        {
            goHomeBtn.onClick.RemoveListener(HandleGoHomeClicked);
            goHomeBtn.onClick.AddListener(HandleGoHomeClicked);
        }
    }

    private void OnDisable()
    {
        KillSettingTweens();

        if (rollBackBlockBtn != null)
        {
            rollBackBlockBtn.onClick.RemoveListener(HandleRollBackBlockClicked);
        }

        if (shuffleBlockBtn != null)
        {
            shuffleBlockBtn.onClick.RemoveListener(HandleShuffleBlockClicked);
        }

        if (magnetBlockBtn != null)
        {
            magnetBlockBtn.onClick.RemoveListener(HandleMagnetBlockClicked);
        }

        if (settingBtn != null)
        {
            settingBtn.onClick.RemoveListener(HandleSettingClicked);
        }

        if (soundBtn != null)
        {
            soundBtn.onClick.RemoveListener(HandleSoundClicked);
        }

        if (vibrationBtn != null)
        {
            vibrationBtn.onClick.RemoveListener(HandleVibrationClicked);
        }

        if (goHomeBtn != null)
        {
            goHomeBtn.onClick.RemoveListener(HandleGoHomeClicked);
        }

        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.Changed -= RefreshItemUseTexts;
        }
    }

    private void ResolveReferences()
    {
        if (rollBackBlockBtn != null
            && shuffleBlockBtn != null
            && magnetBlockBtn != null
            && settingBtn != null
            && soundBtn != null
            && vibrationBtn != null
            && goHomeBtn != null
            && levelText != null
            && moveText != null
            && rollBackItemText != null
            && shuffleItemText != null
            && magnetItemText != null)
        {
            return;
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (rollBackBlockBtn == null && buttons[i].name == "RollBackBlockBtn")
            {
                rollBackBlockBtn = buttons[i];
            }

            if (shuffleBlockBtn == null && buttons[i].name == "ShuffleBlockBtn")
            {
                shuffleBlockBtn = buttons[i];
            }

            if (magnetBlockBtn == null && buttons[i].name == "MagnetBlockBtn")
            {
                magnetBlockBtn = buttons[i];
            }

            if (settingBtn == null && buttons[i].name == "SettingBtn")
            {
                settingBtn = buttons[i];
            }

            if (soundBtn == null && buttons[i].name == "SoundBtn")
            {
                soundBtn = buttons[i];
            }

            if (vibrationBtn == null && buttons[i].name == "VibrationBtn")
            {
                vibrationBtn = buttons[i];
            }

            if (goHomeBtn == null && buttons[i].name == "GoHomeBtn")
            {
                goHomeBtn = buttons[i];
            }
        }

        if (settingMenuRoot == null)
        {
            Transform found = transform.Find("SettingMenuRoot");
            if (found != null)
            {
                settingMenuRoot = found as RectTransform;
            }
        }

        soundIcon = soundIcon != null || soundBtn == null ? soundIcon : soundBtn.GetComponent<Image>();
        vibrationIcon = vibrationIcon != null || vibrationBtn == null ? vibrationIcon : vibrationBtn.GetComponent<Image>();
        ResolveInfoTexts();
        ResolveItemTexts();

        if (settingIcon == null && settingBtn != null)
        {
            Image[] images = settingBtn.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i].transform != settingBtn.transform)
                {
                    settingIcon = images[i].rectTransform;
                    break;
                }
            }

            if (settingIcon == null)
            {
                settingIcon = settingBtn.GetComponent<RectTransform>();
            }
        }

        if (rollBackBlockBtn == null)
        {
            Debug.LogWarning("PanelGameplay: Không tìm thấy RollBackBlockBtn.");
        }

        if (shuffleBlockBtn == null)
        {
            Debug.LogWarning("PanelGameplay: Không tìm thấy ShuffleBlockBtn.");
        }

        if (magnetBlockBtn == null)
        {
            Debug.LogWarning("PanelGameplay: Không tìm thấy MagnetBlockBtn.");
        }
    }

    private void PrepareSettingMenu()
    {
        ResolveSettingMenuItems();
        isSettingMenuOpen = false;

        if (settingMenuItems == null || settingMenuItems.Length == 0)
        {
            return;
        }

        settingItemClosedPositions = new Vector2[settingMenuItems.Length];
        settingItemOpenedPositions = new Vector2[settingMenuItems.Length];

        for (int i = 0; i < settingMenuItems.Length; i++)
        {
            if (settingMenuItems[i] == null)
            {
                continue;
            }

            Vector2 closedPosition = settingMenuItems[i].anchoredPosition;
            settingItemClosedPositions[i] = closedPosition;
            settingItemOpenedPositions[i] = closedPosition + new Vector2(0f, settingItemYOffset * (i + 1));
            settingMenuItems[i].anchoredPosition = closedPosition;
            settingMenuItems[i].localScale = Vector3.zero;
            settingMenuItems[i].gameObject.SetActive(false);
        }
    }

    public void RefreshLevelMoveInfo()
    {
        ResolveInfoTexts();

        int levelNumber = 1;
        BoardGameplay boardGameplay = null;
        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager != null)
        {
            levelNumber = levelManager.CurrentNormalLevelNumber;
            boardGameplay = levelManager.CurrentBoardGameplay;
        }

        if (boardGameplay == null)
        {
            boardGameplay = FindFirstObjectByType<BoardGameplay>();
        }

        int remainingMoves = boardGameplay != null ? boardGameplay.RemainingMoves : 0;
        SetText(levelText, levelNumber.ToString());
        SetText(moveText, remainingMoves.ToString());
    }

    public void NextLevelBtn()
    {
        LevelManager.Instance.NextLevel();
    }

    public void PreviousLevelBtn()
    {
        LevelManager.Instance.PreviousLevel();
    }

    private void ResolveInfoTexts()
    {
        if (levelText == null)
        {
            levelText = FindText("LevelText");
        }

        if (moveText == null)
        {
            moveText = FindText("MoveText");
        }
    }

    private Text FindText(string objectName)
    {
        Text[] texts = GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].name == objectName)
            {
                return texts[i];
            }
        }

        return null;
    }

    private void SetText(Text targetText, string value)
    {
        if (targetText != null)
        {
            targetText.text = value;
        }
    }

    private void ResolveItemTexts()
    {
        rollBackItemText = rollBackItemText != null ? rollBackItemText : FindButtonText(rollBackBlockBtn);
        shuffleItemText = shuffleItemText != null ? shuffleItemText : FindButtonText(shuffleBlockBtn);
        magnetItemText = magnetItemText != null ? magnetItemText : FindButtonText(magnetBlockBtn);
    }

    private Text FindButtonText(Button button)
    {
        if (button == null)
        {
            return null;
        }

        Text[] texts = button.GetComponentsInChildren<Text>(true);
        return texts.Length > 0 ? texts[0] : null;
    }

    private void ResetItemUses()
    {
        rollBackItemUses = GetInitialItemUses(RollBackFreeUsedKey);
        shuffleItemUses = GetInitialItemUses(ShuffleFreeUsedKey);
        magnetItemUses = GetInitialItemUses(MagnetFreeUsedKey);
    }

    private int GetInitialItemUses(string freeUsedKey)
    {
        bool hasUsedFree = PlayerPrefs.GetInt(freeUsedKey, 0) == 1;
        return hasUsedFree ? 0 : Mathf.Max(0, startingFreeItemUses);
    }

    private void RefreshItemUseTexts()
    {
        ResolveItemTexts();
        SetItemUseText(rollBackItemText, GetAvailableItemUses(ShopItemType.RollBack, rollBackItemUses));
        SetItemUseText(shuffleItemText, GetAvailableItemUses(ShopItemType.Shuffle, shuffleItemUses));
        SetItemUseText(magnetItemText, GetAvailableItemUses(ShopItemType.Magnet, magnetItemUses));
    }

    private void SetItemUseText(Text targetText, int uses)
    {
        SetText(targetText, uses > 0 ? uses.ToString() : addItemText);
    }

    private int GetAvailableItemUses(ShopItemType itemType, int freeUses)
    {
        int shopUses = ShopManager.Instance != null ? ShopManager.Instance.GetItemCount(itemType) : 0;
        return Mathf.Max(0, freeUses) + shopUses;
    }

    private bool HasItemUse(ShopItemType itemType, int freeUses)
    {
        return GetAvailableItemUses(itemType, freeUses) > 0;
    }

    private void ConsumeItemUse(ShopItemType itemType, ref int uses, string freeUsedKey)
    {
        if (uses > 0)
        {
            uses = Mathf.Max(0, uses - 1);
            if (PlayerPrefs.GetInt(freeUsedKey, 0) == 0)
            {
                PlayerPrefs.SetInt(freeUsedKey, 1);
                PlayerPrefs.Save();
            }
        }
        else if (ShopManager.Instance != null)
        {
            ShopManager.Instance.TryUseItem(itemType);
        }

        RefreshItemUseTexts();
    }

    private void ResolveSettingMenuItems()
    {
        if (settingMenuItems != null && settingMenuItems.Length > 0)
        {
            return;
        }

        if (settingMenuRoot == null)
        {
            return;
        }

        int childCount = settingMenuRoot.childCount;
        settingMenuItems = new RectTransform[childCount];
        for (int i = 0; i < childCount; i++)
        {
            settingMenuItems[i] = settingMenuRoot.GetChild(i) as RectTransform;
        }
    }

    private void HandleSettingClicked()
    {
        PlayButtonFeedback();
        SetSettingMenuOpen(!isSettingMenuOpen);
    }

    private void SetSettingMenuOpen(bool open)
    {
        isSettingMenuOpen = open;
        KillSettingTweens();

        if (settingIcon != null)
        {
            float targetRotation = open ? -180f : 0f;
            settingIcon.DOLocalRotate(new Vector3(0f, 0f, targetRotation), settingRotateDuration)
                .SetEase(Ease.OutBack);
        }

        if (settingMenuItems == null)
        {
            return;
        }

        for (int i = 0; i < settingMenuItems.Length; i++)
        {
            RectTransform item = settingMenuItems[i];
            if (item == null)
            {
                continue;
            }

            item.gameObject.SetActive(true);
            Vector2 targetPosition = open ? settingItemOpenedPositions[i] : settingItemClosedPositions[i];
            Vector3 targetScale = open ? Vector3.one : Vector3.zero;
            float delay = open ? i * settingItemDelay : (settingMenuItems.Length - 1 - i) * settingItemDelay;

            item.DOAnchorPos(targetPosition, settingItemDuration)
                .SetDelay(delay)
                .SetEase(open ? Ease.OutBack : Ease.InBack);
            item.DOScale(targetScale, settingItemDuration)
                .SetDelay(delay)
                .SetEase(open ? Ease.OutBack : Ease.InBack)
                .OnComplete(() =>
                {
                    if (!isSettingMenuOpen)
                    {
                        item.gameObject.SetActive(false);
                    }
                });
        }
    }

    private void HandleSoundClicked()
    {
        bool nextState = !AudioManager.IsSoundOn();
        AudioManager.Instance.SetSoundOn(nextState);
        RefreshSettingIcons();
        PlayButtonFeedback(false);
    }

    private void HandleVibrationClicked()
    {
        bool nextState = !AudioManager.IsVibrationOn();
        AudioManager.SetVibrationOn(nextState);
        RefreshSettingIcons();
        PlayButtonFeedback(false);

        if (nextState)
        {
            Handheld.Vibrate();
        }
    }

    private void HandleGoHomeClicked()
    {
        PlayButtonFeedback();
        if (HeartManager.Instance != null)
        {
            HeartManager.Instance.TrySpendHeart(1);
        }

        GameManager.Instance.GoHome();
    }

    private void RefreshSettingIcons()
    {
        if (soundIcon != null)
        {
            soundIcon.sprite = AudioManager.IsSoundOn() ? soundOnSprite : soundOffSprite;
        }

        if (vibrationIcon != null)
        {
            vibrationIcon.sprite = AudioManager.IsVibrationOn() ? vibrationOnSprite : vibrationOffSprite;
        }
    }

    private void PlayButtonFeedback(bool includeVibration = true)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButton();
        }

        if (includeVibration && AudioManager.IsVibrationOn())
        {
            Handheld.Vibrate();
        }
    }

    private void KillSettingTweens()
    {
        if (settingIcon != null)
        {
            settingIcon.DOKill();
        }

        if (settingMenuItems == null)
        {
            return;
        }

        for (int i = 0; i < settingMenuItems.Length; i++)
        {
            if (settingMenuItems[i] != null)
            {
                settingMenuItems[i].DOKill();
            }
        }
    }

    private void HandleRollBackBlockClicked()
    {
        PlayButtonFeedback();
        if (!HasItemUse(ShopItemType.RollBack, rollBackItemUses))
        {
            return;
        }

        BoardGameplay boardGameplay = FindFirstObjectByType<BoardGameplay>();
        if (boardGameplay != null && boardGameplay.RollbackLastMove())
        {
            ConsumeItemUse(ShopItemType.RollBack, ref rollBackItemUses, RollBackFreeUsedKey);
        }
    }

    private void HandleShuffleBlockClicked()
    {
        PlayButtonFeedback();
        if (!HasItemUse(ShopItemType.Shuffle, shuffleItemUses))
        {
            return;
        }

        BoardGameplay boardGameplay = FindFirstObjectByType<BoardGameplay>();
        if (boardGameplay != null && boardGameplay.ShuffleBlocks())
        {
            ConsumeItemUse(ShopItemType.Shuffle, ref shuffleItemUses, ShuffleFreeUsedKey);
        }
    }

    private void HandleMagnetBlockClicked()
    {
        PlayButtonFeedback();
        if (!HasItemUse(ShopItemType.Magnet, magnetItemUses))
        {
            return;
        }

        BoardGameplay boardGameplay = FindFirstObjectByType<BoardGameplay>();
        if (boardGameplay != null && boardGameplay.ActivateMagnetMode())
        {
            ConsumeItemUse(ShopItemType.Magnet, ref magnetItemUses, MagnetFreeUsedKey);
        }
    }
}
