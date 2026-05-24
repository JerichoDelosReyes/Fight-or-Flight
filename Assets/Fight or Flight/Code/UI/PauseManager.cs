// PauseManager — backward-compatibility shim.
// All pause logic and UI are now handled by GamePausedUI.cs.
public static class PauseManager
{
    public static bool IsPaused    => GamePausedUI.IsPaused;
    public static void TogglePause()  => GamePausedUI.TogglePause();
}
