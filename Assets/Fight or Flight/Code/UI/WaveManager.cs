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
    // before we check EnemyMovement.allEnemies. Without the yield, the dying enemy is still in
    // the list when this runs, making Count = 1 when it should be 0.
    private IEnumerator CheckAllEnemiesDead()
    {
        yield return null;
        if (!gameActive || !waveInProgress) yield break;

        if (EnemyMovement.allEnemies.Count == 0)
        {
            waveInProgress = false;
            StartCoroutine(InterWaveCountdown());
        }
        else
        {
            // Enemies are still alive (counter drifted); resync to the real list.
            activeEnemies = EnemyMovement.allEnemies.Count;
        }
    }

    // ── Wave flow ─────────────────────────────────────────────────────────────

    private IEnumerator BeginNextWave()
    {
        yield return null; // one frame so the scene is settled
        yield return StartCoroutine(PreWaveCountdown());
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
        yield return StartCoroutine(PreWaveCountdown());
        StartWave(CurrentWave + 1);
    }

    // "3... 2... 1... FIGHT!" countdown plus a brief dark edge-vignette as
    // the screen-wide cue that a wave is incoming.
    private IEnumerator PreWaveCountdown()
    {
        var vignette = SpawnDarkVignette();

        string[] msgs = { "3", "2", "1", "FIGHT!" };
        foreach (var m in msgs)
        {
            if (announcementText != null)
            {
                announcementText.text = m;
                announcementText.fontSize = (m == "FIGHT!") ? 170 : 140;
            }
            if (announcementGroup != null) announcementGroup.alpha = 1f;
            yield return new WaitForSeconds(0.55f);
            if (announcementGroup != null) announcementGroup.alpha = 0f;
            yield return new WaitForSeconds(0.05f);
        }

        if (vignette != null) Destroy(vignette);
    }

    // One-shot dark edge vignette: opaque-corner / transparent-center.
    // The image's alpha decays over 1s thanks to FadeOutGameObject.
    private GameObject SpawnDarkVignette()
    {
        var canvasGo = new GameObject("WaveStartVignette");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 95;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGo.AddComponent<GraphicRaycaster>();

        var imgGo = new GameObject("Vig");
        imgGo.transform.SetParent(canvasGo.transform, false);
        var rt = imgGo.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var img = imgGo.AddComponent<RawImage>();
        img.texture = MakeDarkVignetteTex(256);
        img.color = new Color(0f, 0f, 0f, 0.85f);
        img.raycastTarget = false;

        StartCoroutine(FadeOutImage(img, 1.0f));
        return canvasGo;
    }

    private IEnumerator FadeOutImage(RawImage img, float duration)
    {
        float t = 0f;
        while (t < duration && img != null)
        {
            t += Time.deltaTime;
            var c = img.color;
            c.a = Mathf.Lerp(0.85f, 0f, t / duration);
            img.color = c;
            yield return null;
        }
    }

    private static Texture2D MakeDarkVignetteTex(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = (x - r + 0.5f) / r;
            float dy = (y - r + 0.5f) / r;
            float d  = Mathf.Sqrt(dx * dx + dy * dy);
            float a  = Mathf.Clamp01((d - 0.35f) / 0.65f);
            a = a * a; // softer inner falloff
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return tex;
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
        // Spawn within 60 user units of Vector3.zero (= ArenaRadius * 0.5).
        // The AI immediately switches to chase mode and hunts the player from
        // there, so spawn POSITION doesn't need to be near the player.
        float arenaR     = ScriptsReference.ArenaRadius;
        float spawnLimit = arenaR * 0.5f;

        Vector3 pos = Random.insideUnitSphere * spawnLimit;
        pos.y *= 0.5f; // pinch vertical so enemies aren't all stacked above/below

        if (pos.magnitude > spawnLimit)
            pos = pos.normalized * spawnLimit;

        // Face the player immediately upon spawn
        Quaternion rotation = Quaternion.identity;
        if (Ship.PlayerShip != null)
        {
            Vector3 toPlayer = (Ship.PlayerShip.transform.position - pos).normalized;
            if (toPlayer != Vector3.zero)
                rotation = Quaternion.LookRotation(toPlayer);
        }

        Instantiate(enemyPrefab, pos, rotation);
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
        // Positioned BELOW the compass bar (which now occupies the top 10-96 px
        // including the heading-number readout).
        var headerBgGo = new GameObject("WaveHeaderBG");
        headerBgGo.transform.SetParent(canvasGo.transform, false);
        var headerBgRt = headerBgGo.AddComponent<RectTransform>();
        headerBgRt.anchorMin = new Vector2(0.5f, 1f);
        headerBgRt.anchorMax = new Vector2(0.5f, 1f);
        headerBgRt.pivot     = new Vector2(0.5f, 1f);
        headerBgRt.anchoredPosition = new Vector2(0f, -116f);
        headerBgRt.sizeDelta = new Vector2(340f, 68f);
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
        headerText.fontSize = 40;
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
        announcementText.fontSize = 150;
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
