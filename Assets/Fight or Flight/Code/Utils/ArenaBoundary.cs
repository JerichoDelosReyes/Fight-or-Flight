using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Manages the visible arena boundary.
///
/// At runtime:
///  • Spawns a ring of asteroid objects at ScriptsReference.ArenaRadius.
///  • Shows a red vignette at the screen edges when the player is near the boundary.
///  • Displays a pulsing cyan force-field sphere at the boundary when the player gets close.
///  • Hard-stops and pushes the player back when they cross ArenaRadius.
///  • Enemies are kept inside by EnemyAI.ApplyBoundaryCorrection + EnforceBoundary().
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

    private const int   AsteroidCount    = 120;
    private const float AsteroidScaleMult = 2.5f;
    private const float WarnThreshold    = 0.82f;  // fraction of ArenaRadius to start warning
    private const float PushForce        = 8000f;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private CanvasGroup vignetteGroup;
    private Texture2D   vignetteTex;

    private Renderer    forceFieldRenderer;
    private Material    forceFieldMat;

    private AudioSource boundaryAudio;
    private bool        wasOutside;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        BuildVignetteHUD();
        BuildForceField();
        BuildBoundaryCollider();
        StartCoroutine(SpawnAsteroidWall());
    }

    private void BuildBoundaryCollider()
    {
        // An inner-facing sphere trigger that pushes ships back.
        // We simulate it in Update() since Unity SphereColliders face outward by default.
        // The actual hard stop is in Update() — this trigger is for physics detection.
        var triggerGo = new GameObject("BoundaryTrigger");
        triggerGo.transform.SetParent(transform, false);
        var sc = triggerGo.AddComponent<SphereCollider>();
        sc.radius    = ScriptsReference.ArenaRadius;
        sc.isTrigger = true;
    }

    private void OnDestroy()
    {
        if (vignetteTex  != null) Destroy(vignetteTex);
        if (forceFieldMat != null) Destroy(forceFieldMat);
    }

    private void Update()
    {
        if (Ship.PlayerShip == null) return;

        float dist  = Ship.PlayerShip.transform.position.magnitude;
        float warn  = ScriptsReference.ArenaRadius * WarnThreshold;
        float t     = Mathf.InverseLerp(warn, ScriptsReference.ArenaRadius, dist);

        // ── Vignette ──────────────────────────────────────────────────────────
        if (vignetteGroup != null)
            vignetteGroup.alpha = Mathf.Clamp01(t * 1.4f);

        // ── Force field ───────────────────────────────────────────────────────
        if (forceFieldMat != null)
        {
            float baseAlpha  = t * 0.22f;
            float pulseAlpha = dist > warn
                ? baseAlpha + Mathf.Abs(Mathf.Sin(Time.time * 3f)) * 0.08f
                : 0f;

            // Flash brighter when the player actually hits the wall
            bool isOutside = dist >= ScriptsReference.ArenaRadius;
            if (isOutside && !wasOutside)
            {
                pulseAlpha = 0.7f;
                PlayBoundaryHitSound();
            }
            wasOutside = isOutside;

            Color col = forceFieldMat.color;
            col.a = Mathf.MoveTowards(col.a, pulseAlpha, Time.deltaTime * 3f);
            forceFieldMat.color = col;
        }

        // ── Hard boundary wall — immediate stop + pushback ───────────────────
        if (dist > ScriptsReference.ArenaRadius)
        {
            var rb = Ship.PlayerShip.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Snap to boundary surface
                Ship.PlayerShip.transform.position =
                    Ship.PlayerShip.transform.position.normalized * ScriptsReference.ArenaRadius;

                // Completely kill the outward velocity component
                Vector3 outward = Ship.PlayerShip.transform.position.normalized;
                Vector3 vel     = rb.linearVelocity;
                float   dotOut  = Vector3.Dot(vel, outward);
                if (dotOut > 0f)
                    rb.linearVelocity = vel - outward * dotOut; // remove outward component entirely

                // Strong inward push so the wall feels solid
                rb.AddForce(-outward * PushForce, ForceMode.Impulse);
            }
        }
    }

    // ── Vignette HUD ─────────────────────────────────────────────────────────

    private void BuildVignetteHUD()
    {
        vignetteTex = MakeVignetteTex(256);

        var canvasGo = new GameObject("BoundaryVignetteCanvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 190;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        var vigGo = new GameObject("VignetteOverlay");
        vigGo.transform.SetParent(canvasGo.transform, false);
        var rt = vigGo.AddComponent<RectTransform>();
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;

        vignetteGroup               = vigGo.AddComponent<CanvasGroup>();
        vignetteGroup.alpha         = 0f;
        vignetteGroup.blocksRaycasts = false;
        vignetteGroup.interactable  = false;

        var img = vigGo.AddComponent<RawImage>();
        img.texture = vignetteTex;
        img.color   = new Color(1f, 0.08f, 0.08f, 1f);
    }

    // Creates a radial gradient: transparent at centre, opaque at edges.
    private static Texture2D MakeVignetteTex(int size)
    {
        var   tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        float r   = size * 0.5f;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = (x - r + 0.5f) / r;
            float dy = (y - r + 0.5f) / r;
            float d  = Mathf.Sqrt(dx * dx + dy * dy);
            // Alpha ramps from 0 (centre) to 1 (outer 20% of radius)
            float a  = Mathf.Clamp01((d - 0.55f) / 0.45f);
            a = a * a; // softer inner fall-off
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }

        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }

    // ── Force Field Sphere ────────────────────────────────────────────────────

    private void BuildForceField()
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "ForceFieldSphere";
        sphere.transform.SetParent(transform, false);
        sphere.transform.localScale = Vector3.one * (ScriptsReference.ArenaRadius * 2f);

        // Remove the collider — the sphere is purely visual
        var col = sphere.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // Build a transparent, double-sided, additive-style material
        forceFieldMat = new Material(Shader.Find("Standard"));
        forceFieldMat.SetFloat("_Mode",     3f); // Transparent
        forceFieldMat.SetFloat("_Glossiness", 0f);
        forceFieldMat.SetFloat("_Metallic",   0f);
        forceFieldMat.SetInt("_SrcBlend",   (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        forceFieldMat.SetInt("_DstBlend",   (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        forceFieldMat.SetInt("_ZWrite",     0);
        forceFieldMat.SetInt("_Cull",       (int)UnityEngine.Rendering.CullMode.Off);
        forceFieldMat.color = new Color(0f, 0.75f, 1f, 0f); // cyan, initially invisible
        forceFieldMat.EnableKeyword("_ALPHABLEND_ON");
        forceFieldMat.renderQueue = 3000;

        forceFieldRenderer = sphere.GetComponent<Renderer>();
        forceFieldRenderer.material     = forceFieldMat;
        forceFieldRenderer.shadowCastingMode    = UnityEngine.Rendering.ShadowCastingMode.Off;
        forceFieldRenderer.receiveShadows       = false;
    }

    // ── Boundary Sound ────────────────────────────────────────────────────────

    private void PlayBoundaryHitSound()
    {
        if (boundaryAudio == null)
        {
            boundaryAudio = gameObject.AddComponent<AudioSource>();
            boundaryAudio.spatialBlend = 0f;
            boundaryAudio.volume       = 0.6f;
        }
        // Use ScreenShake as an additional tactile cue on boundary hit
        ScreenShake.Trigger(0.3f, 4f);
    }

    // ── Asteroid Wall ─────────────────────────────────────────────────────────

    private IEnumerator SpawnAsteroidWall()
    {
        yield return null;
        yield return null;

        var templateAsteroid = Object.FindAnyObjectByType<Asteroid>();
        GameObject template = templateAsteroid != null ? templateAsteroid.gameObject : null;

        float r = ScriptsReference.ArenaRadius;

        for (int i = 0; i < AsteroidCount; i++)
        {
            float baseAngle = (360f / AsteroidCount) * i;
            float jitter    = Random.Range(-180f / AsteroidCount, 180f / AsteroidCount);
            float angleDeg  = baseAngle + jitter;
            float angleRad  = angleDeg * Mathf.Deg2Rad;

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
                var ast = rock.GetComponent<Asteroid>();
                if (ast != null) Destroy(ast);
            }
            else
            {
                rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rock.transform.position = pos;
                rock.transform.rotation = Random.rotation;
            }

            float s = Random.Range(0.8f, 1.4f) * AsteroidScaleMult;
            rock.transform.localScale = Vector3.one * s;
            rock.name = "BoundaryRock";
            rock.transform.SetParent(transform, true);

            if (i % 10 == 0) yield return null;
        }
    }
}
