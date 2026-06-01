using UnityEngine;

public class BlockView : MonoBehaviour
{
    [SerializeField] private BlockColorId colorId;

    public BlockColorId ColorId => colorId;

    public void SetColor(BlockColorId newColorId, Color visualColor)
    {
        colorId = newColorId;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = visualColor;
            spriteRenderer.sortingOrder = 10;
        }
    }

    public void SetSortingOrder(int sortingOrder)
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = sortingOrder;
        }
    }
}
