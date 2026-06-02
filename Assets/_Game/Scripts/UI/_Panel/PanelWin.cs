using UnityEngine;

public class PanelWin : UICanvas
{
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
}
