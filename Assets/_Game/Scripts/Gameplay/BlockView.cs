using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Spine.Unity;

public class BlockView : MonoBehaviour
{
    private const int MaxFreezeLayerCount = 3;
    private const float SpineBlockLocalYOffset = -0.5f;

    [SerializeField] private BlockColorId colorId;
    [SerializeField] private SpriteRenderer coverRenderer;
    [SerializeField] private Sprite coverSprite;
    [SerializeField] private float coverBurnDuration = 0.25f;
    [SerializeField] private GameObject coverDisappearVfx;
    [SerializeField] private SpriteRenderer freezeRenderer;

    public BlockColorId ColorId => colorId;
    public bool HasCover => coverRenderer != null && coverRenderer.gameObject.activeSelf && !isCoverBurning;
    public float SlotLocalYOffset => IsSpineBlock() ? SpineBlockLocalYOffset : 0f;

    private Vector3 coverStartLocalPosition;
    private Vector3 coverStartLocalScale = Vector3.one;
    private Color coverStartColor = Color.white;
    private SpriteMask coverBurnMask;
    private bool isCoverBurning;
    private bool hasCapturedCoverStart;
    private readonly List<SpriteRenderer> freezeLayers = new List<SpriteRenderer>();
    private SkeletonAnimation skeletonAnimation;

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
        PlayCoverDisappearVfx();

