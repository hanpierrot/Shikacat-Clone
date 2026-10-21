using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoosterButton : MonoBehaviour
{
    [SerializeField] private BoosterType type;
    [SerializeField] private Button button;
    [SerializeField] private GameObject unlockedRoot;
    [SerializeField] private GameObject lockedRoot;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private GameObject addIcon;
    [SerializeField] private GameObject activeMark;
    
    private void OnEnable()
    {
        button.onClick.AddListener(OnClick);
        BoosterInventory.Changed += HandleInventoryChanged;
        BoosterManager.Instance.Unlocked += HandleUnlocked;
        Refresh();
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(OnClick);
        BoosterInventory.Changed -= HandleInventoryChanged;
        if (BoosterManager.Instance != null) BoosterManager.Instance.Unlocked -= HandleUnlocked;
    }

    private void Update()
    {
        bool usable = BoosterManager.Instance.IsUnlocked(type) && GameManager.Instance != null && !GameManager.Instance.IsGameOver;
        if (button.interactable != usable) button.interactable = usable;
    }
    
    public void SetActiveMark(bool active)
    {
        if (activeMark != null) activeMark.SetActive(active);
    }

    private void OnClick() => BoosterManager.Instance.TryUse(type);

    private void HandleInventoryChanged(BoosterType changed, int count)
    {
        if (changed == type) Refresh();
    }

    private void HandleUnlocked(BoosterType unlocked)
    {
        if (unlocked == type) Refresh();
    }

    private void Refresh()
    {
        bool unlocked = BoosterManager.Instance.IsUnlocked(type);
        int count = BoosterInventory.Count(type);
        
        if (unlockedRoot != null) unlockedRoot.SetActive(unlocked);
        if (lockedRoot != null) lockedRoot.SetActive(!unlocked);

        bool hasStock = count > 0;
        if (countText != null)
        {
            countText.gameObject.SetActive(unlocked && hasStock);
            countText.text = count.ToString();
        }
        if (addIcon != null) addIcon.SetActive(unlocked && !hasStock);
    }
}
