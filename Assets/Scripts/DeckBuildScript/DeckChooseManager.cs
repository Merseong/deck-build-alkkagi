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
    }

    public void DeckSelection(int idx)
    {
        //Selected deck is not available
        if(!isDeckAvailable[idx])
        {
            

            return;
        }

        //현재 선택한 덱을 다시 선택하는 경우 제외
        if(CurrentSelectedDeckIdx == idx) return;

        if(CurrentSelectedDeckIdx != -1) curDisplayingDeck[deckCodes[CurrentSelectedDeckIdx]].SetActivation(false);

        curDisplayingDeck[deckCodes[idx]].SetActivation(true);

        CurrentSelectedDeckIdx = idx;
    }
    
    public string GenerateDeckCode(List<CardData> data)
    {
        StringBuilder sb = new StringBuilder();

        foreach(var item in data)
        {
            sb.Append(BinaryStringToHexString(ConvertIdToBinary(item.CardID)));
        }
               
        while(sb.ToString().Length < 12)
        {
            sb.Append("0");
        }

        return sb.ToString();
    }

    public List<CardData> GenerateDeckFromDeckCode(string deckCode)
    {
        List<CardData> result = new();
        for(int i=0; i<deckCode.Length/2; i++)
        {
            CardData temp = GetCardDataFromID(HexStringToInt(deckCode.Substring(i*2,2)));
            if(temp != null) result.Add(temp);
        }
        return result;
    }

    //TODO : Derive CardData from DB
    public CardData GetCardDataFromID(int id)
    {
        if(id == 0) return null;
        return CardDataDic[id];
    }

    public void DisplayDeckFromDeckcode(string deckCode)
    {
        DeckDisplayUI deckUI = Instantiate(deckListPrefab, deckList).GetComponent<DeckDisplayUI>();

        curDisplayingDeck.Add(deckCode, deckUI);

        deckUI.deckName.text = deckNames[deckCodes.IndexOf(deckCode)];
        deckUI.deckIdx = deckCodes.IndexOf(deckCode);
        deckUI.SetValidity(isDeckAvailable[deckCodes.IndexOf(deckCode)]);

        foreach(var item in GenerateDeckFromDeckCode(deckCode))
        {
            GameObject card = Instantiate(cardDisplayPrefab, deckUI.CardList);
            card.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = item.cardName;
            card.transform.GetChild(0).GetComponent<Image>().sprite = Util.GetSpriteState(item, "Idle", stoneAtlas);
        }
    }

    private string ConvertIdToBinary(int id)
    {
        StringBuilder builder = new StringBuilder();

        string binary = Convert.ToString(Convert.ToInt32(id.ToString(), 10), 2);
        if(binary.Length < 8)
        {
            for(int i=0; i< 8-binary.Length; i++)
            {
                builder.Append("0");
            }
        }
        builder.Append(binary);

        return builder.ToString();
    }

    public string BinaryStringToHexString(string binary)
    {
        if (string.IsNullOrEmpty(binary))
            return binary;

        StringBuilder result = new StringBuilder(binary.Length / 8 + 1);

        // TODO: check all 1's or 0's... throw otherwise

        int mod4Len = binary.Length % 8;
        if (mod4Len != 0)
        {
            // pad to length multiple of 8
            binary = binary.PadLeft(((binary.Length / 8) + 1) * 8, '0');
        }

        for (int i = 0; i < binary.Length; i += 8)
        {
            string eightBits = binary.Substring(i, 8);
            result.AppendFormat("{0:X2}", Convert.ToByte(eightBits, 2));
        }

        return result.ToString();
    }

    public int HexStringToInt(string hex)
    {
        int result = 0;
        for(int i=hex.Length-1 ; i>=0 ; i--)
        {
            char cur = hex[i];
            if(cur < 'A')
            {
                result += (cur - '0') * (int)Mathf.Pow(16, hex.Length - i - 1);
            }
            else
            {
                result += (cur - 'A' + 10) * (int)Mathf.Pow(16, hex.Length - i - 1);
            }
        }
        return result;
    }

}
