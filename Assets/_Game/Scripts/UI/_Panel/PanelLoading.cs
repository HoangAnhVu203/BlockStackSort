using UnityEngine;

public class PanelLoading : UICanvas
{
    [SerializeField, Min(0f)] private float animationDuration = 1f;

    public float AnimationDuration => animationDuration;
    public bool IsAnimating { get; private set; }

    public override void Open()
    {
        base.Open();
        IsAnimating = animationDuration > 0f;
        CancelInvoke(nameof(CompleteAnimation));
        if (IsAnimating)
        {
            Invoke(nameof(CompleteAnimation), animationDuration);
        }
    }

    public void SetMinimumVisibleDuration(float duration)
    {
        animationDuration = Mathf.Max(animationDuration, duration);
    }

    private void CompleteAnimation()
    {
        IsAnimating = false;
    }
}