        DOTween.To(() => 0f, progress =>
        {
            if (coverBurnMask == null)
            {
                return;
            }

            Transform maskTransform = coverBurnMask.transform;
            float maskScale = Mathf.Lerp(0.05f, 1.15f, progress);
            Vector3 scale = new Vector3(
                coverStartLocalScale.x * maskScale,
                coverStartLocalScale.y * maskScale,
                coverStartLocalScale.z);
            maskTransform.localScale = scale;

            maskTransform.localPosition = coverStartLocalPosition;
        }, 1f, coverBurnDuration)
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

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingOrder = sortingOrder;
        }

        SyncCoverSortingOrder();
        SyncFreezeSortingOrder();
    }

    public void SnapToSlot(Transform slot)
    {
        if (slot == null)
        {
            return;
        }

        transform.SetParent(slot, true);
        transform.position = GetSlotPosition(slot);
        transform.localScale = Vector3.one;
    }

    public Vector3 GetSlotPosition(Transform slot)
    {
        if (slot == null)
        {
            return transform.position;
        }

        return GetPositionWithSlotOffset(slot.position, slot);
    }

    public Vector3 GetPositionWithSlotOffset(Vector3 position, Transform referenceSlot)
    {
        if (referenceSlot == null)
        {
            return position + new Vector3(0f, SlotLocalYOffset, 0f);
        }

        return position + referenceSlot.TransformVector(new Vector3(0f, SlotLocalYOffset, 0f));
    }

    public void PlaySolvedSpineAnimation(float delay = 0f)
    {
        ResolveSkeletonAnimation();
        if (skeletonAnimation == null)
        {
            return;
        }

        if (delay > 0f)
        {
            DOVirtual.DelayedCall(delay, PlaySolvedSpineAnimationNow).SetTarget(this);
            return;
        }

        PlaySolvedSpineAnimationNow();
    }

    public Vector3 GetRootPositionForVisualCenter(Vector3 targetVisualCenter)
    {
        if (!TryGetVisualBounds(out Bounds visualBounds))
        {
            return targetVisualCenter;
        }

        Vector3 visualOffset = visualBounds.center - transform.position;
        visualOffset.z = 0f;
        return targetVisualCenter - visualOffset;
    }

    public void SetFreezeVisible(bool visible, float alpha)
    {
        SetFreezeLayerCount(visible ? MaxFreezeLayerCount : 0);
    }

    public void FadeOutFreeze(float duration)
    {
        SetFreezeLayerCount(0);
    }

    public void SetFreezeLayerCount(int activeLayerCount)
    {
        ResolveFreezeRenderers();
        if (freezeLayers.Count == 0)
        {
            return;
        }

        activeLayerCount = Mathf.Clamp(activeLayerCount, 0, freezeLayers.Count);
        int hiddenLayerCount = freezeLayers.Count - activeLayerCount;
        for (int i = 0; i < freezeLayers.Count; i++)
        {
            SpriteRenderer layer = freezeLayers[i];
            if (layer == null)
            {
                continue;
            }

            layer.DOKill();
            Color color = layer.color;
            color.a = 1f;
            layer.color = color;
            layer.gameObject.SetActive(i >= hiddenLayerCount);
        }

        SyncFreezeSortingOrder();
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

    private bool TryGetVisualBounds(out Bounds visualBounds)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(false);
        bool hasBounds = false;
        visualBounds = new Bounds(transform.position, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !ShouldUseRendererForVisualCenter(renderer))
            {
                continue;
            }

            if (!hasBounds)
            {
                visualBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                visualBounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private bool ShouldUseRendererForVisualCenter(Renderer renderer)
    {
        if (renderer == null)
        {
            return false;
        }

        string rendererName = renderer.name.ToLowerInvariant();
        if (rendererName.Contains("cover")
            || rendererName.Contains("overlay")
            || rendererName.Contains("freeze")
            || rendererName.Contains("ice"))
        {
            return false;
        }

        return renderer.GetComponentInParent<BlockView>() == this;
    }

    private bool IsSpineBlock()
    {
        ResolveSkeletonAnimation();
        return skeletonAnimation != null;
    }

    private void ResolveSkeletonAnimation()
    {
        if (skeletonAnimation != null)
        {
            return;
        }

        skeletonAnimation = GetComponentInChildren<SkeletonAnimation>(true);
    }

    private void PlaySolvedSpineAnimationNow()
    {
        ResolveSkeletonAnimation();
        if (skeletonAnimation == null
            || skeletonAnimation.AnimationState == null
            || skeletonAnimation.Skeleton == null
            || skeletonAnimation.Skeleton.Data == null
            || skeletonAnimation.Skeleton.Data.FindAnimation("action2") == null)
        {
            return;
        }

        skeletonAnimation.AnimationState.SetAnimation(0, "action2", false);
        if (skeletonAnimation.Skeleton.Data.FindAnimation("action1") != null)
        {
            skeletonAnimation.AnimationState.AddAnimation(0, "action1", true, 0f);
        }
    }

    private void ResolveFreezeRenderers()
    {
        if (freezeLayers.Count > 0)
        {
            return;
        }

        SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i].transform == transform)
            {
                continue;
            }

            string childName = spriteRenderers[i].name.ToLowerInvariant();
            if (childName.Contains("freeze") || childName.Contains("ice"))
            {
                freezeLayers.Add(spriteRenderers[i]);
            }
        }

        freezeLayers.Sort((a, b) => GetFreezeLayerOrder(a).CompareTo(GetFreezeLayerOrder(b)));
        freezeRenderer = freezeLayers.Count > 0 ? freezeLayers[freezeLayers.Count - 1] : null;
    }

    private void SyncFreezeSortingOrder()
    {
        ResolveFreezeRenderers();
        if (freezeLayers.Count == 0)
        {
            return;
        }

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            return;
        }

        for (int i = 0; i < freezeLayers.Count; i++)
        {
            if (freezeLayers[i] == null)
            {
                continue;
            }

            freezeLayers[i].sortingLayerID = spriteRenderer.sortingLayerID;
            freezeLayers[i].sortingOrder = spriteRenderer.sortingOrder + 2 + (freezeLayers.Count - 1 - i);
        }
    }

    private int GetFreezeLayerOrder(SpriteRenderer layer)
    {
        string layerName = layer != null ? layer.name.ToLowerInvariant() : string.Empty;
        if (layerName.Contains("freeze1") || layerName.Contains("ice1"))
        {
            return 1;
        }

        if (layerName.Contains("freeze2") || layerName.Contains("ice2"))
        {
            return 2;
        }

        return 3;
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
        coverBurnMask.transform.localScale = new Vector3(
            coverStartLocalScale.x * 0.05f,
            coverStartLocalScale.y * 0.05f,
            coverStartLocalScale.z);
        SyncCoverBurnMaskSorting();
        SetCoverBurnMaskActive(true);
        coverRenderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
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

    private void PlayCoverDisappearVfx()
    {
        ResolveCoverDisappearVfx();
        if (coverDisappearVfx == null)
        {
            return;
        }

        coverDisappearVfx.SetActive(true);

        ParticleSystem[] particleSystems = coverDisappearVfx.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            particleSystems[i].gameObject.SetActive(true);
            particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystems[i].Play(true);
        }
    }

    private void ResolveCoverDisappearVfx()
    {
        if (coverDisappearVfx != null)
        {
            return;
        }

        ParticleSystem particleSystem = GetComponentInChildren<ParticleSystem>(true);
        if (particleSystem != null)
        {
            coverDisappearVfx = particleSystem.gameObject;
            return;
        }

        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] == transform)
            {
                continue;
            }

            string childName = children[i].name.ToLowerInvariant();
            if (childName.Contains("vfx") || childName.Contains("effect"))
            {
                coverDisappearVfx = children[i].gameObject;
                return;
            }
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
