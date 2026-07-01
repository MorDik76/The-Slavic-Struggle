using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuUI : MonoBehaviour
{
    public Button playButton;
    public Button exitButton;

    void Start()
    {
        if (playButton) playButton.onClick.AddListener(() => SceneManager.LoadScene("Lobby"));
        if (exitButton) exitButton.onClick.AddListener(Application.Quit);
    }
}
