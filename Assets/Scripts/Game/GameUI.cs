using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("Player 1 (Left)")]
    public Text player1NameText;
    public Slider player1HealthSlider;
    public Slider player1StaminaSlider;

    [Header("Player 2 (Right)")]
    public Text player2NameText;
    public Slider player2HealthSlider;
    public Slider player2StaminaSlider;

    public string player1Label = "Player 1";
    public string player2Label = "Player 2";

    void Update()
    {
        PlayerSlavic[] all = FindObjectsOfType<PlayerSlavic>();
        PlayerSlavic p1 = null, p2 = null;
        foreach (PlayerSlavic p in all)
        {
            if (p.playerIndex == 1) p1 = p;
            else if (p.playerIndex == 2) p2 = p;
        }

        if (p1 != null)
        {
            if (player1NameText) player1NameText.text = player1Label;
            if (player1HealthSlider)
                player1HealthSlider.value = (float)p1.currentHealth / p1.maxHealth;
            if (player1StaminaSlider)
                player1StaminaSlider.value = p1.currentStamina / p1.maxStamina;
        }

        if (p2 != null)
        {
            if (player2NameText) player2NameText.text = player2Label;
            if (player2HealthSlider)
                player2HealthSlider.value = (float)p2.currentHealth / p2.maxHealth;
            if (player2StaminaSlider)
                player2StaminaSlider.value = p2.currentStamina / p2.maxStamina;
        }
    }
}
