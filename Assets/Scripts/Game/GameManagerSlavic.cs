using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Collections;

public class GameManagerSlavic : MonoBehaviour
{
    [Header("Settings")]
    public float[] roundDurations = { 240f, 210f, 90f };
    public int roundsToWin = 2;

    [Header("UI References")]
    public Text timerText;
    public Text roundText;
    public Text scoreText;
    public GameObject winnerPanel;
    public Text winnerText;
    public GameObject loserPanel;
    public Text loserText;

    [Header("Spawn Points")]
    public Transform player1Spawn;
    public Transform player2Spawn;

    private static readonly string[] winnerPhrases = {
        "Заслуживающий победы", "Лучший из лучших", "Победитель!", "Непобедимый", "Чемпион"
    };
    private static readonly string[] loserPhrases = {
        "Ничтожество!", "Бесполезный", "Лузер", "Слабак", "Недостойный"
    };

    private PlayerSlavic[] players;
    private int currentRound;
    private int player1Score;
    private int player2Score;
    private float serverTimeRemaining;
    private bool roundActive;
    private float lastTimerSync;
    private const float timerSyncInterval = 0.5f;

    public static GameManagerSlavic Instance { get; private set; }

    public struct RoundStartMessage : NetworkMessage
    {
        public int roundNumber;
        public float duration;
        public int p1Score;
        public int p2Score;
    }

    public struct TimerMessage : NetworkMessage
    {
        public float timeRemaining;
    }

    public struct GameOverMessage : NetworkMessage
    {
        public int winnerIndex;
        public string winnerPhrase;
        public string loserPhrase;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (winnerPanel) winnerPanel.SetActive(false);
        if (loserPanel) loserPanel.SetActive(false);

        if (timerText) timerText.text = "04:00";
        if (roundText) roundText.text = "Раунд 1/2";
        if (scoreText) scoreText.text = "0 : 0";

        NetworkClient.RegisterHandler<RoundStartMessage>(OnRoundStartMessage);
        NetworkClient.RegisterHandler<TimerMessage>(OnTimerMessage);
        NetworkClient.RegisterHandler<GameOverMessage>(OnGameOverMessage);
    }

    void OnDestroy()
    {
        NetworkClient.UnregisterHandler<RoundStartMessage>();
        NetworkClient.UnregisterHandler<TimerMessage>();
        NetworkClient.UnregisterHandler<GameOverMessage>();
    }

    void Start()
    {
        if (!NetworkServer.active) return;
        StartCoroutine(FindPlayersAndStart());
    }

    IEnumerator FindPlayersAndStart()
    {
        yield return new WaitForSeconds(0.5f);
        players = FindObjectsOfType<PlayerSlavic>();
        if (players.Length < 2)
        {
            yield return new WaitForSeconds(0.5f);
            players = FindObjectsOfType<PlayerSlavic>();
        }
        if (players.Length < 2) yield break;

        if (player1Spawn != null)
        {
            float d0 = Vector3.Distance(players[0].transform.position, player1Spawn.position);
            float d1 = Vector3.Distance(players[1].transform.position, player1Spawn.position);
            if (d1 < d0)
            {
                PlayerSlavic temp = players[0];
                players[0] = players[1];
                players[1] = temp;
            }
        }

        StartRound(1);
    }

    void Update()
    {
        if (!NetworkServer.active || !roundActive) return;

        serverTimeRemaining -= Time.deltaTime;
        if (serverTimeRemaining <= 0)
        {
            serverTimeRemaining = 0;
            EndRoundByTimeout();
            return;
        }

        if (Time.time - lastTimerSync >= timerSyncInterval)
        {
            lastTimerSync = Time.time;
            NetworkServer.SendToAll(new TimerMessage { timeRemaining = serverTimeRemaining });
        }
    }

    void StartRound(int roundNumber)
    {
        currentRound = roundNumber;
        float duration = (roundNumber >= 1 && roundNumber <= roundDurations.Length) ? roundDurations[roundNumber - 1] : roundDurations[0];
        serverTimeRemaining = duration;
        roundActive = true;
        lastTimerSync = Time.time;

        NetworkServer.SendToAll(new RoundStartMessage
        {
            roundNumber = roundNumber,
            duration = duration,
            p1Score = player1Score,
            p2Score = player2Score
        });

        for (int i = 0; i < players.Length; i++)
        {
            Transform spawn = (i == 0) ? player1Spawn : player2Spawn;
            Vector3 pos = spawn != null ? spawn.position : players[i].transform.position;
            Quaternion rot = spawn != null ? spawn.rotation : players[i].transform.rotation;
            players[i].ServerRespawn(pos, rot);
        }
    }

