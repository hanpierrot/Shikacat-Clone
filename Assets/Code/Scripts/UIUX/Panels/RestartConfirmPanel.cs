using UnityEngine;
using UnityEngine.UI;

public class RestartConfirmPanel : ModalPanel
{
    [SerializeField] private Button replayButton;

    protected override void Awake()
    {
        base.Awake();
        replayButton.onClick.AddListener(OnReplay);
    }

    private void OnReplay()
    {
        Close();
        GameManager.Instance.RestartLevel();
    }
}
