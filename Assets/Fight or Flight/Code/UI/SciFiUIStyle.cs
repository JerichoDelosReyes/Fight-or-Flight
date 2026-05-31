using UnityEngine;

/// <summary>Shared color palette for sci-fi HUD panels.</summary>
public static class SciFiUIStyle
{
    // ── Primary brand colors ──────────────────────────────────────────────────
    public static readonly Color Teal      = new Color(0f,     1f,     0.831f, 1f);  // #00FFD4
    public static readonly Color DarkBg    = new Color(0.039f, 0.055f, 0.102f, 1f); // #0A0E1A
    public static readonly Color GreenGlow = new Color(0.224f, 1f,     0.078f, 1f); // #39FF14

    // ── Derived UI colors ─────────────────────────────────────────────────────
    public static readonly Color PanelBg     = new Color(0.039f, 0.055f, 0.102f, 0.97f);
    public static readonly Color DimButtonBg = new Color(0.04f,  0.07f,  0.13f,  0.90f);
    public static readonly Color DimText     = new Color(0.50f,  0.80f,  0.90f,  0.75f);
}
