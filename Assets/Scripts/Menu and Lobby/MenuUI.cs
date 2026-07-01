using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuUI : MonoBehaviour
{
    public Button playButton;
    public Button exitButton;

    void Start()
    {
        if (playButton) playButton.onClick.AddListener(PlayGame);
        if (exitButton) exitButton.onClick.AddListener(ExitGame);
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("Lobby");
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
