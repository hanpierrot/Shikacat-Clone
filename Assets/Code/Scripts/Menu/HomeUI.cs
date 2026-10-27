using System;
using UnityEngine;
using UnityEngine.UI;

public class HomeUI : MonoBehaviour
{
    [Serializable]
    private struct Tab
    {
        public Button button;
        public GameObject current;
        public GameObject idleIcon;
    }

    [SerializeField] private HomeScrollSnap snap;
    [SerializeField] private Tab[] tabs;
    [SerializeField] private Button settingsButton;
    [SerializeField] private SettingsPanel settingsPanel;

    private void Start()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i;
            tabs[i].button.onClick.AddListener(() =>
            {
                AudioManager.PlaySfx(Sfx.Click);
                snap.GoTo(index);
            });
        }

        snap.PageChanged += Refresh;
        Refresh(snap.CurrentPage);

        if (settingsButton != null && settingsPanel != null)
        {
            settingsPanel.SetHomeVisible(false);
            settingsButton.onClick.AddListener(() =>
            {
                AudioManager.PlaySfx(Sfx.Click);
                settingsPanel.Open();
            });
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic();
    }

    private void OnDestroy()
    {
        if (snap != null) snap.PageChanged -= Refresh;
    }

    private void Refresh(int page)
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            bool selected = i == page;
            if (tabs[i].current.activeSelf != selected) tabs[i].current.SetActive(selected);
            if (tabs[i].idleIcon != null) tabs[i].idleIcon.SetActive(!selected);
        }
    }
}
