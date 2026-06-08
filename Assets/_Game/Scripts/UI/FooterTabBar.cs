using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FooterTabBar : UICanvas
{
    [System.Serializable]
    public class Tab
    {
        public Button button;
        public RectTransform root;
        public RectTransform icon;
        public GameObject labelGO;
        public Image targetImage;
        public Sprite normalSprite;
        public Sprite selectedSprite;
    }

    [Header("Refs")]
    [SerializeField] private List<Tab> tabs = new();

    [Header("Anim")]
    [SerializeField] private float selectedScale = 1.12f;
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float scaleDuration = 0.12f;
    [SerializeField] private int homeIndex = 1;
    [SerializeField] private TabTransitionController tabTransition;

    private readonly Dictionary<RectTransform, Coroutine> scaleCoroutines = new();
    private int currentIndex = -1;
    private bool suppressNotify;

    private void Awake()
    {
        BindButtons();
        AutoBind();
    }

    private void OnEnable()
    {
        SetFooterY(100f);
        AutoBind();
        BindButtons();
        StartCoroutine(ResetDefaultNextFrame());
    }

    private void SetFooterY(float y)
    {
        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform == null)
        {
            return;
        }

        Vector2 anchoredPosition = rectTransform.anchoredPosition;
        anchoredPosition.y = y;
        rectTransform.anchoredPosition = anchoredPosition;
    }

    private IEnumerator ResetDefaultNextFrame()
    {
        yield return null;
        SelectHome(false);
    }

    public void SelectHome(bool animate) => Select(homeIndex, animate, true);

    public void SelectFromTransition(int index, bool animate)
    {
        suppressNotify = true;
        Select(index, animate, true);
        suppressNotify = false;
    }

    public void Select(int index, bool animate) => Select(index, animate, false);

    private void Select(int index, bool animate, bool force)
    {
        if (tabs == null || tabs.Count == 0)
        {
            return;
        }

        index = Mathf.Clamp(index, 0, tabs.Count - 1);
        if (index == currentIndex && !force)
        {
            return;
        }

        for (int i = 0; i < tabs.Count; i++)
        {
            bool active = i == index;
            ApplyTabState(tabs[i], active, animate);
        }

        currentIndex = index;
        NotifySwitch(index);
    }

    private void ApplyTabState(Tab tab, bool active, bool animate)
    {
        if (tab == null)
        {
            return;
        }

        ResolveTabReferences(tab);

        if (tab.labelGO != null)
        {
            tab.labelGO.SetActive(active);
        }

        if (tab.targetImage != null)
        {
            Sprite targetSprite = active && tab.selectedSprite != null ? tab.selectedSprite : tab.normalSprite;
            if (targetSprite != null)
            {
                tab.targetImage.sprite = targetSprite;
            }
        }

        RectTransform targetRoot = tab.root != null ? tab.root : tab.button != null ? tab.button.transform as RectTransform : null;
        if (targetRoot == null)
        {
            return;
        }

        Vector3 targetScale = Vector3.one * (active ? selectedScale : normalScale);
        if (!animate)
        {
            targetRoot.localScale = targetScale;
            return;
        }

        if (scaleCoroutines.TryGetValue(targetRoot, out Coroutine running) && running != null)
        {
            StopCoroutine(running);
        }

        scaleCoroutines[targetRoot] = StartCoroutine(ScaleTo(targetRoot, targetScale));
    }

    private IEnumerator ScaleTo(RectTransform target, Vector3 targetScale)
    {
        Vector3 startScale = target.localScale;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, scaleDuration);
            float k = EaseOutCubic(t);
            target.localScale = Vector3.LerpUnclamped(startScale, targetScale, k);
            yield return null;
        }

        target.localScale = targetScale;
    }

    private float EaseOutCubic(float x)
    {
        x = Mathf.Clamp01(x);
        float a = 1f - x;
        return 1f - a * a * a;
    }

    private void BindButtons()
    {
        if (tabs == null)
        {
            return;
        }

        for (int i = 0; i < tabs.Count; i++)
        {
            ResolveTabReferences(tabs[i]);
            Button button = tabs[i].button;
            if (button == null)
            {
                continue;
            }

            Navigation nav = new Navigation { mode = Navigation.Mode.None };
            button.navigation = nav;

            int index = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => Select(index, true));
        }
    }

    private void ResolveTabReferences(Tab tab)
    {
        if (tab == null)
        {
            return;
        }

        if (tab.root == null && tab.button != null)
        {
            tab.root = tab.button.transform as RectTransform;
        }

        if (tab.targetImage == null && tab.button != null)
        {
            tab.targetImage = tab.button.GetComponent<Image>();
        }
    }

    private void AutoBind()
    {
        tabTransition = GetComponent<TabTransitionController>();
        if (tabTransition != null)
        {
            return;
        }

        tabTransition = gameObject.AddComponent<TabTransitionController>();
    }

    public void SetTransitionController(TabTransitionController controller)
    {
        tabTransition = controller;
    }

    private void NotifySwitch(int index)
    {
        if (suppressNotify || tabTransition == null)
        {
            return;
        }

        if (index == 0) tabTransition.SwitchToLock();
        else if (index == 1) tabTransition.SwitchToHome();
        else if (index == 2) tabTransition.SwitchToCalendar();
    }
}
