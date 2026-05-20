using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scatters static rock/asteroid debris throughout the arena at startup.
/// Auto-creates itself in MainScene — no scene wiring required.
///
/// Phase 1 — dense ring near the boundary wall (reinforces the arena edge).
/// Phase 2 — medium clusters in the mid-arena for cover and combat obstacles.
///
/// Rocks are purely static (Rigidbody removed if present).
/// </summary>
public class DebrisScatter : MonoBehaviour
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
        if (Object.FindAnyObjectByType<DebrisScatter>() != null) return;
        new GameObject("DebrisScatter").AddComponent<DebrisScatter>();
    }

    // ── Config ────────────────────────────────────────────────────────────────

    private const float ArenaR        = 12000f; // matches ScriptsReference.ArenaRadius
    private const float ClearRadius   = 1500f;  // keep area around origin clear
    private const int   BoundaryRocks = 120;    // dense outer ring
    private const int   MidRocks      = 100;    // mid-arena scatter
    private const int   ClusterRocks  = 60;     // small concentrated clusters

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        StartCoroutine(ScatterDebris());
    }

    // ── Scatter ───────────────────────────────────────────────────────────────

    private IEnumerator ScatterDebris()
    {
        // Wait a couple of frames so the scene (asteroids etc.) has finished loading.
        yield return null;
        yield return null;

        // Collect template objects to clone from scene or project.
        var templates = GatherTemplates();
        if (templates.Count == 0)
        {
            // Nothing to scatter — fall back to sphere primitives so something visible appears.
            templates.Add(null);
        }

        int spawned = 0;

        // ── Phase 1: Dense boundary ring ──────────────────────────────────────
        for (int i = 0; i < BoundaryRocks; i++)
        {
            float angle  = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = Random.Range(ArenaR * 0.80f, ArenaR * 0.97f);
            float yOff   = Random.Range(-ArenaR * 0.12f, ArenaR * 0.12f);
            Vector3 pos  = new Vector3(Mathf.Cos(angle) * radius, yOff,
                                       Mathf.Sin(angle) * radius);

            SpawnRock(templates, pos,
                      scaleMin: 0.9f, scaleMax: 2.8f);
            spawned++;
            if (spawned % 10 == 0) yield return null;
        }

        // ── Phase 2: Mid-arena scatter ────────────────────────────────────────
        for (int i = 0; i < MidRocks; i++)
        {
            Vector3 pos;
            int tries = 0;
            do
            {
                pos = Random.insideUnitSphere * (ArenaR * 0.75f);
                tries++;
            }
            while (pos.magnitude < ClearRadius && tries < 20);

            if (pos.magnitude < ClearRadius) continue;

            SpawnRock(templates, pos, scaleMin: 0.4f, scaleMax: 1.8f);
            spawned++;
            if (spawned % 10 == 0) yield return null;
        }

        // ── Phase 3: Concentrated clusters (combat cover) ────────────────────
        int numClusters = 8;
        int rocksPerCluster = ClusterRocks / numClusters;
        for (int c = 0; c < numClusters; c++)
        {
            // Pick a cluster centre in the mid-arena band
            float clusterAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float clusterR     = Random.Range(ArenaR * 0.25f, ArenaR * 0.68f);
            float clusterY     = Random.Range(-ArenaR * 0.10f, ArenaR * 0.10f);
            Vector3 clusterCenter = new Vector3(
                Mathf.Cos(clusterAngle) * clusterR, clusterY,
                Mathf.Sin(clusterAngle) * clusterR);

            if (clusterCenter.magnitude < ClearRadius) continue;

            for (int r = 0; r < rocksPerCluster; r++)
            {
                Vector3 offset = Random.insideUnitSphere * 1200f;
                Vector3 pos    = clusterCenter + offset;
                if (pos.magnitude < ClearRadius) continue;
                SpawnRock(templates, pos, scaleMin: 0.6f, scaleMax: 2.2f);
                spawned++;
                if (spawned % 10 == 0) yield return null;
            }
        }
    }

    private void SpawnRock(List<GameObject> templates, Vector3 pos,
                           float scaleMin, float scaleMax)
    {
        GameObject tmpl = templates[Random.Range(0, templates.Count)];
        GameObject rock;

        if (tmpl != null)
        {
            rock = Instantiate(tmpl, pos, Random.rotation);
            // Remove any Asteroid script so scatter rocks aren't tracked or self-destructed.
            var ast = rock.GetComponent<Asteroid>();
            if (ast != null) Destroy(ast);
        }
        else
        {
            rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.transform.position = pos;
            rock.transform.rotation = Random.rotation;
        }

        // Static — no physics simulation needed.
        var rb = rock.GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        rock.transform.localScale = Vector3.one * Random.Range(scaleMin, scaleMax);
        rock.name = "DebrisRock";
        rock.transform.SetParent(transform, true);
    }

    // ── Template gathering ────────────────────────────────────────────────────

    private static List<GameObject> GatherTemplates()
    {
        var list = new List<GameObject>();

#if UNITY_EDITOR
        // In the editor, load rock prefabs directly from the content folder.
        var guids = UnityEditor.AssetDatabase.FindAssets(
            "t:Prefab",
            new[] { "Assets/Fight or Flight/Content/Prefabs/Rocks/Prefabs" });

        foreach (var guid in guids)
        {
            string path   = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var    prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) list.Add(prefab);
        }

        // Also try the top-level Prefabs folder for Asteroid_New
        var astGuids = UnityEditor.AssetDatabase.FindAssets(
            "Asteroid_New t:Prefab",
            new[] { "Assets/Fight or Flight/Content/Prefabs" });
        foreach (var guid in astGuids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var    pf   = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (pf != null) list.Add(pf);
        }
#endif

        // Runtime fallback: find any existing Asteroid in the scene and use it as a template.
        if (list.Count == 0)
        {
            var sceneAst = Object.FindAnyObjectByType<Asteroid>();
            if (sceneAst != null) list.Add(sceneAst.gameObject);
        }

        return list;
    }
}
