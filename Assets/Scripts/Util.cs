using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.EventSystems;
using System.Text;
using System;
using System.Linq;

[System.Serializable]
public class RPS
{
    public Vector3 pos;
    public Quaternion rot;
    public Vector3 scale;

    public RPS(Vector3 pos, Quaternion rot, Vector3 scale)
    {
        this.pos = pos;
        this.rot = rot;
        this.scale = scale;
    }
}

public static class Util
{
    public static float GetRadiusFromStoneSize(CardData.StoneSize size)
    {
        switch (size)
        {
            case CardData.StoneSize.Small:
                return .5f;

            case CardData.StoneSize.Medium:
                return .65f;

            case CardData.StoneSize.Large:
                return .8f;

            case CardData.StoneSize.SuperLarge:
                return .95f;

            default:
                Debug.Log("Invalid Stone Size!");
                return 1f;
        }
    }

    public static float GetMassFromStoneWeight(CardData.StoneSize size, CardData.StoneWeight weight)
    {
        switch (weight)
        {
            case CardData.StoneWeight.Light:
                switch (size)
                {
                    case CardData.StoneSize.Small:
                        return .110f;

                    case CardData.StoneSize.Medium:
                        return .115f;

                    case CardData.StoneSize.Large:
                        return .12f;

                    case CardData.StoneSize.SuperLarge:
                        return .13f;

                    default:
                        Debug.Log("Invalid Stone Size!");
                        return 0f;
                }

            case CardData.StoneWeight.Standard:
                switch (size)
                {
                    case CardData.StoneSize.Small:
                        return .12f;

                    case CardData.StoneSize.Medium:
                        return .13f;

                    case CardData.StoneSize.Large:
                        return .14f;

                    case CardData.StoneSize.SuperLarge:
                        return .16f;

                    default:
                        Debug.Log("Invalid Stone Size!");
                        return 0f;
                }

            case CardData.StoneWeight.Heavy:
                switch (size)
                {
                    case CardData.StoneSize.Small:
                        return .13f;

                    case CardData.StoneSize.Medium:
                        return .145f;

                    case CardData.StoneSize.Large:
                        return .16f;

                    case CardData.StoneSize.SuperLarge:
                        return .19f;

                    default:
                        Debug.Log("Invalid Stone Size!");
                        return 0f;
                }

            default:
                Debug.LogError("Invalid stone weight!");
                return 0f;
        }
    }
    
    public static Sprite GetSpriteState(CardData cardData, string state, SpriteAtlas stoneAtlas)
    {
        Sprite sprite = stoneAtlas.GetSprite(cardData.cardEngName + "_" + state);
        while (sprite == null)
        {
            switch (state)
            {
                case "Shoot":
                case "Hit":
                    state = "Idle";
                    break;
                case "Ready":
                case "Break":
                    state = "Shoot";
                    break;
                default:
                    return null;
            }
            sprite = stoneAtlas.GetSprite(cardData.cardName + "_" + state);
        }
        return sprite;
    }

    public static Sprite GetSpriteSize(CardData cardData, SpriteAtlas uiAtlas)
    {
        Sprite sprite;
        switch (cardData.stoneSize)
        {
            case CardData.StoneSize.Small:
                sprite = uiAtlas.GetSprite("card_1");
                break;
            case CardData.StoneSize.Medium:
                sprite = uiAtlas.GetSprite("card_2");
                break;
            case CardData.StoneSize.Large:
                sprite = uiAtlas.GetSprite("card_3");
                break;
            case CardData.StoneSize.SuperLarge:
            default:
                sprite = uiAtlas.GetSprite("card_4");
                break;
        }
        return sprite;
    }
    public static Sprite GetSpriteWeight(CardData cardData, SpriteAtlas uiAtlas)
    {
        Sprite sprite;
        switch (cardData.stoneWeight)
        {
            case CardData.StoneWeight.Light:
                sprite = uiAtlas.GetSprite("card_6");
                break;
            case CardData.StoneWeight.Standard:
                sprite = uiAtlas.GetSprite("card_7");
                break;
            case CardData.StoneWeight.Heavy:
            default:
                sprite = uiAtlas.GetSprite("card_8");
                break;
        }
        return sprite;
    }
    public static bool IsPointerOverUIObject()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }

    public static List<CardData> GenerateDeckFromDeckCode(string deckCode, List<CardData> CardDataDic) =>
        GenerateDeckFromDeckCode(deckCode, CardDataListToDictionary(CardDataDic));

    public static List<CardData> GenerateDeckFromDeckCode(string deckCode, Dictionary<int, CardData> CardDataDic)
    {
        List<CardData> result = new();
        for(int i=0; i<deckCode.Length/2; i++)
        {
            CardData temp = GetCardDataFromID(HexStringToInt(deckCode.Substring(i*2,2)), CardDataDic);
            if(temp != null) result.Add(temp);
        }
        return result;
    }

    public static string ConvertIdToBinary(int id)
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

    public static string BinaryStringToHexString(string binary)
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

    public static int HexStringToInt(string hex)
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

    public static CardData GetCardDataFromID(int id, List<CardData> CardDataDic) =>
        GetCardDataFromID(id, CardDataListToDictionary(CardDataDic));

    //TODO : Derive CardData from DB
    public static CardData GetCardDataFromID(int id, Dictionary<int, CardData> CardDataDic)
    {
        if(id == 0) return null;
        return CardDataDic[id];
    }

    public static Dictionary<int, CardData> CardDataListToDictionary(List<CardData> list)
    {
        var dict = new Dictionary<int, CardData>();
        foreach(var item in list)
        {
            dict.Add(item.CardID, item);
        }
        return dict;
    }
}