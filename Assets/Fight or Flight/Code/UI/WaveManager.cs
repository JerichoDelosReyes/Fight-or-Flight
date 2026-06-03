using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WaveManager : MonoBehaviour
{
    public static int    CurrentWave    { get; private set; }
    public static string WaveStatusText { get; private set; } = "";

    private static WaveManager instance;
    private const float DefaultBgmVolume = 0.35f;
    private const float BgmFadeDuration = 1.5f;
    private const string KeyVolMusic = "VolMusic";
    private const string BgmAssetPath = "Assets/Fight or Flight/Content/Background Music/bgm.mp3";
    private const float InterWaveDelay   = 5f;
    private const float SpawnRadius      = 2500f;
    private const float MinPlayerDist    = 800f;
    private const int   BaseEnemyCount   = 10;
    private const int   EnemyIncrement   = 5;
    public  const int   MaxWave          = 5;
    private const int   MaxEnemiesPerWave = 60;

private GameObject enemyPrefab;
    private int        activeEnemies;
    private bool       gameActive;
    private bool       waveInProgress;
    private bool       _started;
    private static float _matchStartTime;

    public static float MatchElapsedTime => Time.realtimeSinceStartup - _matchStartTime;

    private Text   headerText;
    private Text   announcementText;
    private CanvasGroup announcementGroup;
    private CanvasGroup headerGroup;
    private AudioSource bgmSource;
    private Coroutine bgmFadeRoutine;


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void HookSceneLoad()
    {
        SceneManager.sceneLoaded -= OnSceneLoadedStatic;
        SceneManager.sceneLoaded += OnSceneLoadedStatic;
        TryCreate(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoadedStatic(Scene scene, LoadSceneMode mode)
    {
        TryCreate(scene);
    }

    private static void TryCreate(Scene scene)
    {
        if (scene.name != "MainScene") return;
        if (Object.FindAnyObjectByType<WaveManager>() != null) return;
        new GameObject("WaveManager").AddComponent<WaveManager>();
    }


    private void OnEnable()
    {
        GameEventManager.OnStartGame       += OnGameStart;
        GameEventManager.OnPlayerDestroyed += OnPlayerDied;
        GameEventManager.OnEnemyDestroyed  += OnEnemyKilled;
    }

    private void Awake()
    {
        instance = this;
        EnsureBgmSource();
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
        GameEventManager.StartGame();
    }


    private void OnGameStart()
    {
        if (_started) return;
        _started = true;

        ScoreManager.ResetScore();

        var spawner = Object.FindAnyObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            enemyPrefab = spawner.EnemyPrefab;
            spawner.CancelInvoke();
            spawner.enabled = false;
        }

        CurrentWave   = 0;
        activeEnemies = 0;
        gameActive    = true;
        _matchStartTime = Time.realtimeSinceStartup;
        PlayBackgroundMusic();
        StartCoroutine(BeginNextWave());
    }

    private void OnPlayerDied()
    {
        gameActive     = false;
        waveInProgress = false;
        StopAllCoroutines();
        StopBackgroundMusic();
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

    private IEnumerator CheckAllEnemiesDead()
    {
        yield return null;
        if (!gameActive || !waveInProgress) yield break;

        if (EnemyMovement.allEnemies.Count == 0)
            WaveCompleted();
        else
        {
            activeEnemies = EnemyMovement.allEnemies.Count;
        }
    }

    private void WaveCompleted()
    {
        waveInProgress = false;

        bool campaignFinished =
            GameModeManager.Selected == GameModeManager.Mode.Campaign &&
            CurrentWave >= MaxWave;

        if (campaignFinished)
        {
            GameModeManager.UnlockSurvival();
            gameActive = false;
            StopBackgroundMusic();
            MissionCompleteScreen.Show(ScoreManager.Score, MatchElapsedTime, ScoreManager.Kills, CurrentWave, MaxWave);
        }
        else
        {
            StartCoroutine(InterWaveCountdown());
        }
    }


    private IEnumerator BeginNextWave()
    {
        yield return null;
        yield return StartCoroutine(PreWaveCountdown());
        StartWave(CurrentWave + 1);
    }

    private IEnumerator InterWaveCountdown()
    {
        var vignette = SpawnDarkVignette();

        for (int t = (int)InterWaveDelay; t > 0; t--)
        {
            bool isSurvival = GameModeManager.Selected == GameModeManager.Mode.Survival;
            WaveStatusText = isSurvival ? string.Format("NEXT SURGE IN {0}s", t)
                                        : string.Format("NEXT WAVE IN {0}s", t);
            if (headerText != null) headerText.text = WaveStatusText;

            if (t <= 3)
            {
                if (announcementText != null) { announcementText.text = t.ToString(); announcementText.fontSize = 140; }
                if (announcementGroup != null) announcementGroup.alpha = 1f;
                yield return new WaitForSeconds(0.6f);
                if (announcementGroup != null) announcementGroup.alpha = 0f;
                yield return new WaitForSeconds(0.4f);
            }
            else
            {
                yield return new WaitForSeconds(1f);
            }
        }

        if (announcementText != null) { announcementText.text = "FIGHT!"; announcementText.fontSize = 170; }
        if (announcementGroup != null) announcementGroup.alpha = 1f;
        yield return new WaitForSeconds(0.6f);
        if (announcementGroup != null) announcementGroup.alpha = 0f;

        if (vignette != null) Destroy(vignette);
        StartWave(CurrentWave + 1);
    }

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
            a = a * a;
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

        bool survival = GameModeManager.Selected == GameModeManager.Mode.Survival;
        WaveStatusText = survival ? string.Format("SURGE {0}", wave)
                                  : string.Format("WAVE {0}", wave);
        if (headerText != null) headerText.text = WaveStatusText;
        ShowAnnouncement(survival ? string.Format("SURGE {0}", wave)
                                  : string.Format("WAVE {0}", wave));

        StartCoroutine(SpawnEnemies(count, wave));
    }

    private static int ComputeEnemyCount(int wave)
    {
        int baseCount = BaseEnemyCount + EnemyIncrement * (wave - 1);
        baseCount = Mathf.Min(baseCount, MaxEnemiesPerWave);
        return Mathf.Max(1, Mathf.RoundToInt(baseCount * DifficultyManager.EnemyCountMultiplier));
    }

    private IEnumerator SpawnEnemies(int count, int wave)
    {
        if (enemyPrefab == null) yield break;

        for (int i = 0; i < count; i++)
        {
            SpawnEnemy(wave);
            yield return new WaitForSeconds(2.5f);
        }
    }

    private void SpawnEnemy(int wave)
    {
        float arenaR     = ScriptsReference.ArenaRadius;
        float spawnLimit = arenaR * 0.5f;

        Vector3 pos = Random.insideUnitSphere * spawnLimit;
        pos.y *= 0.5f;
        if (pos.magnitude > spawnLimit) pos = pos.normalized * spawnLimit;

        Quaternion rotation = Quaternion.identity;
        if (Ship.PlayerShip != null)
        {
            Vector3 toPlayer = (Ship.PlayerShip.transform.position - pos).normalized;
            if (toPlayer != Vector3.zero) rotation = Quaternion.LookRotation(toPlayer);
        }

        var go = Instantiate(enemyPrefab, pos, rotation);
        ApplyWaveScaling(go, wave);
    }

    private static void ApplyWaveScaling(GameObject go, int wave)
    {
        if (wave <= 1) return;

        float waveIndex = wave - 1;

        var health = go.GetComponent<ShipHealth>();
        if (health != null)
        {
            health.maxHealth     *= 1f + waveIndex * 0.30f;
            health.currentHealth  = health.maxHealth;
        }

        var movement = go.GetComponent<EnemyMovement>();
        if (movement != null)
        {
            movement._movementSpeed *= 1f + waveIndex * 0.12f;
            movement._turnSpeed     *= 1f + waveIndex * 0.06f;
        }

        var attack = go.GetComponent<EnemyAttack>();
        if (attack != null)
        {
            attack.fireRate    = Mathf.Max(0.25f, attack.fireRate * (1f - waveIndex * 0.10f));
            attack.bulletSpeed *= 1f + waveIndex * 0.07f;
            attack.laserDamage *= 1f + waveIndex * 0.20f;
        }
    }


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

        var headerBgGo = new GameObject("WaveHeaderBG");
        headerBgGo.transform.SetParent(canvasGo.transform, false);
        var headerBgRt = headerBgGo.AddComponent<RectTransform>();
        headerBgRt.anchorMin = new Vector2(0.5f, 0f);
        headerBgRt.anchorMax = new Vector2(0.5f, 0f);
        headerBgRt.pivot     = new Vector2(0.5f, 0f);
        headerBgRt.anchoredPosition = new Vector2(0f, 60f);
        headerBgRt.sizeDelta = new Vector2(340f, 68f);
        headerBgGo.AddComponent<UnityEngine.UI.Image>().color = new Color(0f, 0f, 0f, 0f);

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
        headerText.color = new Color(0.6f, 0.85f, 1f, 1f);
        headerText.alignment = TextAnchor.MiddleCenter;
headerText.horizontalOverflow = HorizontalWrapMode.Overflow;
        headerText.verticalOverflow = VerticalWrapMode.Overflow;
        headerText.text = "";

        var annContainer = new GameObject("AnnouncementContainer");
        annContainer.transform.SetParent(canvasGo.transform, false);
        var annContainerRt = annContainer.AddComponent<RectTransform>();
        annContainerRt.anchorMin = annContainerRt.anchorMax = new Vector2(0.5f, 0.55f);
        annContainerRt.anchoredPosition = Vector2.zero;
        annContainerRt.sizeDelta = new Vector2(920f, 170f);

        announcementGroup = annContainer.AddComponent<CanvasGroup>();
        announcementGroup.alpha = 0f;
        announcementGroup.blocksRaycasts = false;

        var annBgGo = new GameObject("AnnouncementBG");
        annBgGo.transform.SetParent(annContainer.transform, false);
        var annBgRt = annBgGo.AddComponent<RectTransform>();
        annBgRt.anchorMin = Vector2.zero;
        annBgRt.anchorMax = Vector2.one;
        annBgRt.offsetMin = annBgRt.offsetMax = Vector2.zero;
        annBgGo.AddComponent<UnityEngine.UI.Image>().color = new Color(0f, 0f, 0f, 0f);

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
    public static void StopBackgroundMusic()
    {
        if (instance != null)
            instance.StopBgm();
    }

    private void EnsureBgmSource()
    {
        if (bgmSource != null) return;

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.spatialBlend = 0f;
        bgmSource.volume = GetMusicVolume();
        bgmSource.priority = 128;
    }

    private void PlayBackgroundMusic()
    {
        EnsureBgmSource();

        if (bgmSource.clip == null)
            bgmSource.clip = LoadBgmClip();

        if (bgmSource.clip == null)
        {
            Debug.LogWarning("WaveManager: bgm audio clip could not be found.");
            return;
        }

        float targetVolume = GetMusicVolume();
        bgmSource.volume = targetVolume;
        bgmSource.loop = true;

        if (!bgmSource.isPlaying)
        {
            bgmSource.volume = 0f;
            bgmSource.Play();

            if (bgmFadeRoutine != null) StopCoroutine(bgmFadeRoutine);
            bgmFadeRoutine = StartCoroutine(FadeBgm(0f, targetVolume, BgmFadeDuration, false));
        }
    }

    private void StopBgm()
    {
        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.Stop();
    }

    public static void FadeOutBackgroundMusic()
    {
        if (instance != null)
            instance.FadeOutBgm();
    }

    public static void SetMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat(KeyVolMusic, volume);
        if (instance != null)
            instance.ApplyMusicVolume(volume);
    }

    private void FadeOutBgm()
    {
        if (bgmSource == null || !bgmSource.isPlaying)
            return;

        if (bgmFadeRoutine != null) StopCoroutine(bgmFadeRoutine);
        bgmFadeRoutine = StartCoroutine(FadeBgm(bgmSource.volume, 0f, BgmFadeDuration, true));
    }

    private void ApplyMusicVolume(float volume)
    {
        EnsureBgmSource();
        bgmSource.volume = volume;
    }

    private float GetMusicVolume()
    {
        return PlayerPrefs.GetFloat(KeyVolMusic, DefaultBgmVolume);
    }

    private IEnumerator FadeBgm(float from, float to, float duration, bool stopWhenDone)
    {
        if (bgmSource == null) yield break;

        float elapsed = 0f;
        bgmSource.volume = from;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            bgmSource.volume = Mathf.Lerp(from, to, t);
            yield return null;
        }

        bgmSource.volume = to;
        if (stopWhenDone && bgmSource.isPlaying)
            bgmSource.Stop();

        bgmFadeRoutine = null;
    }

    private AudioClip LoadBgmClip()
    {
#if UNITY_EDITOR
        var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(BgmAssetPath);
        if (clip != null) return clip;
#endif
        var loaded = Resources.Load<AudioClip>("Background Music/bgm");
        if (loaded != null) return loaded;

        return Resources.Load<AudioClip>("bgm");
    }
}
