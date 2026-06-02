using UnityEngine;

public class PanelLose : UICanvas
{
    public void ReplayBtn()
    {
        GameManager.Instance.ReplayLevel();
    }

    public void HomeBtn()
    {
        GameManager.Instance.GoHome();
    }
}
