using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelChooseUI : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private string gameScene = "SampleScene";

    private void Awake()
    {
        playButton.onClick.AddListener(OnPlay);
    }

    private void OnEnable()
    {
        levelText.text = "Level " + (PlayerProgress.LoadLevelIndex() + 1);
    }

    private void OnPlay()
    {
        playButton.interactable = false;
        AudioManager.PlaySfx(Sfx.Click);
        SceneLoader.Load(gameScene);
    }
}
