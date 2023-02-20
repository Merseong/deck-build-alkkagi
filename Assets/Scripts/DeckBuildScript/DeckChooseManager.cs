using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using UnityEngine.U2D;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
    [SerializeField] private int deckUnlockSelectedIdx = -1;

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

    [Header("Main view")]
    [SerializeField] private TextMeshProUGUI moneyText;

    [Header("User profile")]
    [SerializeField] private TextMeshProUGUI nicknameText;
    [SerializeField] private TextMeshProUGUI recordText;
    [SerializeField] private TextMeshProUGUI honorWinText;
    [SerializeField] private TextMeshProUGUI honorLoseText;

    private void Start()
    {
        Initiallize();
    }

    private void Initiallize()
    {
        //TODO : Later should derived from DB
        foreach (var item in cardDataSource)
        {
            CardDataDic.Add(item.CardID, item);
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

        foreach (var item in deckCodes)
        {
            DisplayDeckFromDeckcode(item);
        }
    }

    public void DeckSelection(int idx)
    {
        //Selected deck is not available
        if(!isDeckAvailable[idx])
        {
            deckUnlockSelectedIdx = idx;
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

    public void OnDeckUnlockButtonClick()
    {
        if (deckUnlockSelectedIdx < 0) return;
        if (isDeckAvailable[deckUnlockSelectedIdx]) return;
        if (NetworkManager.Inst.UserData.moneyPoint < 5) return; // 덱의 가격

        NetworkManager.Inst.UserData.moneyPoint -= 5;
        isDeckAvailable[deckUnlockSelectedIdx] = true;
        var deckUnlockString = GetDeckAvailableString();
        NetworkManager.Inst.UserData.deckUnlock = deckUnlockString;
        NetworkManager.Inst.SendData(new MessagePacket
        {
            senderID = NetworkManager.Inst.NetworkId,
            message = $"SHOP/ {deckUnlockString} 5 0"
        }, PacketType.USER_ACTION);

        SetPlayerProfile();
        curDisplayingDeck[deckCodes[deckUnlockSelectedIdx]].SetValidity(true);
        deckUnlockPanel.gameObject.SetActive(false);
        deckUnlockSelectedIdx = -1;
    }

    public void OnMatchButtonClick()
    {
        if (CurrentSelectedDeckIdx < 0) return;
        EnterRoomSendNetworkAction();
    }

    public void OnLogoutButtonClick()
    {
        LogoutSendNetworkAction();
    }

    public void CardSelection(int cardID)
    {
        cardInformPanel.gameObject.SetActive(true);
        cardInformPanel.SetInformation(Util.GetCardDataFromID(cardID, CardDataDic), stoneAtlas, UIAtlas);
    }

    public void SetPlayerProfile()
    {
        if (NetworkManager.Inst.UserData == null) return;

        // set ui from userdata
        var userData = NetworkManager.Inst.UserData;
        moneyText.text = $"{userData.moneyPoint} G";
        nicknameText.text = userData.nickname;
        recordText.text = $"{userData.win} win / {userData.lose} lose";
        honorWinText.text = $"{userData.honorWin} wins with HONOR";
        honorLoseText.text = $"{userData.honorLose} loses with HONOR";

        for (int i = 0; i < isDeckAvailable.Count; ++i)
        {
            isDeckAvailable[i] = (userData.deckUnlock.Length > i && userData.deckUnlock[i] == '1');
        }
        isDeckAvailable[0] = true; // 반드시 해금되어있음
    }

    private string GetDeckAvailableString()
    {
        string output = "";
        for (int i = 0; i < isDeckAvailable.Count; ++i)
        {
            output += isDeckAvailable[i] ? '1' : '0';
        }
        return output;
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

    private void EnterRoomSendNetworkAction()
    {
        if (NetworkManager.Inst.ConnectionStatus != NetworkManager.ConnectionStatusEnum.IDLE) return;

        NetworkManager.Inst.ConnectionStatus = NetworkManager.ConnectionStatusEnum.MATCHMAKING;
        NetworkManager.Inst.SendData(new MessagePacket
        {
            senderID = NetworkManager.Inst.NetworkId,
            message = $"ENTER/ {deckCodes[CurrentSelectedDeckIdx]}" // ENTER/ (덱코드 혹은 덱인덱스)
        }, PacketType.ROOM_CONTROL);
    }

    private void LogoutSendNetworkAction()
    {
        NetworkManager.Inst.SendData(new MessagePacket
        {
            senderID = 0,
        }, PacketType.USER_LOGIN);

        NetworkManager.Inst.SetNetworkId(0, true);
        SceneManager.LoadScene("LoginScene");
    }
}
