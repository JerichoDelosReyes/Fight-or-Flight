public static class PauseManager
{
    public static bool IsPaused    => GamePausedUI.IsPaused;
    public static void TogglePause()  => GamePausedUI.TogglePause();
}
