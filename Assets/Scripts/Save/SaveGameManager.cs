using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public struct SavedVector3
{
    public float x;
    public float y;
    public float z;

    public SavedVector3(Vector3 value)
    {
        x = value.x;
        y = value.y;
        z = value.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

[Serializable]
public struct SavedQuaternion
{
    public float x;
    public float y;
    public float z;
    public float w;

    public SavedQuaternion(Quaternion value)
    {
        x = value.x;
        y = value.y;
        z = value.z;
        w = value.w;
    }

    public Quaternion ToQuaternion()
    {
        return new Quaternion(x, y, z, w);
    }
}

[Serializable]
public class SaveSlotData
{
    public int version = 1;
    public int slot;
    public string savedAtUtc;
    public string sceneName;
    public bool hasPlayerTransform;
    public SavedVector3 playerPosition;
    public SavedQuaternion playerRotation;
    public WorldStateSnapshot worldState;
}

public class SaveGameManager : MonoBehaviour
{
    private const string SavePrefix = "virus9.save.slot.";
    private const int AutosaveSlot = 1;
    private const int FirstManualSlot = 2;
    private const int LastManualSlot = 3;
    private const float SceneLoadAutosaveDelaySeconds = 1.5f;

    public static SaveGameManager Instance { get; private set; }
    public static event Action SavesChanged;

    private SaveSlotData pendingLoad;
    private bool skipNextAutosave;

    public static SaveGameManager EnsureInstance()
    {
        if (Instance != null) return Instance;

        SaveGameManager existing = FindFirstObjectByType<SaveGameManager>(FindObjectsInactive.Include);
        if (existing != null) return existing;

        return new GameObject("SaveGameManager").AddComponent<SaveGameManager>();
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
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        Instance = null;
    }

    public bool HasAnySave()
    {
        return GetLatestSlot() > 0;
    }

    public SaveSlotData GetSlot(int slot)
    {
        if (slot < AutosaveSlot || slot > LastManualSlot) return null;

        string json = PlayerPrefs.GetString(SavePrefix + slot, string.Empty);
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            return JsonUtility.FromJson<SaveSlotData>(json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Save slot {slot} could not be parsed: {exception.Message}");
            return null;
        }
    }

    public bool SaveAutosave()
    {
        return SaveSlot(AutosaveSlot);
    }

    public bool SaveManual(int slot)
    {
        return slot >= FirstManualSlot && slot <= LastManualSlot && SaveSlot(slot);
    }

    public void StartNewGame()
    {
        WorldState state = EnsureWorldState();
        state.ResetRun();
        pendingLoad = null;
        StartCoroutine(StartNewGameRoutine());
    }

    public bool ContinueLatest()
    {
        int slot = GetLatestSlot();
        return slot > 0 && LoadSlot(slot);
    }

    public bool LoadSlot(int slot)
    {
        SaveSlotData data = GetSlot(slot);
        if (data == null || !SceneIds.IsGameplay(data.sceneName)) return false;

        pendingLoad = data;
        skipNextAutosave = true;
        EnsureWorldState();
        SceneManager.LoadScene(data.sceneName);
        return true;
    }

    public int GetLatestSlot()
    {
        int latestSlot = 0;
        DateTime latestTimestamp = DateTime.MinValue;
        for (int slot = AutosaveSlot; slot <= LastManualSlot; slot++)
        {
            SaveSlotData data = GetSlot(slot);
            if (data == null) continue;

            if (!DateTime.TryParse(data.savedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime timestamp))
            {
                timestamp = DateTime.MinValue;
            }

            if (latestSlot == 0 || timestamp > latestTimestamp)
            {
                latestSlot = slot;
                latestTimestamp = timestamp;
            }
        }

        return latestSlot;
    }

    private bool SaveSlot(int slot)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!SceneIds.IsGameplay(scene)) return false;

        WorldState state = EnsureWorldState();
        PlayerController3D player = FindFirstObjectByType<PlayerController3D>();
        SaveSlotData data = new SaveSlotData
        {
            slot = slot,
            savedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            sceneName = scene.name,
            worldState = WorldStateSnapshot.Capture(state),
            hasPlayerTransform = player != null
        };

        if (player != null)
        {
            data.playerPosition = new SavedVector3(player.transform.position);
            data.playerRotation = new SavedQuaternion(player.transform.rotation);
        }

        PlayerPrefs.SetString(SavePrefix + slot, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        SavesChanged?.Invoke();
        return true;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!SceneIds.IsGameplay(scene)) return;

        if (pendingLoad != null)
        {
            StartCoroutine(RestorePendingLoad());
            return;
        }

        if (skipNextAutosave)
        {
            skipNextAutosave = false;
            return;
        }

        StartCoroutine(SaveAfterSceneReady());
    }

    private IEnumerator RestorePendingLoad()
    {
        yield return null;
        SaveSlotData data = pendingLoad;
        pendingLoad = null;

        WorldState state = EnsureWorldState();
        data.worldState?.Apply(state);

        PlayerController3D player = FindFirstObjectByType<PlayerController3D>();
        if (player != null && data.hasPlayerTransform)
        {
            Vector3 position = data.playerPosition.ToVector3();
            if (IsFreePosition(player, position))
            {
                player.Teleport(position, data.playerRotation.ToQuaternion());
            }
        }

        skipNextAutosave = false;
    }

    private IEnumerator StartNewGameRoutine()
    {
        skipNextAutosave = true;
        yield return SceneManager.LoadSceneAsync(SceneIds.Exterior);
        yield return new WaitForSecondsRealtime(SceneLoadAutosaveDelaySeconds);
        skipNextAutosave = false;
        SaveAutosave();
    }

    private IEnumerator SaveAfterSceneReady()
    {
        yield return new WaitForSecondsRealtime(SceneLoadAutosaveDelaySeconds);
        SaveAutosave();
    }

    private static bool IsFreePosition(PlayerController3D player, Vector3 position)
    {
        Collider playerCollider = player.GetComponent<Collider>();
        if (playerCollider == null) return true;

        Bounds currentBounds = playerCollider.bounds;
        Vector3 centerOffset = currentBounds.center - player.transform.position;
        Vector3 extents = currentBounds.extents * 0.72f;
        extents.y = Mathf.Max(0.1f, extents.y * 0.82f);
        Collider[] overlaps = Physics.OverlapBox(position + centerOffset, extents, player.transform.rotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        foreach (Collider overlap in overlaps)
        {
            if (overlap == null || overlap == playerCollider || overlap.transform.IsChildOf(player.transform)) continue;
            return false;
        }

        return true;
    }

    private static WorldState EnsureWorldState()
    {
        if (WorldState.Instance != null) return WorldState.Instance;

        WorldState existing = FindFirstObjectByType<WorldState>();
        if (existing != null) return existing;

        GameObject managers = GameObject.Find("Managers_Runtime") ?? new GameObject("Managers_Runtime");
        return managers.AddComponent<WorldState>();
    }
}
