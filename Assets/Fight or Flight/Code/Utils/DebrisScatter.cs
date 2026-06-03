using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DebrisScatter : MonoBehaviour
{

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


    private const float ClearZoneFrac    = 0.08f;
    private const float InteriorMaxFrac  = 0.89f;
    private const float ShellInnerFrac   = 1.00f;
    private const float ShellOuterFrac   = 1.10f;

    private const int   ShellRocks       = 220;
    private const int   InteriorRocks    = 900;
    private const int   DeepInteriorRocks = 400;
    private const int   ClusterCount     = 18;
    private const int   RocksPerCluster  = 14;

    private const float ScaleMin = 0.5f;
    private const float ScaleMax = 4.0f;


    private void Start()
    {
        StartCoroutine(BuildField());
    }

    private IEnumerator BuildField()
    {
        yield return null;
        yield return null;

        ClearPreviousRocks();

        var templates = GatherTemplates();
        if (templates.Count == 0) templates.Add(null);

        float arenaR = ScriptsReference.ArenaRadius;
        float clear  = arenaR * ClearZoneFrac;
        float interiorMax = arenaR * InteriorMaxFrac;
        float shellInner  = arenaR * ShellInnerFrac;
        float shellOuter  = arenaR * ShellOuterFrac;

        int spawned = 0;

        for (int i = 0; i < ShellRocks; i++)
        {
            Vector3 dir = Random.onUnitSphere;
            dir.y *= 0.45f;
            dir = dir.normalized;
            float r = Random.Range(shellInner, shellOuter);
            Vector3 pos = dir * r;
            SpawnRock(templates, pos, 1.0f, ScaleMax);
            spawned++;
            if (spawned % 12 == 0) yield return null;
        }

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


    private static void ClearPreviousRocks()
    {
        string[] names = { "DebrisRock", "BoundaryRock", "ScatterRock" };
        foreach (var n in names)
        {
            GameObject g;
            while ((g = GameObject.Find(n)) != null)
                DestroyImmediate(g);
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
            var ast = rock.GetComponent<Asteroid>();
            if (ast != null) Destroy(ast);
        }
        else
        {
            rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.transform.position = pos;
            rock.transform.rotation = Random.rotation;
        }

        var rb = rock.GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        rock.transform.localScale = Vector3.one * Random.Range(scaleMin, scaleMax);
        rock.name = "DebrisRock";
        rock.transform.SetParent(transform, true);
    }


    private static List<GameObject> GatherTemplates()
    {
        var list = new List<GameObject>();

#if UNITY_EDITOR
        var guids = UnityEditor.AssetDatabase.FindAssets(
            "t:Prefab",
            new[] { "Assets/Fight or Flight/Content/Prefabs/Environment/Rocks" });
        foreach (var guid in guids)
        {
            string path   = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var    prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) list.Add(prefab);
        }

        var astGuids = UnityEditor.AssetDatabase.FindAssets(
            "Asteroid_New t:Prefab",
            new[] { "Assets/Fight or Flight/Content/Prefabs/Environment" });
foreach (var guid in astGuids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var    pf   = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (pf != null) list.Add(pf);
        }
#endif

        if (list.Count == 0)
        {
            var sceneAst = Object.FindAnyObjectByType<Asteroid>();
            if (sceneAst != null) list.Add(sceneAst.gameObject);
        }

        return list;
    }
}
