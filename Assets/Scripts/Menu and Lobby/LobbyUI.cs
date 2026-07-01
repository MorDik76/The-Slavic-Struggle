using UnityEngine;
using UnityEngine.UI;
using Mirror;
using Mirror.Discovery;
using System.Collections;
using System.Collections.Generic;

public class LobbyUI : MonoBehaviour
{
    [System.Serializable]
    public struct CharacterData
    {
        public string characterName;
        public Sprite preview;
    }

    [System.Serializable]
    public struct MapData
    {
        public string sceneName;
        public Sprite preview;
        public string mapName;
    }

    [Header("Tabs")]
    public Button characterTabButton;
    public Button mapTabButton;
    public GameObject characterPanel;
    public GameObject mapPanel;

    [Header("Characters")]
    public CharacterData[] characters;
    public Transform characterGridContainer;

    [Header("Maps")]
    public MapData[] maps;
    public Transform mapGridContainer;

    [Header("UI")]
    public Button findMatchButton;
    public GameObject searchingPanel;

    [Header("Network")]
    public NetworkDiscovery networkDiscovery;

    private int selectedCharacterIndex;
    private int selectedMapIndex;
    private List<GameObject> characterCards = new List<GameObject>();
    private List<Outline> characterOutlines = new List<Outline>();
    private List<GameObject> mapCards = new List<GameObject>();
    private List<Outline> mapOutlines = new List<Outline>();

    void Start()
    {
        if (characterTabButton) characterTabButton.onClick.AddListener(OnCharacterTab);
        if (mapTabButton) mapTabButton.onClick.AddListener(OnMapTab);
        if (findMatchButton) findMatchButton.onClick.AddListener(OnFindMatchClick);
        if (searchingPanel) searchingPanel.SetActive(false);

        GenerateCharacterCards();
        GenerateMapCards();

        if (characters.Length > 0) SelectCharacter(0);
        if (maps.Length > 0) SelectMap(0);

        OnCharacterTab();
    }

    void GenerateCharacterCards()
    {
        if (characterGridContainer == null) return;
        ClearCards(characterCards, characterOutlines);

        for (int i = 0; i < characters.Length; i++)
        {
            int index = i;
            GameObject card = CreateCard(characterGridContainer, characters[i].preview);
            card.GetComponent<Button>().onClick.AddListener(() => SelectCharacter(index));

            Outline outline = card.GetComponent<Outline>();
            outline.enabled = false;

            characterCards.Add(card);
            characterOutlines.Add(outline);
        }
    }

    void GenerateMapCards()
    {
        if (mapGridContainer == null) return;
        ClearCards(mapCards, mapOutlines);

        for (int i = 0; i < maps.Length; i++)
        {
            int index = i;
            GameObject card = CreateCard(mapGridContainer, maps[i].preview);
            card.GetComponent<Button>().onClick.AddListener(() => SelectMap(index));

            Outline outline = card.GetComponent<Outline>();
            outline.enabled = false;

            mapCards.Add(card);
            mapOutlines.Add(outline);
        }
    }

    GameObject CreateCard(Transform parent, Sprite sprite)
    {
        GameObject card = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(Button));
        card.transform.SetParent(parent, false);

        RectTransform rt = card.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Image img = card.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;

        card.AddComponent<Outline>();
        card.GetComponent<Outline>().effectColor = Color.yellow;
        card.GetComponent<Outline>().effectDistance = new Vector2(4, 4);

        return card;
    }

    void ClearCards(List<GameObject> cards, List<Outline> outlines)
    {
        foreach (GameObject c in cards)
            Destroy(c);
        cards.Clear();
        outlines.Clear();
    }

    void SelectCharacter(int index)
    {
        if (selectedCharacterIndex >= 0 && selectedCharacterIndex < characterOutlines.Count)
            characterOutlines[selectedCharacterIndex].enabled = false;

        selectedCharacterIndex = index;

        if (selectedCharacterIndex >= 0 && selectedCharacterIndex < characterOutlines.Count)
            characterOutlines[selectedCharacterIndex].enabled = true;
    }

    void SelectMap(int index)
    {
        if (selectedMapIndex >= 0 && selectedMapIndex < mapOutlines.Count)
            mapOutlines[selectedMapIndex].enabled = false;

        selectedMapIndex = index;

        if (selectedMapIndex >= 0 && selectedMapIndex < mapOutlines.Count)
            mapOutlines[selectedMapIndex].enabled = true;
    }

    void OnCharacterTab()
    {
        if (characterPanel) characterPanel.SetActive(true);
        if (mapPanel) mapPanel.SetActive(false);
    }

    void OnMapTab()
    {
        if (characterPanel) characterPanel.SetActive(false);
        if (mapPanel) mapPanel.SetActive(true);
    }

    void OnFindMatchClick()
    {
        if (maps.Length == 0) return;
        if (NetworkManagerSlavic.Instance == null) return;
        if (networkDiscovery == null) return;

        NetworkManagerSlavic.pendingCharacterIndex = selectedCharacterIndex;
        NetworkManagerSlavic.Instance.onlineScene = maps[selectedMapIndex].sceneName;

        if (searchingPanel) searchingPanel.SetActive(true);
        if (findMatchButton) findMatchButton.interactable = false;

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
