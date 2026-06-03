
using UnityEngine;
using UnityEngine.UI;

public class MouseCrosshairUI : MonoBehaviour
{
    private Image crosshair;

    private void Awake()
    {
        crosshair = GetComponent<Image>();
    }

    private void Update()
    {
        if (crosshair == null) return;

        bool show = ControlSchemeManager.IsMouseKeyboard;
        crosshair.enabled = show;

        if (show)
        {
            crosshair.transform.position = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        }
    }
}
