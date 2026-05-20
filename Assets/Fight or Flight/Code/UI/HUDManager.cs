using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    [Header("Bars")]
    public Slider healthBar;
    public Slider heatBar;
    public Image heatBarFill;
    public Color normalHeatColor = Color.cyan;
    public Color overheatColor = Color.red;

    [Header("Texts")]
    public Text scoreText;
    public Text killText;
    public Text speedText;
    public Text throttleText; // Added throttle text

    private ShipHealth playerHealth;
    private ShipCombat playerCombat;
    private ShipInput playerInput; // Added input reference
    private Rigidbody playerRb;

    private void Update()
    {
        if (Ship.PlayerShip == null) 
        {
            return;
        }

        if (playerHealth == null) playerHealth = Ship.PlayerShip.GetComponent<ShipHealth>();
        if (playerCombat == null) playerCombat = Ship.PlayerShip.GetComponent<ShipCombat>();
        if (playerInput == null) playerInput = Ship.PlayerShip.GetComponent<ShipInput>();
        if (playerRb == null) playerRb = Ship.PlayerShip.GetComponent<Rigidbody>();

        // Update Health
        if (healthBar != null && playerHealth != null)
        {
            healthBar.maxValue = playerHealth.maxHealth;
            healthBar.value = playerHealth.currentHealth;
        }

        // Update Heat
        if (heatBar != null && playerCombat != null)
        {
            heatBar.maxValue = playerCombat.overheatThreshold;
            heatBar.value = playerCombat.heat;

            if (heatBarFill != null)
            {
                heatBarFill.color = playerCombat.isOverheated ? overheatColor : normalHeatColor;
            }
        }

        // Update Score
        if (scoreText != null)
        {
            scoreText.text = string.Format("SCORE:\n{0:D6}", ScoreManager.Score);
        }

        // Update Kill counter (separate from score — pickups don't increment this)
        if (killText != null)
        {
            killText.text = string.Format("KILLS: {0}", ScoreManager.Kills);
        }

        // Update Speed
        if (speedText != null && playerRb != null)
        {
            speedText.text = string.Format("SPD: {0:000}", (int)playerRb.linearVelocity.magnitude);
        }

        // Update Throttle
        if (throttleText != null && playerInput != null)
        {
            throttleText.text = string.Format("THR: {0:000}", (int)(playerInput.throttle * 100f));
        }
    }
}
