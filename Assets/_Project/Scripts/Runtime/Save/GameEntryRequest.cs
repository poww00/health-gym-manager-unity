using UnityEngine;

public static class GameEntryRequest
{
    private const string EntryModeKey = "GYM_ENTRY_MODE";

    public enum EntryMode
    {
        None = 0,
        NewGame = 1,
        ContinueFromAutoSave = 2,
        LoadManualSlot1 = 3,
        LoadManualSlot2 = 4
    }

    public static void Set(EntryMode mode)
    {
        PlayerPrefs.SetInt(EntryModeKey, (int)mode);
        PlayerPrefs.Save();
    }

    public static EntryMode Consume()
    {
        EntryMode mode = (EntryMode)PlayerPrefs.GetInt(EntryModeKey, 0);
        PlayerPrefs.DeleteKey(EntryModeKey);
        PlayerPrefs.Save();
        return mode;
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(EntryModeKey);
        PlayerPrefs.Save();
    }
}
