using UnityEngine;
using UnityEngine.UI;

public class SoundToggleButton : MonoBehaviour
{
    public enum Channel { Music, Sfx, Vibrate }

    [SerializeField] private Channel channel;
    [SerializeField] private Button button;
    [SerializeField] private GameObject onIcon;
    [SerializeField] private GameObject offIcon;

    private bool IsOn => channel == Channel.Music ? SoundSettings.MusicOn : channel == Channel.Sfx ? SoundSettings.SfxOn : SoundSettings.VibrateOn;

    private void OnEnable()
    {
        button.onClick.AddListener(Toggle);
        Refresh();
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(Toggle);
    }

    private void Toggle()
    {
        if (channel == Channel.Music) SoundSettings.MusicOn = !SoundSettings.MusicOn;
        else if (channel == Channel.Sfx) SoundSettings.SfxOn = !SoundSettings.SfxOn;
        else SoundSettings.VibrateOn = !SoundSettings.VibrateOn;
        Refresh();
    }

    private void Refresh()
    {
        if (onIcon != null) onIcon.SetActive(IsOn);
        if (offIcon != null) offIcon.SetActive(!IsOn);
    }
}
