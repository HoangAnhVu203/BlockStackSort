using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class ShopItemButton : MonoBehaviour
{
    [SerializeField] private ShopProductData productData;

    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Text nameText;
    [SerializeField] private Text rewardText;
    [SerializeField] private Text priceText;
    [SerializeField] private GameObject purchasedState;

    public string IapProductId => productData != null ? productData.IapProductId : string.Empty;
    public ShopProductData ProductData => productData;

    private void OnEnable()
    {
        ResolveReferences();
        Bind();
        Refresh();

        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.Changed -= Refresh;
            ShopManager.Instance.Changed += Refresh;
        }
    }

    private void OnDisable()
    {
        Unbind();

        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.Changed -= Refresh;
        }
    }

    public void Purchase()
    {
        if (productData == null || ShopManager.Instance == null)
        {
            return;
        }

        // Later this should call IAP with IapProductId, then GrantProduct only after purchase succeeds.
        bool granted = ShopManager.Instance.GrantProduct(productData);
        if (granted && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButton();
        }

        Refresh();
    }

    public void Refresh()
    {
        ResolveReferences();

        if (nameText != null)
        {
            nameText.text = productData != null ? productData.DisplayName : string.Empty;
        }

        if (rewardText != null)
        {
            rewardText.text = productData != null ? GetRewardLabel(productData.Rewards) : string.Empty;
        }

        if (priceText != null)
        {
            priceText.text = productData != null ? productData.DisplayPrice : string.Empty;
        }

        bool noAdsPurchased = productData != null
            && productData.HasReward(ShopProductType.NoAds)
            && ShopManager.Instance != null
            && ShopManager.Instance.HasNoAds;

        if (purchasedState != null)
        {
            purchasedState.SetActive(noAdsPurchased);
        }

        if (button != null)
        {
            button.interactable = productData != null && !noAdsPurchased;
        }
    }

    public void SetPriceText(string price)
    {
        if (priceText != null)
        {
            priceText.text = price;
        }
    }

    private string GetRewardLabel(ShopProductReward[] rewards)
    {
        if (rewards == null || rewards.Length == 0)
        {
            return string.Empty;
        }

        List<string> labels = new List<string>();
        for (int i = 0; i < rewards.Length; i++)
        {
            string label = GetRewardLabel(rewards[i]);
            if (!string.IsNullOrEmpty(label))
            {
                labels.Add(label);
            }
        }

        return string.Join(" + ", labels);
    }

    private string GetRewardLabel(ShopProductReward reward)
    {
        if (reward == null)
        {
            return string.Empty;
        }

        switch (reward.ProductType)
        {
            case ShopProductType.NoAds:
                return "No Ads";
            case ShopProductType.Item:
            case ShopProductType.Coins:
                return $"+{reward.GrantAmount}";
            case ShopProductType.HeartTime:
                return FormatMinutes(reward.HeartTimeMinutes);
            default:
                return string.Empty;
        }
    }

    private string FormatMinutes(int minutes)
    {
        if (minutes >= 60 && minutes % 60 == 0)
        {
            return $"{minutes / 60}h";
        }

        return $"{minutes}m";
    }

    private void ResolveReferences()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }

    private void Bind()
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(Purchase);
        button.onClick.AddListener(Purchase);
    }

    private void Unbind()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(Purchase);
        }
    }
}
