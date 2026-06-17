using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlotPanelController : MonoBehaviour
{
    [SerializeField] private TMP_Text[] slotLabels;
    [SerializeField] private Button[] loadButtons;
    [SerializeField] private Button[] saveButtons;
    [SerializeField] private Button backButton;

    private Action closeAction;
    private bool allowManualSave;

    public event Action SlotLoaded;

    private void Awake()
    {
        for (int i = 0; loadButtons != null && i < loadButtons.Length; i++)
        {
            int slot = i + 1;
            if (loadButtons[i] != null) loadButtons[i].onClick.AddListener(() => Load(slot));
        }

        for (int i = 0; saveButtons != null && i < saveButtons.Length; i++)
        {
            int slot = i + 1;
            if (saveButtons[i] != null) saveButtons[i].onClick.AddListener(() => Save(slot));
        }

        if (backButton != null) backButton.onClick.AddListener(Close);
        LocalizationManager.LanguageChanged += Refresh;
        SaveGameManager.SavesChanged += RefreshIfVisible;
    }

    private void OnDestroy()
    {
        LocalizationManager.LanguageChanged -= Refresh;
        SaveGameManager.SavesChanged -= RefreshIfVisible;
    }

    public void Open(bool canSave, Action onClose)
    {
        allowManualSave = canSave;
        closeAction = onClose;
        gameObject.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        Action action = closeAction;
        closeAction = null;
        action?.Invoke();
    }

    public void Refresh()
    {
        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        SaveGameManager saves = SaveGameManager.EnsureInstance();
        int count = slotLabels != null ? slotLabels.Length : 0;

        for (int i = 0; i < count; i++)
        {
            int slot = i + 1;
            SaveSlotData data = saves.GetSlot(slot);
            string kind = slot == 1
                ? localizer.Get("save.autosave", "Автосохранение")
                : localizer.Format("save.manual_slot", slot);
            string details = FormatSlotDetails(data, localizer);

            if (slotLabels[i] != null) slotLabels[i].text = $"{kind}\n{details}";
            if (loadButtons != null && loadButtons.Length > i && loadButtons[i] != null) loadButtons[i].interactable = data != null;
            if (saveButtons != null && saveButtons.Length > i && saveButtons[i] != null)
            {
                saveButtons[i].gameObject.SetActive(allowManualSave && slot >= 2);
            }
        }
    }

    private void Load(int slot)
    {
        if (SaveGameManager.EnsureInstance().LoadSlot(slot))
        {
            Time.timeScale = 1f;
            gameObject.SetActive(false);
            SlotLoaded?.Invoke();
        }
    }

    private void Save(int slot)
    {
        if (!allowManualSave || slot < 2) return;
        SaveGameManager.EnsureInstance().SaveManual(slot);
        Refresh();
    }

    private void RefreshIfVisible()
    {
        if (isActiveAndEnabled) Refresh();
    }

    private static string FormatSlotDetails(SaveSlotData data, LocalizationManager localizer)
    {
        if (data == null) return localizer.Get("save.empty", "Пусто");

        string sceneLabel = data.sceneName;
        string sceneKey = SceneIds.GetLocalizationKey(data.sceneName);
        if (!string.IsNullOrWhiteSpace(sceneKey))
        {
            sceneLabel = localizer.Get(sceneKey, data.sceneName);
        }

        string savedAt = data.savedAtUtc;
        if (DateTime.TryParse(
                data.savedAtUtc,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out DateTime timestamp))
        {
            savedAt = timestamp.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
        }

        return localizer.Format("save.saved_at", sceneLabel, savedAt);
    }
}
