using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ArenaBoundary : MonoBehaviour
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
        if (Object.FindAnyObjectByType<ArenaBoundary>() != null) return;
        new GameObject("ArenaBoundary").AddComponent<ArenaBoundary>();
    }


    private const int   AsteroidCount    = 120;
    private const float AsteroidScaleMult = 2.5f;
    private const float WarnThreshold    = 0.82f;
    private const float PushForce        = 8000f;


    private Renderer    forceFieldRenderer;
    private Material    forceFieldMat;

    private AudioSource boundaryAudio;
    private bool        wasOutside;


    private void Start()
    {
        BuildForceField();
        BuildBoundaryCollider();
    }

    private void BuildBoundaryCollider()
    {
        var triggerGo = new GameObject("BoundaryTrigger");
        triggerGo.transform.SetParent(transform, false);
        var sc = triggerGo.AddComponent<SphereCollider>();
        sc.radius    = ScriptsReference.ArenaRadius;
        sc.isTrigger = true;
    }

    private void OnDestroy()
    {
        if (forceFieldMat != null) Destroy(forceFieldMat);
    }

    private void Update()
    {
        if (Ship.PlayerShip == null) return;

        float dist  = Ship.PlayerShip.transform.position.magnitude;
        float warn  = ScriptsReference.ArenaRadius * WarnThreshold;
        float t     = Mathf.InverseLerp(warn, ScriptsReference.ArenaRadius, dist);

        if (forceFieldMat != null)
        {
            float baseAlpha  = t * 0.22f;
            float pulseAlpha = dist > warn
                ? baseAlpha + Mathf.Abs(Mathf.Sin(Time.time * 3f)) * 0.08f
                : 0f;

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

        if (dist > ScriptsReference.ArenaRadius)
        {
            var rb = Ship.PlayerShip.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
Ship.PlayerShip.transform.position =
                    Ship.PlayerShip.transform.position.normalized * ScriptsReference.ArenaRadius;

                Vector3 outward = Ship.PlayerShip.transform.position.normalized;
                Vector3 vel     = rb.linearVelocity;
                float   dotOut  = Vector3.Dot(vel, outward);
                if (dotOut > 0f)
                    rb.linearVelocity = vel - outward * dotOut;

                rb.AddForce(-outward * PushForce, ForceMode.Impulse);
            }
        }
    }


    private void BuildForceField()
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "ForceFieldSphere";
        sphere.transform.SetParent(transform, false);
        sphere.transform.localScale = Vector3.one * (ScriptsReference.ArenaRadius * 2f);

        var col = sphere.GetComponent<Collider>();
        if (col != null) Destroy(col);

        forceFieldMat = new Material(Shader.Find("Standard"));
        forceFieldMat.SetFloat("_Mode",     3f);
        forceFieldMat.SetFloat("_Glossiness", 0f);
        forceFieldMat.SetFloat("_Metallic",   0f);
        forceFieldMat.SetInt("_SrcBlend",   (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        forceFieldMat.SetInt("_DstBlend",   (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        forceFieldMat.SetInt("_ZWrite",     0);
        forceFieldMat.SetInt("_Cull",       (int)UnityEngine.Rendering.CullMode.Off);
        forceFieldMat.color = new Color(0f, 0.75f, 1f, 0f);
        forceFieldMat.EnableKeyword("_ALPHABLEND_ON");
        forceFieldMat.renderQueue = 3000;

        forceFieldRenderer = sphere.GetComponent<Renderer>();
        forceFieldRenderer.material     = forceFieldMat;
        forceFieldRenderer.shadowCastingMode    = UnityEngine.Rendering.ShadowCastingMode.Off;
        forceFieldRenderer.receiveShadows       = false;
    }


    private void PlayBoundaryHitSound()
    {
        if (boundaryAudio == null)
        {
            boundaryAudio = gameObject.AddComponent<AudioSource>();
            boundaryAudio.spatialBlend = 0f;
            boundaryAudio.volume       = 0.6f;
        }
        ScreenShake.Trigger(0.3f, 4f);
    }


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
