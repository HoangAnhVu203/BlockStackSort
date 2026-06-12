using UnityEngine;

public class PanelLeaderBoard : UICanvas
{
    [SerializeField] private ShopItemButton[] shopItems;

    private void OnEnable()
    {
        ResolveShopItems();
        RefreshShopItems();

        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.Changed -= RefreshShopItems;
            ShopManager.Instance.Changed += RefreshShopItems;
        }
    }

    private void OnDisable()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.Changed -= RefreshShopItems;
        }
    }

    public void RefreshShopItems()
    {
        ResolveShopItems();
        for (int i = 0; i < shopItems.Length; i++)
        {
            if (shopItems[i] != null)
            {
                shopItems[i].Refresh();
            }
        }
    }

    private void ResolveShopItems()
    {
        if (shopItems != null && shopItems.Length > 0)
        {
            return;
        }

        shopItems = GetComponentsInChildren<ShopItemButton>(true);
    }
}
