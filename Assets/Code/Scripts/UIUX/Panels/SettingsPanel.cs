using System;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : ModalPanel
{
    [SerializeField] private Button homeButton;
    [SerializeField] private Text versionText;

    public event Action HomeRequested;

    protected override void Awake()
    {
        base.Awake();
        homeButton.onClick.AddListener(() => HomeRequested?.Invoke());
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (versionText != null) versionText.text = "Ver " + Application.version;
    }
}
