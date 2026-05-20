using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Manages enemy waves. Auto-creates itself in MainScene — no scene setup required.
/// Disables EnemySpawner on game start and takes over all enemy spawning.
/// Wave formula: enemies = (3 + 2 * (wave - 1)) * difficulty.enemyCountMult.
/// </summary>
public class WaveManager : MonoBehaviour
{
    // ── Static state polled by HUDManager ────────────────────────────────────
    public static int    CurrentWave    { get; private set; }
    public static string WaveStatusText { get; private set; } = "";

    // ── Config ────────────────────────────────────────────────────────────────
    private const float InterWaveDelay   = 5f;
    private const float SpawnRadius      = 2500f;
    private const float MinPlayerDist    = 800f;
    private const int   BaseEnemyCount   = 3;
    private const int   EnemyIncrement   = 2;

    // ── Runtime state ─────────────────────────────────────────────────────────
    private GameObject enemyPrefab;
    private int        activeEnemies;
    private bool       gameActive;
    private bool       waveInProgress;

    // Persistent top-center header ("WAVE 3") + transient announcement overlay.
    private Text   headerText;
    private Text   announcementText;
    private CanvasGroup announcementGroup;
    private CanvasGroup headerGroup;

    // ── Auto-creation ─────────────────────────────────────────────────────────
    // RuntimeInitializeOnLoadMethod fires once at game startup, NOT on every
    // scene load. We need to react every time MainScene becomes active (e.g.
    // after the player hits Play in the menu, or Try Again on defeat), so we
    // subscribe to SceneManager.sceneLoaded instead.

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void HookSceneLoad()
    {
        SceneManager.sceneLoaded -= OnSceneLoadedStatic;
        SceneManager.sceneLoaded += OnSceneLoadedStatic;
        // Also try once now in case the active scene is already MainScene (e.g.
        // when pressing Play in the editor while MainScene is open).
        TryCreate(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoadedStatic(Scene scene, LoadSceneMode mode)
    {
        TryCreate(scene);
    }

    private static void TryCreate(Scene scene)
    {
        if (scene.name != "MainScene") return;
        if (Object.FindAnyObjectByType<WaveManager>() != null) return; // idempotent
        new GameObject("WaveManager").AddComponent<WaveManager>();
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        GameEventManager.OnStartGame       += OnGameStart;
        GameEventManager.OnPlayerDestroyed += OnPlayerDied;
        GameEventManager.OnEnemyDestroyed  += OnEnemyKilled;
    }

    private void OnDisable()
    {
        GameEventManager.OnStartGame       -= OnGameStart;
        GameEventManager.OnPlayerDestroyed -= OnPlayerDied;
        GameEventManager.OnEnemyDestroyed  -= OnEnemyKilled;
    }

    private void Start()
    {
        BuildHudUI();
        // Bootstrap the gameplay session. MainMenuController just loads MainScene
        // without firing OnStartGame, so without this nothing else (ScoreManager,
        // EnemySpawner, etc.) would reset/start. Safe to call here because
        // subscriptions are already wired by everyone's OnEnable.
        GameEventManager.StartGame();
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void OnGameStart()
    {
        // Reset score/kills here as a static call so it always happens regardless
        // of whether a ScoreManager MonoBehaviour instance is present in the scene.
        ScoreManager.ResetScore();

        // Grab enemy prefab from the scene's EnemySpawner and shut it down so we
        // don't have two systems spawning enemies at the same time.
        var spawner = Object.FindAnyObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            enemyPrefab = spawner.EnemyPrefab;
            spawner.CancelInvoke(); // kill any InvokeRepeating that just got queued
            spawner.enabled = false;
        }

        CurrentWave  = 0;
        activeEnemies = 0;
        gameActive   = true;
        StartCoroutine(BeginNextWave());
    }

    private void OnPlayerDied()
    {
        gameActive     = false;
        waveInProgress = false;
        StopAllCoroutines();
        WaveStatusText = "";
        if (headerGroup != null) headerGroup.alpha = 0f;
    }

    private void OnEnemyKilled()
    {
        if (!gameActive || !waveInProgress) return;
        activeEnemies = Mathf.Max(0, activeEnemies - 1);

        if (activeEnemies <= 0)
            StartCoroutine(CheckAllEnemiesDead());
    }

    // Deferred by one frame so Unity can process the Destroy() call from ShipHealth.Die()
    // before we check EnemyAI.allEnemies. Without the yield, the dying enemy is still in
    // the list when this runs, making Count = 1 when it should be 0.
    private IEnumerator CheckAllEnemiesDead()
    {
        yield return null;
        if (!gameActive || !waveInProgress) yield break;

        if (EnemyAI.allEnemies.Count == 0)
        {
            waveInProgress = false;
            StartCoroutine(InterWaveCountdown());
        }
        else
        {
            // Enemies are still alive (counter drifted); resync to the real list.
            activeEnemies = EnemyAI.allEnemies.Count;
        }
    }

    // ── Wave flow ─────────────────────────────────────────────────────────────

    private IEnumerator BeginNextWave()
    {
        yield return null; // one frame so the scene is settled
        StartWave(CurrentWave + 1);
    }

    private IEnumerator InterWaveCountdown()
    {
        for (int t = (int)InterWaveDelay; t > 0; t--)
        {
            WaveStatusText = string.Format("NEXT WAVE IN {0}s", t);
            if (headerText != null) headerText.text = WaveStatusText;
            yield return new WaitForSeconds(1f);
        }
        StartWave(CurrentWave + 1);
    }

    private void StartWave(int wave)
    {
        CurrentWave    = wave;
        waveInProgress = true;

        int count = ComputeEnemyCount(wave);
        activeEnemies = count;

        WaveStatusText = string.Format("WAVE {0}", wave);
        if (headerText != null) headerText.text = WaveStatusText;
        ShowAnnouncement(string.Format("WAVE {0}", wave));

        StartCoroutine(SpawnEnemies(count));
    }

    private static int ComputeEnemyCount(int wave)
    {
        int baseCount = BaseEnemyCount + EnemyIncrement * (wave - 1);
        return Mathf.Max(1, Mathf.RoundToInt(baseCount * DifficultyManager.EnemyCountMultiplier));
    }

    private IEnumerator SpawnEnemies(int count)
    {
        if (enemyPrefab == null) yield break;

        for (int i = 0; i < count; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(0.4f);
        }
    }

    private void SpawnEnemy()
    {
        // Spawn 30-80 units (user scale) from the player — i.e. between the
        // orbit distance and chase-trigger distance, scaled to ArenaRadius.
        // Falls back to a random spot well inside the arena if no player.
        float arenaR  = ScriptsReference.ArenaRadius;
        float safeMax = arenaR * 0.85f;

        Vector3 pos;
        if (Ship.PlayerShip != null)
        {
            Vector3 playerPos = Ship.PlayerShip.transform.position;
            float   minDist   = arenaR * 0.17f;  // user "30"
            float   maxDist   = arenaR * 0.45f;  // user "80"
            Vector3 dir       = Random.onUnitSphere;
            dir.y *= 0.35f;
            dir = dir.normalized;
            float dist = Random.Range(minDist, maxDist);
            pos = playerPos + dir * dist;
        }
        else
        {
            pos = Random.insideUnitSphere * (arenaR * 0.55f);
        }

        // Keep inside the arena.
        if (pos.magnitude > safeMax)
            pos = pos.normalized * safeMax;

        Instantiate(enemyPrefab, pos, Quaternion.identity);
    }

    // ── HUD UI (built at runtime — no scene wiring needed) ────────────────────

    private void BuildHudUI()
    {
        var canvasGo = new GameObject("WaveHUD");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Persistent top-center "WAVE X" header — with dark panel background.
        // Positioned BELOW the compass bar (which occupies the top 10-72 px).
        var headerBgGo = new GameObject("WaveHeaderBG");
        headerBgGo.transform.SetParent(canvasGo.transform, false);
        var headerBgRt = headerBgGo.AddComponent<RectTransform>();
        headerBgRt.anchorMin = new Vector2(0.5f, 1f);
        headerBgRt.anchorMax = new Vector2(0.5f, 1f);
        headerBgRt.pivot     = new Vector2(0.5f, 1f);
        headerBgRt.anchoredPosition = new Vector2(0f, -90f);
        headerBgRt.sizeDelta = new Vector2(260f, 52f);
        headerBgGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        var headerGo = new GameObject("WaveHeader");
        headerGo.transform.SetParent(headerBgGo.transform, false);
        var headerRt = headerGo.AddComponent<RectTransform>();
        headerRt.anchorMin = Vector2.zero;
        headerRt.anchorMax = Vector2.one;
        headerRt.offsetMin = headerRt.offsetMax = Vector2.zero;

        headerGroup = headerBgGo.AddComponent<CanvasGroup>();
        headerGroup.alpha = 1f;
        headerGroup.blocksRaycasts = false;

        headerText = headerGo.AddComponent<Text>();
        headerText.font = font;
        headerText.fontSize = 30;
        headerText.fontStyle = FontStyle.Bold;
        headerText.color = new Color(1f, 0.95f, 0.5f);
        headerText.alignment = TextAnchor.MiddleCenter;
        headerText.horizontalOverflow = HorizontalWrapMode.Overflow;
        headerText.verticalOverflow = VerticalWrapMode.Overflow;
        headerText.text = "";

        // Transient announcement (big "WAVE X" that fades) — wrapped in a container for BG.
        var annContainer = new GameObject("AnnouncementContainer");
        annContainer.transform.SetParent(canvasGo.transform, false);
        var annContainerRt = annContainer.AddComponent<RectTransform>();
        annContainerRt.anchorMin = annContainerRt.anchorMax = new Vector2(0.5f, 0.55f);
        annContainerRt.anchoredPosition = Vector2.zero;
        annContainerRt.sizeDelta = new Vector2(920f, 170f);

        announcementGroup = annContainer.AddComponent<CanvasGroup>();
        announcementGroup.alpha = 0f;
        announcementGroup.blocksRaycasts = false;

        // Dark background behind the big text
        var annBgGo = new GameObject("AnnouncementBG");
        annBgGo.transform.SetParent(annContainer.transform, false);
        var annBgRt = annBgGo.AddComponent<RectTransform>();
        annBgRt.anchorMin = Vector2.zero;
        annBgRt.anchorMax = Vector2.one;
        annBgRt.offsetMin = annBgRt.offsetMax = Vector2.zero;
        annBgGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        var annGo = new GameObject("WaveAnnouncement");
        annGo.transform.SetParent(annContainer.transform, false);
        var annRt = annGo.AddComponent<RectTransform>();
        annRt.anchorMin = Vector2.zero;
        annRt.anchorMax = Vector2.one;
        annRt.offsetMin = annRt.offsetMax = Vector2.zero;

        announcementText = annGo.AddComponent<Text>();
        announcementText.font = font;
        announcementText.fontSize = 120;
        announcementText.fontStyle = FontStyle.Bold;
        announcementText.color = Color.white;
        announcementText.alignment = TextAnchor.MiddleCenter;
        announcementText.horizontalOverflow = HorizontalWrapMode.Overflow;
        announcementText.verticalOverflow = VerticalWrapMode.Overflow;
    }

    private void ShowAnnouncement(string message)
    {
        StopCoroutine("FadeAnnouncement");
        announcementText.text = message;
        StartCoroutine(FadeAnnouncement());
    }

    private IEnumerator FadeAnnouncement()
    {
        // Visible for ~2 seconds total, then gone. Fade in fast, fade out smooth.
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            announcementGroup.alpha = t / 0.2f;
            yield return null;
        }
        announcementGroup.alpha = 1f;

        yield return new WaitForSeconds(1.0f);

        t = 0f;
        while (t < 0.8f)
        {
            t += Time.deltaTime;
            announcementGroup.alpha = 1f - t / 0.8f;
            yield return null;
        }
        announcementGroup.alpha = 0f;
    }
}
