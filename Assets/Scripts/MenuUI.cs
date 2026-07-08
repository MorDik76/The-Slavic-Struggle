using UnityEngine;
using UnityEngine.UI;
using Mirror;
using Mirror.Discovery;
using System.Collections;

public class MenuUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject searchingPanel;

    [Header("UI")]
    public InputField nicknameInput;

    [Header("Network")]
    public NetworkDiscovery networkDiscovery;

    [Header("Settings")]
    public string defaultSceneName = "Villagh";

    void Start()
    {
        string saved = PlayerPrefs.GetString("Nickname", "");
        if (!string.IsNullOrEmpty(saved) && nicknameInput)
            nicknameInput.text = saved;

        if (searchingPanel) searchingPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnPlayButton()
    {
        StartGame();
    }

    public void OnQuitButton()
    {
        Application.Quit();
    }

    void StartGame()
    {
        string nick = nicknameInput ? nicknameInput.text.Trim() : "";

        if (string.IsNullOrEmpty(nick))
            nick = "Player" + Random.Range(100, 999);

        PlayerPrefs.SetString("Nickname", nick);
        PlayerPrefs.Save();

        if (NetworkManagerSlavic.Instance == null)
        {
            Debug.LogError("MenuUI: NetworkManagerSlavic.Instance is null. Add NetworkManagerSlavic to the scene.");
            return;
        }

        if (networkDiscovery == null)
        {
            Debug.LogError("MenuUI: NetworkDiscovery is null.");
            return;
        }

        NetworkManagerSlavic.pendingNickname = nick;
        NetworkManagerSlavic.Instance.onlineScene = defaultSceneName;

        if (searchingPanel) searchingPanel.SetActive(true);
        if (mainMenuPanel) mainMenuPanel.SetActive(false);

        networkDiscovery.OnServerFound.RemoveAllListeners();
        networkDiscovery.OnServerFound.AddListener(OnDiscoveredServer);
        networkDiscovery.StartDiscovery();

        StartCoroutine(AutoHostTimeout());
    }

    IEnumerator AutoHostTimeout()
    {
        yield return new WaitForSeconds(3f);

        if (NetworkClient.active || NetworkServer.active) yield break;

        networkDiscovery.StopDiscovery();

        NetworkManagerSlavic.Instance.StartHost();
        networkDiscovery.AdvertiseServer();

        if (searchingPanel) searchingPanel.SetActive(false);
    }

    void OnDiscoveredServer(ServerResponse response)
    {
        networkDiscovery.StopDiscovery();
        StopAllCoroutines();

        NetworkManagerSlavic.Instance.StartClient(response.uri);

        if (searchingPanel) searchingPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (networkDiscovery != null)
            networkDiscovery.OnServerFound.RemoveListener(OnDiscoveredServer);
    }
}
