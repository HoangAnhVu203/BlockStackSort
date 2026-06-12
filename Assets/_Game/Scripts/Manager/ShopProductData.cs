using UnityEngine;

[System.Serializable]
public sealed class ShopProductReward
{
    [SerializeField] private ShopProductType productType;
    [SerializeField] private ShopItemType itemType;
    [SerializeField, Min(1)] private int grantAmount = 1;
    [SerializeField, Min(0)] private int heartTimeMinutes;

    public ShopProductType ProductType => productType;
    public ShopItemType ItemType => itemType;
    public int GrantAmount => Mathf.Max(1, grantAmount);
    public int HeartTimeMinutes => Mathf.Max(0, heartTimeMinutes);
    public int HeartTimeSeconds => HeartTimeMinutes * 60;
}

[CreateAssetMenu(fileName = "ShopProductData", menuName = "Block Stack Sort/Shop Product")]
public sealed class ShopProductData : ScriptableObject
{
    [Header("Info")]
    [SerializeField] private string iapProductId;
    [SerializeField] private string displayName;
    [SerializeField] private string displayPrice;

    [Header("Rewards")]
    [SerializeField] private ShopProductReward[] rewards;

    public string IapProductId => iapProductId;
    public string DisplayName => displayName;
    public string DisplayPrice => displayPrice;
    public ShopProductReward[] Rewards => rewards;

    public bool HasReward(ShopProductType productType)
    {
        if (rewards == null)
        {
            return false;
        }

        for (int i = 0; i < rewards.Length; i++)
        {
            if (rewards[i] != null && rewards[i].ProductType == productType)
            {
                return true;
            }
        }

        return false;
    }
}
