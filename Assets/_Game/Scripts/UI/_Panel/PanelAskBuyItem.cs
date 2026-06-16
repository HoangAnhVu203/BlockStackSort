using System;
using UnityEngine;
using UnityEngine.UI;

public class PanelAskBuyItem : UICanvas
{
    [Header("Item Icons")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private Sprite rollBackItemIcon;
    [SerializeField] private Sprite shuffleItemIcon;
    [SerializeField] private Sprite magnetItemIcon;

    [Header("Buttons")]
    [SerializeField] private Button coinBuyButton;
    [SerializeField] private Button freeButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Text coinPriceText;

    private const int DefaultCoinCost = 500;

    private Func<bool> onUseWithCoins;
    private Func<bool> onUseFree;
    private int coinCost = DefaultCoinCost;

    public void Configure(ShopItemType itemType, Func<bool> useWithCoinsCallback, Func<bool> useFreeCallback, int cost = DefaultCoinCost)
    {
        Configure(GetItemIcon(itemType), useWithCoinsCallback, useFreeCallback, cost);
    }

    public override void Open()
    {
        base.Open();
        transform.SetAsLastSibling();
        ResolveReferences();
        BindButtons();
        BindCoinChanged();
        RefreshPrice();
    }

    public override void CloseDirectly()
    {
        UnbindCoinChanged();
        UnbindButtons();
        base.CloseDirectly();
    }

    public void Configure(Sprite icon, Func<bool> useWithCoinsCallback, Func<bool> useFreeCallback, int cost = DefaultCoinCost)
    {
        ResolveReferences();

        onUseWithCoins = useWithCoinsCallback;
        onUseFree = useFreeCallback;
        coinCost = Mathf.Max(0, cost);

        if (itemIcon != null)
        {
            itemIcon.sprite = icon;
            itemIcon.enabled = icon != null;
        }

        RefreshPrice();
    }

    private Sprite GetItemIcon(ShopItemType itemType)
    {
        switch (itemType)
        {
            case ShopItemType.Shuffle:
                return shuffleItemIcon;
            case ShopItemType.Magnet:
                return magnetItemIcon;
            default:
                return rollBackItemIcon;
        }
    }

    private void HandleCoinBuyClicked()
    {
        PlayButtonSound();

        if (CoinManager.Instance == null || CoinManager.Instance.CoinCount < coinCost)
        {
            Debug.Log($"PanelAskBuyItem: Not enough coins. Need {coinCost}.");
            RefreshCoinBuyState();
            return;
        }

        if (onUseWithCoins == null || !onUseWithCoins.Invoke())
        {
            return;
        }

        if (CoinManager.Instance.TrySpendCoins(coinCost))
        {
            CloseDirectly();
        }
    }

    private void HandleFreeClicked()
    {
        PlayButtonSound();

        if (onUseFree != null && onUseFree.Invoke())
        {
            CloseDirectly();
        }
    }

    private void HandleCloseClicked()
    {
        PlayButtonSound();
        CloseDirectly();
    }

    private void ResolveReferences()
    {
        if (itemIcon == null)
        {
            Transform iconTransform = FindChildByName(transform, "IconItem");
            itemIcon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
        }

        if (coinPriceText == null)
        {
            coinPriceText = FindTextByValue("500");
        }

        if (coinBuyButton == null)
        {
            coinBuyButton = ResolveButtonFromText(coinPriceText);
        }

        if (freeButton == null)
        {
            freeButton = ResolveButtonFromText(FindTextByValue("Free"));
        }

        if (closeButton == null)
        {
            Transform closeTransform = FindChildByName(transform, "close");
            closeButton = ResolveButtonFromTransform(closeTransform);
        }
    }

    private void BindButtons()
    {
        if (coinBuyButton != null)
        {
            coinBuyButton.onClick.RemoveListener(HandleCoinBuyClicked);
            coinBuyButton.onClick.AddListener(HandleCoinBuyClicked);
        }

        if (freeButton != null)
        {
            freeButton.onClick.RemoveListener(HandleFreeClicked);
            freeButton.onClick.AddListener(HandleFreeClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleCloseClicked);
            closeButton.onClick.AddListener(HandleCloseClicked);
        }
    }

    private void UnbindButtons()
    {
        if (coinBuyButton != null)
        {
            coinBuyButton.onClick.RemoveListener(HandleCoinBuyClicked);
        }

        if (freeButton != null)
        {
            freeButton.onClick.RemoveListener(HandleFreeClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleCloseClicked);
        }
    }

    private void BindCoinChanged()
    {
        if (CoinManager.Instance == null)
        {
            return;
        }

        CoinManager.Instance.CoinChanged -= HandleCoinChanged;
        CoinManager.Instance.CoinChanged += HandleCoinChanged;
    }

    private void UnbindCoinChanged()
    {
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.CoinChanged -= HandleCoinChanged;
        }
    }

    private void HandleCoinChanged(int coinCount)
    {
        RefreshCoinBuyState();
    }

    private void RefreshPrice()
    {
        if (coinPriceText != null)
        {
            coinPriceText.text = coinCost.ToString();
        }

        RefreshCoinBuyState();
    }

    private void RefreshCoinBuyState()
    {
        if (coinBuyButton == null)
        {
            return;
        }

        coinBuyButton.interactable = CoinManager.Instance != null && CoinManager.Instance.CoinCount >= coinCost;
    }

    private Text FindTextByValue(string value)
    {
        Text[] texts = GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].text == value)
            {
                return texts[i];
            }
        }

        return null;
    }

    private Button ResolveButtonFromText(Text text)
    {
        if (text == null)
        {
            return null;
        }

        Button existingButton = text.GetComponentInParent<Button>(true);
        if (existingButton != null)
        {
            return existingButton;
        }

        Image[] parentImages = text.GetComponentsInParent<Image>(true);
        for (int i = 0; i < parentImages.Length; i++)
        {
            if (parentImages[i].transform == transform)
            {
                continue;
            }

            return ResolveButtonFromTransform(parentImages[i].transform);
        }

        return null;
    }

    private Button ResolveButtonFromTransform(Transform target)
    {
        if (target == null)
        {
            return null;
        }

        Button button = target.GetComponent<Button>();
        if (button == null)
        {
            Image image = target.GetComponent<Image>();
            if (image == null)
            {
                image = target.gameObject.AddComponent<Image>();
                image.color = Color.clear;
            }

            image.raycastTarget = true;
            button = target.gameObject.AddComponent<Button>();
        }

        return button;
    }

    private Transform FindChildByName(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildByName(root.GetChild(i), childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private void PlayButtonSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButton();
        }
    }
}
