using UnityEngine;
using UnityEngine.UI;
using Mirror;
using Mirror.Discovery;
using System.Collections;
using System.Collections.Generic;

public class MenuUI : MonoBehaviour
{
    [System.Serializable]
    public struct MapData
    {
        public string sceneName;
        public Sprite preview;
        public string mapName;
    }

    [Header("Maps")]
    public MapData[] maps;
    public Transform mapGridContainer;

    [Header("UI")]
    public Button findMatchButton;
    public GameObject searchingPanel;

    [Header("Network")]
    public NetworkDiscovery networkDiscovery;

    private int selectedMapIndex;
    private List<GameObject> cardObjects = new List<GameObject>();
    private List<Outline> cardOutlines = new List<Outline>();

    void Start()
    {
        if (findMatchButton) findMatchButton.onClick.AddListener(OnFindMatchClick);
        if (searchingPanel) searchingPanel.SetActive(false);

        GenerateMapCards();
        if (maps.Length > 0)
            SelectMap(0);
    }

    void GenerateMapCards()
    {
        if (mapGridContainer == null) return;

        for (int i = 0; i < cardObjects.Count; i++)
            Destroy(cardObjects[i]);

        cardObjects.Clear();
        cardOutlines.Clear();

        for (int i = 0; i < maps.Length; i++)
        {
            int index = i;
            GameObject card = new GameObject($"MapCard_{maps[i].mapName}", typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(mapGridContainer, false);

            RectTransform rt = card.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            Image preview = card.GetComponent<Image>();
            preview.sprite = maps[i].preview;
            preview.preserveAspect = true;

            Button btn = card.GetComponent<Button>();
            btn.onClick.AddListener(() => SelectMap(index));

            Outline outline = card.AddComponent<Outline>();
            outline.effectColor = Color.yellow;
            outline.effectDistance = new Vector2(4, 4);
            outline.enabled = false;

            cardObjects.Add(card);
            cardOutlines.Add(outline);
        }
    }

    void SelectMap(int index)
    {
        if (selectedMapIndex >= 0 && selectedMapIndex < cardOutlines.Count)
            cardOutlines[selectedMapIndex].enabled = false;

        selectedMapIndex = index;

        if (selectedMapIndex >= 0 && selectedMapIndex < cardOutlines.Count)
            cardOutlines[selectedMapIndex].enabled = true;
    }

    void OnFindMatchClick()
    {
        if (maps.Length == 0) return;
        if (NetworkManagerSlavic.Instance == null) return;
        if (networkDiscovery == null) return;

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
