using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string key;
    [SerializeField] private string fallback;

    private TMP_Text label;

    public void Configure(string newKey, string newFallback = null)
    {
        key = newKey;
        fallback = newFallback;
        if (Application.isPlaying) Refresh();
    }

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        LocalizationManager.LanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= Refresh;
    }

    public void Refresh()
    {
        if (label == null) label = GetComponent<TMP_Text>();
        LocalizationManager manager = LocalizationManager.EnsureInstance();
        label.text = manager.Get(key, fallback);
    }
}
