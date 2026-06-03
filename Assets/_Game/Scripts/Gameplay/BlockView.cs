using UnityEngine;
using DG.Tweening;

public class BlockView : MonoBehaviour
{
    [SerializeField] private BlockColorId colorId;
    [SerializeField] private SpriteRenderer coverRenderer;
    [SerializeField] private Sprite coverSprite;
    [SerializeField] private float coverBurnDuration = 0.45f;

    public BlockColorId ColorId => colorId;
    public bool HasCover => coverRenderer != null && coverRenderer.gameObject.activeSelf && !isCoverBurning;

    private Vector3 coverStartLocalPosition;
    private Vector3 coverStartLocalScale = Vector3.one;
    private Color coverStartColor = Color.white;
    private SpriteMask coverBurnMask;
    private bool isCoverBurning;
    private bool hasCapturedCoverStart;

    public void SetColorId(BlockColorId newColorId)
    {
        colorId = newColorId;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = 10;
        }

        EnsureTapCollider();
    }

    public void SetCoverActive(bool active)
    {
        ResolveCoverRenderer();
        if (coverRenderer == null)
        {
            return;
        }

        coverRenderer.DOKill();
        isCoverBurning = false;
        coverRenderer.gameObject.SetActive(active);
        coverRenderer.transform.localPosition = coverStartLocalPosition;
        coverRenderer.transform.localScale = coverStartLocalScale;
        coverRenderer.color = coverStartColor;
        coverRenderer.maskInteraction = SpriteMaskInteraction.None;
        SetCoverBurnMaskActive(false);
        SyncCoverSortingOrder();
    }

    public void BurnCoverTopToBottom()
    {
        ResolveCoverRenderer();
        if (!HasCover)
        {
            return;
        }

        isCoverBurning = true;
        coverRenderer.DOKill();
        SyncCoverSortingOrder();
        PrepareCoverBurnMask();

        float visualHeight = GetCoverVisualHeight();
        DOTween.To(() => 1f, progress =>
        {
            if (coverBurnMask == null)
            {
                return;
            }

            Transform maskTransform = coverBurnMask.transform;
            Vector3 scale = coverStartLocalScale;
            scale.y = coverStartLocalScale.y * progress;
            maskTransform.localScale = scale;

            Vector3 position = coverStartLocalPosition;
            position.y = coverStartLocalPosition.y + ((progress - 1f) * visualHeight * 0.5f);
            maskTransform.localPosition = position;
        }, 0f, coverBurnDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                isCoverBurning = false;
                coverRenderer.gameObject.SetActive(false);
                coverRenderer.transform.localPosition = coverStartLocalPosition;
                coverRenderer.transform.localScale = coverStartLocalScale;
                coverRenderer.color = coverStartColor;
                coverRenderer.maskInteraction = SpriteMaskInteraction.None;
                SetCoverBurnMaskActive(false);
            });
    }

    public void SetSortingOrder(int sortingOrder)
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = sortingOrder;
        }

        SyncCoverSortingOrder();
    }

    private void ResolveCoverRenderer()
    {
        if (coverRenderer == null)
        {
            SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i].transform == transform)
                {
                    continue;
                }

                string childName = spriteRenderers[i].name.ToLowerInvariant();
                if (childName.Contains("cover") || childName.Contains("overlay"))
                {
                    coverRenderer = spriteRenderers[i];
                    break;
                }
            }
        }

        if (coverRenderer == null && coverSprite != null)
        {
            GameObject coverObject = new GameObject("Cover");
            coverObject.transform.SetParent(transform, false);
            coverRenderer = coverObject.AddComponent<SpriteRenderer>();
            coverRenderer.sprite = coverSprite;
        }

        if (coverRenderer == null)
        {
            return;
        }

        if (!hasCapturedCoverStart)
        {
            coverStartLocalPosition = coverRenderer.transform.localPosition;
            coverStartLocalScale = coverRenderer.transform.localScale;
            coverStartColor = coverRenderer.color;
            hasCapturedCoverStart = true;
        }
    }

    private void SyncCoverSortingOrder()
    {
        if (coverRenderer == null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            coverRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            coverRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        }

        SyncCoverBurnMaskSorting();
    }

    private void PrepareCoverBurnMask()
    {
        if (coverRenderer == null)
        {
            return;
        }

        if (coverBurnMask == null)
        {
            GameObject maskObject = new GameObject("CoverBurnMask");
            maskObject.transform.SetParent(coverRenderer.transform.parent, false);
            coverBurnMask = maskObject.AddComponent<SpriteMask>();
            coverBurnMask.alphaCutoff = 0.01f;
            coverBurnMask.isCustomRangeActive = true;
        }

        coverBurnMask.sprite = coverRenderer.sprite;
        coverBurnMask.transform.localPosition = coverStartLocalPosition;
        coverBurnMask.transform.localRotation = coverRenderer.transform.localRotation;
        coverBurnMask.transform.localScale = coverStartLocalScale;
        SyncCoverBurnMaskSorting();
        SetCoverBurnMaskActive(true);
        coverRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        coverRenderer.transform.localPosition = coverStartLocalPosition;
        coverRenderer.transform.localScale = coverStartLocalScale;
        coverRenderer.color = coverStartColor;
    }

    private void SyncCoverBurnMaskSorting()
    {
        if (coverRenderer == null || coverBurnMask == null)
        {
            return;
        }

        coverBurnMask.frontSortingLayerID = coverRenderer.sortingLayerID;
        coverBurnMask.backSortingLayerID = coverRenderer.sortingLayerID;
        coverBurnMask.frontSortingOrder = coverRenderer.sortingOrder + 1;
        coverBurnMask.backSortingOrder = coverRenderer.sortingOrder - 1;
    }

    private void SetCoverBurnMaskActive(bool active)
    {
        if (coverBurnMask != null)
        {
            coverBurnMask.gameObject.SetActive(active);
        }
    }

    private float GetCoverVisualHeight()
    {
        if (coverRenderer == null || coverRenderer.sprite == null)
        {
            return 1f;
        }

        return coverRenderer.sprite.bounds.size.y * Mathf.Abs(coverStartLocalScale.y);
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
