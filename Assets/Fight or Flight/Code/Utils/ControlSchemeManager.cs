using UnityEngine;

/// <summary>
/// Static control-scheme system. Persists via PlayerPrefs.
/// Key "ControlScheme" (0 = KeyboardOnly, 1 = MouseKeyboard).
/// Key "InvertY" (0 / 1) — only meaningful in MouseKeyboard mode.
/// </summary>
public static class ControlSchemeManager
{
    public enum Scheme { KeyboardOnly, MouseKeyboard }

    private const string SchemeKey  = "ControlScheme";
    private const string InvertYKey = "InvertY";

    public static Scheme Current  { get; private set; } = Scheme.KeyboardOnly;
    public static bool   InvertY  { get; private set; } = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Load()
    {
        Current = (Scheme)PlayerPrefs.GetInt(SchemeKey, (int)Scheme.KeyboardOnly);
        InvertY = PlayerPrefs.GetInt(InvertYKey, 0) != 0;
    }

    public static void SetScheme(Scheme s)
    {
        Current = s;
        PlayerPrefs.SetInt(SchemeKey, (int)s);
        PlayerPrefs.Save();
    }

    public static void SetInvertY(bool invert)
    {
        InvertY = invert;
        PlayerPrefs.SetInt(InvertYKey, invert ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static bool IsMouseKeyboard => Current == Scheme.MouseKeyboard;
}
