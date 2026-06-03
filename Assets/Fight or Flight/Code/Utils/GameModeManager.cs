using UnityEngine;

public static class GameModeManager
{
    public enum Mode { Campaign, Survival }

    private const string UnlockKey = "SurvivalUnlocked";

    public static Mode Selected { get; private set; } = Mode.Campaign;

    public static bool SurvivalUnlocked => PlayerPrefs.GetInt(UnlockKey, 0) != 0;

    public static void Select(Mode m) => Selected = m;

    public static void UnlockSurvival()
    {
        PlayerPrefs.SetInt(UnlockKey, 1);
        PlayerPrefs.Save();
    }

    public static void ResetData()
    {
        PlayerPrefs.DeleteKey(UnlockKey);
        PlayerPrefs.Save();
    }
}
