using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    private Text text;
    private ShipHealth playerHealth;

    private void Awake()
    {
        text = GetComponent<Text>();
    }

    private void Update()
    {
        if (playerHealth == null)
        {
            if (Ship.PlayerShip != null)
            {
                playerHealth = Ship.PlayerShip.GetComponent<ShipHealth>();
            }
        }

        if (text != null && playerHealth != null)
        {
            text.text = string.Format("HLT: {0}", playerHealth.currentHealth.ToString("000"));
        }
    }
}
