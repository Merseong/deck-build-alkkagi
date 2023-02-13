using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;

public abstract class PlayerBehaviour : MonoBehaviour
{
    [Header("Player Behaviour")]
    [SerializeField] private GameManager.PlayerEnum player;
    public GameManager.PlayerEnum Player => player;

    [SerializeField] protected int cost;
    public int Cost
    {
        get => cost;

        private set
        {
            if (value < 0)
                Debug.LogError("Cost is less than 0! Fix this!");

            costTextUi.text = value.ToString();
            cost = value;
        }
    }

    [SerializeField] private int handCount;
    protected int HandCount
    {
        get => handCount;
        set
        {
            if (value < 0)
                Debug.LogError("Hand Count is less than 0! Fix this!");

            if (handCountTextUi != null)
            {
                handCountTextUi.text = value.ToString();
            }
            handCount = value;
        }
    }

    public Dictionary<int, StoneBehaviour> Stones = new();

    [SerializeField] protected int nextStoneId;

    [SerializeField] private bool shootTokenAvailable;
    public bool ShootTokenAvailable
    {
        get => shootTokenAvailable;
        set
        {
            shootTokenAvailable = value;

            ColorUtility.TryParseHtmlString(value ? "#C0FFBD" : "#FF8D91", out Color color);
            shootTokenImage.color = color;
        }
    }

    //TODO : should move to UIManager
    [SerializeField] private TextMeshProUGUI costTextUi;
    [SerializeField] private TextMeshProUGUI handCountTextUi;
    [SerializeField] protected Image shootTokenImage;

    public virtual void InitPlayer(GameManager.PlayerEnum pEnum)
    {
        player = pEnum;
        Stones.Clear();
        if (GameManager.Inst.isLocalGoFirst && player == GameManager.PlayerEnum.LOCAL ||
            !GameManager.Inst.isLocalGoFirst && player == GameManager.PlayerEnum.OPPO)
        {
            nextStoneId = 0;
        }
        else
        {
            nextStoneId = 1;
        }
        ShootTokenAvailable = true;

        
        GameManager.Inst.SetPlayerData(() =>
        {
            GameManager.Inst.players[(int)Player] = this;
        });
    }

    #region Cost functions

    ///<summary>
    ///return true when succesfully spend costs<br></br>return false when current cost is less then parameter
    ///</summary>
    ///<param name = "used">Cost to spend</param>
    protected bool SpendCost(int used)
    {
        if(Cost < used) return false;
        Cost -= used;
        return true;
    }

    public void ResetCost(int resetTo = -1)
    {
        if (resetTo > 0)
            Cost = resetTo;
        else if (GameManager.Inst.TurnCount == 0)
            Cost = GameManager.Inst.initialTurnCost;
        else
            Cost = GameManager.Inst.normalTurnCost;
    }
    #endregion

    #region Card functions
    public virtual void DrawCards(int number)
    {
        HandCount += number;
    }

    protected virtual void RemoveCards(int idx)
    {
        HandCount--;
    }

    /// <param name="cardData"></param>
    /// <param name="spawnPosition"></param>
    /// <param name="stoneId">use only in oppo code</param>
    /// <returns>spawned stone's stoneID</returns>
    public abstract int SpawnStone(CardData cardData, Vector3 spawnPosition, int stoneId = -1);
    #endregion

    #region Stone List Control
    public int AddStone(StoneBehaviour stone)
    {
        var toReturn = nextStoneId;
        Stones.Add(nextStoneId, stone);
        GameManager.Inst.AllStones.Add(nextStoneId, stone);
        nextStoneId += 2;
        return toReturn;
    }

    public void RemoveStone(int stoneId)
    {
        Stones.Remove(stoneId);
        GameManager.Inst.AllStones.Remove(stoneId);

        if (Stones.Count == 0)
        {
            // Game over
            GameManager.Inst.GameOverAction(player);
        }
    }

    public StoneBehaviour FindStone(int stoneId)
    {
        var isFound = Stones.TryGetValue(stoneId, out var stone);

        if (!isFound)
        {
            Debug.LogError($"[GAME] stone id {stoneId} not found");
        }

        return stone;
    }
    #endregion

    protected Vector3 StringToVector3(string vec3)
    {
        var stringList = vec3.Split('|');
        return new Vector3(float.Parse(stringList[1]), float.Parse(stringList[2]), float.Parse(stringList[3]));
    }

    protected string Vector3ToString(Vector3 vec3)
    {
        return $"|{vec3.x}|{vec3.y}|{vec3.z}|";
    }
}