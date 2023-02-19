using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using UnityEngine.U2D;
using TMPro;
using UnityEngine.UI;

public class DeckChooseManager : SingletonBehavior<DeckChooseManager>
{
    [SerializeField] private SpriteAtlas stoneAtlas;
    [SerializeField] private SpriteAtlas UIAtlas;

    [SerializeField] private List<CardData> cardDataSource = new();
    private Dictionary<int, CardData> CardDataDic = new();

    //Temporal deck name container
    [SerializeField] private List<string> deckNames = new();
    [SerializeField] private List<string> deckCodes = new();
    
    //Temporal deck validity checker
    [SerializeField] private List<bool> isDeckAvailable = new();
    [SerializeField] private Dictionary<string, DeckDisplayUI> curDisplayingDeck = new();

    [SerializeField] private int CurrentSelectedDeckIdx = -1;

    [Header("UI Prefab")]
    [SerializeField] GameObject deckListPrefab;
    [SerializeField] GameObject cardDisplayPrefab;
 
    [Header("UI Component")]
    [SerializeField] RectTransform deckList;
    [SerializeField] RectTransform deckUnlockPanel;
    [SerializeField] Button deckUnlockCancelButton;
    [SerializeField] TextMeshProUGUI deckUnlockInformText;
    [SerializeField] TextMeshProUGUI unlockRequirementGoldText;
    [SerializeField] RectTransform profilePanel;
    [SerializeField] Button profileCloseButton;
    [SerializeField] Button profileOpenButton;
    [SerializeField] RectTransform menuPanel;
    [SerializeField] RectTransform menuBackgroundPanel;
    [SerializeField] Button menuOpenButton;
    [SerializeField] Button menuCloseButton;
    [SerializeField] InformationPanel cardInformPanel;

    private void Start()
    {
        Initiallize();
    }

    private void Initiallize()
    {
        //TODO : Later should derived from DB
        foreach(var item in cardDataSource)
        {
            CardDataDic.Add(item.CardID, item);
        }

        foreach(var item in deckCodes)
        {
            DisplayDeckFromDeckcode(item);
        }

        deckUnlockCancelButton.onClick.AddListener(()=>{
            deckUnlockPanel.gameObject.SetActive(false);
        });

        profileOpenButton.onClick.AddListener(()=>{
            profilePanel.gameObject.SetActive(true);
        });

        profileCloseButton.onClick.AddListener(()=>{
            profilePanel.gameObject.SetActive(false);
        });

        menuOpenButton.onClick.AddListener(()=>{
            menuPanel.gameObject.SetActive(true);
            menuBackgroundPanel.gameObject.SetActive(true);
        });
        
        menuCloseButton.onClick.AddListener(()=>{
            menuPanel.gameObject.SetActive(false);
            menuBackgroundPanel.gameObject.SetActive(false);
        });


        SetPlayerProfile();
    }

    public void DeckSelection(int idx)
    {
        //Selected deck is not available
        if(!isDeckAvailable[idx])
        {
            deckUnlockInformText.text = "Will you unlock " + deckNames[idx] +"?";

            deckUnlockPanel.gameObject.SetActive(true);
            return;
        }

        //현재 선택한 덱을 다시 선택하는 경우 제외
        if(CurrentSelectedDeckIdx == idx) return;

        if(CurrentSelectedDeckIdx != -1) curDisplayingDeck[deckCodes[CurrentSelectedDeckIdx]].SetActivation(false);

        curDisplayingDeck[deckCodes[idx]].SetActivation(true);

        CurrentSelectedDeckIdx = idx;
    }
    
    public void CardSelection(int cardID)
    {
        cardInformPanel.gameObject.SetActive(true);
        cardInformPanel.SetInformation(Util.GetCardDataFromID(cardID, CardDataDic), stoneAtlas, UIAtlas);
    }

    public void SetPlayerProfile()
    {

    }

    public string GenerateDeckCode(List<CardData> data)
    {
        StringBuilder sb = new StringBuilder();

        foreach(var item in data)
        {
            sb.Append(Util.BinaryStringToHexString(Util.ConvertIdToBinary(item.CardID)));
        }
               
        while(sb.ToString().Length < 12)
        {
            sb.Append("0");
        }

        return sb.ToString();
    }

    public void DisplayDeckFromDeckcode(string deckCode)
    {
        DeckDisplayUI deckUI = Instantiate(deckListPrefab, deckList).GetComponent<DeckDisplayUI>();

        curDisplayingDeck.Add(deckCode, deckUI);

        deckUI.deckName.text = deckNames[deckCodes.IndexOf(deckCode)];
        deckUI.deckIdx = deckCodes.IndexOf(deckCode);
        deckUI.SetValidity(isDeckAvailable[deckCodes.IndexOf(deckCode)]);

        foreach(var item in Util.GenerateDeckFromDeckCode(deckCode, CardDataDic))
        {
            GameObject card = Instantiate(cardDisplayPrefab, deckUI.CardList);
            card.GetComponent<DeckChooseCardUI>().cardID = item.CardID;
            card.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = item.cardName;
            card.transform.GetChild(0).GetComponent<Image>().sprite = Util.GetSpriteState(item, "Idle", stoneAtlas);
        }
    }

}
