using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LocalizationCatalog", menuName = "Virus 9/Localization Catalog")]
public class LocalizationCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string key;
        [TextArea] public string russian;
        [TextArea] public string english;
        [TextArea] public string portuguese;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    public IReadOnlyList<Entry> Entries => entries;

    public void ReplaceEntries(IEnumerable<Entry> newEntries)
    {
        entries.Clear();
        if (newEntries != null) entries.AddRange(newEntries);
    }
}
