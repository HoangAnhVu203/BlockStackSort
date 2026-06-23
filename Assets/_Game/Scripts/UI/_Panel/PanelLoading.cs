using UnityEngine;
using UnityEngine.UI;

public class PanelLoading : UICanvas
{
    [SerializeField, Min(0f)] private float animationDuration = 1f;
    [SerializeField] private Image fillImage;
    [SerializeField] private RectTransform movingSlice;
    [SerializeField] private RectTransform sliceStartPoint;
    [SerializeField] private RectTransform sliceEndPoint;

    public float AnimationDuration => animationDuration;
    public bool IsAnimating { get; private set; }

    private Coroutine animationCoroutine;

    public override void Open()
    {
        base.Open();
        PlayLoading(animationDuration);
    }

    public override void CloseDirectly()
    {
        StopLoadingAnimation();
        base.CloseDirectly();
    }

    public void PlayLoading(float duration)
    {
        ResolveReferences();
        StopLoadingAnimation();

        animationDuration = Mathf.Max(0f, duration);
        SetProgress(0f);

        if (animationDuration <= 0f)
        {
            SetProgress(1f);
            IsAnimating = false;
            return;
        }

        IsAnimating = true;
        animationCoroutine = StartCoroutine(PlayLoadingCoroutine(animationDuration));
    }

    public void SetMinimumVisibleDuration(float duration)
    {
        animationDuration = Mathf.Max(animationDuration, duration);
    }

    private System.Collections.IEnumerator PlayLoadingCoroutine(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetProgress(Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        SetProgress(1f);
        IsAnimating = false;
        animationCoroutine = null;
    }

    private void StopLoadingAnimation()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        CancelInvoke();
        IsAnimating = false;
    }

    private void SetProgress(float progress)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = progress;
        }

        if (movingSlice != null && sliceStartPoint != null && sliceEndPoint != null)
        {
            movingSlice.anchoredPosition = Vector2.Lerp(
                sliceStartPoint.anchoredPosition,
                sliceEndPoint.anchoredPosition,
                progress);
        }
    }

    private void ResolveReferences()
    {
        if (fillImage == null)
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i].type == Image.Type.Filled)
                {
                    fillImage = images[i];
                    break;
                }
            }
        }

        if (movingSlice == null)
        {
            Transform found = FindChildByName(transform, "Slice");
            if (found == null)
            {
                found = FindChildByName(transform, "LoadingSlice");
            }

            movingSlice = found as RectTransform;
        }
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
}
