using UnityEditor;
using UnityEngine;

/// <summary>
/// Auto-wires the ExplosionEffect prefab into any ShipHealth component that still has a null
/// explosionPrefab reference. Runs once per domain reload so no manual setup step is needed.
/// </summary>
[InitializeOnLoad]
public static class PrefabSetup
{
    private const string ExplosionPath = "Assets/Fight or Flight/Content/Prefabs/VFX/ExplosionEffect.prefab";

    private static readonly string[] ShipPrefabPaths =
    {
        "Assets/Fight or Flight/Content/Prefabs/Player/PlayerShip.prefab",
        "Assets/Fight or Flight/Content/Prefabs/Enemies/EnemyShip (1).prefab",
    };

    static PrefabSetup()
    {
        // Defer until asset database is ready.
        EditorApplication.delayCall += WireExplosionPrefabs;
    }

    [MenuItem("Fight or Flight/Setup Explosion Prefabs")]
    static void WireExplosionPrefabs()
    {
        var explosion = AssetDatabase.LoadAssetAtPath<GameObject>(ExplosionPath);
        if (explosion == null)
        {
            Debug.LogWarning("[PrefabSetup] ExplosionEffect.prefab not found at: " + ExplosionPath);
            return;
        }

        bool anyChanged = false;
        foreach (string path in ShipPrefabPaths)
        {
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabAsset == null)
            {
                Debug.LogWarning("[PrefabSetup] Prefab not found: " + path);
                continue;
            }

            // Open the prefab for editing so changes can be written back.
            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                var health = scope.prefabContentsRoot.GetComponent<ShipHealth>();
                if (health == null)
                {
                    // Also check the first child that might have ShipHealth.
                    health = scope.prefabContentsRoot.GetComponentInChildren<ShipHealth>(true);
                }

                if (health == null)
                {
                    Debug.LogWarning("[PrefabSetup] No ShipHealth found on " + path);
                    continue;
                }

                if (health.explosionPrefab != null)
                    continue; // already set, nothing to do

                health.explosionPrefab = explosion;
                scope.prefabContentsRoot.name = scope.prefabContentsRoot.name; // mark dirty
                anyChanged = true;
                Debug.Log("[PrefabSetup] Wired ExplosionEffect → " + path);
            }
        }

        if (anyChanged)
            AssetDatabase.SaveAssets();
    }
}
