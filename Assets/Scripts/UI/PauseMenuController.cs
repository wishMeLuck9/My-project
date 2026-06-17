using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    private const string PrefabResource = "UI/PauseMenu";

    public static PauseMenuController Instance { get; private set; }

    [SerializeField] private GameObject pauseDimmer;
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private TMP_Text confirmText;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button loadSaveButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;
    [SerializeField] private SettingsPanelController settingsPanel;
    [SerializeField] private SaveSlotPanelController saveSlotPanel;

    private PlayerInputReader inputReader;
    private System.Action confirmAction;
    private float nextResolveAt;

    public bool IsPaused { get; private set; }

    public static PauseMenuController EnsureInstance()
    {
        if (Instance != null) return Instance;

        PauseMenuController existing = FindFirstObjectByType<PauseMenuController>(FindObjectsInactive.Include);
        if (existing != null) return existing;

        GameObject prefab = Resources.Load<GameObject>(PrefabResource);
        if (prefab == null) return null;
        return Instantiate(prefab).GetComponent<PauseMenuController>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        resumeButton?.onClick.AddListener(Close);
        settingsButton?.onClick.AddListener(OpenSettings);
        loadSaveButton?.onClick.AddListener(OpenSaveSlots);
        mainMenuButton?.onClick.AddListener(() => OpenConfirm("confirm.main_menu", ReturnToMainMenu));
        exitButton?.onClick.AddListener(() => OpenConfirm("confirm.exit", QuitGame));
        confirmYesButton?.onClick.AddListener(ConfirmYes);
        confirmNoButton?.onClick.AddListener(ConfirmNo);
        if (saveSlotPanel != null) saveSlotPanel.SlotLoaded += Close;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        if (pauseDimmer != null) pauseDimmer.SetActive(false);
        if (rootPanel != null) rootPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        BindInput(null);
        if (saveSlotPanel != null) saveSlotPanel.SlotLoaded -= Close;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        Time.timeScale = 1f;
        Instance = null;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextResolveAt) return;
        nextResolveAt = Time.unscaledTime + 0.5f;
        PlayerController3D player = FindFirstObjectByType<PlayerController3D>();
        BindInput(player != null ? player.GetComponent<PlayerInputReader>() : null);
    }

    public void Open()
    {
        if (!SceneIds.IsGameplay(SceneManager.GetActiveScene())) return;

        IsPaused = true;
        Time.timeScale = 0f;
        if (pauseDimmer != null) pauseDimmer.SetActive(true);
        if (rootPanel != null) rootPanel.SetActive(true);
        ShowRootOnly();
        UpdateCursor();
    }

    public void Close()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        if (pauseDimmer != null) pauseDimmer.SetActive(false);
        if (rootPanel != null) rootPanel.SetActive(false);
        UpdateCursor();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindInput(null);
        if (!SceneIds.IsGameplay(scene)) Close();
    }

    private void BindInput(PlayerInputReader newInput)
    {
        if (inputReader == newInput) return;
        if (inputReader != null) inputReader.PausePressed -= Toggle;
        inputReader = newInput;
        if (inputReader != null) inputReader.PausePressed += Toggle;
    }

    private void Toggle()
    {
        if (IsPaused) Close();
        else Open();
    }

    private void OpenSettings()
    {
        if (settingsPanel == null)
        {
            ShowRootOnly();
            return;
        }

        if (rootPanel != null) rootPanel.SetActive(false);
        settingsPanel.Open(() =>
        {
            if (rootPanel != null) rootPanel.SetActive(true);
            ShowRootOnly();
        });
    }

    private void OpenSaveSlots()
    {
        if (saveSlotPanel == null)
        {
            ShowRootOnly();
            return;
        }

        if (rootPanel != null) rootPanel.SetActive(false);
        saveSlotPanel.Open(true, () =>
        {
            if (rootPanel != null) rootPanel.SetActive(true);
            ShowRootOnly();
        });
    }

    private void OpenConfirm(string key, System.Action action)
    {
        confirmAction = action;
        if (confirmText != null) confirmText.text = LocalizationManager.EnsureInstance().Get(key);
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

    private void ShowRootOnly()
    {
        if (settingsPanel != null) settingsPanel.gameObject.SetActive(false);
        if (saveSlotPanel != null) saveSlotPanel.gameObject.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (rootPanel != null) rootPanel.SetActive(true);
    }

    private void ReturnToMainMenu()
    {
        SaveGameManager.EnsureInstance().SaveAutosave();
        Close();
        SceneManager.LoadScene(SceneIds.Menu);
    }

    private void UpdateCursor()
    {
        bool dialogueOpen = DialogueController.Instance != null && DialogueController.Instance.IsDialogueOpen;
        bool release = IsPaused || dialogueOpen;
        Cursor.lockState = release ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = release;
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
