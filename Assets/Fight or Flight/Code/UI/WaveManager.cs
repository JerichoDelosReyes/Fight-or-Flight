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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (SceneManager.GetActiveScene().name != "MainScene") return;
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

        if (activeEnemies == 0)
        {
            waveInProgress = false;
            StartCoroutine(InterWaveCountdown());
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
        Vector3 pos = Random.insideUnitSphere * SpawnRadius;
        pos.y = 0f;

        if (pos.magnitude > ScriptsReference.BoundaryLimit - 500f)
            pos = pos.normalized * (ScriptsReference.BoundaryLimit - 500f);

        if (Ship.PlayerShip != null &&
            Vector3.Distance(pos, Ship.PlayerShip.transform.position) < MinPlayerDist)
        {
            pos += (pos - Ship.PlayerShip.transform.position).normalized * MinPlayerDist;
        }

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

        // Persistent top-center "WAVE X" header.
        var headerGo = new GameObject("WaveHeader");
        headerGo.transform.SetParent(canvasGo.transform, false);
        var headerRt = headerGo.AddComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0.5f, 1f);
        headerRt.anchorMax = new Vector2(0.5f, 1f);
        headerRt.pivot     = new Vector2(0.5f, 1f);
        headerRt.anchoredPosition = new Vector2(0, -30);
        headerRt.sizeDelta = new Vector2(600, 70);

        headerGroup = headerGo.AddComponent<CanvasGroup>();
        headerGroup.alpha = 1f;
        headerGroup.blocksRaycasts = false;

        headerText = headerGo.AddComponent<Text>();
        headerText.font = font;
        headerText.fontSize = 48;
        headerText.fontStyle = FontStyle.Bold;
        headerText.color = new Color(1f, 0.95f, 0.5f);
        headerText.alignment = TextAnchor.MiddleCenter;
        headerText.horizontalOverflow = HorizontalWrapMode.Overflow;
        headerText.verticalOverflow = VerticalWrapMode.Overflow;
        headerText.text = "";

        // Transient announcement (big "WAVE X" that fades).
        var annGo = new GameObject("WaveAnnouncement");
        annGo.transform.SetParent(canvasGo.transform, false);
        var annRt = annGo.AddComponent<RectTransform>();
        annRt.anchorMin = annRt.anchorMax = new Vector2(0.5f, 0.55f);
        annRt.anchoredPosition = Vector2.zero;
        annRt.sizeDelta = new Vector2(900, 160);

        announcementGroup = annGo.AddComponent<CanvasGroup>();
        announcementGroup.alpha = 0f;
        announcementGroup.blocksRaycasts = false;

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
