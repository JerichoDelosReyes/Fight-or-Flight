using UnityEngine;

/// <summary>
/// Static control-scheme system. Persists via PlayerPrefs.
/// Key "ControlScheme"        (0 = KeyboardOnly, 1 = MouseKeyboard).
/// Key "InvertY"              (0 / 1) — only meaningful in MouseKeyboard mode.
/// Key "InvertPitchKeyboard"  (0 / 1) — only meaningful in KeyboardOnly mode.
/// </summary>
public static class ControlSchemeManager
{
    public enum Scheme { KeyboardOnly, MouseKeyboard }

    private const string SchemeKey               = "ControlScheme";
    private const string InvertYKey              = "InvertY";
    private const string InvertPitchKeyboardKey  = "InvertPitchKeyboard";

    public static Scheme Current              { get; private set; } = Scheme.KeyboardOnly;
    public static bool   InvertY              { get; private set; } = false;
    public static bool   InvertPitchKeyboard  { get; private set; } = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Load()
    {
        Current             = (Scheme)PlayerPrefs.GetInt(SchemeKey, (int)Scheme.KeyboardOnly);
        InvertY             = PlayerPrefs.GetInt(InvertYKey, 0) != 0;
        InvertPitchKeyboard = PlayerPrefs.GetInt(InvertPitchKeyboardKey, 0) != 0;
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

    public static void SetInvertPitchKeyboard(bool invert)
    {
        InvertPitchKeyboard = invert;
        PlayerPrefs.SetInt(InvertPitchKeyboardKey, invert ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static bool IsMouseKeyboard => Current == Scheme.MouseKeyboard;
}
