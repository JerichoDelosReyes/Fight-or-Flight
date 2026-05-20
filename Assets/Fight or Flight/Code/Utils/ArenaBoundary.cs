using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Manages the visible arena boundary.
///
/// At runtime:
///  • Spawns a ring of asteroid objects at ScriptsReference.ArenaRadius
///    (tries cloning an existing scene asteroid first; falls back to primitive spheres).
///  • Shows a flashing red "BOUNDARY WARNING" HUD text when the player is within
///    15 % of the boundary edge.
///  • Hard-stops and pushes the player back when they cross ArenaRadius.
///  • Enemies are kept inside by EnemyAI.ApplyBoundaryCorrection() (already respects
///    EnemyRoamRadius which equals ArenaRadius).
///
/// Auto-creates itself in MainScene — no scene setup required.
/// </summary>
public class ArenaBoundary : MonoBehaviour
{
    // ── Auto-creation ─────────────────────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void HookSceneLoad()
    {
        SceneManager.sceneLoaded -= OnSceneLoadedStatic;
        SceneManager.sceneLoaded += OnSceneLoadedStatic;
        TryCreate(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoadedStatic(Scene scene, LoadSceneMode mode) => TryCreate(scene);

    private static void TryCreate(Scene scene)
    {
        if (scene.name != "MainScene") return;
        if (Object.FindAnyObjectByType<ArenaBoundary>() != null) return;
        new GameObject("ArenaBoundary").AddComponent<ArenaBoundary>();
    }

    // ── Config ────────────────────────────────────────────────────────────────

    private const int   AsteroidCount   = 120;
    private const float AsteroidScaleMult = 2.5f; // makes wall rocks visibly large
    private const float WarnThreshold  = 0.85f;   // fraction of ArenaRadius to start warning
    private const float PushForce      = 8000f;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private Text        warningText;
    private CanvasGroup warningGroup;
    private bool        warningVisible;
    private float       warnFlashTimer;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        BuildWarningHUD();
        StartCoroutine(SpawnAsteroidWall());
    }

    private void Update()
    {
        if (Ship.PlayerShip == null) return;

        float dist = Ship.PlayerShip.transform.position.magnitude;
        float warn = ScriptsReference.ArenaRadius * WarnThreshold;

        // ── Warning flash ─────────────────────────────────────────────────────
        bool shouldWarn = dist > warn;
        if (shouldWarn != warningVisible)
        {
            warningVisible = shouldWarn;
            if (!warningVisible && warningGroup != null)
                warningGroup.alpha = 0f;
        }

        if (warningVisible && warningGroup != null)
        {
            warnFlashTimer += Time.deltaTime * 3.5f;
            warningGroup.alpha = Mathf.Abs(Mathf.Sin(warnFlashTimer));
        }

        // ── Hard boundary push-back ───────────────────────────────────────────
        if (dist > ScriptsReference.ArenaRadius)
        {
            var rb = Ship.PlayerShip.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Clamp position to boundary surface
                Ship.PlayerShip.transform.position =
                    Ship.PlayerShip.transform.position.normalized * ScriptsReference.ArenaRadius;

                // Kill outward velocity component
                Vector3 outward = Ship.PlayerShip.transform.position.normalized;
                if (Vector3.Dot(rb.linearVelocity, outward) > 0f)
                    rb.linearVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, outward) * 0.6f;

                // Push inward
                rb.AddForce(-outward * PushForce, ForceMode.Force);
            }
        }
    }

    // ── Asteroid Wall ─────────────────────────────────────────────────────────

    private IEnumerator SpawnAsteroidWall()
    {
        // Wait two frames so the scene finishes loading (asteroids, etc.)
        yield return null;
        yield return null;

        // Try to find a scene asteroid to clone; fall back to a sphere primitive.
        var templateAsteroid = Object.FindAnyObjectByType<Asteroid>();
        GameObject template = templateAsteroid != null ? templateAsteroid.gameObject : null;

        float r = ScriptsReference.ArenaRadius;

        for (int i = 0; i < AsteroidCount; i++)
        {
            // Evenly spaced + randomised along a ring in XZ, with vertical scatter
            float baseAngle  = (360f / AsteroidCount) * i;
            float jitter     = Random.Range(-180f / AsteroidCount, 180f / AsteroidCount);
            float angleDeg   = baseAngle + jitter;
            float angleRad   = angleDeg * Mathf.Deg2Rad;

            float radialR = r + Random.Range(-r * 0.05f, r * 0.05f);
            float yOffset = Random.Range(-r * 0.08f, r * 0.08f);

            Vector3 pos = new Vector3(
                Mathf.Cos(angleRad) * radialR,
                yOffset,
                Mathf.Sin(angleRad) * radialR);

            GameObject rock;
            if (template != null)
            {
                rock = Instantiate(template, pos, Random.rotation);
                // Remove Asteroid script so they don't register in AllAsteroids
                // or self-destruct from laser hits (they're a permanent boundary)
                var ast = rock.GetComponent<Asteroid>();
                if (ast != null) Destroy(ast);
            }
            else
            {
                rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rock.transform.position = pos;
                rock.transform.rotation = Random.rotation;
            }

            // Scale up so they look like a proper wall
            float s = Random.Range(0.8f, 1.4f) * AsteroidScaleMult;
            rock.transform.localScale = Vector3.one * s;

            rock.name = "BoundaryRock";
            rock.transform.SetParent(transform, true);

            // Spread the spawning over a few frames to avoid a hitch
            if (i % 10 == 0) yield return null;
        }
    }

    // ── Warning HUD ───────────────────────────────────────────────────────────

    private void BuildWarningHUD()
    {
        var canvasGo = new GameObject("BoundaryWarningCanvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 190;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight   = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        var warnGo = new GameObject("WarningText");
        warnGo.transform.SetParent(canvasGo.transform, false);
        var rt = warnGo.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.9f);
        rt.pivot             = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition  = Vector2.zero;
        rt.sizeDelta         = new Vector2(700f, 60f);

        warningGroup               = warnGo.AddComponent<CanvasGroup>();
        warningGroup.alpha         = 0f;
        warningGroup.blocksRaycasts = false;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        warningText            = warnGo.AddComponent<Text>();
        warningText.text       = "⚠  BOUNDARY WARNING  ⚠";
        warningText.font       = font;
        warningText.fontSize   = 44;
        warningText.fontStyle  = FontStyle.Bold;
        warningText.color      = new Color(1f, 0.1f, 0.1f, 1f);
        warningText.alignment  = TextAnchor.MiddleCenter;
        warningText.horizontalOverflow = HorizontalWrapMode.Overflow;
        warningText.verticalOverflow   = VerticalWrapMode.Overflow;
    }
}
