using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FrontendMenuController : MonoBehaviour
{
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject introductionPanel;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private TMP_Text confirmText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button introContinueButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button loadSaveButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button creditsBackButton;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;
    [SerializeField] private TMP_Dropdown languageDropdown;
    [SerializeField] private SettingsPanelController settingsPanel;
    [SerializeField] private SaveSlotPanelController saveSlotPanel;

    private System.Action confirmAction;

    private void Awake()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        LocalizationManager.EnsureInstance();
        SettingsManager.EnsureInstance();
        SaveGameManager.EnsureInstance();

        startButton?.onClick.AddListener(() => ShowOnly(introductionPanel));
        introContinueButton?.onClick.AddListener(() => ShowOnly(mainMenuPanel));
        newGameButton?.onClick.AddListener(() => SaveGameManager.Instance.StartNewGame());
        continueButton?.onClick.AddListener(() => SaveGameManager.Instance.ContinueLatest());
        loadSaveButton?.onClick.AddListener(() => OpenSaveSlots(false));
        settingsButton?.onClick.AddListener(OpenSettings);
        creditsButton?.onClick.AddListener(() => ShowOnly(creditsPanel));
        exitButton?.onClick.AddListener(() => OpenConfirm("confirm.exit", QuitGame));
        creditsBackButton?.onClick.AddListener(() => ShowOnly(mainMenuPanel));
        confirmYesButton?.onClick.AddListener(ConfirmYes);
        confirmNoButton?.onClick.AddListener(ConfirmNo);
        languageDropdown?.onValueChanged.AddListener(value => LocalizationManager.Instance.SetLanguage((GameLanguage)value));

        RefreshLanguageDropdown();
        LocalizationManager.LanguageChanged += RefreshLanguageDropdown;
        SaveGameManager.SavesChanged += RefreshContinueButton;
        ShowOnly(startPanel);
    }

    private void OnDestroy()
    {
        LocalizationManager.LanguageChanged -= RefreshLanguageDropdown;
        SaveGameManager.SavesChanged -= RefreshContinueButton;
    }

    private void OpenSettings()
    {
        if (settingsPanel == null)
        {
            ShowOnly(mainMenuPanel);
            return;
        }

        HideBasePanels();
        settingsPanel.Open(() => ShowOnly(mainMenuPanel));
    }

    private void OpenSaveSlots(bool canSave)
    {
        if (saveSlotPanel == null)
        {
            ShowOnly(mainMenuPanel);
            return;
        }

        HideBasePanels();
        saveSlotPanel.Open(canSave, () => ShowOnly(mainMenuPanel));
    }

    private void OpenConfirm(string key, System.Action action)
    {
        confirmAction = action;
        if (confirmText != null) confirmText.text = LocalizationManager.Instance.Get(key);
        if (confirmPanel != null) confirmPanel.SetActive(true);
    }

    private void ConfirmYes()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);
        System.Action action = confirmAction;
        confirmAction = null;
        action?.Invoke();
    }

    private void ConfirmNo()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);
        confirmAction = null;
    }

    private void ShowOnly(GameObject panel)
    {
        HideBasePanels();
        panel?.SetActive(true);
    }

    private void HideBasePanels()
    {
        startPanel?.SetActive(false);
        introductionPanel?.SetActive(false);
        mainMenuPanel?.SetActive(false);
        creditsPanel?.SetActive(false);
        confirmPanel?.SetActive(false);
        if (settingsPanel != null) settingsPanel.gameObject.SetActive(false);
        if (saveSlotPanel != null) saveSlotPanel.gameObject.SetActive(false);
    }

    private void RefreshLanguageDropdown()
    {
        if (languageDropdown == null)
        {
            RefreshContinueButton();
            return;
        }

        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        int currentLanguage = (int)localizer.CurrentLanguage;
        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(new List<string>
        {
            localizer.Get("language.russian", "Русский"),
            localizer.Get("language.english", "English"),
            localizer.Get("language.portuguese", "Português")
        });
        languageDropdown.SetValueWithoutNotify(currentLanguage);
        RefreshContinueButton();
    }

    private void RefreshContinueButton()
    {
        if (continueButton != null) continueButton.interactable = SaveGameManager.EnsureInstance().HasAnySave();
    }

    private static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