    public void OnPlayerDied(PlayerSlavic victim)
    {
        if (!NetworkServer.active || !roundActive) return;
        roundActive = false;
        int winner = (victim == players[0]) ? 2 : 1;
        StartCoroutine(EndRoundAfterDelay(winner, 2.5f));
    }

    IEnumerator EndRoundAfterDelay(int winner, float delay)
    {
        yield return new WaitForSeconds(delay);
        EndRound(winner);
    }

    void EndRoundByTimeout()
    {
        roundActive = false;
        int winner = DetermineWinnerByHealth();
        EndRound(winner);
    }

    int DetermineWinnerByHealth()
    {
        if (players[0].currentHealth > players[1].currentHealth) return 1;
        if (players[1].currentHealth > players[0].currentHealth) return 2;
        return Random.Range(0, 2) == 0 ? 1 : 2;
    }

    void EndRound(int winnerIndex)
    {
        if (winnerIndex == 1) player1Score++;
        else if (winnerIndex == 2) player2Score++;

        roundActive = false;

        if (player1Score >= roundsToWin || player2Score >= roundsToWin)
        {
            int loser = winnerIndex == 1 ? 2 : 1;
            string wp = winnerPhrases[Random.Range(0, winnerPhrases.Length)];
            string lp = loserPhrases[Random.Range(0, loserPhrases.Length)];

            NetworkServer.SendToAll(new GameOverMessage
            {
                winnerIndex = winnerIndex,
                winnerPhrase = $"Игрок {winnerIndex}: {wp}",
                loserPhrase = $"Игрок {loser}: {lp}"
            });

            StartCoroutine(ReturnToMenuAfterDelay(5f));
        }
        else
        {
            StartCoroutine(StartNextRoundAfterDelay(3f));
        }
    }

    IEnumerator StartNextRoundAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartRound(currentRound + 1);
    }

    IEnumerator ReturnToMenuAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (NetworkManagerSlavic.Instance != null)
            NetworkManagerSlavic.Instance.ServerChangeScene(NetworkManagerSlavic.Instance.offlineScene);
    }

    void OnRoundStartMessage(RoundStartMessage msg)
    {
        ShowGameUI();
        if (timerText)
        {
            int mins = Mathf.FloorToInt(msg.duration / 60);
            int secs = Mathf.FloorToInt(msg.duration % 60);
            timerText.text = $"{mins:00}:{secs:00}";
        }
        if (roundText)
        {
            if (msg.roundNumber <= 2)
                roundText.text = $"Раунд {msg.roundNumber}/2";
            else
                roundText.text = "Раунд 3 — Решающий!";
        }
        if (scoreText) scoreText.text = $"{msg.p1Score} : {msg.p2Score}";
    }

    void OnTimerMessage(TimerMessage msg)
    {
        if (timerText)
        {
            int mins = Mathf.FloorToInt(msg.timeRemaining / 60);
            int secs = Mathf.FloorToInt(msg.timeRemaining % 60);
            timerText.text = $"{mins:00}:{secs:00}";
        }
    }

    void OnGameOverMessage(GameOverMessage msg)
    {
        HideGameUI();
        if (winnerPanel)
        {
            winnerPanel.SetActive(true);
            if (winnerText) winnerText.text = msg.winnerPhrase;
        }
        if (loserPanel)
        {
            loserPanel.SetActive(true);
            if (loserText) loserText.text = msg.loserPhrase;
        }
    }

    void ShowGameUI()
    {
        if (timerText) timerText.gameObject.SetActive(true);
        if (roundText) roundText.gameObject.SetActive(true);
        if (scoreText) scoreText.gameObject.SetActive(true);
        if (winnerPanel) winnerPanel.SetActive(false);
        if (loserPanel) loserPanel.SetActive(false);
    }

    void HideGameUI()
    {
        if (timerText) timerText.gameObject.SetActive(false);
        if (roundText) roundText.gameObject.SetActive(false);
        if (scoreText) scoreText.gameObject.SetActive(false);
    }
}
