//
// Copyright (c) Brian Hernandez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a centered crosshair when Mouse+Keyboard mode is active.
/// In that mode the cursor is locked, so the crosshair stays at screen center —
/// exactly where the ship is aiming. ShipInput manages cursor lock state; this
/// script intentionally does not touch Cursor.lockState or Cursor.visible.
/// </summary>
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
            // Screen center — the cursor is locked so this is always where the ship aims.
            crosshair.transform.position = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        }
    }
}
