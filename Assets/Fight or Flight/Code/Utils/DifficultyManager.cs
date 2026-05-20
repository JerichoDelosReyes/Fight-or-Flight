using UnityEngine;

/// <summary>
/// Static difficulty system. Persists via PlayerPrefs key "Difficulty" (0=Easy,1=Normal,2=Hard).
/// Call DifficultyManager.GetMultipliers() from any enemy or wave spawner to scale stats.
/// </summary>
public static class DifficultyManager
{
    public enum Difficulty { Easy, Normal, Hard }

    private const string PrefKey = "Difficulty";

    public static Difficulty Current { get; private set; } = Difficulty.Normal;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Load()
    {
        Current = (Difficulty)PlayerPrefs.GetInt(PrefKey, (int)Difficulty.Normal);
    }

    public static void Set(Difficulty d)
    {
        Current = d;
        PlayerPrefs.SetInt(PrefKey, (int)d);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Per-difficulty multipliers consumed by EnemyAI.
    /// fireRate multiplies the fire-rate INTERVAL, so values > 1 mean slower fire.
    /// aggression scales turn speed/throttle (and inversely scales orbit radius)
    /// to make Hard enemies push the player harder.
    /// </summary>
    public static (float health, float speed, float fireRate, float aggression) GetMultipliers()
    {
        switch (Current)
        {
            case Difficulty.Easy:   return (0.70f, 0.80f, 1.50f, 0.85f);
            case Difficulty.Hard:   return (1.50f, 1.30f, 0.65f, 1.30f);
            default:                return (1.00f, 1.00f, 1.00f, 1.00f); // Normal
        }
    }

    /// <summary>Multiplier on wave enemy counts. Hard ramps the swarm; Easy thins it.</summary>
    public static float EnemyCountMultiplier
    {
        get
        {
            switch (Current)
            {
                case Difficulty.Easy: return 0.85f;
                case Difficulty.Hard: return 1.35f;
                default:              return 1.00f;
            }
        }
    }

    public static string CurrentName()
    {
        switch (Current)
        {
            case Difficulty.Easy: return "Easy";
            case Difficulty.Hard: return "Hard";
            default:              return "Normal";
        }
    }
}
