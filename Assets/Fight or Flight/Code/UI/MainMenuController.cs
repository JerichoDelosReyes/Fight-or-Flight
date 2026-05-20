using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public string startSceneName = "MainScene";

    private void Start()
    {
        EnsureSettingsButton();
    }

    public void StartGame()
    {
        SceneManager.LoadScene(startSceneName);
    }

    public void OpenInstructions()
    {
        Debug.Log("Instructions clicked");
        // Add logic to show instructions panel
    }

    public void OpenSettings()
    {
        SettingsMenu.Show();
    }

    public void QuitGame()
    {
        Debug.Log("Quit clicked");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    // ── Settings button injection ─────────────────────────────────────────────
    // The MainMenu scene was authored with Play / Instructions / Quit buttons
    // but no Settings button. Rather than require the user to wire one in the
    // editor, we clone the Instructions button at runtime and place it in the
    // same vertical stack so the menu visually matches.
    private void EnsureSettingsButton()
    {
        // Already created? Bail.
        var existing = GameObject.Find("SettingsButton");
        if (existing != null) return;

        var instructions = GameObject.Find("InstructionsButton");
        if (instructions == null) return; // unexpected scene layout — give up quietly

        var clone = Instantiate(instructions, instructions.transform.parent);
        clone.name = "SettingsButton";

        // Slot it below Quit (Play=-40, Instructions=-160, Quit=-280) → Settings=-400.
        var rt = clone.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = new Vector2(0, -400);

        // Rename the visible label.
        foreach (var text in clone.GetComponentsInChildren<Text>(true))
        {
            text.text = "SETTINGS";
        }

        // Rewire the button to call OpenSettings instead of OpenInstructions.
        // The clone keeps the original's *persistent* OnClick binding (set in
        // the scene editor) — UnityEvent.RemoveAllListeners() only clears
        // runtime listeners, not serialized ones. To get a clean event we
        // strip the Button component and re-add it, preserving visual style.
        var oldBtn = clone.GetComponent<Button>();
        if (oldBtn != null)
        {
            var colors          = oldBtn.colors;
            var transition      = oldBtn.transition;
            var navigation      = oldBtn.navigation;
            var targetGraphic   = oldBtn.targetGraphic;
            var spriteState     = oldBtn.spriteState;
            var animationTriggers = oldBtn.animationTriggers;

            DestroyImmediate(oldBtn);

            var btn = clone.AddComponent<Button>();
            btn.colors = colors;
            btn.transition = transition;
            btn.navigation = navigation;
            btn.targetGraphic = targetGraphic;
            btn.spriteState = spriteState;
            btn.animationTriggers = animationTriggers;
            btn.onClick.AddListener(OpenSettings);
        }
    }
}
