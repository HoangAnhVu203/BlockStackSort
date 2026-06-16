using System;
using UnityEngine;
using UnityEngine.UI;

public class PanelShop : UICanvas
{
    [Header("Buttons")]
    [SerializeField] private Button soundBtn;
    [SerializeField] private Button musicBtn;
    [SerializeField] private Button vibrationBtn;

    [Header("Icons")]
    [SerializeField] private Image soundIcon;
    [SerializeField] private Image musicIcon;
    [SerializeField] private Image vibrationIcon;

    [Header("Sprites")]
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite soundOffSprite;
    [SerializeField] private Sprite musicOnSprite;
    [SerializeField] private Sprite musicOffSprite;
    [SerializeField] private Sprite vibrationOnSprite;
    [SerializeField] private Sprite vibrationOffSprite;

    private void OnEnable()
    {
        ResolveReferences();
        RefreshIcons();
        BindButtons();
    }

    private void OnDisable()
    {
        UnbindButtons();
    }

    private void BindButtons()
    {
        if (soundBtn != null)
        {
            soundBtn.onClick.RemoveListener(HandleSoundClicked);
            soundBtn.onClick.AddListener(HandleSoundClicked);
        }

        if (musicBtn != null)
        {
            musicBtn.onClick.RemoveListener(HandleMusicClicked);
            musicBtn.onClick.AddListener(HandleMusicClicked);
        }

        if (vibrationBtn != null)
        {
            vibrationBtn.onClick.RemoveListener(HandleVibrationClicked);
            vibrationBtn.onClick.AddListener(HandleVibrationClicked);
        }
    }

    private void UnbindButtons()
    {
        if (soundBtn != null)
        {
            soundBtn.onClick.RemoveListener(HandleSoundClicked);
        }

        if (musicBtn != null)
        {
            musicBtn.onClick.RemoveListener(HandleMusicClicked);
        }

        if (vibrationBtn != null)
        {
            vibrationBtn.onClick.RemoveListener(HandleVibrationClicked);
        }
    }

    private void HandleSoundClicked()
    {
        bool nextState = !AudioManager.IsSoundOn();
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSoundOn(nextState);
            AudioManager.Instance.PlayButton();
        }

        RefreshIcons();
    }

    private void HandleMusicClicked()
    {
        bool nextState = !AudioManager.IsMusicOn();
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicOn(nextState);
            AudioManager.Instance.PlayButton();
        }

        RefreshIcons();
    }

    private void HandleVibrationClicked()
    {
        bool nextState = !AudioManager.IsVibrationOn();
        AudioManager.SetVibrationOn(nextState);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButton();
        }

        RefreshIcons();
    }

    private void RefreshIcons()
    {
        SetIcon(soundIcon, AudioManager.IsSoundOn(), soundOnSprite, soundOffSprite);
        SetIcon(musicIcon, AudioManager.IsMusicOn(), musicOnSprite, musicOffSprite);
        SetIcon(vibrationIcon, AudioManager.IsVibrationOn(), vibrationOnSprite, vibrationOffSprite);
    }

    private void SetIcon(Image icon, bool isOn, Sprite onSprite, Sprite offSprite)
    {
        if (icon == null)
        {
            return;
        }

        Sprite targetSprite = isOn ? onSprite : offSprite;
        if (targetSprite != null)
        {
            icon.sprite = targetSprite;
        }
    }

    private void ResolveReferences()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            string buttonName = buttons[i].name;
            if (soundBtn == null && NameContains(buttonName, "Sound"))
            {
                soundBtn = buttons[i];
            }
            else if (musicBtn == null && NameContains(buttonName, "Music"))
            {
                musicBtn = buttons[i];
            }
            else if (vibrationBtn == null && (NameContains(buttonName, "Vibration") || NameContains(buttonName, "Vibrate")))
            {
                vibrationBtn = buttons[i];
            }
        }

        soundIcon = soundIcon != null ? soundIcon : ResolveIcon(soundBtn);
        musicIcon = musicIcon != null ? musicIcon : ResolveIcon(musicBtn);
        vibrationIcon = vibrationIcon != null ? vibrationIcon : ResolveIcon(vibrationBtn);
    }

    private Image ResolveIcon(Button button)
    {
        if (button == null)
        {
            return null;
        }

        Image[] images = button.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i].transform != button.transform)
            {
                return images[i];
            }
        }

        return button.GetComponent<Image>();
    }

    private bool NameContains(string target, string value)
    {
        return target.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
