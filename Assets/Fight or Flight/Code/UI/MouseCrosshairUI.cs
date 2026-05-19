//
// Copyright (c) Brian Hernandez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Updates the position of this GameObject to reflect the position of the mouse
/// when the player ship is using mouse input. Otherwise, it just hides it.
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
        if (crosshair != null && Ship.PlayerShip != null)
        {
            crosshair.enabled = Ship.PlayerShip.UsingMouseInput;

            if (crosshair.enabled)
            {
                ShipInput input = Ship.PlayerShip.GetComponent<ShipInput>();
                if (input != null)
                {
                    crosshair.transform.position = input.VirtualMousePosition;
                }
                
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Confined;
            }
            else
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }
    }
}
