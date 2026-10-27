using UnityEngine;
using UnityEngine.UI;

public class HudButtons : MonoBehaviour
{
    [SerializeField] private Button restartButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private RestartConfirmPanel restartPanel;
    [SerializeField] private SettingsPanel settingsPanel;
    [SerializeField] private string menuScene = "MainMenu";

    private void Start()
    {
        restartButton.onClick.AddListener(restartPanel.Open);
        settingsButton.onClick.AddListener(settingsPanel.Open);
        settingsPanel.HomeRequested += GoHome;
    }

    private void OnDestroy()
    {
        if (settingsPanel != null) settingsPanel.HomeRequested -= GoHome;
    }

    private void GoHome()
    {
        AudioManager.PlaySfx(Sfx.Click);
        settingsPanel.Close();
        SceneLoader.Load(menuScene);
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        bool usable = gm != null && !gm.IsGameOver && !gm.IsTutorial;

        if (restartButton.interactable != usable) restartButton.interactable = usable;
        if (settingsButton.interactable != usable) settingsButton.interactable = usable;
    }
}
