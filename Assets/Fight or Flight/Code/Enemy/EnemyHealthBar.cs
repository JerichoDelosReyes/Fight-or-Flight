using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ShipHealth))]
public class EnemyHealthBar : MonoBehaviour
{

    private const float WorldYOffset  = 250f;
    private const float BarWidthPx    = 160f;
    private const float BarHeightPx   = 14f;
    private const float LabelHeightPx = 14f;
    private const float ScaleAtDist   = 2000f;


    private ShipHealth   _health;
    private Canvas       _canvas;
    private RectTransform _root;
    private Image        _fill;
    private CanvasGroup  _group;
    private bool         _everDamaged;
    private bool         _subscribed;


    private void Start()
    {
        _health = GetComponent<ShipHealth>();

        if (_health != null)
        {
            _health.OnDamaged += HandleDamaged;
            _subscribed = true;
        }

        if (_health != null && _health.currentHealth < _health.maxHealth)
        {
            _everDamaged = true;
            BuildUI();
        }
        else
        {
            enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (_subscribed && _health != null)
            _health.OnDamaged -= HandleDamaged;
    }

    private void LateUpdate()
    {
        if (_health == null || _root == null || Camera.main == null)
            return;

        if (!_everDamaged && _health.currentHealth < _health.maxHealth)
            _everDamaged = true;

        _group.alpha = _everDamaged ? 1f : 0f;
        if (!_everDamaged) return;

        Vector3 worldPos  = transform.position + Vector3.up * WorldYOffset;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        if (screenPos.z < 0f) { _group.alpha = 0f; return; }

        float refX = (screenPos.x / Screen.width  - 0.5f) * 1920f;
        float refY = (screenPos.y / Screen.height - 0.5f) * 1080f;
        _root.anchoredPosition = new Vector2(refX, refY);

        float dist  = Vector3.Distance(transform.position, Camera.main.transform.position);
        float scale = Mathf.Clamp(ScaleAtDist / Mathf.Max(1f, dist), 0.35f, 1.8f);
        _root.localScale = Vector3.one * scale;

        float frac = Mathf.Clamp01(_health.currentHealth / Mathf.Max(1f, _health.maxHealth));
        _fill.fillAmount = frac;
        _fill.color = Color.Lerp(new Color(0.9f, 0.1f, 0.1f), new Color(0.15f, 0.9f, 0.15f), frac);
    }

    private void HandleDamaged()
    {
        if (_health == null) return;

        if (!_everDamaged)
        {
            _everDamaged = true;
            if (_root == null)
                BuildUI();
        }

        enabled = true;
    }


    private void BuildUI()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvasGo = new GameObject("EnemyHPCanvas");
        canvasGo.transform.SetParent(transform, false);

        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 50;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        var rootGo = new GameObject("HPRoot");
        rootGo.transform.SetParent(canvasGo.transform, false);
        _root = rootGo.AddComponent<RectTransform>();
        _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 0.5f);
        _root.pivot            = new Vector2(0.5f, 0.5f);
        _root.sizeDelta        = new Vector2(BarWidthPx, BarHeightPx + LabelHeightPx + 4f);
        _root.anchoredPosition = new Vector2(0f, 3000f);

        _group               = rootGo.AddComponent<CanvasGroup>();
        _group.alpha         = 0f;
        _group.blocksRaycasts = false;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(rootGo.transform, false);
        var labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = labelRt.anchorMax = new Vector2(0.5f, 1f);
        labelRt.pivot            = new Vector2(0.5f, 0f);
        labelRt.anchoredPosition = new Vector2(0f, 0f);
        labelRt.sizeDelta        = new Vector2(BarWidthPx, LabelHeightPx);
        var labelTxt = labelGo.AddComponent<Text>();
        labelTxt.text      = "ENEMY";
        labelTxt.font      = font;
        labelTxt.fontSize  = 11;
        labelTxt.fontStyle = FontStyle.Bold;
        labelTxt.color     = new Color(0.85f, 0.85f, 0.85f, 0.9f);
        labelTxt.alignment = TextAnchor.MiddleCenter;
        labelTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        labelTxt.verticalOverflow   = VerticalWrapMode.Overflow;

        var bgGo = new GameObject("BarBg");
        bgGo.transform.SetParent(rootGo.transform, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = bgRt.anchorMax = new Vector2(0.5f, 0f);
        bgRt.pivot            = new Vector2(0.5f, 1f);
        bgRt.anchoredPosition = new Vector2(0f, 0f);
        bgRt.sizeDelta        = new Vector2(BarWidthPx, BarHeightPx);
        bgGo.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.9f);

        var fillGo = new GameObject("BarFill");
        fillGo.transform.SetParent(bgGo.transform, false);
        var fillRt = fillGo.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
        _fill = fillGo.AddComponent<Image>();
        _fill.type       = Image.Type.Filled;
        _fill.fillMethod = Image.FillMethod.Horizontal;
        _fill.fillAmount = 1f;
        _fill.color      = new Color(0.15f, 0.9f, 0.15f, 1f);
    }
}
