using UnityEngine;

/// <summary>
/// Static game-mode selector, mirroring <see cref="DifficultyManager"/>.
///
///   • Campaign — the finite 5-wave run. Always available.
///   • Survival — endless mode. Locked until the player clears Campaign once;
///     the unlock flag persists via PlayerPrefs ("SurvivalUnlocked").
///
/// <see cref="Selected"/> is a plain static so it survives the MainMenu→MainScene
/// load. It resets to Campaign on app restart, which is fine because the mode is
/// re-chosen from the menu every play session.
/// </summary>
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
}
