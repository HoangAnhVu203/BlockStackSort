using UnityEngine;
using Spine.Unity;
using UnityEngine.UI;

public class PanelWin : UICanvas
{
    [SerializeField] private SkeletonGraphic spineGraphic;
    [SerializeField] private string[] winAnimationNames = { "action1", "action2", "action3" };
    [SerializeField] private bool loopWinAnimation;
    [SerializeField] private Text winTxt;
    [SerializeField] private string[] winTexts =
    {
        "Great!",
        "Excellent!",
        "Amazing!"
    };

    public override void Open()
    {
        base.Open();
        PlayRandomWinSpine();
        ShowRandomWinText();
    }

    public void NextLevelBtn()
    {
        GameManager.Instance.PlayNextLevel();
    }

    public void ReplayBtn()
    {
        GameManager.Instance.ReplayLevel();
    }

    public void HomeBtn()
    {
        GameManager.Instance.GoHome();
    }

    private void PlayRandomWinSpine()
    {
        string animationName = GetRandomAnimationName();
        if (string.IsNullOrEmpty(animationName))
        {
            return;
        }

        ResolveSpine();

        if (spineGraphic != null)
        {
            spineGraphic.AnimationState.SetAnimation(0, animationName, loopWinAnimation);
            return;
        }
    }

    private string GetRandomAnimationName()
    {
        if (winAnimationNames == null || winAnimationNames.Length == 0)
        {
            return string.Empty;
        }

        int validCount = 0;
        for (int i = 0; i < winAnimationNames.Length; i++)
        {
            if (!string.IsNullOrEmpty(winAnimationNames[i]))
            {
                validCount++;
            }
        }

        if (validCount == 0)
        {
            return string.Empty;
        }

        int targetIndex = Random.Range(0, validCount);
        for (int i = 0; i < winAnimationNames.Length; i++)
        {
            if (string.IsNullOrEmpty(winAnimationNames[i]))
            {
                continue;
            }

            if (targetIndex == 0)
            {
                return winAnimationNames[i];
            }

            targetIndex--;
        }

        return string.Empty;
    }

    private void ResolveSpine()
    {
        if (spineGraphic == null)
        {
            spineGraphic = GetComponentInChildren<SkeletonGraphic>(true);
        }
    }

    private void ShowRandomWinText()
    {
        ResolveWinText();
        if (winTxt == null)
        {
            return;
        }

        string text = GetRandomWinText();
        if (!string.IsNullOrEmpty(text))
        {
            winTxt.text = text;
        }
    }

    private string GetRandomWinText()
    {
        if (winTexts == null || winTexts.Length == 0)
        {
            return string.Empty;
        }

        int validCount = 0;
        for (int i = 0; i < winTexts.Length; i++)
        {
            if (!string.IsNullOrEmpty(winTexts[i]))
            {
                validCount++;
            }
        }

        if (validCount == 0)
        {
            return string.Empty;
        }

        int targetIndex = Random.Range(0, validCount);
        for (int i = 0; i < winTexts.Length; i++)
        {
            if (string.IsNullOrEmpty(winTexts[i]))
            {
                continue;
            }

            if (targetIndex == 0)
            {
                return winTexts[i];
            }

            targetIndex--;
        }

        return string.Empty;
    }

    private void ResolveWinText()
    {
        if (winTxt != null)
        {
            return;
        }

        Text[] texts = GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].name == "WinTxt")
            {
                winTxt = texts[i];
                return;
            }
        }
    }
}
