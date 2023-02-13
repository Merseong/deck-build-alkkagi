using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    
    public static Sprite GetSpriteState(CardData cardData, string state)
    {
        Sprite sprite = GameManager.Inst.stoneAtlas.GetSprite(cardData.cardName + "_" + state);
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
                    state = "Idle";
                    break;
            }
            sprite = GameManager.Inst.stoneAtlas.GetSprite(cardData.cardName + "_" + state);
        }
        return sprite;
    }
}