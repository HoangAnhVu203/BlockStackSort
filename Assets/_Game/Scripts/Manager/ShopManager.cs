using System;
using System.Text;
using UnityEngine;

public enum ShopItemType
{
    RollBack,
    Shuffle,
    Magnet
}

public enum ShopProductType
{
    NoAds,
    Item,
    Coins,
    HeartTime
}

public sealed class ShopManager : Singleton<ShopManager>
{
    private const string NoAdsKey = "BlockStackSort.Shop.NoAds";
    private const string RollBackCountKey = "BlockStackSort.Shop.Item.RollBack";
    private const string ShuffleCountKey = "BlockStackSort.Shop.Item.Shuffle";
    private const string MagnetCountKey = "BlockStackSort.Shop.Item.Magnet";

    public event Action Changed;

    public bool HasNoAds => PlayerPrefs.GetInt(NoAdsKey, 0) == 1;

    public int GetItemCount(ShopItemType itemType)
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(GetItemKey(itemType), 0));
    }

    public void AddItem(ShopItemType itemType, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        SetItemCount(itemType, GetItemCount(itemType) + amount);
    }

    public bool TryUseItem(ShopItemType itemType)
    {
        int currentCount = GetItemCount(itemType);
        if (currentCount <= 0)
        {
            return false;
        }

        SetItemCount(itemType, currentCount - 1);
        return true;
    }

    public void UnlockNoAds()
    {
        PlayerPrefs.SetInt(NoAdsKey, 1);
        PlayerPrefs.Save();
        PublishChanged();
    }

    public void AddCoins(int amount)
    {
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(amount);
        }

        PublishChanged();
    }

    public void AddHeartTimeSeconds(int seconds)
    {
        if (HeartManager.Instance != null)
        {
            HeartManager.Instance.AddUnlimitedHeartTime(seconds);
        }

        PublishChanged();
    }

    public bool GrantProduct(ShopProductData productData)
    {
        if (productData == null)
        {
            return false;
        }

        ShopProductReward[] rewards = productData.Rewards;
        if (rewards == null || rewards.Length == 0)
        {
            PublishChanged();
            return true;
        }

        for (int i = 0; i < rewards.Length; i++)
        {
            GrantReward(rewards[i]);
        }

        LogPurchase(productData);
        PublishChanged();
        return true;
    }

    private void GrantReward(ShopProductReward reward)
    {
        if (reward == null)
        {
            return;
        }

        switch (reward.ProductType)
        {
            case ShopProductType.NoAds:
                UnlockNoAds();
                break;
            case ShopProductType.Item:
                AddItem(reward.ItemType, reward.GrantAmount);
                break;
            case ShopProductType.Coins:
                AddCoins(reward.GrantAmount);
                break;
            case ShopProductType.HeartTime:
                AddHeartTimeSeconds(reward.HeartTimeSeconds);
                break;
        }
    }

    private void SetItemCount(ShopItemType itemType, int count)
    {
        PlayerPrefs.SetInt(GetItemKey(itemType), Mathf.Max(0, count));
        PlayerPrefs.Save();
        PublishChanged();
    }

    private string GetItemKey(ShopItemType itemType)
    {
        switch (itemType)
        {
            case ShopItemType.Shuffle:
                return ShuffleCountKey;
            case ShopItemType.Magnet:
                return MagnetCountKey;
            default:
                return RollBackCountKey;
        }
    }

    private void LogPurchase(ShopProductData productData)
    {
        StringBuilder builder = new StringBuilder();
        string productName = !string.IsNullOrEmpty(productData.DisplayName)
            ? productData.DisplayName
            : productData.IapProductId;

        builder.AppendLine($"Shop purchased: {productName}");
        builder.AppendLine("Rewards:");

        ShopProductReward[] rewards = productData.Rewards;
        for (int i = 0; i < rewards.Length; i++)
        {
            string rewardLabel = GetRewardLogLabel(rewards[i]);
            if (!string.IsNullOrEmpty(rewardLabel))
            {
                builder.AppendLine($"- {rewardLabel}");
            }
        }

        builder.AppendLine("Current inventory:");
        builder.AppendLine($"- RollBack: {GetItemCount(ShopItemType.RollBack)}");
        builder.AppendLine($"- Shuffle: {GetItemCount(ShopItemType.Shuffle)}");
        builder.AppendLine($"- Magnet: {GetItemCount(ShopItemType.Magnet)}");
        builder.AppendLine($"- Coins: {(CoinManager.Instance != null ? CoinManager.Instance.CoinCount : 0)}");
        builder.AppendLine($"- No Ads: {HasNoAds}");

        if (HeartManager.Instance != null)
        {
            TimeSpan heartTime = HeartManager.Instance.GetUnlimitedHeartRemaining();
            builder.AppendLine($"- Heart Time: {FormatTimeSpan(heartTime)}");
        }

        Debug.Log(builder.ToString());
    }

    private string GetRewardLogLabel(ShopProductReward reward)
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
                return $"{reward.ItemType} +{reward.GrantAmount}";
            case ShopProductType.Coins:
                return $"Coins +{reward.GrantAmount}";
            case ShopProductType.HeartTime:
                return $"Heart Time +{FormatMinutes(reward.HeartTimeMinutes)}";
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

    private string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan <= TimeSpan.Zero)
        {
            return "0";
        }

        if (timeSpan.TotalHours >= 1d)
        {
            return $"{Mathf.FloorToInt((float)timeSpan.TotalHours)}h {timeSpan.Minutes}m";
        }

        return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
    }

    private void PublishChanged()
    {
        Changed?.Invoke();
    }
}
