using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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
        List<PlayerSlavic> alive = new List<PlayerSlavic>();
        foreach (PlayerSlavic p in all)
        {
            if (p.isDead) continue;
            alive.Add(p);
        }

        if (alive.Count >= 1)
        {
            if (player1NameText) player1NameText.text = player1Label;
            if (player1HealthSlider)
                player1HealthSlider.value = (float)alive[0].currentHealth / alive[0].maxHealth;
            if (player1StaminaSlider)
                player1StaminaSlider.value = alive[0].currentStamina / alive[0].maxStamina;
        }

        if (alive.Count >= 2)
        {
            if (player2NameText) player2NameText.text = player2Label;
            if (player2HealthSlider)
                player2HealthSlider.value = (float)alive[1].currentHealth / alive[1].maxHealth;
            if (player2StaminaSlider)
                player2StaminaSlider.value = alive[1].currentStamina / alive[1].maxStamina;
        }
    }
}
