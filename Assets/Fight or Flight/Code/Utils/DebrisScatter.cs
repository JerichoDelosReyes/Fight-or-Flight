using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the asteroid field arena in two passes:
///   1) Dense shell of asteroids at 1.00x – 1.10x ArenaRadius — these ARE the walls.
///   2) Interior scatter from the clear-zone radius out to 0.89x ArenaRadius.
///
/// Uses every Rock Type prefab in Assets/Fight or Flight/Content/Prefabs/Rocks
/// plus Asteroid_New, so the field is visually varied.
///
/// Cleans up any "DebrisRock" / "BoundaryRock" left over from previous scatter
/// runs before spawning, so reloading the scene does not create duplicates.
///
/// Auto-creates itself in MainScene.
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

    // All radii are fractions of ScriptsReference.ArenaRadius (12,000 by default).
    private const float ClearZoneFrac    = 0.08f;  // user "15"  — keep clear around origin
    private const float InteriorMaxFrac  = 0.89f;  // user "160"
    private const float ShellInnerFrac   = 1.00f;  // user "180"
    private const float ShellOuterFrac   = 1.10f;  // user "200"

    private const int   ShellRocks       = 220;    // dense boundary shell
    private const int   InteriorRocks    = 900;    // mid-arena fill (was 480 — much denser)
    private const int   DeepInteriorRocks = 400;   // extra pass in the inner 0-50% band
    private const int   ClusterCount     = 18;     // was 10
    private const int   RocksPerCluster  = 14;     // was 12

    private const float ScaleMin = 0.5f;
    private const float ScaleMax = 4.0f;           // was 3.0 — bigger boulders for variety

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        StartCoroutine(BuildField());
    }

    private IEnumerator BuildField()
    {
        // Yield a couple of frames so any pre-existing scene asteroids settle.
        yield return null;
        yield return null;

        ClearPreviousRocks();

        var templates = GatherTemplates();
        if (templates.Count == 0) templates.Add(null); // fallback to primitive spheres

        float arenaR = ScriptsReference.ArenaRadius;
        float clear  = arenaR * ClearZoneFrac;
        float interiorMax = arenaR * InteriorMaxFrac;
        float shellInner  = arenaR * ShellInnerFrac;
        float shellOuter  = arenaR * ShellOuterFrac;

        int spawned = 0;

        // ── Pass 1: Boundary shell ────────────────────────────────────────────
        for (int i = 0; i < ShellRocks; i++)
        {
            Vector3 dir = Random.onUnitSphere;
            // Pinch Y a little so the shell looks like a fat ring rather than a
            // perfect sphere — easier to navigate.
            dir.y *= 0.45f;
            dir = dir.normalized;
            float r = Random.Range(shellInner, shellOuter);
            Vector3 pos = dir * r;
            SpawnRock(templates, pos, 1.0f, ScaleMax);
            spawned++;
            if (spawned % 12 == 0) yield return null;
        }

        // ── Pass 2: Interior scatter (full 0.08x – 0.89x ArenaRadius) ─────────
        for (int i = 0; i < InteriorRocks; i++)
        {
            Vector3 pos;
            int tries = 0;
            do
            {
                pos = Random.insideUnitSphere * interiorMax;
                tries++;
            }
            while (pos.magnitude < clear && tries < 25);
            if (pos.magnitude < clear) continue;

            SpawnRock(templates, pos, ScaleMin, ScaleMax * 0.75f);
            spawned++;
            if (spawned % 14 == 0) yield return null;
        }

        // ── Pass 2b: Deep-interior pass — denser fill in the inner band ──────
        // Concentrates rocks in the 0.08x – 0.50x region so the player isn't
        // staring at empty space anywhere inside the arena.
        float deepMax = arenaR * 0.50f;
        for (int i = 0; i < DeepInteriorRocks; i++)
        {
            Vector3 pos;
            int tries = 0;
            do
            {
                pos = Random.insideUnitSphere * deepMax;
                tries++;
            }
            while (pos.magnitude < clear && tries < 25);
            if (pos.magnitude < clear) continue;

            SpawnRock(templates, pos, ScaleMin, ScaleMax * 0.6f);
            spawned++;
            if (spawned % 14 == 0) yield return null;
        }

        // ── Pass 3: Concentrated clusters (combat cover) ──────────────────────
        for (int c = 0; c < ClusterCount; c++)
        {
            Vector3 dir = Random.onUnitSphere;
            dir.y *= 0.35f;
            dir = dir.normalized;
            float r = Random.Range(arenaR * 0.25f, arenaR * 0.75f);
            Vector3 centre = dir * r;
            if (centre.magnitude < clear) continue;

            for (int j = 0; j < RocksPerCluster; j++)
            {
                Vector3 offset = Random.insideUnitSphere * (arenaR * 0.08f);
                Vector3 pos    = centre + offset;
                if (pos.magnitude < clear) continue;
                if (pos.magnitude > interiorMax) pos = pos.normalized * interiorMax;

                SpawnRock(templates, pos, ScaleMin, ScaleMax * 0.85f);
                spawned++;
                if (spawned % 16 == 0) yield return null;
            }
        }
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    private static void ClearPreviousRocks()
    {
        // Names produced by any previous scatter pass — wipe them all so we
        // don't accumulate duplicates after a scene reload.
        string[] names = { "DebrisRock", "BoundaryRock", "ScatterRock" };
        foreach (var n in names)
        {
            GameObject g;
            while ((g = GameObject.Find(n)) != null)
                DestroyImmediate(g);
        }
    }

    // ── Spawning ──────────────────────────────────────────────────────────────

    private void SpawnRock(List<GameObject> templates, Vector3 pos,
                           float scaleMin, float scaleMax)
    {
        GameObject tmpl = templates[Random.Range(0, templates.Count)];
        GameObject rock;

        if (tmpl != null)
        {
            rock = Instantiate(tmpl, pos, Random.rotation);
            // Strip Asteroid script so scatter rocks aren't tracked/killed by gameplay.
            var ast = rock.GetComponent<Asteroid>();
            if (ast != null) Destroy(ast);
        }
        else
        {
            rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.transform.position = pos;
            rock.transform.rotation = Random.rotation;
        }

        // Strip physics — these are static obstacles.
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
        // Pull every prefab from the Rocks folder + Asteroid_New from top-level Prefabs.
        var guids = UnityEditor.AssetDatabase.FindAssets(
            "t:Prefab",
            new[] { "Assets/Fight or Flight/Content/Prefabs/Rocks" });
        foreach (var guid in guids)
        {
            string path   = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var    prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) list.Add(prefab);
        }

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

        // Runtime fallback — find a scene Asteroid as a template.
        if (list.Count == 0)
        {
            var sceneAst = Object.FindAnyObjectByType<Asteroid>();
            if (sceneAst != null) list.Add(sceneAst.gameObject);
        }

        return list;
    }
}
