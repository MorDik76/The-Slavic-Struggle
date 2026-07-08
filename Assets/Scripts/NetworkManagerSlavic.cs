using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class NetworkManagerSlavic : NetworkManager
{
    [Header("Slavic Settings")]
    public Transform player1Spawn;
    public Transform player2Spawn;
    public int maxPlayerCount = 2;
    public float autoBootDelay = 1f;

    public static NetworkManagerSlavic Instance { get; private set; }
    public static int pendingCharacterIndex;
    public static string pendingNickname = "Player";

    public override void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        maxConnections = maxPlayerCount;
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        CancelInvoke(nameof(BootClientsBackToMenu));
        if (numPlayers >= maxPlayerCount)
        {
            conn.Disconnect();
            Debug.Log($"Server full ({numPlayers}/{maxPlayerCount}), rejected connection");
            return;
        }
        base.OnServerConnect(conn);
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Transform spawn = FindSpawnPoint(numPlayers == 0);
        GameObject player = Instantiate(playerPrefab, spawn.position, spawn.rotation);
        NetworkServer.AddPlayerForConnection(conn, player);
    }

    Transform FindSpawnPoint(bool isPlayer1)
    {
        string name = isPlayer1 ? "Player1Spawn" : "Player2Spawn";
        GameObject found = GameObject.Find(name);
        if (found != null) return found.transform;
        Transform serialized = isPlayer1 ? player1Spawn : player2Spawn;
        if (serialized != null) return serialized;
        Debug.LogError($"Spawn point '{name}' not found in scene! Place a GameObject named '{name}' in the scene.");
        return transform;
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
        if (numPlayers < 2 && numPlayers > 0)
        {
            Invoke(nameof(BootClientsBackToMenu), autoBootDelay);
        }
    }

    void BootClientsBackToMenu()
    {
        if (NetworkServer.active)
        {
            ServerChangeScene(offlineScene);
        }
    }

    public void HostGame()
    {
        StartHost();
    }

    public void JoinGame(string address)
    {
        networkAddress = address;
        StartClient();
    }
}
