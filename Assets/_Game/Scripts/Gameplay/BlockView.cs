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

        EnsureTapCollider();
    }

    public void SetSortingOrder(int sortingOrder)
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = sortingOrder;
        }
    }

    private void EnsureTapCollider()
    {
        if (GetComponent<Collider2D>() != null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
        boxCollider.size = spriteRenderer != null && spriteRenderer.sprite != null ? spriteRenderer.sprite.bounds.size : Vector2.one;
    }
}
