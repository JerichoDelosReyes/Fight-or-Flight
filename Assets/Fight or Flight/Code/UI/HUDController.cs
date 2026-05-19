using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    public RectTransform fixedCrosshair;
    public RectTransform mouseCrosshair;
    public float projectionDistance = 100f;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Ship.PlayerShip == null || mainCamera == null)
            return;

        // Project the ship's forward vector onto the screen
        Vector3 forwardPoint = Ship.PlayerShip.transform.position + Ship.PlayerShip.transform.forward * projectionDistance;
        Vector3 screenPoint = mainCamera.WorldToScreenPoint(forwardPoint);

        if (screenPoint.z > 0)
        {
            if (fixedCrosshair != null)
            {
                fixedCrosshair.position = screenPoint;
                fixedCrosshair.gameObject.SetActive(true);
            }
        }
        else
        {
            if (fixedCrosshair != null)
                fixedCrosshair.gameObject.SetActive(false);
        }

        // Mouse crosshair follows the virtual mouse position from ShipInput
        if (mouseCrosshair != null)
        {
            ShipInput input = Ship.PlayerShip.GetComponent<ShipInput>();
            if (input != null)
            {
                mouseCrosshair.position = input.VirtualMousePosition;
                mouseCrosshair.gameObject.SetActive(true); // Assuming we want it on if ship is active
            }
            else
            {
                mouseCrosshair.gameObject.SetActive(false);
            }
        }
        }
        }
